> ⚠️ **Superseded — do not paste from this file.**
> The live pages were restyled on 2026-08-04 and this BBCode is the *pre-style* version.
> The live page is now the source of truth; pull its BBCode from the edit form's description
> field. Structure: [14-description-review.md](../../14-description-review.md). Look:
> [15-page-style.md](../../15-page-style.md). Mechanics: [13-nexus-page-standard.md](../../13-nexus-page-standard.md).

# Plant Peek — Nexus page source

**Nexus page:** [mod 120](https://www.nexusmods.com/moonlightpeaks/mods/120)

The description field is **SCEditor with a BBCode source**, so the block below is the literal
value that gets set. Structure per [14-description-review.md](../../14-description-review.md).

Description prose and Main features wording are **yours, unchanged**.

Fixes carried here: the config path is no longer in a code block mid-sentence (that was
breaking the line into three), and the entchen66 / Farming QoL shout out that was dropped on
upload is restored.

**One bullet removed from Main features** — the last one, *"Configurable — rebind the key,
switch to click or toggle… All of it in Mod Menu."* It named Mod Menu, and the new
Configuration section now says the same thing better. Say the word if you want it kept.

## Other fields

| Field | Change |
|---|---|
| Name | `Plant Peek` — no change |
| Category | User Interface — no change |
| Tags | User Interface, Quality of Life — no change |
| Short description | no change, the live one is good |

## Description source

```bbcode
[size=4][b]Description[/b][/size]
[color=#D4D4D8]Ever looked at a row of crops and wondered which ones you already watered, which are ready to pick, and which one has been sat there for a week doing nothing?

Plant Peek answers that without opening a menu. Point at a plant and hold [b]Left Alt[/b].

The catch with crops in Moonlight Peaks is that growth isn't a timer. Plants move through stages gated by requirements — water, the right season, being fed, being petted, being near the right neighbours, and sometimes a daily roll of the dice. A plant can sit unchanged indefinitely and look no different from one that's about to fruit.

So Plant Peek won't invent a countdown. It shows a day estimate only when waiting is genuinely all that's left, and when something else is blocking, it tells you what.

It stays out of the way. Nothing is drawn until you ask for it, and it draws in the game's own nameplate banner — the same one used for character names — so it looks like part of the game rather than something bolted on.

[b]Save-safe.[/b] It only reads. No Harmony patches, no data added to your save, nothing left behind if you uninstall.[/color]

[size=4][b]Main features[/b][/size]
[list]
[*][b]Hold a key, see the plant.[/b] Default Left Alt. Nothing on screen until you ask.
[*][b]Named after the crop[/b] — a vine reads [i]Grapes[/i], not [i]Grape Seeds[/i].
[*][b]Growth stage[/b] — which stage it's on and how many are left.
[*][b]Watering[/b] — whether today's water has been given, checked against the water type that crop actually needs, so magic-water crops aren't wrongly shown as done.
[*][b]Days to grown[/b] — but only when that can be answered honestly.
[*][b]What it's waiting on[/b] — water, season, neighbours, a gramophone, and the rest. Only what's still outstanding is listed, so a healthy plant doesn't read as blocked.
[*][b]Ready to harvest[/b] — visible from across the field, not only when you're stood on it.
[*][b]Regrowing crops[/b] — a picked vine reads [i]picked · fruits again in 2d[/i], instead of looking identical to a bare one.
[*][b]Trees[/b] — [i]chopped 60%[/i] while you're felling one, and [i]chopped[/i] for a stump.
[*][b]Fits in[/b] — drawn in the game's own nameplate banner, in the game's font and colours.
[/list]

[size=4][b]Requirements[/b][/size]
[list]
[*][b]BepInEx 5 (win_x64)[/b], version 5.4.23.5 or newer — the only thing this mod needs
[/list]
[color=#D4D4D8]PC/Steam only. The Switch and mobile builds can't load BepInEx.[/color]

[size=4][b]Installation[/b][/size]
[b]With Vortex[/b]
[color=#D4D4D8]Open the Files tab, click the Vortex button, and enable the mod. Done.[/color]

[b]Manually[/b]
[list=1]
[*]Install [b]BepInEx 5 (win_x64)[/b] into your Moonlight Peaks folder, if you do not have it already. The BepInEx folder sits beside Moonlight Peaks.exe.
[*]Launch the game once, then quit. This creates the BepInEx/plugins folder.
[*]Download Plant Peek from the Files tab and extract the archive over your Moonlight Peaks folder, so the file ends up at BepInEx/plugins/PlantPeek/PlantPeek.dll
[*]Launch the game.
[/list]
[color=#D4D4D8]To uninstall, delete BepInEx/plugins/PlantPeek. Nothing is written to your save, so there is nothing else to clean up.[/color]

[size=4][b]Configuration[/b][/size]
[color=#D4D4D8]Settings are written to BepInEx/config/com.dirtyredz.moonlightpeaks.plantpeek.cfg on first launch. The defaults are meant to be left alone.

Install [url=https://www.nexusmods.com/moonlightpeaks/mods/127][b]Mod Nook[/b][/url] and you can change them in game instead. Plant Peek shows up in it on its own, so you can rebind the peek key by pressing the key you want, pick the detail level off a list rather than typing the word, and recolour the banner with a picker. Nothing here needs it — it just makes this mod easier to live with.[/color]

[size=4][b]Compatibility[/b][/size]
[color=#D4D4D8]Plays nicely with [b]Extra Tooltip[/b] and [b]Farming QoL[/b] — those cover menus and icons, this covers pointing at the plant itself. Requirement checks call the game's own code, so mods that change growth rules, like [b]Endless Harvest[/b], are reflected correctly rather than contradicted.

Sits alongside [b]Last Swing[/b] happily: this one tells you how a crop is doing when you hover it, that one tells you how many swings a tree or rock has left while you chop.[/color]

[size=4][b]Shout outs[/b][/size]
[list]
[*][b]Little Chicken Game Company[/b], for a game whose code is clean enough to read and whose growth system turned out to be far more interesting than a timer.
[*]The [b]BepInEx[/b] and [b]HarmonyX[/b] teams, which every mod here stands on.
[*][b]entchen66's Extra Tooltip[/b] and [b]Elsiabeth's Farming QoL[/b], which cover the menu and icon side of this so thoroughly that the gap left over was worth building.
[*]The [b]Moonlight Peaks wiki modding guides[/b], which are genuinely good and saved a lot of guessing.
[*][b]My Mate[/b], for being my inspiration.
[/list]
```
