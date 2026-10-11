# RoR2 Comfort

**RU** · [EN below](#english)

Мод для **Risk of Rain 2** против укачивания (motion sickness): убирает или ослабляет то, что чаще всего вызывает тошноту, — тряску камеры, отдачу, резкие скачки FOV, рывки мыши, вспышки на экране, лишние частицы и «вырвиглазные» цвета. Каждая настройка меняется прямо в игре.

> Сделан человеком, которого сильно укачивает в поздней игре. Проверен на одном компьютере и в основном на ранних стадиях. Мод не меняет геймплей и баланс: только камеру и картинку на вашем экране (в сетевой игре работает локально, хосту ничего не нужно).

## Что делает

| Группа (клавиша) | Что делает |
|---|---|
| **Камера** (F2) | Камера дальше и выше от персонажа (`ExtraDistance`, `ExtraHeight`) |
| **Тряска и отдача** (F3) | Масштаб тряски экрана и отдачи при стрельбе (`ShakeScale`, `RecoilScale`) |
| **FOV** (F4) | Убирает расширение FOV при спринте и от способностей/предметов (`SprintFovScale`, `LimitAbilityFov`, `AbilityFovMaxIncrease`); приближение (прицелы) не трогает |
| **Мышь** (F5) | Сглаживание поворота (`LookSmoothing`, сохраняет общий угол) и ограничитель резких рывков (`MaxLookPerFrame`) |
| **Картинка** (F6) | Числа урона, красные вспышки при уроне, количество частиц эффектов, насыщенность/контраст/яркость, bloom, сила глубинного тумана |
| **Мастер** (F7) | Весь мод вкл/выкл. Выключено = обычная игра |

Дополнительно: скрывает текст-бейдж «НОВИНКА/NEW» в меню (в русской локализации он налезает на надписи) — `HideNewBadges`.

### Индикатор

При переключении вся картинка на долю секунды вспыхивает **зелёным (вкл)** или **красным (выкл)**, а сверху экрана висит надпись `COMFORT ON` / `COMFORT OFF`. Так сразу видно, работает ли мод, и можно быстро сравнивать «с модом» и «без». Надпись отключается (`ShowIndicator`).

## Установка

Нужны:
- **BepInEx 5** для RoR2 (пакет [RoR2BepInExPack](https://thunderstore.io/package/RiskofThunder/RoR2BepInExPack/));
- [Risk of Options](https://thunderstore.io/package/Rune580/Risk_Of_Options/) — по желанию, чтобы настройки были в меню игры (иначе правьте `BepInEx/config/local.RoR2Comfort.cfg`).

Затем положите папку `RoR2Comfort` из архива релиза в `BepInEx/plugins/` (если у вас менеджер модов — в `plugins` нужного профиля).

В игре: *Настройки → вкладка RoR2 Comfort*.

## Настройки по умолчанию

| Параметр | Значение | Смысл |
|---|---|---|
| `ExtraDistance` / `ExtraHeight` | 4 / 1.5 | камера дальше / выше |
| `ShakeScale` / `RecoilScale` | 0.1 / 0.2 | 0 = нет, 1 = как в игре |
| `SprintFovScale` | 0 | 0 = FOV не расширяется при спринте |
| `LookSmoothing` | 0.03 с | 0 = выкл |
| `HideDamageNumbers` | вкл | |
| `ScreenDamageScale` | 0.3 | красная вспышка при уроне |
| `ParticleCountScale` | 0.5 | количество частиц эффектов |
| `Saturation` / `Contrast` / `Exposure` | −25 / 0 / +0.5 | цветокоррекция |
| `LimitBloom` / `BloomIntensity` | вкл / 0.15 | |
| `FogScale` | 0.65 | 1 = как в игре; туман ещё и подсвечивает даль, при очень низких значениях сцена темнеет — поднимайте `Exposure` |

Значения подобраны под одного человека; подстраивайте под себя.

## Известные ограничения

- Другие моды, которые меняют камеру/FOV/постобработку, могут конфликтовать.
- Частицы уменьшаются в количестве, а не отключаются полностью, чтобы не терялись предупреждения о атаках врагов.
- Цвет на поздних стадиях (например, «всё красное») подстраивается только общей цветокоррекцией и туманом; освещение сцены не переписывается.
- Проверено на сборке игры 1.5.0 (Steam build 25475991). После обновлений игры может потребоваться пересборка.
- Если вас укачивает, не полагайтесь только на мод: короткие сессии и перерывы тоже помогают.

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

A **Risk of Rain 2** mod against motion sickness: it removes or tones down the things that most often cause nausea — camera shake, recoil, FOV bursts, mouse flicks, screen flashes, excess particles and eye-searing colors. Every setting is adjustable in-game.

> Made by a player who gets seriously motion-sick in the late game. Tested on one machine, mostly in early stages. It does not change gameplay or balance, only your own camera and picture (works locally in multiplayer, the host needs nothing).

### Features

| Group (key) | What it does |
|---|---|
| **Camera** (F2) | Camera farther and higher (`ExtraDistance`, `ExtraHeight`) |
| **Shake & recoil** (F3) | Scale screen shake and firing recoil (`ShakeScale`, `RecoilScale`) |
| **FOV** (F4) | Removes FOV widening from sprinting and abilities/items (`SprintFovScale`, `LimitAbilityFov`, `AbilityFovMaxIncrease`); zooming in (scopes) is untouched |
| **Mouse** (F5) | Look smoothing (conserves total turned angle) and a flick limiter (`LookSmoothing`, `MaxLookPerFrame`) |
| **Visuals** (F6) | Damage numbers, hurt flashes, effect particle counts, saturation/contrast/exposure, bloom, depth fog strength |
| **Master** (F7) | Whole mod on/off. Off = vanilla |

Also hides the "NEW" badge text in menus (it overlaps labels in some localizations) — `HideNewBadges`.

**Indicator:** toggling flashes the screen **green (on)** or **red (off)** and a `COMFORT ON` / `COMFORT OFF` label stays at the top of the screen (`ShowIndicator`).

### Install

Requires **BepInEx 5** ([RoR2BepInExPack](https://thunderstore.io/package/RiskofThunder/RoR2BepInExPack/)); [Risk of Options](https://thunderstore.io/package/Rune580/Risk_Of_Options/) is optional but gives you an in-game settings tab (otherwise edit `BepInEx/config/local.RoR2Comfort.cfg`). Put the `RoR2Comfort` folder from the release zip into `BepInEx/plugins/`.

### Notes

- Defaults are tuned for one person; adjust to taste. `FogScale` below 1 darkens the scene because the game's fog also lights the distance — raise `Exposure` to compensate.
- Built and checked against game version 1.5.0 (Steam build 25475991).
- Other mods touching camera / FOV / post-processing may conflict.

### Build

```bash
dotnet build -c Release -p:RoR2Dir="C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2"
```

### License

MIT
