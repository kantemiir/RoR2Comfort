using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RoR2;
using RoR2.CameraModes;
using RoR2.PostProcessing;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace RoR2Comfort
{
    // Sums damage numbers per (target area, team, color) over a short window and spawns one number per bucket.
    internal static class DamageNumbers
    {
        private struct Bucket
        {
            public float amount;
            public Vector3 position;
            public bool crit;
            public TeamIndex team;
            public DamageColorIndex color;
            public float start;
        }

        private static readonly Dictionary<(int, int, int, int), Bucket> buckets = new Dictionary<(int, int, int, int), Bucket>();
        private static readonly List<(int, int, int, int)> due = new List<(int, int, int, int)>();
        private static bool spawning;

        // Returns true when the original SpawnDamageNumber should run.
        public static bool Handle(float amount, Vector3 position, bool crit, TeamIndex team, DamageColorIndex color)
        {
            if (spawning) return true;
            DamageNumberMode mode = Eff.Numbers;
            if (mode == DamageNumberMode.Show) return true;
            if (mode == DamageNumberMode.Hide) return false;

            // 4-unit grid cell + team/color
            var key = (Mathf.RoundToInt(position.x * 0.25f), Mathf.RoundToInt(position.y * 0.25f),
                       Mathf.RoundToInt(position.z * 0.25f), ((int)team << 8) | (int)color);

            Bucket b;
            if (!buckets.TryGetValue(key, out b))
            {
                b = new Bucket { start = Time.unscaledTime, team = team, color = color };
            }
            b.amount += amount;
            b.position = position;
            b.crit |= crit;
            buckets[key] = b;
            return false;
        }

        public static void Flush(bool all)
        {
            if (buckets.Count == 0) return;
            float now = Time.unscaledTime;
            float window = Mathf.Max(0.1f, Cfg.DamageNumberWindow.Value);

            due.Clear();
            foreach (KeyValuePair<(int, int, int, int), Bucket> kv in buckets)
            {
                if (all || now - kv.Value.start >= window) due.Add(kv.Key);
            }
            if (due.Count == 0) return;

            DamageNumberManager manager = DamageNumberManager.instance;
            spawning = true;
            try
            {
                for (int i = 0; i < due.Count; i++)
                {
                    Bucket b = buckets[due[i]];
                    buckets.Remove(due[i]);
                    if (manager) manager.SpawnDamageNumber(b.amount, b.position, b.crit, b.team, b.color);
                }
            }
            finally
            {
                spawning = false;
            }
        }
    }

    internal static class Patches
    {
        // Refreshed every frame by the plugin.
        public static Vector3 CamPos;                       // camera position (distance-based effect scaling)
        public static bool CamPosValid;

        // ---------------------------------------------------------------- camera shake / recoil

        // Camera shake: scale the displacement the camera receives.
        public static void ShakePost(ref Vector3 __result)
        {
            float s = Eff.Shake;
            if (s < 0.999f) __result *= s;
        }

        // Recoil kick when firing.
        public static void RecoilPre(ref float verticalMin, ref float verticalMax, ref float horizontalMin, ref float horizontalMax)
        {
            float s = Eff.Recoil;
            if (s >= 0.999f) return;
            verticalMin *= s; verticalMax *= s; horizontalMin *= s; horizontalMax *= s;
        }

        // ---------------------------------------------------------------- camera position / FOV

        // Camera position (farther/higher) and ability FOV limit.
        public static void CalcParamsPost(CameraTargetParams __instance, ref CharacterCameraParamsData dest)
        {
            float dist = Eff.ExtraDistance;
            float height = Eff.ExtraHeight;
            if (dist != 0f || height != 0f)
                dest.idealLocalCameraPos.value += new Vector3(0f, height, -dist);

            if (Eff.LimitFov)
            {
                // Characters that don't set their own FOV have value 0 with alpha 0 (= "don't override"),
                // so only trust the value when the override is actually active.
                float baseFov = CharacterCameraParamsData.basic.fov.value;
                if (__instance.cameraParams)
                {
                    CharacterCameraParamsData own = __instance.cameraParams.data;
                    if (own.fov.alpha > 0f && own.fov.value >= 30f) baseFov = own.fov.value;
                }
                float cap = Mathf.Max(30f, baseFov + Eff.FovMaxIncrease);
                if (dest.fov.value > cap) dest.fov.value = cap;
            }
        }

        // Keep gameplay independent of how far the camera is pulled back.
        // The game starts several searches (auto-targeting, interaction, equipment targeting, ping, some ranged
        // skills) at the camera position and pads the range with the camera-to-body distance. A camera that sits
        // farther than vanilla would change those cones and ranges, so move the search origin forward by the extra
        // distance we added; the far end of every search stays exactly where it was.
        public static void AimRayPost(ref Ray __result, ref float extraRaycastDistance)
        {
            float extra = extraRaycastDistance;
            if (extra <= 0.001f) return; // no camera rig applied, original ray was returned
            float shift = Mathf.Min(Eff.ExtraDistance, extra);
            if (shift <= 0f) return;
            Ray r = __result;
            r.origin += r.direction * shift;
            __result = r;
            extraRaycastDistance = extra - shift;
        }

        // The camera mode's instance data class is protected, so fields are reached through Harmony's
        // ref-returning accessors (no boxing, no per-call reflection).
        private static AccessTools.FieldRef<object, float> sprintRef;
        private static AccessTools.FieldRef<object, HurtBox> crosshairRef;
        private static FieldInfo sprintField;      // plain-reflection fallbacks
        private static FieldInfo crosshairField;
        private static bool accessorsTried;

        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static void EnsureAccessors(object rawInstanceData)
        {
            if (accessorsTried) return;
            accessorsTried = true;
            Type t = rawInstanceData.GetType();
            try { sprintRef = AccessTools.FieldRefAccess<float>(t, "sprintFovMultiplier"); }
            catch (Exception) { sprintRef = null; }
            try { crosshairRef = AccessTools.FieldRefAccess<HurtBox>(t, "lastCrosshairHurtBox"); }
            catch (Exception) { crosshairRef = null; }
            if (sprintRef == null) sprintField = t.GetField("sprintFovMultiplier", AnyInstance);
            if (crosshairRef == null) crosshairField = t.GetField("lastCrosshairHurtBox", AnyInstance);
        }

        // Sprint FOV widening.
        public static void CameraUpdatePost(object rawInstanceData, ref CameraModeBase.UpdateResult result)
        {
            float keep = Eff.SprintFov;
            if (keep >= 0.999f || rawInstanceData == null) return;
            EnsureAccessors(rawInstanceData);
            float m;
            if (sprintRef != null) m = sprintRef(rawInstanceData);
            else if (sprintField != null) m = (float)sprintField.GetValue(rawInstanceData);
            else return;
            if (m < 0.01f) return;
            result.cameraState.fov = result.cameraState.fov / m * Mathf.Lerp(1f, m, keep);
        }

        // ---------------------------------------------------------------- mouse

        // Mouse look smoothing / flick limiter / optional aim stickiness.
        // Smoothing conserves the total turned angle.
        private static Vector2 pendingLook;

        private static bool CrosshairOnEnemy(object rawInstanceData)
        {
            EnsureAccessors(rawInstanceData);
            HurtBox hb;
            if (crosshairRef != null) hb = crosshairRef(rawInstanceData);
            else if (crosshairField != null) hb = crosshairField.GetValue(rawInstanceData) as HurtBox;
            else return false;
            return hb != null && hb;
        }

        public static void LookInputPost(object rawInstanceData, ref CameraModeBase.CollectLookInputResult output)
        {
            Vector2 v = output.lookInput;

            float stick = Eff.AimStickiness;
            if (stick > 0f && rawInstanceData != null && CrosshairOnEnemy(rawInstanceData)) v *= 1f - stick;

            float tau = Eff.LookSmoothing;
            float maxD = Eff.MaxLookPerFrame;
            if (maxD > 0f && v.magnitude > maxD) v = v.normalized * maxD;

            if (tau > 0f)
            {
                pendingLook += v;
                float a = 1f - Mathf.Exp(-Time.unscaledDeltaTime / tau);
                v = pendingLook * a;
                pendingLook -= v;
            }
            else if (pendingLook != Vector2.zero)
            {
                // Hand over anything still pending so no motion is lost when smoothing is switched off.
                v += pendingLook;
                pendingLook = Vector2.zero;
            }
            output.lookInput = v;
        }

        // ---------------------------------------------------------------- fog / flashes / hurt tint

        // Depth fog / haze: scale the intensity of the game's RampFog post-process for the duration of its render.
        // The finalizer restores the value even if the render throws.
        private static RampFog FogSettings(object renderer)
        {
            return ((PostProcessEffectRenderer<RampFog>)renderer).settings;
        }

        public static void FogPre(object __instance, out float __state)
        {
            __state = -1f;
            float s = Eff.Fog;
            if (s >= 0.999f) return;
            RampFog fog = FogSettings(__instance);
            if (fog == null) return;
            __state = fog.fogIntensity.value;
            fog.fogIntensity.value = __state * s;
        }

        public static Exception FogFinalizer(Exception __exception, object __instance, float __state)
        {
            if (__state >= 0f)
            {
                RampFog fog = FogSettings(__instance);
                if (fog != null) fog.fogIntensity.value = __state;
            }
            return __exception;
        }

        // Full-screen flash volumes driven by PostProcessDuration (lightning storm, worm death, beams...).
        public static void FlashPost(PostProcessDuration __instance)
        {
            float f = Eff.Flash;
            if (f >= 0.999f) return;
            if (__instance.ppVolume) __instance.ppVolume.weight *= f;
        }

        public static void ScreenDamagePre(ScreenDamage __instance)
        {
            float s = Eff.ScreenDamage;
            __instance.DistortionScale = s;
            __instance.DesaturationScale = s;
            __instance.TintScale = s;
        }

        // ---------------------------------------------------------------- color grading / bloom

        // Neutralizing gain of the automatic color balance (set by the plugin every frame).
        public static Vector3 BalanceGain = Vector3.one;

        private static float lastGradingLog = -100f;
        private static int gradingLogCount;
        private static string lastGradingLogText = "";

        // Runs once per camera per frame, right after the game's own post-process volumes (shared profile, stage
        // volumes, hurt tints...) were blended into the camera's settings. The color changes are applied as offsets
        // ON TOP of that result, so with strength 0 (or the mod off) the picture is exactly the original one.
        public static void BlendedPost(PostProcessLayer postProcessLayer, Camera camera)
        {
            if (!State.VisualsOn || State.Strength <= 0.001f || postProcessLayer == null) return;
            float k = State.Strength;

            float sat = Cfg.Saturation.Value * k;
            float con = Cfg.Contrast.Value * k;
            float exp = Cfg.Exposure.Value * k;
            Vector3 gain = BalanceGain;
            bool tint = (gain - Vector3.one).sqrMagnitude > 0.0001f;

            ColorGrading g = postProcessLayer.GetSettings<ColorGrading>();
            if (g != null)
            {
                LogVanillaGrading(g, camera, sat, con, exp);
                if (sat != 0f || con != 0f || exp != 0f || tint)
                {
                    g.enabled.value = true;
                    g.saturation.value = Mathf.Clamp(g.saturation.value + sat, -100f, 100f);
                    g.contrast.value = Mathf.Clamp(g.contrast.value + con, -100f, 100f);
                    g.postExposure.value += exp;
                    if (tint)
                    {
                        Color f = g.colorFilter.value;
                        g.colorFilter.value = new Color(f.r * gain.x, f.g * gain.y, f.b * gain.z, f.a);
                    }
                }
            }

            // Bloom is never switched on here: the game's own setting decides; we only cap the intensity.
            if (Cfg.LimitBloom.Value)
            {
                Bloom b = postProcessLayer.GetSettings<Bloom>();
                if (b != null && b.enabled.value)
                {
                    float v = b.intensity.value;
                    float cap = Cfg.BloomIntensity.Value;
                    if (v > cap) b.intensity.value = Mathf.Lerp(v, cap, k);
                }
            }
        }

        // A few log lines with the game's own grading values (before our offsets), to see what the picture is
        // based on. Throttled and capped; only written when the values changed.
        private static void LogVanillaGrading(ColorGrading g, Camera camera, float sat, float con, float exp)
        {
            if (gradingLogCount >= 12) return;
            float now = Time.unscaledTime;
            if (now - lastGradingLog < 15f) return;
            Color f = g.colorFilter.value;
            string text = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "enabled={0} mode={1} tonemapper={2} saturation={3:0.##} contrast={4:0.##} postExposure={5:0.##} colorFilter=({6:0.##},{7:0.##},{8:0.##}) temperature={9:0.#} tint={10:0.#}",
                g.enabled.value, g.gradingMode.value, g.tonemapper.value, g.saturation.value, g.contrast.value,
                g.postExposure.value, f.r, f.g, f.b, g.temperature.value, g.tint.value);
            if (text == lastGradingLogText) return;
            lastGradingLogText = text;
            lastGradingLog = now;
            gradingLogCount++;
            Plugin.Log.LogInfo("Game color grading on '" + (camera ? camera.name : "?") + "': " + text
                + " | our offsets: saturation " + sat.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)
                + ", contrast " + con.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)
                + ", exposure " + exp.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
        }

        // ---------------------------------------------------------------- menus / startup

        // Menu "NEW" badge text overlaps long labels in some languages: blank it before it is written to the label.
        public static void NewBadgePre(string ____token, ref string ___resolvedString)
        {
            if (Cfg.HideNewBadges.Value && ____token == "NEW_UNLOCKABLE") ___resolvedString = "";
        }

        // Startup: skip logo splash + intro video. Uses the game's own skip path (same as the splash_skip / intro_skip convars).
        // Must NOT jump to the title before the game finished loading (profiles are read at the end of the load),
        // otherwise the title shows "create profile". So we wait for RoR2Application.loadFinished first.
        private static bool splashFinishAllowed;
        private static bool splashWaiting;

        public static bool SplashStartPre(SplashScreenController __instance)
        {
            if (!Cfg.SkipIntro.Value) return true;
            FinishWhenLoaded(__instance);
            return false;
        }

        // Ignore the splash animation calling Finish() before loading is done; we call it ourselves when ready.
        public static bool SplashFinishPre(SplashScreenController __instance)
        {
            if (!Cfg.SkipIntro.Value) return true;
            if (splashFinishAllowed) return true;
            FinishWhenLoaded(__instance);
            return false;
        }

        private static void FinishWhenLoaded(SplashScreenController instance)
        {
            if (RoR2Application.loadFinished)
            {
                DoFinish(instance);
            }
            else if (!splashWaiting)
            {
                splashWaiting = true;
                RoR2Application.onLoad += () => { if (instance) DoFinish(instance); };
            }
        }

        private static void DoFinish(SplashScreenController instance)
        {
            splashFinishAllowed = true;
            IntroCutsceneController.shouldSkip = true;
            instance.Finish();
        }

        // ---------------------------------------------------------------- damage numbers

        public static bool DamageNumberPre(float amount, Vector3 position, bool crit, TeamIndex teamIndex, DamageColorIndex damageColorIndex)
        {
            return DamageNumbers.Handle(amount, position, crit, teamIndex, damageColorIndex);
        }

        // ---------------------------------------------------------------- particles

        // Original values are remembered per system so pooled effects don't compound.
        private class Original
        {
            public float rate;
            public int maxParticles;
            public ParticleSystem.Burst[] bursts;
        }

        private static readonly ConditionalWeakTable<ParticleSystem, Original> originals = new ConditionalWeakTable<ParticleSystem, Original>();
        private static readonly List<ParticleSystem> buffer = new List<ParticleSystem>();
        private static ParticleSystem.Burst[] burstScratch = new ParticleSystem.Burst[8];
        private static readonly Dictionary<int, bool> protectedByIndex = new Dictionary<int, bool>();
        private static readonly Dictionary<string, bool> protectedByName = new Dictionary<string, bool>();
        private static bool anyScaled;
        private static float spawnRate;      // exponentially decayed counter, ~ effects per second
        private static float lastSpawnTime;

        private static bool NameLooksProtected(string name)
        {
            string n = name.ToLowerInvariant();
            return n.Contains("telegraph") || n.Contains("warning") || n.Contains("indicator") || n.Contains("charge")
                || n.Contains("prepare") || n.Contains("marker") || n.Contains("targeting") || n.Contains("aoe");
        }

        // Cached per effect type (catalog index) so the name string is only built once per effect.
        private static bool IsProtected(EffectComponent effect)
        {
            if (!Cfg.ProtectTelegraphs.Value) return false;
            bool v;
            int index = (int)effect.effectIndex;
            if (index >= 0)
            {
                if (protectedByIndex.TryGetValue(index, out v)) return v;
                v = NameLooksProtected(effect.name);
                protectedByIndex[index] = v;
                return v;
            }
            string name = effect.name;
            if (protectedByName.TryGetValue(name, out v)) return v;
            v = NameLooksProtected(name);
            protectedByName[name] = v;
            return v;
        }

        public static void EffectResetPost(EffectComponent __instance)
        {
            float now = Time.unscaledTime;
            spawnRate = spawnRate * Mathf.Exp(-(now - lastSpawnTime)) + 1f;
            lastSpawnTime = now;

            float minScale = Eff.ParticleMin;
            float s = minScale;
            if (Cfg.AdaptiveParticles.Value && minScale < 0.999f)
            {
                float load = Mathf.InverseLerp(Cfg.AdaptiveLowRate.Value, Mathf.Max(Cfg.AdaptiveLowRate.Value + 1f, Cfg.AdaptiveHighRate.Value), spawnRate);
                s = Mathf.Lerp(1f, minScale, load);
            }

            float farDist = Cfg.FarEffectDistance.Value;
            if (farDist > 0f && s > 0.001f && s < 0.999f && State.VisualsOn && CamPosValid && __instance.effectData != null)
            {
                if ((__instance.effectData.origin - CamPos).sqrMagnitude > farDist * farDist)
                    s *= Mathf.Lerp(1f, Mathf.Clamp(Cfg.FarEffectScale.Value, 0.05f, 1f), State.Strength);
            }

            if (!State.VisualsOn) s = 1f;
            if (s >= 0.999f && !anyScaled) return;
            if (s < 0.999f && IsProtected(__instance)) s = 1f;

            __instance.GetComponentsInChildren(true, buffer);
            for (int i = 0; i < buffer.Count; i++)
            {
                ParticleSystem ps = buffer[i];
                // Particle systems that drive trigger/collision events can be gameplay-relevant: leave them alone.
                if (ps.trigger.enabled || ps.collision.enabled) continue;

                Original o;
                if (!originals.TryGetValue(ps, out o))
                {
                    if (s >= 0.999f) continue; // nothing changed yet, nothing to restore
                    o = new Original();
                    ParticleSystem.EmissionModule em0 = ps.emission;
                    o.rate = em0.rateOverTimeMultiplier;
                    o.maxParticles = ps.main.maxParticles;
                    o.bursts = new ParticleSystem.Burst[em0.burstCount];
                    em0.GetBursts(o.bursts);
                    originals.Add(ps, o);
                    anyScaled = true;
                }

                ParticleSystem.EmissionModule em = ps.emission;
                em.rateOverTimeMultiplier = o.rate * s;

                int count = o.bursts.Length;
                if (count > 0)
                {
                    if (burstScratch.Length < count) burstScratch = new ParticleSystem.Burst[count];
                    for (int b = 0; b < count; b++)
                    {
                        ParticleSystem.Burst burst = o.bursts[b];
                        // Leave tiny bursts (flash quads, single sprites) alone so effects don't vanish.
                        if (s < 0.999f)
                        {
                            ParticleSystem.MinMaxCurve c = burst.count;
                            if (c.mode == ParticleSystemCurveMode.Constant && c.constant > 2f)
                            {
                                c.constant = Mathf.Max(2f, c.constant * s);
                                burst.count = c;
                            }
                            else if (c.mode == ParticleSystemCurveMode.TwoConstants && c.constantMax > 2f)
                            {
                                c.constantMin = Mathf.Max(1f, c.constantMin * s);
                                c.constantMax = Mathf.Max(2f, c.constantMax * s);
                                burst.count = c;
                            }
                        }
                        burstScratch[b] = burst;
                    }
                    em.SetBursts(burstScratch, count);
                }

                ParticleSystem.MainModule main = ps.main;
                main.maxParticles = o.maxParticles > 8 ? Mathf.Max(8, Mathf.RoundToInt(o.maxParticles * s)) : o.maxParticles;
            }
            buffer.Clear();
        }
    }
}
