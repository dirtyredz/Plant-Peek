# Nexus Page Copy

Paste-ready copy for the mod page, plus the shot list.

**Category:** User Interface
**Tags:** Quality of Life, Utilities for Players
**Requirements:** BepInEx 5.4.23.5

---

## Summary (one line, shows in listings)

Hover a plant to see how far along it is, whether it needs water, and what it's waiting for.

---

# Paste-ready page copy

Nexus splits the page into named fields. Each heading below maps to one of them.

## Field: Description

Ever looked at a row of crops and wondered which ones you already watered, which are ready to
pick, and which one has been sat there for a week doing nothing?

**Plant Peek** answers that without opening a menu. Point at a plant and hold **Left Alt**.

The catch with crops in Moonlight Peaks is that growth isn't a timer. Plants move through
stages gated by requirements — water, the right season, being fed, being petted, being near the
right neighbours, and sometimes a daily roll of the dice. A plant can sit unchanged
indefinitely and look no different from one that's about to fruit.

So Plant Peek won't invent a countdown. It shows a day estimate only when waiting is genuinely
all that's left, and when something else is blocking, it tells you what.

It stays out of the way. Nothing is drawn until you ask for it, and it draws in the game's own
nameplate banner — the same one used for character names — so it looks like part of the game
rather than something bolted on.

**Save-safe.** It only reads. No Harmony patches, no data added to your save, nothing left
behind if you uninstall.

## Field: Installation instructions

1. Install **BepInEx 5 (win_x64)** into your Moonlight Peaks folder
2. Launch the game once, then quit — this creates the `BepInEx/plugins` folder
3. Download Plant Peek and extract the archive **over your Moonlight Peaks folder**, so the
   DLL lands in `BepInEx/plugins/PlantPeek/`
4. Launch the game

To uninstall, delete `BepInEx/plugins/PlantPeek`. Nothing is written to your save, so there is
nothing else to clean up.

## Field: Main features

- **Hold a key, see the plant.** Default Left Alt. Nothing on screen until you ask.
- **Named after the crop** — a vine reads *Grapes*, not *Grape Seeds*.
- **Growth stage** — which stage it's on and how many are left.
- **Watering** — whether today's water has been given, checked against the water type that crop
  actually needs, so magic-water crops aren't wrongly shown as done.
- **Days to grown** — but only when that can be answered honestly.
- **What it's waiting on** — water, season, neighbours, a gramophone, and the rest. Only what's
  still outstanding is listed, so a healthy plant doesn't read as blocked.
- **Ready to harvest** — visible from across the field, not only when you're stood on it.
- **Regrowing crops** — a picked vine reads *picked · fruits again in 2d*, instead of looking
  identical to a bare one.
- **Trees** — *chopped 60%* while you're felling one, and *chopped* for a stump.
- **Fits in** — drawn in the game's own nameplate banner, in the game's font and colours.
- **Configurable** — rebind the key, switch to click or toggle, change how much shows at rest,
  turn off the estimate, or recolour the banner. All of it in Mod Menu.

## Field: Requirements

- **BepInEx 5 (win_x64)** — required
- **Mod Menu** — optional, for changing settings in-game instead of editing a config file
- PC/Steam only. The Switch and mobile builds can't load BepInEx.

Plays nicely with **Extra Tooltip** and **Farming QoL** — those cover menus and icons, this
covers pointing at the plant itself. Requirement checks call the game's own code, so mods that
change growth rules, like **Endless Harvest**, are reflected correctly rather than contradicted.

## Field: Shout outs

- **Little Chicken Game Company**, for a game whose code is clean enough to read and whose
  growth system turned out to be far more interesting than a timer.
- **BepInEx** and **HarmonyX**, which every mod here stands on.
- **Elsiabeth**, for **Mod Menu** — settings in-game instead of a text file is the difference
  between a config being used and being ignored.
- **entchen66's Extra Tooltip** and **Elsiabeth's Farming QoL**, which cover the menu and icon
  side of this so thoroughly that the gap left over was worth building.
- The **Moonlight Peaks wiki modding guides**, which are genuinely good and saved a lot of
  guessing.

---

## Long-form description (reference)

Ever looked at a row of crops and wondered which ones you already watered, which are ready to
pick, and which one has been sat there for a week doing nothing?

**Plant Peek** answers that without opening a menu. Point at a plant and hold **Left Alt**.

### 🌱 What it tells you

- **What it is** — named after the crop, so a vine reads *Grapes*, not *Grape Seeds*
- **How far along** — which growth stage it is on, and how many are left
- **Whether it is watered** — for the water type that crop actually needs, so magic-water crops
  are not wrongly shown as done
- **Roughly how long** — days to fully grown, when that can honestly be answered
- **What it is waiting for** — water, the right season, neighbours, a gramophone, and so on
- **`ready to harvest`** — from across the field, not only when you are stood on it
- **`picked · fruits again in 2d`** — for crops that regrow, so a bare vine is not a mystery
- **`chopped 60%`** on a tree you have started felling

### 🌙 Quiet by default

Nothing is drawn until you ask. Hold the key and the panel appears; let go and your farm is
your farm again. Prefer it always on? Set `Detail` to `Standard` or `Full`.

It draws in the game's **own nameplate banner** — the same one used for character names — so
it looks like part of the game rather than something bolted on.

### 🌿 Honest about growth

Growth in Moonlight Peaks is not a timer. Plants advance through stages gated by requirements:
water, season, being fed, being petted, being near the right neighbours, and in some cases a
daily roll of the dice. A plant can sit unchanged indefinitely.

So Plant Peek **will not invent a countdown**. The day estimate appears only when waiting is
genuinely all that is left. When something else is blocking, it says what.

### 💾 Save-safe

**Read-only.** No config written into your save, no Harmony patches, nothing added to your save
file. Uninstall at any time and the game will not notice.

That claim was worth checking: several of the game's own "is this requirement satisfied?"
methods quietly create records when asked. Plant Peek never calls those — it reads the data
itself, or says nothing.

### ⚙️ Configuration

Every setting is available in-game through **Mod Menu**, or in
`BepInEx/config/com.dirtyredz.moonlightpeaks.plantpeek.cfg`.

Rebind the key, switch to click or toggle, change how much shows at rest, turn off the day
estimate, or recolour the banner.

### 📦 Installation

1. Install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) into your Moonlight Peaks folder
2. Launch the game once, then quit
3. Extract this archive over your Moonlight Peaks folder
4. Launch

### 🤝 Compatibility

Works alongside **Extra Tooltip** and **Farming QoL** — those cover menus and icons, this
covers pointing at the plant itself. Requirement checks call the game's own code, so mods that
change growth rules (**Endless Harvest**, for example) are reflected correctly.

---

## Screenshot shot list

Capture on the **current build**, at `Detail = Hidden` with the panel expanded — that is what a
new install does.

| # | Shot | Must show |
|---|---|---|
| 1 | `01-hover-crop.png` | A mid-growth crop: name, stage, days left, watered state |
| 2 | `02-waiting-on.png` | A plant with something outstanding — the "waiting on" line in gold |
| 3 | `03-ready-harvest.png` | A crop reading `ready to harvest` |
| 4 | `04-regrow.png` | A picked vine reading `picked · fruits again in Nd` |
| 5 | `05-tree.png` | A part-chopped tree showing `chopped N%` |
| 6 | `06-quiet.png` | The same view with nothing held, showing how quiet it is at rest |

Shot 6 matters more than it looks — "quiet by default" is the pitch, and it is hard to convey
in words.
