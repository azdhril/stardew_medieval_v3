# Phase 2: Items & Inventory - Context

**Gathered:** 2026-04-11
**Status:** Ready for planning

<domain>
## Phase Boundary

Sistema completo de inventario, hotbar, equipamento, e integracao farming->inventario. Players podem coletar, gerenciar, equipar itens, e a colheita agora produz itens que fluem para o inventario via item drops no chao com magnetismo.

</domain>

<decisions>
## Implementation Decisions

### Inventario UI
- **D-01:** Grid estilo Stardew Valley com 20 slots em caixinhas. Usar sprite `UI_Slot_Normal.png` para slots normais e `UI_Slot_Selected.png` para slot selecionado. Abre com tecla I.
- **D-02:** Click para selecionar item, click em outro slot para mover. Drag-and-drop opcional (Claude's discretion se vale a complexidade).
- **D-03:** Cores de raridade nos itens seguem o sistema existente (Common/Uncommon/Rare) com borda ou glow colorido no slot.

### Hotbar
- **D-04:** Hotbar visual fixa na parte inferior da tela, estilo Stardew Valley (referencia: HUD.png do kit). Slots numerados 1-8, acessiveis por number keys. Slot ativo tem destaque visual (UI_Slot_Selected).
- **D-05:** Hotbar faz parte do HUD permanente (sempre visivel durante gameplay), nao do inventario overlay.

### Equipment Slots
- **D-06:** Equipment slots como aba separada do inventario (nao misturado com grid de itens). Abrir inventario mostra grid; trocar de aba mostra equipment.
- **D-07:** Layout Tibia-style com formato de "homenzinho" — slots fixos posicionados em torno de uma silhueta: arma (mao), armadura (torso). Expandir para mais slots em fases futuras se necessario.

### Item Drops e Magnetismo
- **D-08:** Itens dropados no chao usam o sprite real do item (nao saquinho generico). Ficam no chao ate o player se aproximar.
- **D-09:** Magnetismo com distancia inicial pequena (~48-64px). Velocidade de atracao e animacao estilo Stardew Valley (item acelera em direcao ao player com curva suave). Distancia de magnetismo pode ser aumentada em fases futuras (upgrades).
- **D-10:** Item drop tem um pequeno "bounce" ao spawnar no chao (feedback visual de que algo dropou).

### Farming -> Inventario
- **D-11:** Colheita usa foice (ou ferramenta equivalente). Player usa ferramenta no crop maduro -> crop some do campo -> item dropa no chao no local da colheita -> magnetismo puxa para o player -> vai para inventario.
- **D-12:** Fix visual do crop system (FARM-01): corrigir posicao do player ao arar/semear/regar. FARM-02: sprites adequados para colheita (substituir overlays coloridos).
- **D-13:** Se inventario estiver cheio quando item e coletado, item permanece no chao (nao desaparece). Feedback visual/sonoro de "inventario cheio" opcional.

### Claude's Discretion
- Implementacao interna do drag-and-drop vs click-to-move no inventario
- Layout exato dos elementos de UI (margens, espaçamento)
- Animacao exata do magnetismo (curva de aceleracao)
- Como organizar o codigo internamente (InventoryManager, ItemDropEntity, etc.)
- Ordem das abas no inventario (grid primeiro ou equipment primeiro)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### UI Assets (CRITICO — sprites ja prontos)
- `assets/Sprites/System/Preview/HUD.png` — Referencia visual completa da HUD desejada (hotbar, barras, retrato, minimapa)
- `assets/Sprites/System/Preview/Preview.png` — Catalogo completo de todos sprites UI disponiveis no kit
- `assets/Sprites/System/UI Elements/Slot/` — Sprites de slot: Normal, Selected, Dim, Normal_Small, Selected_Small
- `assets/Sprites/System/Icons/System/` — Icones de sistema: Gold, Attack, Defense, Lock, Gem, etc.
- `assets/Sprites/System/UI Elements/Bars/` — Barras de HP, progress bars, icones de pocao

### Codebase (Phase 1 outputs)
- `src/Data/ItemDefinition.cs` — Modelo de item unificado (Id, Name, Type, Rarity, StackLimit, SpriteId, Stats)
- `src/Data/ItemRegistry.cs` — Registry estatico com 45 itens carregados de JSON
- `src/Data/items.json` — Definicoes de itens (crops, seeds, tools)
- `src/Data/ItemStack.cs` — Stack de itens (item + quantidade)
- `src/Core/GameState.cs` — Ja tem Inventory, Gold, HotbarSlots, WeaponId/ArmorId placeholders
- `src/Core/Entity.cs` — Base class para ItemDrop entity (Position, SpriteSheet, CollisionBox, Update, Draw)
- `src/Core/SaveManager.cs` — Save/load com migracao de versao

### Codebase Maps
- `.planning/codebase/ARCHITECTURE.md` — Arquitetura atual, layers, data flow
- `.planning/codebase/STRUCTURE.md` — Layout de diretorios
- `.planning/codebase/CONVENTIONS.md` — Naming patterns, code style

### Requirements
- `.planning/REQUIREMENTS.md` — INV-01 a INV-05, FARM-01 a FARM-03, HUD-02

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ItemDefinition` / `ItemRegistry` / `ItemStack`: Sistema de itens ja pronto na Phase 1, so precisa ser consumido
- `Entity` base class: ItemDrop pode herdar Entity para position, sprite, collision, update/draw
- `UI_Slot_*.png` sprites: Kit completo de slots para inventario e hotbar
- `UI_Icon_Sys_*.png`: Icones para gold, attack, defense, etc.
- `SceneManager` com stack: Inventario pode ser uma Scene empilhada (push/pop) sobre FarmScene

### Established Patterns
- Event-driven via delegates (OnDayAdvanced, OnStaminaChanged) — usar para OnInventoryChanged, OnItemPickup
- Dictionary<Point, CellData> sparse storage — inventario usa array de slots (List<ItemStack?>)
- Console.WriteLine com [ModuleName] prefix — manter para [Inventory], [ItemDrop], etc.
- Scene push/pop para overlays — inventario como InventoryScene empilhada

### Integration Points
- `src/Scenes/FarmScene.cs` — Onde item drops sao spawned apos colheita, onde magnetismo opera
- `src/Farming/CropManager.cs` — Harvest logic precisa mudar: crop -> item drop no chao
- `src/Farming/ToolController.cs` — Adicionar foice como ferramenta de colheita
- `src/Core/GameState.cs` — src/Inventory/HotbarSlots/Equipment ja tem campos placeholder
- `src/Player/PlayerEntity.cs` — Collision check com item drops para magnetismo

</code_context>

<specifics>
## Specific Ideas

- Referencia visual principal: HUD.png e Preview.png no kit de sprites (assets/Sprites/System/Preview/)
- Hotbar identica ao estilo mostrado no HUD.png (slots numerados na base da tela, com itens dentro)
- Equipment screen Tibia-style com silhueta de personagem e slots fixos ao redor
- Magnetismo estilo Stardew Valley: item flutua suavemente em direcao ao player, acelerando
- Colheita obrigatoriamente com ferramenta (foice), nao click vazio — item dropa no chao

</specifics>

<deferred>
## Deferred Ideas

- Aumento de distancia de magnetismo via upgrades (futuro, progression system)
- Mais equipment slots alem de arma/armadura (anel, amuleto, etc.)
- Tooltip detalhado ao passar mouse sobre item (v2 polish)
- Sort/filter no inventario

</deferred>

---

*Phase: 02-items-inventory*
*Context gathered: 2026-04-11*
