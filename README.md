# RoR2 Comfort

**RU** · [EN below](#english)

Мод для **Risk of Rain 2** против укачивания (motion sickness): убирает или ослабляет то, что чаще всего вызывает тошноту, — тряску камеры, отдачу, резкие скачки FOV, рывки мыши, вспышки на экране, лишние частицы и «вырвиглазные» однотонные цвета. Каждая настройка меняется прямо в игре (через [Risk of Options](https://thunderstore.io/package/Rune580/Risk_Of_Options/)).

> Сделан человеком, которого сильно укачивает в поздней игре. Проверен на одном компьютере и в основном на ранних стадиях. Мод не меняет геймплей и баланс: только камеру и картинку на вашем экране (в сетевой игре работает локально, хосту ничего не нужно). Игра использует положение камеры для некоторых поисков (самонаведение, взаимодействие, наведение снаряжения, пинг); мод компенсирует сдвиг камеры, чтобы они работали как в оригинале. Единственная функция, которая меняет сложность, — `AimStickiness` (помощь прицела), она выключена по умолчанию.

## Что делает

| Группа (клавиша) | Что делает |
|---|---|
| **Камера** (`CameraToggle`) | Камера дальше и выше от персонажа (`ExtraDistance`, `ExtraHeight`) |
| **Тряска и отдача** (`ShakeToggle`) | Масштаб тряски экрана; отдача при стрельбе следует за ним (`ShakeScale`) |
| **FOV** (`FovToggle`) | Убирает расширение FOV при спринте и от способностей/предметов (`LimitAbilityFov`; тонко — `SprintFovScale`, `AbilityFovMaxIncrease` в файле); приближение (прицелы) не трогает |
| **Мышь** (`MouseToggle`) | Сглаживание поворота (`LookSmoothing`, сохраняет общий угол), опциональная помощь прицела (`AimStickiness`); ограничитель резких рывков `MaxLookPerFrame` — только в файле |
| **Картинка** (`VisualsToggle`) | Числа урона (суммирование), красная подсветка урона, вспышки, частицы эффектов, насыщенность/контраст/яркость, bloom, туман, автобаланс цвета |
| **Мастер** (`MasterToggle`) | Весь мод вкл/выкл. Выключено = обычная игра |
| **Сила** (`StrengthCycle`) | Переключает общую силу 100% / 70% / 40% (`Strength`) |

**Горячие клавиши по умолчанию не назначены.** Чтобы пользоваться ими, впишите клавишу в файле `BepInEx/config/local.RoR2Comfort.cfg`, раздел `[Hotkeys]`, например `MasterToggle = F7`.

### Новое в 0.4.x

- **Общая сила и «усиление по стадиям»:** `Strength` смешивает всё с оригиналом; `ProgressiveComfort` постепенно усиливает мод от первой стадии к поздним.
- **Умный лимит эффектов:** `AdaptiveParticles` — пока спокойно, эффекты как в оригинале; чем больше эффектов в секунду, тем сильнее сокращение до `ParticleCountScale`. Дальние эффекты сокращаются дополнительно, предупреждения о атаках (по названию эффекта) защищены.
- **Суммирование чисел урона:** `DamageNumbers = Aggregate` — одно суммарное число на цель раз в полсекунды вместо потока.
- **Ограничитель вспышек:** `ScreenDamageScale` ослабляет и красную подсветку урона, и полноэкранные вспышки (грозы, смерть червя, лучи).
- **Автобаланс цвета:** `AutoColorStrength` (0 = выкл) вытягивает к нейтральному сцены, где почти всё одного цвета (например, «всё красное»); обычные сцены не трогает.
- **Напоминание о перерыве:** `BreakReminderMinutes` — раз в N минут (по умолчанию выключено).
- **Упрощённое меню настроек:** 4 раздела (Общее, Камера и мышь, Картинка и цвет, Эффекты и интерфейс), понятные названия (на русском, если система русская). Тонкие параметры помечены `[Advanced]` в файле `BepInEx/config/local.RoR2Comfort.cfg`.
- **Помощь прицела (по желанию):** `AimStickiness` замедляет мышь, пока прицел на враге. Меняет сложность, выключено.
- Пакет для Thunderstore (манифест и иконка) в папке `thunderstore/`.

Дополнительно:
- скрывает текст-бейдж «НОВИНКА/NEW» в меню (в русской локализации он налезает на надписи) — `HideNewBadges`;
- пропускает логотипы и вступительный ролик при запуске игры (дожидается окончания загрузки, чтобы профили успели прочитаться) — `SkipIntro`.

### Индикатор

**По умолчанию выключен** (`ShowIndicator = false`). Если включить: при запуске и при переключении мода клавишей вся картинка на долю секунды вспыхивает **зелёным (вкл)** или **красным (выкл)**, а сверху экрана висит надпись `COMFORT ON` / `COMFORT 70%` / `COMFORT OFF`. Так сразу видно, работает ли мод, и можно быстро сравнивать «с модом» и «без».

## Установка

Нужны:
- **BepInEx 5** для RoR2 (пакет [RoR2BepInExPack](https://thunderstore.io/package/RiskofThunder/RoR2BepInExPack/));
- [Risk of Options](https://thunderstore.io/package/Rune580/Risk_Of_Options/) — по желанию, чтобы настройки были в меню игры (иначе правьте `BepInEx/config/local.RoR2Comfort.cfg`).

Затем положите папку `RoR2Comfort` из архива релиза в `BepInEx/plugins/` (если у вас менеджер модов — в `plugins` нужного профиля).

В игре: *Настройки → вкладка RoR2 Comfort*.

## Основные настройки по умолчанию

| Параметр | Значение | Смысл |
|---|---|---|
| `Strength` | 1 | общая сила: 1 = ваши значения, 0 = оригинал |
| `ExtraDistance` / `ExtraHeight` | 4 / 1.5 | камера дальше / выше |
| `ShakeScale` | 0.1 | тряска и отдача: 0 = нет, 1 = как в игре |
| `SprintFovScale` | 0 | 0 = FOV не расширяется при спринте |
| `LookSmoothing` | 0.03 с | 0 = выкл |
| `DamageNumbers` | Aggregate | Show / Aggregate / Hide |
| `ScreenDamageScale` | 0.3 | красная подсветка урона и полноэкранные вспышки |
| `ParticleCountScale` / `AdaptiveParticles` | 0.5 / вкл | минимум количества частиц под нагрузкой |
| `Saturation` / `Contrast` / `Exposure` | −25 / 0 / +0.5 | цветокоррекция |
| `LimitBloom` / `BloomIntensity` | вкл / 0.15 | ограничивает силу bloom; сам bloom мод не включает, если он выключен в настройках игры |
| `FogScale` | 0.65 | 1 = как в игре; туман ещё и подсвечивает даль, при очень низких значениях сцена темнеет — поднимайте `Exposure` |
| `AutoColorStrength` | 0.6 | автобаланс однотонных сцен (0 = выкл) |
| `BreakReminderMinutes` | 0 | напоминание о перерыве (0 = выкл) |
| `AimStickiness` | 0 | помощь прицела, выкл |

Значения подобраны под одного человека; подстраивайте под себя. Всё, что скрывает информацию (`DamageNumbers`, `ScreenDamageScale`, `ParticleCountScale`), делает игру менее читаемой — если стало труднее, верните эти значения ближе к оригиналу. Быстрое сравнение «с модом / без»: назначьте `MasterToggle` (например, F7).

## Известные ограничения

- Другие моды, которые меняют камеру/FOV/постобработку, могут конфликтовать.
- Частицы уменьшаются в количестве, а не отключаются полностью, а эффекты с «telegraph/warning/indicator/charge» в названии не затрагиваются. Это эвристика по имени, она не идеальна.
- Автобаланс цвета оценивает среднюю картинку экрана 2 раза в секунду и плавно (за секунды) корректирует цвет; он новый и проверялся мало. Если мешает — поставьте `AutoColorStrength` в 0 (или назначьте `VisualsToggle` и выключайте всю группу «Картинка»).
- Освещение сцены не переписывается; цвет подстраивается цветокоррекцией, туманом и автобалансом.
- Сглаживание мыши (`LookSmoothing`) добавляет небольшую задержку прицела; для точной стрельбы можно поставить 0.
- Проверено на сборке игры 1.5.0 (Steam build 25475991). После обновлений игры может потребоваться пересборка.
- Если вас укачивает, не полагайтесь только на мод: короткие сессии и перерывы тоже помогают.

## История версий

- **0.4.3** — иконка мода в списке Risk of Options (вместо знака вопроса).
- **0.4.2** — надпись `COMFORT` сверху и вспышка выключены по умолчанию; все горячие клавиши по умолчанию не назначены (назначаются в файле настроек).
- **0.4.1** — убрана комфортная виньетка (затемнение краёв при поворотах мешала); из настроек удалены все её параметры.
- **0.4.0** — общая сила (`Strength`, F8) и усиление по стадиям; адаптивный лимит эффектов; суммирование чисел урона; ограничитель вспышек; автобаланс цвета; напоминание о перерыве; опциональная помощь прицела; пакет для Thunderstore.
- **0.3.2** — камера больше не влияет на поиск целей/взаимодействие (компенсация в `ModifyAimRayIfApplicable`); bloom больше не включается принудительно; эффекты с триггерами/столкновениями частиц не затрагиваются.
- **0.3.1** — пропуск заставок при запуске (с ожиданием конца загрузки).
- **0.3.0** — первый публичный релиз.

## Сборка

Нужен .NET SDK 8+, игра с установленным BepInEx и (для UI) Risk of Options:

```bash
dotnet build -c Release -p:RoR2Dir="C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2"
```

Готовый `RoR2Comfort.dll` лежит в `bin/Release/netstandard2.1/`.

## Лицензия

MIT

---

## English

A **Risk of Rain 2** mod against motion sickness: it removes or tones down the things that most often cause nausea — camera shake, recoil, FOV bursts, mouse flicks, screen flashes, excess particles and eye-searing one-color scenes. Every setting is adjustable in-game (via [Risk of Options](https://thunderstore.io/package/Rune580/Risk_Of_Options/)).

> Made by a player who gets seriously motion-sick in the late game. Tested on one machine, mostly in early stages. It does not change gameplay or balance, only your own camera and picture (works locally in multiplayer, the host needs nothing). The game uses the camera position as the origin of some searches (auto-targeting, interaction, equipment targeting, pings); the mod compensates for the moved camera so they behave as in vanilla. The only feature that changes difficulty is `AimStickiness` (aim assist), which is off by default.

### Features

| Group (key) | What it does |
|---|---|
| **Camera** (`CameraToggle`) | Camera farther and higher (`ExtraDistance`, `ExtraHeight`) |
| **Shake & recoil** (`ShakeToggle`) | Scale screen shake; firing recoil follows it (`ShakeScale`) |
| **FOV** (`FovToggle`) | Removes FOV widening from sprinting and abilities/items (`LimitAbilityFov`; fine-tuning `SprintFovScale`, `AbilityFovMaxIncrease` in the file); zooming in (scopes) is untouched |
| **Mouse** (`MouseToggle`) | Look smoothing (conserves total turned angle) and optional aim assist (`LookSmoothing`, `AimStickiness`); the flick limiter `MaxLookPerFrame` is file-only |
| **Visuals** (`VisualsToggle`) | Damage numbers (aggregation), hurt tint, flashes, effect particle counts, saturation/contrast/exposure, bloom, fog, automatic color balance |
| **Master** (`MasterToggle`) | Whole mod on/off. Off = vanilla |
| **Strength** (`StrengthCycle`) | Cycles overall strength 100% / 70% / 40% (`Strength`) |

**Hotkeys are unassigned by default.** To use them, put a key name into `BepInEx/config/local.RoR2Comfort.cfg`, section `[Hotkeys]`, e.g. `MasterToggle = F7`.

### New in 0.4.x

- **Overall strength and progressive ramp:** `Strength` blends everything with vanilla; `ProgressiveComfort` ramps the mod up from stage 1 to the late game.
- **Adaptive effect limit:** vanilla effects while it is calm, scaling down toward `ParticleCountScale` as effects per second grow; far effects are reduced extra; effects named like warnings/telegraphs are protected.
- **Aggregated damage numbers:** one summed number per target every half second instead of a stream.
- **Flash limiter:** `ScreenDamageScale` tones down both the red hurt tint and full-screen flashes (storms, worm death, beams).
- **Automatic color balance:** `AutoColorStrength` (0 = off) pulls overwhelmingly one-colored scenes (e.g. an all-red stage) toward neutral; normal scenes are left alone.
- **Break reminder:** `BreakReminderMinutes`, every N minutes (off by default).
- **Simplified settings tab:** 4 categories (General, Camera & mouse, Picture & color, Effects & HUD) with plain names (in Russian on a Russian system). Fine-tuning values are marked `[Advanced]` in `BepInEx/config/local.RoR2Comfort.cfg`.
- **Optional aim assist:** `AimStickiness` slows the mouse while the crosshair is on an enemy. Changes difficulty; off by default.
- Thunderstore package (manifest and icon) in `thunderstore/`.

Also:
- hides the "NEW" badge text in menus (it overlaps labels in some localizations) — `HideNewBadges`;
- skips the startup logos and intro video (it waits for the game to finish loading so profiles are read first) — `SkipIntro`.

**Indicator:** off by default (`ShowIndicator = false`). When on, startup and hotkey toggles flash the screen **green (on)** or **red (off)** and a `COMFORT ON` / `COMFORT 70%` / `COMFORT OFF` label stays at the top of the screen.

### Install

Requires **BepInEx 5** ([RoR2BepInExPack](https://thunderstore.io/package/RiskofThunder/RoR2BepInExPack/)); [Risk of Options](https://thunderstore.io/package/Rune580/Risk_Of_Options/) is optional but gives you an in-game settings tab (otherwise edit `BepInEx/config/local.RoR2Comfort.cfg`). Put the `RoR2Comfort` folder from the release zip into `BepInEx/plugins/`.

### Notes

- Defaults are tuned for one person; adjust to taste. `FogScale` below 1 darkens the scene because the game's fog also lights the distance — raise `Exposure` to compensate.
- Anything that hides information (`DamageNumbers`, `ScreenDamageScale`, `ParticleCountScale`) makes the game less readable; bring those back toward vanilla if it feels harder. Assign `MasterToggle` (e.g. F7) for a quick with/without comparison.
- Bloom is never force-enabled: if you turned it off in the game's settings it stays off (`LimitBloom` only caps its intensity).
- Automatic color balance samples the screen twice a second and corrects slowly; it is new and lightly tested. Set `AutoColorStrength` to 0 if it bothers you.
- `LookSmoothing` adds a little input lag; set it to 0 for the snappiest aim.
- Built and checked against game version 1.5.0 (Steam build 25475991).
- Other mods touching camera / FOV / post-processing may conflict.

### Changelog

- **0.4.3** — mod icon in the Risk of Options list (instead of the question mark).
- **0.4.2** — the on-screen `COMFORT` label and flash are off by default; all hotkeys are unassigned by default (assign them in the config file).
- **0.4.1** — removed the comfort vignette (edge darkening on turns was bothersome); all its settings are gone.
- **0.4.0** — overall strength (`Strength`, F8) and stage ramp; adaptive effect limit; aggregated damage numbers; flash limiter; automatic color balance; break reminder; optional aim assist; Thunderstore package.
- **0.3.2** — camera offset no longer affects targeting/interaction searches; bloom is no longer force-enabled; particle systems with trigger/collision modules are left alone.
- **0.3.1** — startup splash/intro skip (waits for loading to finish).
- **0.3.0** — first public release.

### Build

```bash
dotnet build -c Release -p:RoR2Dir="C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2"
```

### License

MIT
