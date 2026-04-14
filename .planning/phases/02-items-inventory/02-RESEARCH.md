# Phase 2: Items & Inventory - Research

**Researched:** 2026-04-10
**Domain:** Inventory UI, Item Management, Hotbar, Equipment, Item Drops with Magnetism (MonoGame/C#)
**Confidence:** HIGH

## Summary

Phase 2 builds a complete inventory system on top of Phase 1's data foundation (`ItemDefinition`, `ItemRegistry`, `ItemStack`, `GameState` placeholders). The existing data layer is solid -- 45 items in `items.json`, typed enums (`ItemType`, `Rarity`), and serialization-ready `ItemStack`. The work is primarily: (1) an `InventoryManager` class for slot-based item management, (2) UI rendering for inventory grid, hotbar, and equipment tabs using the existing UI sprite kit, (3) an `ItemDropEntity` in world-space with bounce spawn + magnetic pickup, (4) farming integration changing harvest from console-log-only to item-drop-to-inventory flow, and (5) save/load integration through the existing `GameState` fields.

The UI sprite kit (`UI_Slot_Normal.png`, `UI_Slot_Selected.png`, `UI_Panel_SlotPane.png`) and item icon spritesheet (`Pickup_Items.png`) are already available. The `HUD.png` preview shows the target hotbar aesthetic (numbered slots at screen bottom with item icons). The `SceneManager.Push()` pattern enables inventory as an overlay scene that pauses gameplay underneath.

**Primary recommendation:** Build InventoryManager as a pure data class (no rendering), then build InventoryScene (overlay via SceneManager.Push) and HotbarRenderer (always-visible HUD component) as separate UI layers consuming InventoryManager state. ItemDropEntity extends Entity base class for world-space drops with physics.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Grid estilo Stardew Valley com 20 slots em caixinhas. Usar sprite `UI_Slot_Normal.png` para slots normais e `UI_Slot_Selected.png` para slot selecionado. Abre com tecla I.
- **D-02:** Click para selecionar item, click em outro slot para mover. Drag-and-drop opcional (Claude's discretion se vale a complexidade).
- **D-03:** Cores de raridade nos itens seguem o sistema existente (Common/Uncommon/Rare) com borda ou glow colorido no slot.
- **D-04:** Hotbar visual fixa na parte inferior da tela, estilo Stardew Valley (referencia: HUD.png do kit). Slots numerados 1-8, acessiveis por number keys. Slot ativo tem destaque visual (UI_Slot_Selected).
- **D-05:** Hotbar faz parte do HUD permanente (sempre visivel durante gameplay), nao do inventario overlay.
- **D-06:** Equipment slots como aba separada do inventario (nao misturado com grid de itens). Abrir inventario mostra grid; trocar de aba mostra equipment.
- **D-07:** Layout Tibia-style com formato de "homenzinho" -- slots fixos posicionados em torno de uma silhueta: arma (mao), armadura (torso). Expandir para mais slots em fases futuras se necessario.
- **D-08:** Itens dropados no chao usam o sprite real do item (nao saquinho generico). Ficam no chao ate o player se aproximar.
- **D-09:** Magnetismo com distancia inicial pequena (~48-64px). Velocidade de atracao e animacao estilo Stardew Valley (item acelera em direcao ao player com curva suave). Distancia de magnetismo pode ser aumentada em fases futuras (upgrades).
- **D-10:** Item drop tem um pequeno "bounce" ao spawnar no chao (feedback visual de que algo dropou).
- **D-11:** Colheita usa foice (ou ferramenta equivalente). Player usa ferramenta no crop maduro -> crop some do campo -> item dropa no chao no local da colheita -> magnetismo puxa para o player -> vai para inventario.
- **D-12:** Fix visual do crop system (FARM-01): corrigir posicao do player ao arar/semear/regar. FARM-02: sprites adequados para colheita (substituir overlays coloridos).
- **D-13:** Se inventario estiver cheio quando item e coletado, item permanece no chao (nao desaparece). Feedback visual/sonoro de "inventario cheio" opcional.

### Claude's Discretion
- Implementacao interna do drag-and-drop vs click-to-move no inventario
- Layout exato dos elementos de UI (margens, espacamento)
- Animacao exata do magnetismo (curva de aceleracao)
- Como organizar o codigo internamente (InventoryManager, ItemDropEntity, etc.)
- Ordem das abas no inventario (grid primeiro ou equipment primeiro)

### Deferred Ideas (OUT OF SCOPE)
- Aumento de distancia de magnetismo via upgrades (futuro, progression system)
- Mais equipment slots alem de arma/armadura (anel, amuleto, etc.)
- Tooltip detalhado ao passar mouse sobre item (v2 polish)
- Sort/filter no inventario
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| INV-01 | Inventory grid com 20 slots, suporte a stacking de consumiveis | InventoryManager with ItemStack[] array, stacking logic via ItemDefinition.StackLimit |
| INV-02 | Hotbar com 8 slots acessiveis por number keys (1-8) | HotbarRenderer as HUD component, reads from InventoryManager hotbar slots |
| INV-03 | Equipment slots separados (arma, armadura) que afetam combat stats | EquipmentData in InventoryManager, stat application via ItemDefinition.Stats dictionary |
| INV-04 | Sistema de raridade de itens com cores distintas e stat multipliers | Already exists: Rarity enum (Common/Uncommon/Rare), color map for UI tinting |
| INV-05 | Itens dropados no chao com magnetismo | ItemDropEntity extends Entity, magnetic attraction physics in Update() |
| FARM-01 | Posicao correta do player ao arar, semear e regar | Fix in GridManager/ToolController -- use GetFacingTile() instead of GetTilePosition() |
| FARM-02 | Sprites adequados para colheita (substituir overlays coloridos) | Use final stage from crop growth spritesheet or item icon from 7_Pickup_Items spritesheet |
| FARM-03 | Farming integrado ao novo sistema de inventario | TryHarvest() spawns ItemDropEntity, magnetism pulls to player, InventoryManager.TryAdd() |
| HUD-02 | Inventory UI abrivel (tecla I) mostrando grid + equipment slots | InventoryScene as overlay via SceneManager.Push(), two tabs (grid + equipment) |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| MonoGame.Framework.DesktopGL | 3.8.* | Game engine, rendering, input | Already in project [VERIFIED: csproj] |
| System.Text.Json | built-in | Serialization for save data | Already used by SaveManager [VERIFIED: codebase] |

### Supporting
No new libraries needed. This phase uses only existing MonoGame APIs:
- `SpriteBatch` for UI rendering
- `Texture2D.FromStream()` for loading sprite PNGs
- `Rectangle` for sprite source regions and slot hit testing
- `MouseState` via `Mouse.GetState()` for inventory click interactions
- `SpriteFont` for slot quantity text

**No new NuGet packages required.** [VERIFIED: codebase analysis]

## Architecture Patterns

### Recommended Project Structure
```
stardew_medieval_v3/
├── src/Inventory/
│   ├── InventoryManager.cs    # Pure data: 20 slots + 8 hotbar + equipment
│   └── EquipmentData.cs       # Weapon/Armor slot state + stat calculation
├── src/Entities/
│   └── ItemDropEntity.cs      # World-space dropped item with bounce + magnetism
├── src/Scenes/
│   └── InventoryScene.cs      # Overlay scene: grid tab + equipment tab
├── src/UI/
│   ├── HUD.cs                 # (modify) Add hotbar rendering
│   ├── HotbarRenderer.cs      # Renders 8 slots at screen bottom
│   ├── InventoryGridRenderer.cs  # Renders 20-slot grid with items
│   └── EquipmentRenderer.cs   # Renders equipment slots (Tibia-style)
└── src/Data/
    ├── items.json              # (modify) Add weapon/armor items for testing
    └── SpriteAtlas.cs          # Maps SpriteId -> source Rectangle in spritesheet
```

### Pattern 1: src/Data/View Separation for Inventory
**What:** `InventoryManager` owns all item slot state (add, remove, move, stack, equip). UI classes only read from it and call its methods on user input.
**When to use:** Always -- this separation makes save/load trivial and testing possible without rendering.
**Example:**
```csharp
// InventoryManager.cs -- pure data, no Texture2D references
public class InventoryManager
{
    public const int SlotCount = 20;
    public const int HotbarSize = 8;
    
    private readonly ItemStack?[] _slots = new ItemStack?[SlotCount];
    private int _activeHotbarIndex = 0;
    
    // Equipment
    public string? WeaponId { get; private set; }
    public string? ArmorId { get; private set; }
    
    public event Action? OnInventoryChanged;
    
    public bool TryAdd(string itemId, int quantity = 1) { ... }
    public ItemStack? GetSlot(int index) => _slots[index];
    public void MoveItem(int fromSlot, int toSlot) { ... }
    public void SetActiveHotbar(int index) { ... }
    public bool TryEquip(int slotIndex) { ... }
}
```
[ASSUMED: pattern recommendation based on standard game architecture]

### Pattern 2: Overlay Scene for Inventory UI
**What:** Inventory is a `Scene` pushed onto `SceneManager` stack. While open, it receives Update (handles clicks, tab switching, close). FarmScene underneath continues to Draw but does NOT Update (game paused while inventory open).
**When to use:** Per D-01 (opens with I key) and existing SceneManager.Push() pattern.
**Example:**
```csharp
// In FarmScene.Update():
if (input.IsKeyPressed(Keys.I))
{
    Services.SceneManager.Push(new InventoryScene(Services, _inventoryManager));
    return; // stop processing FarmScene input
}
```
**Key insight:** SceneManager already does exactly this -- `Push()` adds overlay, only top scene gets Update, all scenes get Draw (bottom-to-top). The fade transition is already built in. [VERIFIED: SceneManager.cs]

### Pattern 3: ItemDropEntity with Physics
**What:** Dropped items are Entity subclasses in world-space with two behavior phases: (1) bounce spawn animation (small arc upward then settle), (2) idle on ground until player approaches magnetism range, then accelerate toward player.
**When to use:** Every time a crop is harvested or (future) enemy drops loot.
**Example:**
```csharp
public class ItemDropEntity : Entity
{
    private readonly string _itemId;
    private readonly int _quantity;
    private float _bounceTimer;
    private bool _isBouncing = true;
    private const float MagnetRange = 56f; // pixels (~3.5 tiles)
    private const float PickupRange = 8f;  // pixels (touching)
    private const float MaxMagnetSpeed = 200f;
    
    public override void Update(float deltaTime)
    {
        if (_isBouncing) { UpdateBounce(deltaTime); return; }
        // Check distance to player, apply magnetic acceleration
    }
}
```
[ASSUMED: physics values tunable during implementation]

### Pattern 4: SpriteAtlas for Item Icons
**What:** A mapping from `ItemDefinition.SpriteId` to a source `Rectangle` within the `Pickup_Items.png` spritesheet. Items render their icon by looking up the atlas.
**When to use:** Anywhere an item icon needs rendering (inventory slots, hotbar, item drops, equipment screen).
**Key insight:** The spritesheet is 16x16 per icon, arranged in rows. A simple JSON or hardcoded dictionary maps crop names to grid positions. Crop growth sheets can also provide the "ripe" frame as a fallback icon.
[VERIFIED: Pickup_Items.png exists with ~70 icons in grid layout]

### Anti-Patterns to Avoid
- **Mixing UI state with data state:** Do NOT store "selected slot" or "active tab" in InventoryManager. Those belong in the Scene/Renderer.
- **Drawing inventory in FarmScene.Draw():** Use InventoryScene overlay instead. FarmScene should not know about inventory UI.
- **Coupling ToolController directly to InventoryManager:** ToolController should receive "active item" from hotbar, not directly manage inventory. A thin adapter or property access is fine.
- **Hardcoding sprite positions:** Use a lookup table (SpriteAtlas) so adding new items only requires adding entries, not code changes.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Slot hit testing | Custom math for each slot | `Rectangle.Contains(mousePoint)` for each slot rect | MonoGame Rectangle already handles this perfectly |
| Item stacking merge logic | Ad-hoc quantity math | Centralized `InventoryManager.TryAdd()` with StackLimit check | Edge cases with partial stacks are tricky to get right in multiple places |
| Sprite region lookup | Inline Rectangle constants | `SpriteAtlas` dictionary from SpriteId to Rectangle | Single source of truth, easy to extend |
| Fade transitions | Manual alpha for inventory open/close | `SceneManager.Push()/Pop()` | Already built with fade-to-black in Phase 1 |

**Key insight:** The Phase 1 architecture (SceneManager, Entity, ServiceContainer) was specifically designed to support this phase. Use the existing patterns rather than building parallel systems.

## Common Pitfalls

### Pitfall 1: Mouse Coordinates in Scaled Window
**What goes wrong:** Inventory uses mouse clicks for slot selection, but MonoGame window may be scaled (Camera.Zoom = 3f). Mouse coordinates in screen space vs world space get confused.
**Why it happens:** FarmScene renders in camera-transformed world space, but inventory UI renders in screen space. If you use `Mouse.GetState()` directly for screen-space UI, it works. But if camera zoom affects the viewport differently, coordinates can be off.
**How to avoid:** Inventory UI is always in screen space (no camera transform). Use `Mouse.GetState().Position` directly for hit testing against screen-space slot rectangles. The camera transform is only for world-space rendering.
**Warning signs:** Clicks landing on wrong slots, especially at window edges.

### Pitfall 2: Stack Overflow on Partial Adds
**What goes wrong:** Player picks up 50 carrots but only has room for 30 more in an existing stack. The remaining 20 should go to a new slot or stay on the ground.
**Why it happens:** `TryAdd()` only handles the simple case (empty slot or existing stack with room).
**How to avoid:** Implement `TryAdd()` with a two-pass approach: (1) fill existing stacks of the same item, (2) place remainder in first empty slot, (3) return remaining quantity if inventory is full. The caller (magnetism pickup) checks return value and keeps the ItemDrop alive if quantity > 0.
**Warning signs:** Items disappearing when inventory is nearly full, or items failing to pick up when there is room in existing stacks.

### Pitfall 3: Hotbar Slot Desync with Inventory
**What goes wrong:** Hotbar shows items that were moved or consumed from inventory, or inventory shows items that were used from hotbar.
**Why it happens:** Hotbar slots are a VIEW into the first 8 inventory slots (or a separate mapping). If they're separate data structures, they can desync.
**How to avoid:** Hotbar IS inventory slots 0-7. The hotbar renderer reads `InventoryManager.GetSlot(0..7)`. No separate hotbar data structure. `GameState.HotbarSlots` maps to ItemIds in those first 8 slots.
**Warning signs:** Placing item in inventory slot 3 not showing up in hotbar slot 4.

### Pitfall 4: Item Drop Z-Order
**What goes wrong:** Items drawn on ground appear on top of the player or under the terrain.
**Why it happens:** SpriteBatch draw order in MonoGame is call order (deferred mode). Items must be drawn at the right point in the render pipeline.
**How to avoid:** Draw item drops AFTER tiles but BEFORE the player if they're behind the player (Y-sort), or after the player if in front. For simplicity in Phase 2, draw all drops before the player (they're on the ground, player walks over them). Y-sorting is a Phase 3+ concern when enemies exist.
**Warning signs:** Visual "popping" as items appear above/below player unexpectedly.

### Pitfall 5: SceneManager Fade Delay for Inventory
**What goes wrong:** Opening inventory has a 0.8s fade-to-black delay (0.4s out + 0.4s in) which feels sluggish for a UI that should be instant.
**Why it happens:** SceneManager.Push() always uses fade transition.
**How to avoid:** Add a `PushImmediate()` variant or use the existing `PushImmediate()` method (already exists in SceneManager!) for inventory overlay. Or add a fast-fade option (0.1s). Inventory should open/close instantly or near-instantly.
**Warning signs:** Player pressing I and waiting almost a second before seeing inventory.

### Pitfall 6: SaveManager Not Persisting Inventory
**What goes wrong:** Player closes game, reopens, inventory is empty.
**Why it happens:** `FarmScene.OnDayAdvanced()` creates GameState but doesn't populate src/Inventory/HotbarSlots/WeaponId/ArmorId fields.
**How to avoid:** Update `OnDayAdvanced()` save logic to serialize InventoryManager state into GameState fields. Also update `LoadContent()` to restore from save.
**Warning signs:** Save file shows empty Inventory array.

## Code Examples

### Item Stacking Logic
```csharp
// Source: Standard inventory pattern for stackable items [ASSUMED]
public int TryAdd(string itemId, int quantity)
{
    var def = ItemRegistry.Get(itemId);
    if (def == null) return quantity; // unknown item, reject

    // Pass 1: Fill existing stacks
    for (int i = 0; i < SlotCount; i++)
    {
        if (quantity <= 0) break;
        if (_slots[i]?.ItemId == itemId)
        {
            int room = def.StackLimit - _slots[i]!.Quantity;
            int toAdd = Math.Min(quantity, room);
            _slots[i]!.Quantity += toAdd;
            quantity -= toAdd;
        }
    }

    // Pass 2: Place in empty slots
    for (int i = 0; i < SlotCount; i++)
    {
        if (quantity <= 0) break;
        if (_slots[i] == null)
        {
            int toAdd = Math.Min(quantity, def.StackLimit);
            _slots[i] = new ItemStack { ItemId = itemId, Quantity = toAdd };
            quantity -= toAdd;
        }
    }

    OnInventoryChanged?.Invoke();
    return quantity; // 0 = all added, >0 = overflow
}
```

### Magnetic Pickup Physics
```csharp
// Source: Stardew Valley-style magnetism pattern [ASSUMED]
private void UpdateMagnetism(float deltaTime, Vector2 playerPos)
{
    float dist = Vector2.Distance(Position, playerPos);
    
    if (dist <= PickupRange)
    {
        // Collected! Caller removes from scene
        IsCollected = true;
        return;
    }
    
    if (dist <= MagnetRange)
    {
        // Accelerate toward player with ease-in curve
        float t = 1f - (dist / MagnetRange); // 0 at edge, 1 at center
        float speed = MathHelper.Lerp(40f, MaxMagnetSpeed, t * t); // quadratic ease-in
        
        Vector2 direction = Vector2.Normalize(playerPos - Position);
        Position += direction * speed * deltaTime;
    }
}
```

### Bounce Spawn Animation
```csharp
// Source: Common drop animation pattern [ASSUMED]
private Vector2 _startPos;
private float _bounceTime;
private const float BounceDuration = 0.4f;
private const float BounceHeight = 12f; // pixels up
private Vector2 _bounceOffset; // random lateral offset

private void UpdateBounce(float deltaTime)
{
    _bounceTime += deltaTime;
    float t = MathHelper.Clamp(_bounceTime / BounceDuration, 0f, 1f);
    
    // Parabolic arc: goes up then down
    float yOffset = -BounceHeight * 4f * t * (1f - t); // peaks at t=0.5
    float xOffset = _bounceOffset.X * t;
    
    Position = _startPos + new Vector2(xOffset, yOffset + _bounceOffset.Y * t);
    
    if (t >= 1f) _isBouncing = false;
}
```

### Inventory Grid Rendering
```csharp
// Source: MonoGame standard UI rendering [ASSUMED]
private void DrawSlotGrid(SpriteBatch sb, int startX, int startY)
{
    const int cols = 5;
    const int slotSize = 36; // slot sprite size in pixels
    const int padding = 4;
    
    for (int i = 0; i < InventoryManager.SlotCount; i++)
    {
        int col = i % cols;
        int row = i / cols;
        int x = startX + col * (slotSize + padding);
        int y = startY + row * (slotSize + padding);
        
        // Slot background
        var slotTex = (i == _selectedSlot) ? _slotSelected : _slotNormal;
        sb.Draw(slotTex, new Rectangle(x, y, slotSize, slotSize), Color.White);
        
        // Item icon
        var stack = _inventory.GetSlot(i);
        if (stack != null)
        {
            var srcRect = _spriteAtlas.GetRect(stack.ItemId);
            sb.Draw(_itemSheet, new Rectangle(x + 2, y + 2, slotSize - 4, slotSize - 4), 
                    srcRect, Color.White);
            
            // Quantity text
            if (stack.Quantity > 1)
                sb.DrawString(_font, stack.Quantity.ToString(), 
                    new Vector2(x + slotSize - 12, y + slotSize - 14), Color.White);
        }
        
        // Rarity border tint
        if (stack != null)
        {
            var def = ItemRegistry.Get(stack.ItemId);
            Color tint = def?.Rarity switch
            {
                Rarity.Uncommon => Color.LimeGreen * 0.5f,
                Rarity.Rare => Color.Gold * 0.5f,
                _ => Color.Transparent
            };
            if (tint != Color.Transparent)
                sb.Draw(_pixel, new Rectangle(x, y, slotSize, slotSize), tint);
        }
    }
}
```

### Equipment Stat Application
```csharp
// Source: Standard RPG stat pattern [ASSUMED]
public (float attack, float defense) GetEquipmentStats()
{
    float attack = 0f, defense = 0f;
    
    if (WeaponId != null)
    {
        var weapon = ItemRegistry.Get(WeaponId);
        if (weapon?.Stats.TryGetValue("damage", out float dmg) == true)
            attack += dmg;
    }
    
    if (ArmorId != null)
    {
        var armor = ItemRegistry.Get(ArmorId);
        if (armor?.Stats.TryGetValue("defense", out float def) == true)
            defense += def;
    }
    
    return (attack, defense);
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Console.WriteLine harvest log only | Item drop -> magnetism -> inventory | This phase | Core gameplay loop finally produces tangible results |
| H/G/R/F key tool selection | Hotbar number keys (1-8) with equipped item from inventory | This phase | Tool selection via inventory, not hardcoded keys |
| ToolType enum hardcoded | Active hotbar slot determines current tool/weapon | This phase | Extensible -- any item can go in hotbar |

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Hotbar slots = inventory slots 0-7 (shared, not separate) | Architecture Patterns | Medium -- if separate, need sync logic and different save format |
| A2 | Magnetism range 48-64px works well at Zoom=3f with 16px tiles | Code Examples | Low -- easily tunable constant |
| A3 | Click-to-move (no drag-and-drop) is sufficient for MVP | Architecture Patterns | Low -- drag can be added later, click works |
| A4 | `Pickup_Items.png` icons map 1:1 to items.json SpriteIds | Architecture Patterns | Medium -- may need manual sprite atlas mapping |
| A5 | Equipment stat changes are computed on-demand, not cached | Code Examples | Low -- only 2 equipment slots, trivial computation |
| A6 | PushImmediate() works for inventory (no fade desired) | Common Pitfalls | Low -- method exists, may need slight tweak for semi-transparent background |
| A7 | Farming fix (FARM-01) is about using GetFacingTile() vs GetTilePosition() | Phase Requirements | Medium -- actual bug may be different, needs investigation during execution |

## Open Questions

1. **Sprite Atlas Mapping**
   - What we know: `Pickup_Items.png` contains ~70 icons in a grid. `SpriteId` field exists in each `ItemDefinition`.
   - What's unclear: Exact grid positions of each icon within the spritesheet. Some crops (cabbage, carrot, etc.) may not have matching icons in this sheet.
   - Recommendation: During implementation, visually inspect the spritesheet and build a hardcoded mapping. For crops without dedicated icons, use the final growth stage frame from the crop spritesheet.

2. **FARM-01: Exact Nature of Position Bug**
   - What we know: Crops get planted at the player's standing tile (`GetTilePosition()`), not the tile they're facing.
   - What's unclear: Whether the fix is as simple as changing to `GetFacingTile()` or if there's a deeper issue with tile alignment.
   - Recommendation: Investigate during execution wave, should be a 1-line fix in `ToolController.DoAction()`.

3. **FARM-02: What "Sprites Adequados" Means**
   - What we know: Currently crops render colored overlays on the grid. Real crop growth spritesheets exist (`Cabbage_Growth_Stages_16x16.png` etc.).
   - What's unclear: Whether the issue is in `GridManager.DrawCrops()` not using the correct sprite frames, or the overlay rendering approach itself.
   - Recommendation: Inspect `GridManager.DrawCrops()` during execution to identify specific rendering fix needed.

4. **Mouse Input for Inventory**
   - What we know: `InputManager` currently only handles keyboard. Inventory needs mouse clicks for slot interaction.
   - What's unclear: Whether to extend `InputManager` with mouse state or handle mouse directly in `InventoryScene`.
   - Recommendation: Add `MouseState` tracking to `InputManager` (IsLeftClickPressed, MousePosition) to keep input centralized. Follow existing pattern.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None currently -- no test project exists |
| Config file | None |
| Quick run command | `dotnet test` (once test project created) |
| Full suite command | `dotnet test --verbosity normal` |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| INV-01 | 20-slot inventory with stacking | unit | `dotnet test --filter InventoryManager` | No -- Wave 0 |
| INV-02 | Hotbar 1-8 key selection | manual-only | Visual verification in-game | N/A |
| INV-03 | Equipment slots affect stats | unit | `dotnet test --filter Equipment` | No -- Wave 0 |
| INV-04 | Rarity colors on items | manual-only | Visual verification in-game | N/A |
| INV-05 | Item drop magnetism | manual-only | Visual verification in-game | N/A |
| FARM-01 | Player position fix | manual-only | Visual verification in-game | N/A |
| FARM-02 | Crop sprites fix | manual-only | Visual verification in-game | N/A |
| FARM-03 | Harvest -> inventory flow | manual-only | Harvest crop, verify in inventory | N/A |
| HUD-02 | Inventory opens with I key | manual-only | Press I, verify overlay | N/A |

### Sampling Rate
- **Per task commit:** `dotnet build` (compilation check)
- **Per wave merge:** `dotnet build && dotnet run` (manual smoke test)
- **Phase gate:** Full build + manual playtest of all success criteria

### Wave 0 Gaps
- [ ] Test project setup (`dotnet new xunit -n StardewMedieval.Tests`) -- if unit tests desired
- [ ] InventoryManager is pure data class -- unit testable without MonoGame
- Note: Most requirements are visual/interactive and require manual testing. Unit tests only practical for InventoryManager data operations.

## Security Domain

> Not applicable. Single-player offline game with local save files. No authentication, network, or user input validation concerns beyond normal game robustness. `security_enforcement` effectively N/A for this project type.

## Sources

### Primary (HIGH confidence)
- Codebase analysis of all .cs files in project [VERIFIED: direct file reads]
- UI sprite assets inspected visually [VERIFIED: image file reads]
- `items.json` with 45 item definitions [VERIFIED: file read]
- `SceneManager.cs` Push/Pop/PushImmediate patterns [VERIFIED: file read]
- `Entity.cs` base class for ItemDropEntity [VERIFIED: file read]
- `GameState.cs` existing src/Inventory/HotbarSlots/WeaponId/ArmorId fields [VERIFIED: file read]

### Secondary (MEDIUM confidence)
- MonoGame SpriteBatch, Mouse, Rectangle APIs [ASSUMED: standard MonoGame 3.8 API surface]

### Tertiary (LOW confidence)
- None

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - no new libraries, all existing MonoGame APIs
- Architecture: HIGH - builds directly on Phase 1 patterns (Scene, Entity, ServiceContainer)
- Pitfalls: HIGH - identified from direct codebase analysis (SceneManager fade, save gaps, mouse coordinates)
- UI implementation: MEDIUM - sprite sizes and exact layout need tuning during implementation

**Research date:** 2026-04-10
**Valid until:** 2026-05-10 (stable -- MonoGame 3.8 is mature, no breaking changes expected)
