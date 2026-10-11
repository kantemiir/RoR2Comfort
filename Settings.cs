using System;
using BepInEx.Configuration;
using UnityEngine;

namespace RoR2Comfort
{
    internal enum DamageNumberMode
    {
        Show,       // vanilla
        Aggregate,  // one summed number per target every few tenths of a second
        Hide
    }

    // Runtime switches (hotkeys) and the current overall strength.
    internal static class State
    {
        public static bool Master = true;
        public static bool Camera = true, Shake = true, Fov = true, Mouse = true, Visuals = true;

        // 0..1: Cfg.Strength * progressive ramp. Updated every frame by the plugin.
        public static float Strength = 1f;

        public static bool CameraOn { get { return Master && Camera; } }
        public static bool ShakeOn { get { return Master && Shake; } }
        public static bool FovOn { get { return Master && Fov; } }
        public static bool MouseOn { get { return Master && Mouse; } }
        public static bool VisualsOn { get { return Master && Visuals; } }
    }

    // Effective values: configured value blended with the vanilla value by the current strength.
    // Master/group switches off => the vanilla value.
    internal static class Eff
    {
        private static float L(float vanilla, float configured)
        {
            return Mathf.Lerp(vanilla, configured, State.Strength);
        }

        public static float ExtraDistance { get { return State.CameraOn ? Cfg.ExtraDistance.Value * State.Strength : 0f; } }
        public static float ExtraHeight { get { return State.CameraOn ? Cfg.ExtraHeight.Value * State.Strength : 0f; } }

        public static float Shake { get { return State.ShakeOn ? L(1f, Cfg.ShakeScale.Value) : 1f; } }
        // Recoil follows the shake slider (twice as strong, capped at vanilla) so there is a single control.
        public static float Recoil { get { return State.ShakeOn ? L(1f, Mathf.Min(1f, Cfg.ShakeScale.Value * 2f)) : 1f; } }

        // One switch (LimitAbilityFov) governs both the sprint FOV widening and ability/item FOV bursts.
        public static float SprintFov { get { return State.FovOn && Cfg.LimitAbilityFov.Value ? L(1f, Cfg.SprintFovScale.Value) : 1f; } }
        public static bool LimitFov { get { return State.FovOn && Cfg.LimitAbilityFov.Value && State.Strength > 0.01f; } }
        public static float FovMaxIncrease { get { return L(60f, Cfg.AbilityFovMaxIncrease.Value); } }

        public static float LookSmoothing { get { return State.MouseOn ? Cfg.LookSmoothing.Value * State.Strength : 0f; } }
        public static float MaxLookPerFrame
        {
            get
            {
                if (!State.MouseOn || Cfg.MaxLookPerFrame.Value <= 0f) return 0f;
                return L(90f, Cfg.MaxLookPerFrame.Value);
            }
        }
        public static float AimStickiness { get { return State.MouseOn ? Mathf.Clamp01(Cfg.AimStickiness.Value) : 0f; } }

        public static float ScreenDamage { get { return State.VisualsOn ? L(1f, Cfg.ScreenDamageScale.Value) : 1f; } }
        // Full-screen flash volumes share the hurt-tint slider.
        public static float Flash { get { return State.VisualsOn ? L(1f, Cfg.ScreenDamageScale.Value) : 1f; } }
        public static float Fog { get { return State.VisualsOn ? L(1f, Cfg.FogScale.Value) : 1f; } }
        public static float ParticleMin { get { return State.VisualsOn ? L(1f, Mathf.Clamp(Cfg.ParticleCountScale.Value, 0.05f, 1f)) : 1f; } }

        public static DamageNumberMode Numbers
        {
            get
            {
                if (!State.VisualsOn || State.Strength < 0.5f) return DamageNumberMode.Show;
                return Cfg.DamageNumbers.Value;
            }
        }
    }

    internal static class Cfg
    {
        public static event Action Changed;

        // General
        public static ConfigEntry<float> Strength;
        public static ConfigEntry<bool> Progressive;
        public static ConfigEntry<float> ProgressiveStart;
        public static ConfigEntry<int> ProgressiveFullAt;
        public static ConfigEntry<int> BreakMinutes;

        // Camera / shake / FOV / mouse
        public static ConfigEntry<float> ExtraDistance, ExtraHeight;
        public static ConfigEntry<float> ShakeScale;
        public static ConfigEntry<float> SprintFovScale, AbilityFovMaxIncrease;
        public static ConfigEntry<bool> LimitAbilityFov;
        public static ConfigEntry<float> LookSmoothing, MaxLookPerFrame, AimStickiness;

        // Vignette
        public static ConfigEntry<float> VignetteMax, VignetteSensitivity, VignetteSmoothness;
        public static ConfigEntry<float> VignetteSpeedStart, VignetteSpeedFull, VignetteTurnStart, VignetteTurnFull;

        // HUD / effects
        public static ConfigEntry<DamageNumberMode> DamageNumbers;
        public static ConfigEntry<float> DamageNumberWindow;
        public static ConfigEntry<float> ScreenDamageScale;
        public static ConfigEntry<bool> HideNewBadges;
        public static ConfigEntry<float> ParticleCountScale;
        public static ConfigEntry<bool> AdaptiveParticles, ProtectTelegraphs;
        public static ConfigEntry<float> AdaptiveLowRate, AdaptiveHighRate, FarEffectDistance, FarEffectScale;

        // Color
        public static ConfigEntry<float> Saturation, Contrast, Exposure, BloomIntensity, FogScale;
        public static ConfigEntry<bool> LimitBloom;
        public static ConfigEntry<float> AutoColorStrength;

        // Startup / indicator / hotkeys
        public static ConfigEntry<bool> SkipIntro, ShowIndicator;
        public static ConfigEntry<KeyboardShortcut> MasterKey, CameraKey, ShakeKey, FovKey, MouseKey, VisualsKey, StrengthKey;

        public static void Bind(ConfigFile c)
        {
            Strength = c.Bind("General", "Strength", 1f, "Overall comfort strength: 1 = your configured values, 0 = vanilla. Everything below is blended by this. F8 cycles 100% / 70% / 40%.");
            Progressive = c.Bind("General", "ProgressiveComfort", false, "Ramp the strength up with the stage number: gentle early, full strength in the late game.");
            ProgressiveStart = c.Bind("General", "ProgressiveStartStrength", 0.35f, "[Advanced] Strength on stage 1 when ProgressiveComfort is on.");
            ProgressiveFullAt = c.Bind("General", "ProgressiveFullAtStage", 5, "Stage number at which full strength is reached when ProgressiveComfort is on.");
            BreakMinutes = c.Bind("General", "BreakReminderMinutes", 0, "Show a short on-screen 'time for a break' note every N minutes of play. 0 = never (default).");

            ExtraDistance = c.Bind("Camera", "ExtraDistance", 4f, "How much farther the camera sits behind the character (added to the game's value). Targeting/interaction searches are compensated, so this does not change gameplay.");
            ExtraHeight = c.Bind("Camera", "ExtraHeight", 1.5f, "How much higher the camera sits above the character.");

            // Camera shake; firing recoil follows it (twice as strong, capped at vanilla).
            ShakeScale = c.Bind("Shake", "ShakeScale", 0.1f, "Camera shake strength: 0 = none, 1 = vanilla. Firing recoil follows it (twice this value, max 1).");

            SprintFovScale = c.Bind("Fov", "SprintFovScale", 0f, "[Advanced] How much of the sprint FOV widening to keep when LimitAbilityFov is on: 0 = none, 1 = vanilla.");
            LimitAbilityFov = c.Bind("Fov", "LimitAbilityFov", true, "Limit FOV widening from sprinting, abilities and items (zooming in, e.g. scopes, is untouched).");
            AbilityFovMaxIncrease = c.Bind("Fov", "AbilityFovMaxIncrease", 0f, "[Advanced] Max FOV widening in degrees allowed by abilities when the limit is on.");

            LookSmoothing = c.Bind("Mouse", "LookSmoothing", 0.03f, "Mouse look smoothing time in seconds. 0 = off. Higher = smoother but more input lag.");
            MaxLookPerFrame = c.Bind("Mouse", "MaxLookPerFrame", 0f, "[Advanced] Max camera turn per frame in degrees (limits sudden flicks). 0 = off.");
            AimStickiness = c.Bind("Mouse", "AimStickiness", 0f, "ASSIST (changes difficulty, off by default): slows the mouse by this fraction while the crosshair is over an enemy, so small corrections need less flicking. 0 = off.");

            VignetteMax = c.Bind("Vignette", "VignetteMax", 0.4f, "Comfort vignette: the screen edges darken while you move fast or turn fast, like VR comfort modes. Maximum intensity (0 = off, 1 = very dark edges).");
            VignetteSensitivity = c.Bind("Vignette", "VignetteSensitivity", 1f, "How easily the vignette appears: 1 = default thresholds, 2 = twice as sensitive, 0.5 = half as sensitive.");
            VignetteSmoothness = c.Bind("Vignette", "VignetteSmoothness", 0.55f, "[Advanced] Softness of the vignette edge (0.01..1).");
            VignetteSpeedStart = c.Bind("Vignette", "SpeedStart", 14f, "[Advanced] Camera speed (units/s) where the vignette starts to appear.");
            VignetteSpeedFull = c.Bind("Vignette", "SpeedFull", 40f, "[Advanced] Camera speed (units/s) for the full vignette.");
            VignetteTurnStart = c.Bind("Vignette", "TurnStart", 150f, "[Advanced] Turn rate (degrees/s) where the vignette starts to appear.");
            VignetteTurnFull = c.Bind("Vignette", "TurnFull", 500f, "[Advanced] Turn rate (degrees/s) for the full vignette.");

            DamageNumbers = c.Bind("Hud", "DamageNumbers", DamageNumberMode.Aggregate, "Show = vanilla, Aggregate = one summed number per target every DamageNumberWindow seconds, Hide = none.");
            DamageNumberWindow = c.Bind("Hud", "DamageNumberWindow", 0.5f, "[Advanced] Seconds over which damage numbers are summed in Aggregate mode.");
            ScreenDamageScale = c.Bind("Hud", "ScreenDamageScale", 0.3f, "Strength of the red hurt tint / desaturation / distortion AND of the game's full-screen flash effects (storms, worm death, beams): 0 = none, 1 = vanilla.");
            HideNewBadges = c.Bind("Hud", "HideNewBadges", true, "Hide the \"NEW\" (НОВИНКА) badge text in menus; in some languages it overlaps the labels.");

            ParticleCountScale = c.Bind("Effects", "ParticleCountScale", 0.5f, "Particle emission scale of spawned effects. With AdaptiveParticles this is the value reached under heavy load; otherwise it is always applied. 1 = vanilla.");
            AdaptiveParticles = c.Bind("Effects", "AdaptiveParticles", true, "Vanilla effects while it is calm, scaling down toward ParticleCountScale as the number of effects per second grows.");
            AdaptiveLowRate = c.Bind("Effects", "AdaptiveLowRate", 12f, "[Advanced] Effects per second below which nothing is reduced.");
            AdaptiveHighRate = c.Bind("Effects", "AdaptiveHighRate", 70f, "[Advanced] Effects per second at which the full reduction is applied.");
            FarEffectDistance = c.Bind("Effects", "FarEffectDistance", 60f, "[Advanced] Effects farther than this from the camera are reduced extra. 0 = off.");
            FarEffectScale = c.Bind("Effects", "FarEffectScale", 0.5f, "[Advanced] Extra scale for far effects.");
            ProtectTelegraphs = c.Bind("Effects", "ProtectTelegraphs", true, "[Advanced] Never reduce effects whose name looks like a warning/telegraph/indicator/charge-up.");

            Saturation = c.Bind("Color", "Saturation", -25f, "Color saturation adjustment (-100 grey .. 0 vanilla).");
            Contrast = c.Bind("Color", "Contrast", 0f, "Contrast adjustment (negative = flatter/hazier, 0 = vanilla, positive = punchier).");
            Exposure = c.Bind("Color", "Exposure", 0.5f, "Brightness in EV stops (negative = darker, positive = brighter).");
            FogScale = c.Bind("Color", "FogScale", 0.65f, "Strength of the game's depth fog / haze: 1 = vanilla. Note: the fog also lights up the distance, so very low values make the scene dark - raise Exposure to compensate.");
            LimitBloom = c.Bind("Color", "LimitBloom", true, "Cap bloom (glow) intensity with the value below. Bloom is never switched on if the game's own setting has it off.");
            BloomIntensity = c.Bind("Color", "BloomIntensity", 0.15f, "Bloom intensity used when LimitBloom is on.");
            AutoColorStrength = c.Bind("Color", "AutoColorStrength", 0.6f, "Automatically pulls an overwhelmingly one-colored scene (e.g. an all-red stage) back toward neutral. 0 = off; higher = even mildly tinted scenes get corrected. Normal-looking scenes are left alone at the default.");

            SkipIntro = c.Bind("Startup", "SkipIntro", true, "Skip the startup splash screens and the intro cutscene video and go straight to the title screen. Applies on the next game start.");
            ShowIndicator = c.Bind("Indicator", "ShowIndicator", true, "Show a small COMFORT ON/OFF label at the top of the screen. Toggling always flashes the screen green/red.");

            MasterKey = c.Bind("Hotkeys", "MasterToggle", new KeyboardShortcut(KeyCode.F7), "Mod on/off (green/red flash). Off = fully vanilla.");
            CameraKey = c.Bind("Hotkeys", "CameraToggle", new KeyboardShortcut(KeyCode.F2), "Toggle camera distance/height.");
            ShakeKey = c.Bind("Hotkeys", "ShakeToggle", new KeyboardShortcut(KeyCode.F3), "Toggle shake and recoil reduction.");
            FovKey = c.Bind("Hotkeys", "FovToggle", new KeyboardShortcut(KeyCode.F4), "Toggle FOV limits.");
            MouseKey = c.Bind("Hotkeys", "MouseToggle", new KeyboardShortcut(KeyCode.F5), "Toggle mouse smoothing and aim stickiness.");
            VisualsKey = c.Bind("Hotkeys", "VisualsToggle", new KeyboardShortcut(KeyCode.F6), "Toggle colors, bloom, vignette, flashes, particles and damage numbers.");
            StrengthKey = c.Bind("Hotkeys", "StrengthCycle", new KeyboardShortcut(KeyCode.F8), "Cycle overall strength 100% / 70% / 40%.");

            c.SettingChanged += (s, a) => { if (Changed != null) Changed(); };
        }
    }
}
