using System;
using System.Collections;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using RoR2;
using RoR2.CameraModes;
using RoR2.PostProcessing;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace RoR2Comfort
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "local.RoR2Comfort";
        public const string Name = "RoR2 Comfort";
        public const string Version = "0.4.4";

        internal static ManualLogSource Log;
        private static Harmony harmony;

        // Automatic color balance (screen average -> neutralizing color filter). The gain is applied on top of the
        // game's own color grading in Patches.BlendedPost.
        private Vector3 balanceTarget = Vector3.one;
        private Vector3 balanceGain = Vector3.one;
        private RenderTexture captureRt;
        private int captureFailures;

        // Indicator (same idea as the Abyssus mod: green/red flash on toggle + small permanent label).
        private float flashStart = -10f;
        private Color flashColor = Color.green;
        private string flashText = "";
        private GUIStyle labelStyle, shadowStyle, bigStyle, bannerStyle;
        private string indicatorText = "COMFORT ON";
        private int indicatorPct = -1;
        private bool indicatorMaster = true;

        // Break reminder.
        private float playSeconds;
        private float breakBannerUntil;
        private int breakBannerMinutes;
        private int breakBannerTextMinutes = -1;
        private string breakBannerText;

        private float nextErrorLog;

        private void Awake()
        {
            Log = Logger;
            Cfg.Bind(Config);

            harmony = new Harmony(Guid);
            Patch(typeof(PostProcessManager), "UpdateSettings", post: nameof(Patches.BlendedPost));
            Patch(typeof(ShakeEmitter), nameof(ShakeEmitter.ComputeTotalShakeAtPoint), post: nameof(Patches.ShakePost));
            Patch(typeof(CameraTargetParams), nameof(CameraTargetParams.AddRecoil), pre: nameof(Patches.RecoilPre));
            Patch(typeof(CameraTargetParams), nameof(CameraTargetParams.CalcParams), post: nameof(Patches.CalcParamsPost));
            Patch(typeof(CameraModePlayerBasic), "UpdateInternal", post: nameof(Patches.CameraUpdatePost));
            Patch(typeof(CameraModePlayerBasic), "CollectLookInputInternal", post: nameof(Patches.LookInputPost));
            Patch(typeof(CameraRigController), nameof(CameraRigController.ModifyAimRayIfApplicable), post: nameof(Patches.AimRayPost));
            Patch(typeof(DamageNumberManager), nameof(DamageNumberManager.SpawnDamageNumber), pre: nameof(Patches.DamageNumberPre));
            Patch(typeof(ScreenDamage), "Update", pre: nameof(Patches.ScreenDamagePre));
            Patch(typeof(PostProcessDuration), "UpdatePostProccess", post: nameof(Patches.FlashPost));
            Patch(typeof(EffectComponent), nameof(EffectComponent.Reset), post: nameof(Patches.EffectResetPost));
            Patch(typeof(RampFogRenderer), "Render", pre: nameof(Patches.FogPre), finalizer: nameof(Patches.FogFinalizer));
            Patch(typeof(RoR2.UI.LanguageTextMeshController), "UpdateLabel", pre: nameof(Patches.NewBadgePre));
            Patch(typeof(SplashScreenController), "Start", pre: nameof(Patches.SplashStartPre));
            Patch(typeof(SplashScreenController), "Finish", pre: nameof(Patches.SplashFinishPre));

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
            {
                try { OptionsUi.Register(); }
                catch (Exception e) { Log.LogWarning("Risk of Options UI not registered: " + e.Message); }
            }

            StartCoroutine(ColorSampler());
            if (Cfg.ShowIndicator.Value) Flash(true, "COMFORT MOD LOADED");
        }

        private static void Patch(Type type, string method, string pre = null, string post = null, string finalizer = null)
        {
            try
            {
                MethodBase target = AccessTools.Method(type, method);
                if (target == null) { Log.LogError("Patch target not found: " + type.Name + "." + method); return; }
                harmony.Patch(target,
                    prefix: pre != null ? new HarmonyMethod(typeof(Patches), pre) : null,
                    postfix: post != null ? new HarmonyMethod(typeof(Patches), post) : null,
                    finalizer: finalizer != null ? new HarmonyMethod(typeof(Patches), finalizer) : null);
                Log.LogInfo("Patched " + type.Name + "." + method);
            }
            catch (Exception e)
            {
                Log.LogError("Failed to patch " + type.Name + "." + method + ": " + e);
            }
        }

        private void Flash(bool on, string text)
        {
            flashStart = Time.unscaledTime;
            flashColor = on ? new Color(0.1f, 0.9f, 0.2f) : new Color(0.95f, 0.1f, 0.1f);
            flashText = text;
        }

        // ------------------------------------------------------------------ per-frame

        private void Update()
        {
            try { UpdateInner(); }
            catch (Exception e)
            {
                if (Time.unscaledTime >= nextErrorLog)
                {
                    nextErrorLog = Time.unscaledTime + 10f;
                    Log.LogError("Update error (will keep running): " + e);
                }
            }
        }

        private void UpdateInner()
        {
            float dt = Time.unscaledDeltaTime;

            // hotkeys
            if (Cfg.MasterKey.Value.IsDown())
            {
                State.Master = !State.Master;
                Flash(State.Master, State.Master ? "COMFORT ON" : "COMFORT OFF (vanilla)");
            }
            Group(Cfg.CameraKey, ref State.Camera, "Camera position");
            Group(Cfg.ShakeKey, ref State.Shake, "Shake + recoil");
            Group(Cfg.FovKey, ref State.Fov, "FOV limits");
            Group(Cfg.MouseKey, ref State.Mouse, "Mouse smoothing");
            Group(Cfg.VisualsKey, ref State.Visuals, "Colors, flashes, particles, numbers");
            if (Cfg.StrengthKey.Value.IsDown()) CycleStrength();

            UpdateStrength();
            UpdateCamera();
            DamageNumbers.Flush(Eff.Numbers != DamageNumberMode.Aggregate);
            UpdateBreakReminder(dt);
            UpdateBalance(dt);
        }

        private void Group(ConfigEntry<KeyboardShortcut> key, ref bool flag, string label)
        {
            if (!key.Value.IsDown()) return;
            flag = !flag;
            Flash(flag, label + (flag ? ": ON" : ": OFF"));
        }

        private void CycleStrength()
        {
            float cur = Cfg.Strength.Value;
            float next = cur > 0.85f ? 0.7f : (cur > 0.55f ? 0.4f : 1f);
            Cfg.Strength.Value = next;
            Flash(true, "Comfort strength " + Mathf.RoundToInt(next * 100f) + "%");
        }

        private void UpdateStrength()
        {
            float k = Mathf.Clamp01(Cfg.Strength.Value);
            if (Cfg.Progressive.Value)
            {
                Run run = Run.instance;
                if (run)
                {
                    int full = Mathf.Max(2, Cfg.ProgressiveFullAt.Value);
                    float t = Mathf.Clamp01(run.stageClearCount / (float)(full - 1));
                    k *= Mathf.Lerp(Mathf.Clamp01(Cfg.ProgressiveStart.Value), 1f, t);
                }
            }
            State.Strength = k;
        }

        private static CameraRigController FindRig()
        {
            var list = CameraRigController.readOnlyInstancesList;
            for (int i = 0; i < list.Count; i++)
            {
                CameraRigController r = list[i];
                if (r && r.sceneCam && r.localUserViewer != null && r.target) return r;
            }
            return null;
        }

        // Tracks the camera position for distance-based effect scaling.
        private void UpdateCamera()
        {
            CameraRigController rig = FindRig();
            Transform tr = rig ? rig.sceneCam.transform : null;
            if (!tr)
            {
                Patches.CamPosValid = false;
                return;
            }
            Patches.CamPos = tr.position;
            Patches.CamPosValid = true;
        }

        private void UpdateBreakReminder(float dt)
        {
            int minutes = Cfg.BreakMinutes.Value;
            if (minutes <= 0 || !Application.isFocused) return;
            playSeconds += dt;
            if (playSeconds >= minutes * 60f)
            {
                playSeconds = 0f;
                breakBannerUntil = Time.unscaledTime + 9f;
                breakBannerMinutes = minutes;
            }
        }

        // ------------------------------------------------------------------ color balance gain

        // Smooths the automatic balance gain; the colour grading itself is adjusted in Patches.BlendedPost.
        private void UpdateBalance(float dt)
        {
            if (dt > 0f) balanceGain = Vector3.Lerp(balanceGain, balanceTarget, 1f - Mathf.Exp(-dt / 1.5f));
            Patches.BalanceGain = balanceGain;
        }

        // ------------------------------------------------------------------ automatic color balance

        private IEnumerator ColorSampler()
        {
            WaitForSecondsRealtime wait = new WaitForSecondsRealtime(0.5f);
            WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();
            while (true)
            {
                yield return wait;
                if (Cfg.AutoColorStrength.Value <= 0.01f || !State.VisualsOn || State.Strength < 0.05f || Screen.width < 64)
                {
                    balanceTarget = Vector3.one;
                    continue;
                }
                yield return endOfFrame;
                try
                {
                    CaptureFrame();
                }
                catch (Exception e)
                {
                    captureFailures++;
                    Log.LogWarning("Auto color balance capture failed (" + captureFailures + "): " + e.Message);
                    if (captureFailures >= 3)
                    {
                        Log.LogWarning("Auto color balance disabled for this session.");
                        balanceTarget = Vector3.one;
                        yield break;
                    }
                }
            }
        }

        private void CaptureFrame()
        {
            int w = Screen.width, h = Screen.height;
            if (captureRt == null || captureRt.width != w || captureRt.height != h)
            {
                if (captureRt != null) { captureRt.Release(); Destroy(captureRt); }
                captureRt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
            }
            ScreenCapture.CaptureScreenshotIntoRenderTexture(captureRt);

            // Halve repeatedly down to 16x9 so the average is not aliased.
            RenderTexture src = captureRt;
            int cw = w, ch = h;
            while (cw > 16)
            {
                cw = Mathf.Max(16, cw / 2);
                ch = Mathf.Max(9, ch / 2);
                RenderTexture t = RenderTexture.GetTemporary(cw, ch, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(src, t);
                if (src != captureRt) RenderTexture.ReleaseTemporary(src);
                src = t;
            }

            RenderTexture final = src;
            bool temporary = final != captureRt;
            UnityEngine.Rendering.AsyncGPUReadback.Request(final, 0, TextureFormat.RGBA32, req =>
            {
                try { OnReadback(req); }
                finally { if (temporary) RenderTexture.ReleaseTemporary(final); }
            });
        }

        private void OnReadback(UnityEngine.Rendering.AsyncGPUReadbackRequest req)
        {
            if (req.hasError) return;
            Unity.Collections.NativeArray<Color32> data = req.GetData<Color32>();
            int n = data.Length;
            if (n == 0) return;

            float r = 0f, g = 0f, b = 0f;
            for (int i = 0; i < n; i++)
            {
                r += data[i].r; g += data[i].g; b += data[i].b;
            }
            float inv = 1f / (n * 255f);
            UpdateBalanceTarget(r * inv, g * inv, b * inv);
        }

        // Closed loop on the final image: if one hue dominates more than allowed, nudge the neutralizing gains;
        // otherwise let them relax back to 1.
        private void UpdateBalanceTarget(float r, float g, float b)
        {
            float lum = 0.299f * r + 0.587f * g + 0.114f * b;
            if (lum < 0.03f) return; // black screen / loading

            Vector3 d = new Vector3(r / lum, g / lum, b / lum);
            float dominance = Mathf.Max(d.x, Mathf.Max(d.y, d.z)) - Mathf.Min(d.x, Mathf.Min(d.y, d.z));

            float strength = Mathf.Clamp01(Cfg.AutoColorStrength.Value) * State.Strength;
            float allowed = Mathf.Lerp(1.4f, 0.4f, strength);

            Vector3 t = balanceTarget;
            if (dominance > allowed)
            {
                float push = Mathf.Clamp01((dominance - allowed) / 0.5f);
                float e = 0.3f * push;
                t.x *= Mathf.Pow(1f / Mathf.Clamp(d.x, 0.2f, 3f), e);
                t.y *= Mathf.Pow(1f / Mathf.Clamp(d.y, 0.2f, 3f), e);
                t.z *= Mathf.Pow(1f / Mathf.Clamp(d.z, 0.2f, 3f), e);
            }
            else
            {
                t = Vector3.Lerp(t, Vector3.one, 0.08f);
            }

            // keep overall brightness, limit the range
            float tl = 0.299f * t.x + 0.587f * t.y + 0.114f * t.z;
            if (tl > 0.01f) t /= tl;
            t.x = Mathf.Clamp(t.x, 0.65f, 1.45f);
            t.y = Mathf.Clamp(t.y, 0.65f, 1.45f);
            t.z = Mathf.Clamp(t.z, 0.65f, 1.45f);
            balanceTarget = t;
        }

        // ------------------------------------------------------------------ on-screen indicator

        private string GetIndicatorText()
        {
            int pct = Mathf.RoundToInt(State.Strength * 100f);
            if (pct != indicatorPct || State.Master != indicatorMaster)
            {
                indicatorPct = pct;
                indicatorMaster = State.Master;
                if (!State.Master) indicatorText = "COMFORT OFF";
                else indicatorText = pct >= 99 ? "COMFORT ON" : "COMFORT " + pct + "%";
            }
            return indicatorText;
        }

        private void OnDestroy()
        {
            if (captureRt != null)
            {
                captureRt.Release();
                Destroy(captureRt);
                captureRt = null;
            }
        }

        private void OnGUI()
        {
            // IMGUI runs several event passes per frame; drawing is only needed on Repaint.
            if (Event.current.type != EventType.Repaint) return;

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
                shadowStyle = new GUIStyle(labelStyle);
                bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                bannerStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
            }

            float t = Time.unscaledTime - flashStart;
            if (t >= 0f && t < 0.9f)
            {
                float a = 0.30f * (1f - t / 0.9f);
                Color old = GUI.color;
                GUI.color = new Color(flashColor.r, flashColor.g, flashColor.b, a);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = new Color(1f, 1f, 1f, 1f - t / 0.9f);
                GUI.Label(new Rect(0f, Screen.height * 0.4f, Screen.width, 60f), flashText, bigStyle);
                GUI.color = old;
            }

            if (Cfg.ShowIndicator.Value)
            {
                string text = GetIndicatorText();
                Color c = State.Master ? new Color(0.3f, 1f, 0.4f, 0.9f) : new Color(1f, 0.3f, 0.3f, 0.9f);
                Rect r = new Rect(Screen.width * 0.5f - 100f, 4f, 200f, 24f);
                Color old = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.8f);
                GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), text, shadowStyle);
                GUI.color = c;
                GUI.Label(r, text, labelStyle);
                GUI.color = old;
            }

            if (Time.unscaledTime < breakBannerUntil)
            {
                if (breakBannerText == null || breakBannerTextMinutes != breakBannerMinutes)
                {
                    breakBannerTextMinutes = breakBannerMinutes;
                    breakBannerText = "BREAK TIME? " + breakBannerMinutes + " min of play. Rest your eyes for a few minutes.";
                }
                string msg = breakBannerText;
                Rect r = new Rect(Screen.width * 0.5f - 300f, 34f, 600f, 28f);
                Color old = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.85f);
                GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), msg, bannerStyle);
                GUI.color = new Color(1f, 0.9f, 0.4f, 1f);
                GUI.Label(r, msg, bannerStyle);
                GUI.color = old;
            }
        }
    }
}
