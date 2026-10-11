using System;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using UnityEngine;

namespace RoR2Comfort
{
    // Risk of Options settings tab: four categories, plain-language names, related settings merged.
    // Fine-tuning values (marked [Advanced] in the config file) are intentionally not shown here.
    // Every option is added separately so one failure never hides the rest.
    internal static class OptionsUi
    {
        private static bool ru;

        private static string T(string en, string rus)
        {
            return ru ? rus : en;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Register()
        {
            ru = Application.systemLanguage == SystemLanguage.Russian;

            RiskOfOptions.ModSettingsManager.SetModDescription(T(
                "Reduces motion sickness triggers. Hotkeys are unassigned by default (see the config file, [Hotkeys]).",
                "Снижает причины укачивания. Горячие клавиши по умолчанию не назначены (см. файл настроек, раздел [Hotkeys])."));

            SetIcon();

            string general = T("General", "Общее");
            string camera = T("Camera & mouse", "Камера и мышь");
            string picture = T("Picture & color", "Картинка и цвет");
            string effects = T("Effects & HUD", "Эффекты и интерфейс");

            // ---- General
            Slider(Cfg.Strength, general, 0f, 1f, "{0:0%}",
                T("Overall strength", "Общая сила мода"),
                T("100% = all your settings, 0% = like the original game.",
                  "100% = все ваши настройки, 0% = как в оригинальной игре."));
            Check(Cfg.Progressive, general,
                T("Ramp up with stage", "Усиливать по стадиям"),
                T("Gentle on the first stages, full strength in the late game.",
                  "Мягко на первых стадиях, на полную силу в поздней игре."));
            IntSlider(Cfg.ProgressiveFullAt, general, 2, 12,
                T("Full strength at stage", "Полная сила на стадии"),
                T("Stage at which the mod reaches full strength.", "Стадия, с которой мод работает на полную силу."),
                () => !Cfg.Progressive.Value);
            IntSlider(Cfg.BreakMinutes, general, 0, 120,
                T("Break reminder, minutes (0 = off)", "Напоминание о перерыве, мин (0 = выкл)"),
                T("Shows a short note every N minutes of play.", "Короткая надпись каждые N минут игры."), null);
            Check(Cfg.SkipIntro, general,
                T("Skip intro on startup", "Пропускать заставку при запуске"),
                T("Skips logos and the intro video (waits for loading to finish). Applies on next start.",
                  "Пропускает логотипы и вступительный ролик (дожидается конца загрузки). Со следующего запуска."));
            Check(Cfg.HideNewBadges, general,
                T("Hide \"NEW\" badges in menus", "Скрывать ярлыки «НОВИНКА» в меню"),
                T("The badge text overlaps long labels in some languages.", "Надпись налезает на длинные названия в некоторых языках."));
            Check(Cfg.ShowIndicator, general,
                T("Show COMFORT label on screen", "Показывать надпись COMFORT на экране"),
                T("Small label at the top, plus a green/red screen flash on startup and when a hotkey toggles the mod. Off by default.",
                  "Небольшая надпись сверху и зелёная/красная вспышка при запуске и при переключении мода клавишей. По умолчанию выключено."));

            // ---- Camera & mouse
            Slider(Cfg.ExtraDistance, camera, 0f, 30f, "{0:0.0}",
                T("Camera distance", "Камера: дальше"),
                T("How much farther the camera sits behind the character. Targeting and interaction are compensated.",
                  "Насколько камера дальше от персонажа. Наведение и взаимодействие компенсируются."));
            Slider(Cfg.ExtraHeight, camera, 0f, 15f, "{0:0.0}",
                T("Camera height", "Камера: выше"),
                T("How much higher the camera sits above the character.", "Насколько камера выше над персонажем."));
            Slider(Cfg.ShakeScale, camera, 0f, 1f, "{0:0%}",
                T("Camera shake & recoil", "Тряска камеры и отдача"),
                T("0% = none, 100% = like the original. Firing recoil follows this slider.",
                  "0% = нет, 100% = как в игре. Отдача при стрельбе следует за этим ползунком."));
            Check(Cfg.LimitAbilityFov, camera,
                T("Limit FOV widening", "Не расширять FOV"),
                T("Removes FOV widening from sprinting, abilities and items. Zooming in (scopes) is untouched.",
                  "Убирает расширение FOV при спринте, способностях и предметах. Приближение (прицелы) не трогает."));
            Slider(Cfg.LookSmoothing, camera, 0f, 0.2f, "{0:0.000}",
                T("Mouse smoothing (0 = off)", "Сглаживание мыши (0 = выкл)"),
                T("Smooths sudden mouse movements. Higher = smoother but adds input lag.",
                  "Сглаживает резкие движения мыши. Больше = плавнее, но с задержкой."));
            Slider(Cfg.AimStickiness, camera, 0f, 0.8f, "{0:0%}",
                T("Aim assist (changes difficulty, 0 = off)", "Помощь прицела (меняет сложность, 0 = выкл)"),
                T("Slows the mouse while the crosshair is on an enemy.", "Замедляет мышь, пока прицел на враге."));

            // ---- Picture & color
            Slider(Cfg.Saturation, picture, -100f, 0f, "{0:0}",
                T("Color saturation", "Насыщенность цвета"),
                T("0 = like the original, -100 = grayscale.", "0 = как в игре, -100 = чёрно-белое."));
            Slider(Cfg.Contrast, picture, -50f, 50f, "{0:0}",
                T("Contrast", "Контраст"),
                T("0 = like the original. Negative = flatter and hazier.", "0 = как в игре. Минус = площе и мутнее."));
            Slider(Cfg.Exposure, picture, -3f, 2f, "{0:0.0}",
                T("Brightness", "Яркость"),
                T("In stops. Raise it if the picture is too dark (especially with less fog).",
                  "В ступенях экспозиции. Поднимите, если слишком темно (особенно при слабом тумане)."));
            Slider(Cfg.FogScale, picture, 0f, 1f, "{0:0%}",
                T("Fog / haze", "Туман"),
                T("100% = like the original. Fog also lights the distance, so very low values look dark.",
                  "100% = как в игре. Туман ещё и подсвечивает даль, при очень низких значениях темно."));
            Check(Cfg.LimitBloom, picture,
                T("Limit bloom (glow)", "Ограничить свечение (bloom)"),
                T("Caps the glow intensity. Never switches bloom on if you disabled it in the game's settings.",
                  "Ограничивает силу свечения. Не включает bloom, если он выключен в настройках игры."));
            Slider(Cfg.BloomIntensity, picture, 0f, 3f, "{0:0.00}",
                T("Bloom cap", "Предел свечения"),
                T("Maximum glow intensity when the limit is on.", "Максимальная сила свечения при включённом ограничении."),
                () => !Cfg.LimitBloom.Value);
            Slider(Cfg.AutoColorStrength, picture, 0f, 1f, "{0:0%}",
                T("Auto color balance (0 = off)", "Автобаланс цвета (0 = выкл)"),
                T("Pulls scenes that are almost one color (e.g. all red) toward neutral. Normal scenes are left alone.",
                  "Тянет почти однотонные сцены (например, «всё красное») к нейтральному. Обычные сцены не трогает."));

            // ---- Effects & HUD
            Choice(Cfg.DamageNumbers, effects,
                T("Damage numbers", "Числа урона"),
                T("Show = original, Aggregate = one summed number per target every half second, Hide = none.",
                  "Show = как в игре, Aggregate = одно суммарное число на цель раз в полсекунды, Hide = скрыть."));
            Slider(Cfg.ScreenDamageScale, effects, 0f, 1f, "{0:0%}",
                T("Hurt tint & screen flashes", "Подсветка урона и вспышки"),
                T("Strength of the red hurt tint and the full-screen flashes (storms, beams...). 100% = like the original.",
                  "Сила красной подсветки при уроне и полноэкранных вспышек (грозы, лучи...). 100% = как в игре."));
            Slider(Cfg.ParticleCountScale, effects, 0.05f, 1f, "{0:0%}",
                T("Particles when it gets busy", "Частицы при большом количестве эффектов"),
                T("Particle amount at the busiest moments. 100% = no reduction. Warnings are protected.",
                  "Количество частиц в самые загруженные моменты. 100% = без сокращения. Предупреждения о атаках защищены."));
            Check(Cfg.AdaptiveParticles, effects,
                T("Reduce only when busy", "Сокращать только при большой нагрузке"),
                T("On: effects are original while it is calm. Off: always reduced.",
                  "Вкл: пока спокойно, эффекты как в игре. Выкл: сокращать всегда."));
        }

        // The mod icon shown next to the name in the Risk of Options list (icon.png is embedded in the DLL).
        private static void SetIcon()
        {
            try
            {
                using (System.IO.Stream s = typeof(OptionsUi).Assembly.GetManifestResourceStream("icon.png"))
                {
                    if (s == null) return;
                    byte[] bytes = new byte[s.Length];
                    int read = 0;
                    while (read < bytes.Length)
                    {
                        int n = s.Read(bytes, read, bytes.Length - read);
                        if (n <= 0) break;
                        read += n;
                    }

                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!ImageConversion.LoadImage(tex, bytes)) return;
                    tex.filterMode = FilterMode.Bilinear;
                    Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    RiskOfOptions.ModSettingsManager.SetModIcon(sprite, Plugin.Guid, Plugin.Name);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Could not set the mod icon: " + e.Message);
            }
        }

        private static void Safe(Action add, string what)
        {
            try { add(); }
            catch (Exception e) { Plugin.Log.LogWarning("Risk of Options: could not add " + what + ": " + e.Message); }
        }

        private static void Fill(RiskOfOptions.OptionConfigs.BaseOptionConfig cfg, string category, string name, string description, Func<bool> disabled)
        {
            cfg.category = category;
            cfg.name = name;
            cfg.description = description;
            if (disabled != null)
                cfg.checkIfDisabled = () => disabled();
        }

        private static void Slider(ConfigEntry<float> e, string category, float min, float max, string fmt, string name, string description, Func<bool> disabled = null)
        {
            Safe(() =>
            {
                RiskOfOptions.OptionConfigs.SliderConfig c = new RiskOfOptions.OptionConfigs.SliderConfig { min = min, max = max, FormatString = fmt };
                Fill(c, category, name, description, disabled);
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.SliderOption(e, c));
            }, e.Definition.Key);
        }

        private static void IntSlider(ConfigEntry<int> e, string category, int min, int max, string name, string description, Func<bool> disabled)
        {
            Safe(() =>
            {
                RiskOfOptions.OptionConfigs.IntSliderConfig c = new RiskOfOptions.OptionConfigs.IntSliderConfig { min = min, max = max };
                Fill(c, category, name, description, disabled);
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.IntSliderOption(e, c));
            }, e.Definition.Key);
        }

        private static void Check(ConfigEntry<bool> e, string category, string name, string description, Func<bool> disabled = null)
        {
            Safe(() =>
            {
                RiskOfOptions.OptionConfigs.CheckBoxConfig c = new RiskOfOptions.OptionConfigs.CheckBoxConfig();
                Fill(c, category, name, description, disabled);
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.CheckBoxOption(e, c));
            }, e.Definition.Key);
        }

        private static void Choice(ConfigEntryBase e, string category, string name, string description)
        {
            Safe(() =>
            {
                RiskOfOptions.OptionConfigs.ChoiceConfig c = new RiskOfOptions.OptionConfigs.ChoiceConfig();
                Fill(c, category, name, description, null);
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.ChoiceOption(e, c));
            }, e.Definition.Key);
        }
    }
}
