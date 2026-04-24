---
quick_id: 260423-tu6
description: migrar SpriteFont para FontStashSharp em toda UI
date: 2026-04-24
status: code-complete / awaiting-human-verify
---

# Quick Task 260423-tu6 — Summary

## Goal

Eliminate the bitmap-SpriteFont distortion that appeared when `DrawCenteredTitle` scaled a 12pt glyph at 1.35x on a `SamplerState.PointClamp` batch. Migrate every UI surface from MonoGame's MGCB-baked `SpriteFont` to FontStashSharp's `SpriteFontBase`, with glyphs rasterized at runtime from the existing TTFs. Same font, any native size, crisp.

## What changed

Seven atomic commits on `master` (fast-forwarded from the worktree branch):

| Hash | Message |
|------|---------|
| `df6f1d8` | chore(quick-260423-tu6): add FontStashSharp.MonoGame 1.5.5 package reference |
| `23f3d66` | feat(quick-260423-tu6): add FontService + FontRole for runtime TTF rendering |
| `7c74975` | feat(quick-260423-tu6): wire FontService + retype Font base field to SpriteFontBase |
| `4414d1f` | feat(quick-260423-tu6): migrate 10 src/UI widgets from SpriteFont to SpriteFontBase |
| `ff2cd6c` | feat(quick-260423-tu6): migrate 5 overlay scenes from SpriteFont to SpriteFontBase |
| `80981a4` | fix(quick-260423-tu6): drop scale=1.35f, request native 16pt bold for titles |
| `6fd1a6b` | chore(quick-260423-tu6): delete .spritefont files and clean Content.mgcb |

## Architecture

- **New service**: `src/Core/FontService.cs` — wraps `FontSystem` per family, exposes `GetFont(FontRole role, int size)` that returns a `SpriteFontBase` cached per (role, size).
- **Roles**: `FontRole.Body`, `FontRole.Bold`, `FontRole.Small` — backed by `NotoSerif-Regular.ttf`, `NotoSerif-Bold.ttf` (existing file), and `NotoSerif-Regular.ttf` at small sizes respectively.
- **TTF loading**: `File.ReadAllBytes` anchored at `AppContext.BaseDirectory` + `assets/Sprites/System/Font/`. Bypasses the MGCB content pipeline entirely (per research — that's the idiomatic FSS pattern).
- **ServiceContainer**: new `Fonts` slot, populated in `Game1.LoadContent` before the initial scene push.
- **GameplayScene base**: `Font` field retyped from `SpriteFont` to `SpriteFontBase`; subclasses and all call sites updated.

## Side-effects (free wins)

- **Portuguese accents now render**: `á`, `ã`, `ç`, `Baú` etc. were rendering as empty boxes on the old SpriteFont (`CharacterRegion 32..126` = ASCII). FSS lazy-rasterizes any glyph in the TTF on first draw. ChestScene title restored to `"Baú"`.
- **Title distortion fixed**: Shop and Chest titles now request a native 16pt bold font; no more scaling at draw time.
- **Font atlas garbage collection**: FSS keeps a GPU atlas with lazy glyph insertion; no first-frame hiccup observed at the MonoGame frame budget.

## Manual reconciliation (orchestrator-side)

The main working tree had ~300 lines of uncommitted shop/chest/UI polish (colors, button swaps, icon sizing, panel dimensions, pixel-art button PNG) that conflicted with the migration. Resolution applied during merge:

- 5 scenes (Dialogue/Inventory/Pause/Shop/Test): took migration version (my polish there was only a `DefaultFont → NotoSerif` string swap, which the migration superseded by removing SpriteFont loading altogether).
- `ShopPanel.cs`: hand-merged — kept all visual polish (PriceGold, RowText, RowHoverFill, VisibleRows=7, PanelHeight=396, coin-icon gold display, chest-style centered title with letter-spacing, faux-bold triple-draw, pixel-art action button, 20×20 round +/- buttons, 7 rows, etc.) + applied the `SpriteFontBase` type swap + added `using FontStashSharp;` + simplified `DrawCenteredTitle` to drop the now-unused `scale` parameter (FSS doesn't have the 9-arg `SpriteEffects` overload and native-size fonts make scaling pointless).
- `ChestScene.cs` + `UITheme.cs` + asset files: stash applied cleanly (no overlap with migration).

Build passes: `dotnet build` → 0 errors, 1 pre-existing CS8602 warning (unrelated).

## Human verification

Run `dotnet run` and check each of the 12 UI surfaces:

- [ ] **FarmScene HUD**: stamina/HP bars, gold counter, day/time, hotbar labels — text crisp at native sizes, no blur.
- [ ] **InventoryScene**: panel title, item names, equipment slot labels, tooltip text. Grab tooltip should show full item name and rarity.
- [ ] **ChestScene**: `"Baú"` title renders with acute accent (was boxes on SpriteFont). `"Bolsa"` and `"Baú"` section labels crisp. Tooltip with stats.
- [ ] **ShopScene**: `"Shop"` title crisp (primary regression target — old 1.35x scale bug). Item names, prices in gold, x1/x2 quantity labels in brown, Buy/Sell button text crisp.
- [ ] **DialogueScene**: typewriter effect text crisp, NPC portrait, "Press E to continue" pulse.
- [ ] **PauseScene**: "Paused" title, Resume/Fullscreen/Settings/Quit button labels.
- [ ] **LevelUpBanner**: "Level up!" banner appears crisp on level up (may need to fight in dungeon to trigger).
- [ ] **DeathBanner**: "You died" banner (die to boss or fall to test).
- [ ] **Toast**: purchase/sale toast in shop.
- [ ] **BossHealthBar**: boss fight HP label.
- [ ] **InteractionPrompt**: "Press E to..." prompt near NPCs / chests.
- [ ] **TestScene**: whatever text TestScene renders (if accessible from dev menu).

### First-run console should log

```
[FontService] Loading fonts from: C:\...\assets\Sprites\System\Font
```

### What to watch for

- Missing glyph boxes: if any character doesn't render, the TTF doesn't have it and FSS will show a placeholder. Check Portuguese-heavy text (diálogo, títulos).
- UI overflow: FSS kerning differs slightly from SpriteFont. Text that was fitting tight might now overflow container by 1-2px. Flag anything truncated.
- Performance: if any scene drops below 60fps on first entry, that's first-frame glyph atlas generation — subsequent entries should be smooth.

## Files touched

Code (merged into master):
- `stardew_medieval_v3.csproj` (NuGet reference)
- `Game1.cs`
- `src/Core/FontService.cs` (new)
- `src/Core/ServiceContainer.cs`
- `src/Core/GameplayScene.cs`
- `src/Scenes/ChestScene.cs`, `DialogueScene.cs`, `InventoryScene.cs`, `PauseScene.cs`, `ShopOverlayScene.cs`, `TestScene.cs`
- `src/UI/ShopPanel.cs`, `HUD.cs`, `HotbarRenderer.cs`, `DialogueBox.cs`, `Toast.cs`, `LevelUpBanner.cs`, `DeathBanner.cs`, `BossHealthBar.cs`, `InventoryGridRenderer.cs`, `ContainerGridRenderer.cs`, `InteractionPrompt.cs`

Deleted:
- `assets/DefaultFont.spritefont`
- `assets/NotoSerif.spritefont`
- `assets/NotoSerifBold.spritefont`
- `assets/NotoSerifSmall.spritefont`
- Their entries in `assets/Content.mgcb`
