using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        public const string Version = "0.3.0";

        internal static ManualLogSource Log;
        private static Harmony harmony;

        private PostProcessVolume volume;
        private ColorGrading grading;
        private Bloom bloom;
        private float nextVolumeCheck;
        private bool volumeDirty = true;

        // Indicator (same idea as the Abyssus mod: green/red flash on toggle + small permanent label).
        private float flashStart = -10f;
        private Color flashColor = Color.green;
        private string flashText = "";
        private GUIStyle labelStyle, shadowStyle, bigStyle;

        private void Awake()
        {
            Log = Logger;
            Cfg.Bind(Config);
            Cfg.Changed += () => volumeDirty = true;

            harmony = new Harmony(Guid);
            Patch(typeof(ShakeEmitter), nameof(ShakeEmitter.ComputeTotalShakeAtPoint), post: nameof(Patches.ShakePost));
            Patch(typeof(CameraTargetParams), nameof(CameraTargetParams.AddRecoil), pre: nameof(Patches.RecoilPre));
            Patch(typeof(CameraTargetParams), nameof(CameraTargetParams.CalcParams), post: nameof(Patches.CalcParamsPost));
            Patch(typeof(CameraModePlayerBasic), "UpdateInternal", post: nameof(Patches.CameraUpdatePost));
            Patch(typeof(CameraModePlayerBasic), "CollectLookInputInternal", post: nameof(Patches.LookInputPost));
            Patch(typeof(DamageNumberManager), nameof(DamageNumberManager.SpawnDamageNumber), pre: nameof(Patches.DamageNumberPre));
            Patch(typeof(ScreenDamage), "Update", pre: nameof(Patches.ScreenDamagePre));
            Patch(typeof(EffectComponent), nameof(EffectComponent.Reset), post: nameof(Patches.EffectResetPost));
            Patch(typeof(RampFogRenderer), "Render", pre: nameof(Patches.FogPre), post: nameof(Patches.FogPost));
            Patch(typeof(RoR2.UI.LanguageTextMeshController), "UpdateLabel", pre: nameof(Patches.NewBadgePre));

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
            {
                try { OptionsUi.Register(); }
                catch (Exception e) { Log.LogWarning("Risk of Options UI not registered: " + e.Message); }
            }

            Flash(true, "COMFORT MOD LOADED");
        }

        private static void Patch(Type type, string method, string pre = null, string post = null)
        {
            try
            {
                MethodBase target = AccessTools.Method(type, method);
                if (target == null) { Log.LogError("Patch target not found: " + type.Name + "." + method); return; }
                harmony.Patch(target,
                    prefix: pre != null ? new HarmonyMethod(typeof(Patches), pre) : null,
                    postfix: post != null ? new HarmonyMethod(typeof(Patches), post) : null);
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
            volumeDirty = true;
        }

        private void Update()
        {
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

            if (Time.unscaledTime >= nextVolumeCheck)
            {
                nextVolumeCheck = Time.unscaledTime + 1f;
                if (!volume) TryCreateVolume();
            }
            if (volume && volumeDirty)
            {
                volumeDirty = false;
                ApplyVolume();
            }
        }

        private void Group(ConfigEntry<KeyboardShortcut> key, ref bool flag, string label)
        {
            if (!key.Value.IsDown()) return;
            flag = !flag;
            Flash(flag, label + (flag ? ": ON" : ": OFF"));
        }

        private void OnGUI()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
                shadowStyle = new GUIStyle(labelStyle);
                bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
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
                string text = State.Master ? "COMFORT ON" : "COMFORT OFF";
                Color c = State.Master ? new Color(0.3f, 1f, 0.4f, 0.9f) : new Color(1f, 0.3f, 0.3f, 0.9f);
                Rect r = new Rect(Screen.width * 0.5f - 100f, 4f, 200f, 24f);
                Color old = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.8f);
                GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), text, shadowStyle);
                GUI.color = c;
                GUI.Label(r, text, labelStyle);
                GUI.color = old;
            }
        }

        private void TryCreateVolume()
        {
            PostProcessLayer[] layers = FindObjectsOfType<PostProcessLayer>();
            for (int i = 0; i < layers.Length; i++)
            {
                int mask = layers[i].volumeLayer.value;
                if (mask == 0) continue;
                int layer = 0;
                while (((mask >> layer) & 1) == 0) layer++;

                grading = ScriptableObject.CreateInstance<ColorGrading>();
                bloom = ScriptableObject.CreateInstance<Bloom>();
                volume = PostProcessManager.instance.QuickVolume(layer, 100f, grading, bloom);
                DontDestroyOnLoad(volume.gameObject);
                volumeDirty = true;
                Log.LogInfo("Comfort post-process volume created on layer " + layer);
                return;
            }
        }

        private void ApplyVolume()
        {
            volume.weight = State.VisualsOn ? 1f : 0f;

            grading.enabled.Override(true);
            grading.saturation.Override(Cfg.Saturation.Value);
            grading.contrast.Override(Cfg.Contrast.Value);
            grading.postExposure.Override(Cfg.Exposure.Value);

            bool limitBloom = Cfg.LimitBloom.Value;
            bloom.enabled.Override(limitBloom);
            if (limitBloom) bloom.intensity.Override(Cfg.BloomIntensity.Value);
        }
    }

    // Runtime on/off switches (hotkeys). Master off = fully vanilla behaviour.
    internal static class State
    {
        public static bool Master = true;
        public static bool Camera = true, Shake = true, Fov = true, Mouse = true, Visuals = true;

        public static bool CameraOn { get { return Master && Camera; } }
        public static bool ShakeOn { get { return Master && Shake; } }
        public static bool FovOn { get { return Master && Fov; } }
        public static bool MouseOn { get { return Master && Mouse; } }
        public static bool VisualsOn { get { return Master && Visuals; } }
    }

    internal static class Cfg
    {
        public static event Action Changed;

        public static ConfigEntry<float> ExtraDistance, ExtraHeight;
        public static ConfigEntry<float> ShakeScale, RecoilScale;
        public static ConfigEntry<float> SprintFovScale, AbilityFovMaxIncrease;
        public static ConfigEntry<bool> LimitAbilityFov;
        public static ConfigEntry<float> LookSmoothing, MaxLookPerFrame;
        public static ConfigEntry<bool> HideDamageNumbers;
        public static ConfigEntry<float> ScreenDamageScale, ParticleCountScale;
        public static ConfigEntry<float> Saturation, Contrast, Exposure, BloomIntensity, FogScale;
        public static ConfigEntry<bool> LimitBloom;
        public static ConfigEntry<bool> ShowIndicator, HideNewBadges;
        public static ConfigEntry<KeyboardShortcut> MasterKey, CameraKey, ShakeKey, FovKey, MouseKey, VisualsKey;

        public static void Bind(ConfigFile c)
        {
            ExtraDistance = c.Bind("Camera", "ExtraDistance", 4f, "How much farther the camera sits behind the character (added to the game's value).");
            ExtraHeight = c.Bind("Camera", "ExtraHeight", 1.5f, "How much higher the camera sits above the character.");

            ShakeScale = c.Bind("Shake", "ShakeScale", 0.1f, "Camera shake strength: 0 = none, 1 = vanilla.");
            RecoilScale = c.Bind("Shake", "RecoilScale", 0.2f, "Camera kick when firing: 0 = none, 1 = vanilla.");

            SprintFovScale = c.Bind("Fov", "SprintFovScale", 0f, "How much of the sprint FOV widening to keep: 0 = none, 1 = vanilla.");
            LimitAbilityFov = c.Bind("Fov", "LimitAbilityFov", true, "Stop abilities and items from widening FOV past the limit below (zooming in, e.g. scopes, is untouched).");
            AbilityFovMaxIncrease = c.Bind("Fov", "AbilityFovMaxIncrease", 0f, "Max FOV widening in degrees allowed by abilities when the limit is on.");

            LookSmoothing = c.Bind("Mouse", "LookSmoothing", 0.03f, "Mouse look smoothing time in seconds. 0 = off. Higher = smoother but more input lag.");
            MaxLookPerFrame = c.Bind("Mouse", "MaxLookPerFrame", 0f, "Max camera turn per frame in degrees (limits sudden flicks). 0 = off.");

            HideDamageNumbers = c.Bind("Hud", "HideDamageNumbers", true, "Hide floating damage numbers.");
            ScreenDamageScale = c.Bind("Hud", "ScreenDamageScale", 0.3f, "Strength of red tint / desaturation / distortion when hurt: 0 = none, 1 = vanilla.");

            ParticleCountScale = c.Bind("Effects", "ParticleCountScale", 0.5f, "Scale for particle emission of spawned effects: 1 = vanilla, lower = fewer particles.");

            Saturation = c.Bind("Color", "Saturation", -25f, "Color saturation adjustment (-100 grey .. 0 vanilla).");
            Contrast = c.Bind("Color", "Contrast", 0f, "Contrast adjustment (negative = flatter/hazier, 0 = vanilla, positive = punchier).");
            Exposure = c.Bind("Color", "Exposure", 0.5f, "Brightness in EV stops (negative = darker, positive = brighter).");
            FogScale = c.Bind("Color", "FogScale", 0.65f, "Strength of the game's depth fog / haze: 1 = vanilla. Note: the fog also lights up the distance, so very low values make the scene dark - raise Exposure to compensate.");
            LimitBloom = c.Bind("Color", "LimitBloom", true, "Override bloom (glow) intensity with the value below.");
            BloomIntensity = c.Bind("Color", "BloomIntensity", 0.15f, "Bloom intensity used when LimitBloom is on.");

            HideNewBadges = c.Bind("Hud", "HideNewBadges", true, "Hide the \"NEW\" (НОВИНКА) badge text in menus; in some languages it overlaps the labels.");
            ShowIndicator = c.Bind("Indicator", "ShowIndicator", true, "Show a small COMFORT ON/OFF label at the top of the screen. Toggling always flashes the screen green/red.");
            MasterKey = c.Bind("Hotkeys", "MasterToggle", new KeyboardShortcut(KeyCode.F7), "Mod on/off (green/red flash). Off = fully vanilla.");
            CameraKey = c.Bind("Hotkeys", "CameraToggle", new KeyboardShortcut(KeyCode.F2), "Toggle camera distance/height.");
            ShakeKey = c.Bind("Hotkeys", "ShakeToggle", new KeyboardShortcut(KeyCode.F3), "Toggle shake and recoil reduction.");
            FovKey = c.Bind("Hotkeys", "FovToggle", new KeyboardShortcut(KeyCode.F4), "Toggle FOV limits.");
            MouseKey = c.Bind("Hotkeys", "MouseToggle", new KeyboardShortcut(KeyCode.F5), "Toggle mouse smoothing.");
            VisualsKey = c.Bind("Hotkeys", "VisualsToggle", new KeyboardShortcut(KeyCode.F6), "Toggle colors, bloom, hurt flashes, particles and damage numbers.");

            c.SettingChanged += (s, a) => Changed?.Invoke();
        }
    }

    internal static class Patches
    {
        // Camera shake: scale the displacement the camera receives.
        public static void ShakePost(ref Vector3 __result)
        {
            if (!State.ShakeOn) return;
            __result *= Cfg.ShakeScale.Value;
        }

        // Recoil kick when firing.
        public static void RecoilPre(ref float verticalMin, ref float verticalMax, ref float horizontalMin, ref float horizontalMax)
        {
            if (!State.ShakeOn) return;
            float s = Cfg.RecoilScale.Value;
            verticalMin *= s; verticalMax *= s; horizontalMin *= s; horizontalMax *= s;
        }

        // Camera position (farther/higher) and ability FOV limit.
        public static void CalcParamsPost(CameraTargetParams __instance, ref CharacterCameraParamsData dest)
        {
            if (State.CameraOn)
                dest.idealLocalCameraPos.value += new Vector3(0f, Cfg.ExtraHeight.Value, -Cfg.ExtraDistance.Value);

            if (State.FovOn && Cfg.LimitAbilityFov.Value)
            {
                // Characters that don't set their own FOV have value 0 with alpha 0 (= "don't override"),
                // so only trust the value when the override is actually active.
                float baseFov = CharacterCameraParamsData.basic.fov.value;
                if (__instance.cameraParams)
                {
                    CharacterCameraParamsData own = __instance.cameraParams.data;
                    if (own.fov.alpha > 0f && own.fov.value >= 30f) baseFov = own.fov.value;
                }
                float cap = Mathf.Max(30f, baseFov + Cfg.AbilityFovMaxIncrease.Value);
                if (dest.fov.value > cap) dest.fov.value = cap;
            }
        }

        // Sprint FOV widening.
        private static FieldInfo sprintField;
        public static void CameraUpdatePost(object rawInstanceData, ref CameraModeBase.UpdateResult result)
        {
            if (!State.FovOn) return;
            float keep = Cfg.SprintFovScale.Value;
            if (keep >= 0.999f || rawInstanceData == null) return;
            if (sprintField == null)
                sprintField = rawInstanceData.GetType().GetField("sprintFovMultiplier", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (sprintField == null) return;
            float m = (float)sprintField.GetValue(rawInstanceData);
            if (m < 0.01f) return;
            result.cameraState.fov = result.cameraState.fov / m * Mathf.Lerp(1f, m, keep);
        }

        // Mouse look smoothing / flick limiter. Smoothing conserves total turned angle.
        private static Vector2 pendingLook;
        public static void LookInputPost(ref CameraModeBase.CollectLookInputResult output)
        {
            if (!State.MouseOn)
            {
                // Hand over anything still pending so no motion is lost when toggling off.
                output.lookInput += pendingLook;
                pendingLook = Vector2.zero;
                return;
            }

            Vector2 v = output.lookInput;

            float maxD = Cfg.MaxLookPerFrame.Value;
            if (maxD > 0f && v.magnitude > maxD) v = v.normalized * maxD;

            float tau = Cfg.LookSmoothing.Value;
            if (tau > 0f)
            {
                pendingLook += v;
                float a = 1f - Mathf.Exp(-Time.unscaledDeltaTime / tau);
                v = pendingLook * a;
                pendingLook -= v;
            }
            else
            {
                pendingLook = Vector2.zero;
            }
            output.lookInput = v;
        }

        // Depth fog / haze: scale the intensity of the game's RampFog post-process for the duration of its render.
        private static RampFog FogSettings(object renderer)
        {
            return ((PostProcessEffectRenderer<RampFog>)renderer).settings;
        }

        public static void FogPre(object __instance, out float __state)
        {
            __state = -1f;
            float s = State.VisualsOn ? Cfg.FogScale.Value : 1f;
            if (s >= 0.999f) return;
            RampFog fog = FogSettings(__instance);
            if (fog == null) return;
            __state = fog.fogIntensity.value;
            fog.fogIntensity.value = __state * s;
        }

        public static void FogPost(object __instance, float __state)
        {
            if (__state < 0f) return;
            RampFog fog = FogSettings(__instance);
            if (fog != null) fog.fogIntensity.value = __state;
        }

        // Menu "NEW" badge text overlaps long labels in some languages: blank it before it is written to the label.
        public static void NewBadgePre(string ____token, ref string ___resolvedString)
        {
            if (Cfg.HideNewBadges.Value && ____token == "NEW_UNLOCKABLE") ___resolvedString = "";
        }

        public static bool DamageNumberPre()
        {
            if (!State.VisualsOn) return true;
            return !Cfg.HideDamageNumbers.Value;
        }

        public static void ScreenDamagePre(ScreenDamage __instance)
        {
            float s = State.VisualsOn ? Cfg.ScreenDamageScale.Value : 1f;
            __instance.DistortionScale = s;
            __instance.DesaturationScale = s;
            __instance.TintScale = s;
        }

        // Particle count scaling. Original values are remembered per system so pooled effects don't compound.
        private class Original
        {
            public float rate;
            public int maxParticles;
            public ParticleSystem.Burst[] bursts;
        }

        private static readonly ConditionalWeakTable<ParticleSystem, Original> originals = new ConditionalWeakTable<ParticleSystem, Original>();
        private static readonly List<ParticleSystem> buffer = new List<ParticleSystem>();

        public static void EffectResetPost(EffectComponent __instance)
        {
            float s = State.VisualsOn ? Cfg.ParticleCountScale.Value : 1f;

            __instance.GetComponentsInChildren(true, buffer);
            for (int i = 0; i < buffer.Count; i++)
            {
                ParticleSystem ps = buffer[i];
                if (!originals.TryGetValue(ps, out Original o))
                {
                    if (s >= 0.999f) continue; // nothing changed yet, nothing to restore
                    o = new Original();
                    ParticleSystem.EmissionModule em0 = ps.emission;
                    o.rate = em0.rateOverTimeMultiplier;
                    o.maxParticles = ps.main.maxParticles;
                    o.bursts = new ParticleSystem.Burst[em0.burstCount];
                    em0.GetBursts(o.bursts);
                    originals.Add(ps, o);
                }

                ParticleSystem.EmissionModule em = ps.emission;
                em.rateOverTimeMultiplier = o.rate * s;

                if (o.bursts.Length > 0)
                {
                    ParticleSystem.Burst[] scaled = new ParticleSystem.Burst[o.bursts.Length];
                    for (int b = 0; b < scaled.Length; b++)
                    {
                        ParticleSystem.Burst burst = o.bursts[b];
                        ParticleSystem.MinMaxCurve c = burst.count;
                        // Leave tiny bursts (flash quads, single sprites) alone so effects don't vanish.
                        if (s < 0.999f)
                        {
                            if (c.mode == ParticleSystemCurveMode.Constant && c.constant > 2f)
                                c.constant = Mathf.Max(2f, c.constant * s);
                            else if (c.mode == ParticleSystemCurveMode.TwoConstants && c.constantMax > 2f)
                            {
                                c.constantMin = Mathf.Max(1f, c.constantMin * s);
                                c.constantMax = Mathf.Max(2f, c.constantMax * s);
                            }
                        }
                        burst.count = c;
                        scaled[b] = burst;
                    }
                    em.SetBursts(scaled);
                }

                ParticleSystem.MainModule main = ps.main;
                main.maxParticles = o.maxParticles > 8 ? Mathf.Max(8, Mathf.RoundToInt(o.maxParticles * s)) : o.maxParticles;
            }
            buffer.Clear();
        }
    }

    internal static class OptionsUi
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Register()
        {
            RiskOfOptions.ModSettingsManager.SetModDescription("Reduces motion sickness triggers: camera, shake, FOV, mouse, flashes, particles, color. F7 = mod on/off, F2-F6 = groups.");
            Slider(Cfg.ExtraDistance, 0f, 30f, "{0:0.0}");
            Slider(Cfg.ExtraHeight, 0f, 15f, "{0:0.0}");
            Slider(Cfg.ShakeScale, 0f, 1f, "{0:0.00}");
            Slider(Cfg.RecoilScale, 0f, 1f, "{0:0.00}");
            Slider(Cfg.SprintFovScale, 0f, 1f, "{0:0.00}");
            Check(Cfg.LimitAbilityFov);
            Slider(Cfg.AbilityFovMaxIncrease, 0f, 40f, "{0:0}");
            Slider(Cfg.LookSmoothing, 0f, 0.2f, "{0:0.000}");
            Slider(Cfg.MaxLookPerFrame, 0f, 30f, "{0:0.0}");
            Check(Cfg.HideDamageNumbers);
            Slider(Cfg.ScreenDamageScale, 0f, 1f, "{0:0.00}");
            Slider(Cfg.ParticleCountScale, 0.05f, 1f, "{0:0.00}");
            Slider(Cfg.Saturation, -100f, 0f, "{0:0}");
            Slider(Cfg.Contrast, -50f, 50f, "{0:0}");
            Slider(Cfg.Exposure, -3f, 2f, "{0:0.0}");
            Slider(Cfg.FogScale, 0f, 1f, "{0:0.00}");
            Check(Cfg.LimitBloom);
            Slider(Cfg.BloomIntensity, 0f, 3f, "{0:0.00}");
            Check(Cfg.HideNewBadges);
            Check(Cfg.ShowIndicator);
        }

        private static void Slider(ConfigEntry<float> e, float min, float max, string fmt)
        {
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.SliderOption(e,
                new RiskOfOptions.OptionConfigs.SliderConfig { min = min, max = max, FormatString = fmt }));
        }

        private static void Check(ConfigEntry<bool> e)
        {
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.CheckBoxOption(e));
        }
    }
}
