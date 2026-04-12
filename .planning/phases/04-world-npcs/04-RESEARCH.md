# Phase 4: World & NPCs - Research

**Researched:** 2026-04-12
**Domain:** Top-down 2D RPG — map transitions, NPC interaction, dialogue UI, shop UI, quest state
**Confidence:** HIGH (codebase patterns fully verified; external references are well-known MonoGame/2D RPG conventions)

## Summary

Fase 4 é principalmente **composição sobre infraestrutura existente**, não construção nova. O `SceneManager` já faz transições com fade; o `InventoryManager`/`GameState` já carregam gold, save/load e migração; o `TileMap` já parseia polígonos de Tiled. O trabalho real está em: (1) criar 3 novas scenes (Village/Castle/Shop) seguindo o molde de `FarmScene`/`TestScene`; (2) criar uma `NpcEntity` genérica (superset de `DummyNpc`) com portal de diálogo; (3) criar uma `DialogueScene` overlay (segue o padrão de `InventoryScene`/`PauseScene`); (4) criar uma `ShopScene` com duas abas reusando `InventoryGridRenderer`; (5) adicionar um sistema leve de trigger zones nos mapas Tiled (camadas de object groups nomeadas diferentes de "Collision"). Save-migration vai para v5 com `MainQuestState` persistido.

**Primary recommendation:** Não reinvente — reuse `SceneManager.TransitionTo` para trocas de mapa, `PushImmediate` para overlays (diálogo, shop); crie `NpcEntity` herdando `Entity` (mesmo padrão de `DummyNpc`); parseie um novo object group chamado `Triggers` no TMX (reusar `LoadObjectsFromGroup` estendido com `obj.name`/`obj.type` para distinguir `door_castle`, `exit_village`, etc). Publique um `OnQuestStateChanged` event e deixe HUD/NPCs assinarem.

## User Constraints (from CONTEXT.md)

### Locked Decisions

**Village Layout & Scope**
- **D-01:** Village map é uma única tela 960×540 (sem scroll). Castelo de um lado, Shop do outro, caminho aberto entre eles.
- **D-02:** Apenas dois NPCs vivem na vila nesta fase: King (dentro do Castle) e Shopkeeper (dentro do Shop). Sem villagers de flavor.
- **D-03:** Castle e Shop têm scenes próprias (`CastleScene`, `ShopScene`) entradas via trigger de porta no village. Interiores mínimos (tela única cada, NPC parado dentro).

**Dialogue UX**
- **D-04:** Caixa de diálogo ancorada no bottom com portrait estático no lado esquerdo dentro da caixa.
- **D-05:** Texto revela character-by-character (typewriter). E ou Space: primeira pressão completa a linha atual instantaneamente; segunda avança ou fecha.
- **D-06:** Diálogo é linear-only na Phase 4 — sem menus de escolha do jogador. Aceitação de quest é automática na primeira conversa com King.

**Shop UX**
- **D-07:** Shop UI tem duas abas: **Buy** (inventário do shopkeeper com preços) e **Sell** (inventário do player com valores de venda).
- **D-08:** Estoque do shopkeeper é curado: seeds + consumables básicos (potions) + arma starter + armadura starter (~6–10 itens). Compatível com NPC-03.
- **D-09:** Compras completam em single press de Buy com pequeno toast de confirmação ("Purchased X"). Sem popup de confirmação.
- **D-10:** Botão Buy fica desabilitado com label de razão quando player não pode pagar ou inventário cheio ("Not enough gold" / "Inventory full"). Mesmo padrão para Sell quando nada selecionado.

**Quest State & Tracker**
- **D-11:** Main quest é representada como single `MainQuest` com enum `QuestState`: `NotStarted | Active | Complete`. Armazenada em GameState para save/load. Design permite substituir por quest list futura sem reworkar consumers.
- **D-12:** Phase 4 expõe hook `SetQuestComplete()` mas não wire em gameplay real. Trigger placeholder = dev/debug key para testar branches. Trigger real (boss derrotado) entra na Phase 5.
- **D-13:** Quest tracker renderiza no canto top-right do HUD como texto simples ("Quest: Clear the Dungeon") sempre visível durante gameplay. Polish gráfico diferido para Phase 6 (HUD-04).
- **D-14:** Tanto King quanto Shopkeeper têm três variantes de diálogo keyed pelo MainQuest state (NotStarted / Active / Complete) — satisfaz NPC-04 em dois NPCs.

### Claude's Discretion
- Exato portrait art (placeholder pixel art tá ok — art pass depois).
- Abordagem de trigger-zone (tile-based polygon vs Rectangle em Entity) — researcher/planner escolhe baseado no TileMap collision existente.
- Preços e valores de stock da shop — planner escolhe defaults razoáveis.
- Texto de diálogo (wording) — planner rascunha, user reescreve.
- Spawn points de transição de scene — planner deriva do layout.

### Deferred Ideas (OUT OF SCOPE)
- Flavor/idle villagers
- Innkeeper / sleep-save NPC
- Branching dialogue choices
- Hold-to-buy para itens caros
- Full quest list data structure (multi-quest)
- Quest tracker gráfico com ícones/progresso (HUD-04 na Phase 6)
- Shop stock rotation / refresh diário / quantidades limitadas

## Project Constraints (from CLAUDE.md)

- **Engine fixa:** MonoGame 3.8 DesktopGL — sem adicionar libs externas pesadas. [VERIFIED: CLAUDE.md]
- **Linguagem:** C# 12 / .NET 8.0. Nullable reference types habilitado. [VERIFIED: .csproj checked]
- **Mapas:** Tiled `.tmx`/`.tsx` via TiledCS 3.3.3. Novos mapas devem usar o mesmo padrão de `test_farm.tmx`. [VERIFIED: TileMap.cs + Content/Maps/]
- **Resolução:** 960×540 base. Village D-01 encaixa (40 tiles × 30 tiles a 16px = 640×480; usar 60×34 = 960×544 ou manter 40×30 se cabe — village é "single screen no scroll"). [CITED: CLAUDE.md constraint]
- **Nullable enabled:** Todas refs opcionais devem ser `Type?` e inicializadas com `null!` se preenchidas em `LoadContent`. [VERIFIED: existing scenes]
- **Convenções obrigatórias:** PascalCase classes/métodos, `_camelCase` para campos privados, `On*` para events, `Try*` para fallible ops, `[ModuleName]` em `Console.WriteLine`. [VERIFIED: CLAUDE.md]
- **Composition root:** Todos services instanciados em `Game1`. Scenes recebem `ServiceContainer`. [VERIFIED: FarmScene.cs pattern]
- **Save migration obrigatória:** Qualquer campo novo em `GameState` requer bump de `CURRENT_SAVE_VERSION` e branch em `MigrateIfNeeded`. [VERIFIED: SaveManager.cs pattern]

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| WLD-01 | Map transitions Farm ↔ Village ↔ Dungeon com fade to black | `SceneManager.TransitionTo` já implementa fade-to-black com pending action (Core/SceneManager.cs:37). Phase 4 implementa Farm↔Village; "Dungeon entrance" é placeholder scene vazia wired na Phase 5. |
| WLD-02 | Vila mínima com castelo do Rei e 1 loja | Criar `village.tmx` (Tiled), `CastleScene`, `ShopScene`. `test_farm.tmx` serve de molde. |
| WLD-03 | Trigger zones em bordas/portas | Estender `TileMap` para carregar um object group "Triggers" (análogo ao "Collision" já suportado em TileMap.cs:74–92). Cada objeto tem nome (ex: `exit_village`, `door_castle`) consumido pela Scene. |
| WLD-04 | Estado do player preservado entre transições | `GameState` já serializa posição/stamina/inventário. Adicionar `CurrentScene` (já existe, default `"Farm"`) + scene-specific spawn points. Scenes precisam respeitar `_loadedState.CurrentScene` na entry. |
| NPC-01 | Sistema de diálogo com caixa de texto e retrato | Nova `DialogueScene` overlay, seguindo padrão de `InventoryScene` (PushImmediate, dim background, bottom-anchored panel). Retrato renderizado como `Texture2D` estático à esquerda. |
| NPC-02 | Rei NPC no castelo dando main quest | `NpcEntity` nova classe em `Entities/`. King instanciado em `CastleScene`. Interação via `InputManager.InteractPressed` (E) + proximity check usando `CollisionBox`/`HitBox` distance. |
| NPC-03 | Shopkeeper com UI de compra/venda | `ShopScene` reusa `InventoryGridRenderer` para Sell tab; Buy tab renderiza lista de `ShopItem { ItemDef, Price }`. Gold já está em `InventoryManager`? → **NOTA:** Gold está em `GameState.Gold` mas não em `InventoryManager`. Precisa acessar Gold via `Services` ou passar ref. Planner deve decidir (ver Open Questions). |
| NPC-04 | NPCs com estado de quest alterando diálogo | `NpcEntity.GetDialogueFor(QuestState)` retorna `string[]` (linhas). `MainQuest` publica `OnQuestStateChanged`; HUD e próximo diálogo reagem. |
| HUD-03 | Shop UI com lista, preços, botões comprar/vender | `ShopScene` implementa. Abas via botão/tabs no topo do painel. Item row: ícone (reuse `SpriteAtlas`) + nome + preço + botão. |
| HUD-05 | Caixa de diálogo estilizada com retrato e avanço | Render path: (1) dim overlay full-screen, (2) painel bottom (width=screen-40px, height=~120px), (3) portrait box 80×80 à esquerda dentro do painel, (4) texto typewriter à direita, (5) indicator `▼` piscando quando linha completa. |

## Standard Stack

### Core (existing — do NOT add alternatives)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| MonoGame.Framework.DesktopGL | 3.8.* | Engine, rendering, input | [VERIFIED: .csproj] Locked by CLAUDE.md |
| TiledCS | 3.3.3 | Parse `.tmx`/`.tsx` | [VERIFIED: World/TileMap.cs:7] Já extrai polygons + object groups |
| System.Text.Json | (BCL) | GameState serialization | [VERIFIED: SaveManager.cs:4] |

### Supporting (existing subsystems this phase consumes)

| Class | Purpose | Phase 4 Touch |
|-------|---------|---------------|
| `Core/SceneManager.cs` | Fade transitions, scene stack | Chamar `TransitionTo(new VillageScene(...))` from FarmScene; `PushImmediate(new DialogueScene(...))` from NPC interact |
| `Core/ServiceContainer.cs` | DI bag | Adicionar `QuestManager` ou deixar quest state dentro de `GameState` acessível via Services |
| `Core/GameState.cs` | Persistent state | Add `QuestState` já existe (int, 0=None); promover para enum `MainQuestState`; adicionar `CurrentScene` (já existe) é usado na entry |
| `Core/SaveManager.cs` | Save/load + migration | Bump to v5; migrate `QuestState` int → ensure `MainQuestState` semântica |
| `Core/Entity.cs` | Base para NpcEntity | Herdar; override `CollisionBox`, `Draw`; interação via proximity |
| `World/TileMap.cs` | TMX loader + collision | Estender com `LoadTriggerObjects()` análogo a `LoadCollisionObjects()` (TileMap.cs:70–92) |
| `UI/HUD.cs` | Screen-space HUD | Adicionar quest tracker text top-right |
| `UI/InventoryGridRenderer.cs` | Grid UI | Reusar no Sell tab do ShopScene |
| `Inventory/InventoryManager.cs` | Slots, equipment | Shop chama `TryAdd`/`RemoveAt`/`TryConsume`; precisa acesso ao Gold |

**Não adicionar nada novo no NuGet.** Toda a Phase 4 é construída sobre o que já tem.

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Custom trigger zones in Tiled object group | Tile-based detection (check a layer's GID at player tile) | Object groups são mais flexíveis (qualquer shape, nomes arbitrários) e já suportados pelo TileMap. **Recomendação: object group.** |
| Write `MainQuestManager` standalone | Inline quest state em `GameState` + static helper | Com apenas 1 quest em Phase 4, manager pequeno é aceitável. **Recomendação: criar `Quest/MainQuest.cs` leve agora para facilitar Phase 6 (HUD-04 + save).** |
| Portrait como `Texture2D` separado | Embutir no spritesheet do NPC | Separado é mais simples e permite reuso em múltiplas scenes. **Recomendação: `Content/Sprites/Portraits/king.png`, `shopkeeper.png`.** |
| `DialogueScene` como Scene overlay | Desenhar direto em VillageScene | Overlay Scene é consistente com `InventoryScene`/`PauseScene` — bloqueia input da scene de baixo, UnloadContent limpa. **Recomendação: Scene overlay.** |

### Version Verification

Nenhum package novo — sem necessidade de `npm view`/`dotnet list`. Versões existentes estão locked no `.csproj`. [VERIFIED: direct file read]

## Architecture Patterns

### Recommended Directory Structure

```
stardew_medieval_v3/
├── Scenes/
│   ├── FarmScene.cs          (existing — add transition to VillageScene via edge trigger)
│   ├── VillageScene.cs       (NEW — loads village.tmx, spawns door triggers)
│   ├── CastleScene.cs        (NEW — interior, spawns KingNpc)
│   ├── ShopScene.cs          (NEW — interior + overlay UI)
│   ├── DialogueScene.cs      (NEW — overlay for NPC conversations)
│   └── InventoryScene.cs     (existing — pattern reference)
├── Entities/
│   ├── DummyNpc.cs           (existing — pattern reference)
│   └── NpcEntity.cs          (NEW — base for interactive NPCs)
├── World/
│   ├── TileMap.cs            (existing — extend to parse "Triggers" object group)
│   └── TriggerZone.cs        (NEW — record struct: Name, Polygon/Rect, Target)
├── Quest/                    (NEW directory)
│   ├── MainQuestState.cs     (NEW — enum NotStarted/Active/Complete)
│   └── MainQuest.cs          (NEW — state + event + methods)
├── UI/
│   ├── HUD.cs                (existing — add quest tracker)
│   ├── DialogueBox.cs        (NEW — renderer used by DialogueScene)
│   ├── ShopPanel.cs          (NEW — renderer used by ShopScene; Buy/Sell tabs)
│   └── Toast.cs              (NEW — optional; small confirmation after purchase)
├── Data/
│   ├── ItemDefinition.cs     (existing — needs price field OR separate price table)
│   ├── ShopStock.cs          (NEW — static: list of (ItemId, Price) for shopkeeper)
│   └── DialogueRegistry.cs   (NEW — static: dialogues per NPC per quest state)
├── Content/
│   ├── Maps/
│   │   ├── village.tmx       (NEW)
│   │   ├── castle.tmx        (NEW)
│   │   └── shop.tmx          (NEW)
│   └── Sprites/Portraits/    (NEW dir)
│       ├── king.png          (NEW — 64×64 or 80×80 placeholder)
│       └── shopkeeper.png    (NEW)
```

### Pattern 1: Scene-Per-Map Transition

**What:** Cada mapa é sua própria `Scene`; trocar mapa é `SceneManager.TransitionTo(new TargetScene(Services, entryPoint))`.
**When to use:** Sempre que o jogador atravessa uma borda/porta.
**Example (derived from existing `SceneManager.TransitionTo`):**

```csharp
// In VillageScene.Update()
if (_castleDoorTrigger.Contains(_player.CollisionBox) && input.InteractPressed)
{
    Services.SceneManager.TransitionTo(new CastleScene(Services, entryPoint: "village_door"));
}
```

Source: verified from `Core/SceneManager.cs:37-52` + `Scenes/FarmScene.cs:183-186`.

### Pattern 2: Overlay Scene (Dialogue, Shop)

**What:** `PushImmediate` uma Scene por cima da scene corrente. Underlying scene continua sendo desenhada (bottom-up em `SceneManager.Draw`), mas só a top recebe Update.
**Example:**

```csharp
// In NpcEntity.TryInteract(player):
Services.SceneManager.PushImmediate(new DialogueScene(Services, this, _quest.State));

// In DialogueScene.Update on final advance:
Services.SceneManager.PopImmediate();
```

Source: verified from `Core/SceneManager.cs:82-96, 129-135` + `Scenes/InventoryScene.cs:61-66`.

### Pattern 3: Entity with Interaction Prompt

**What:** NpcEntity desenha um marcador "[E] Talk" quando o player está em range. Proximity via `Vector2.Distance(npc.Position, player.Position) < InteractRange` (tipicamente 24–32px).
**Example:**

```csharp
public class NpcEntity : Entity
{
    public string NpcId { get; }
    public Texture2D Portrait { get; }
    private const float InteractRange = 28f;

    public bool IsInInteractRange(Vector2 playerPos)
        => Vector2.Distance(Position, playerPos) <= InteractRange;
}
```

Model derived from `Entities/DummyNpc.cs` + `Core/Entity.cs:64-77`.

### Pattern 4: Event-Driven Quest State

**What:** `MainQuest` publica `OnQuestStateChanged`. HUD quest tracker e NPCs assinam. Mesmo padrão usado por `TimeManager.OnDayAdvanced` (CLAUDE.md architecture section) e `PlayerStats.OnStaminaChanged`.

```csharp
public class MainQuest
{
    public MainQuestState State { get; private set; }
    public event Action<MainQuestState>? OnQuestStateChanged;

    public void Activate() { State = MainQuestState.Active; OnQuestStateChanged?.Invoke(State); }
    public void Complete() { State = MainQuestState.Complete; OnQuestStateChanged?.Invoke(State); }
}
```

### Pattern 5: Tiled Trigger Zones (extending TileMap)

**What:** Adicionar um segundo object group ao TMX chamado `Triggers`. Cada object tem `name` (e.g. `exit_to_farm`, `door_castle`) que identifica target scene.

**TMX snippet (to be added to village.tmx):**
```xml
<objectgroup id="5" name="Triggers">
  <object id="30" name="door_castle" type="transition" x="120" y="80" width="16" height="16"/>
  <object id="31" name="door_shop" type="transition" x="400" y="80" width="16" height="16"/>
  <object id="32" name="exit_to_farm" type="transition" x="0" y="240" width="16" height="32"/>
</objectgroup>
```

**Code extension (World/TileMap.cs):**
```csharp
public IReadOnlyList<TriggerZone> Triggers => _triggers;
private readonly List<TriggerZone> _triggers = new();

private void LoadTriggerObjects()
{
    foreach (var layer in _map.Layers)
    {
        if (layer.type == TiledLayerType.ObjectLayer &&
            layer.name.Equals("Triggers", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var obj in layer.objects ?? Array.Empty<TiledObject>())
            {
                var rect = new Rectangle((int)obj.x, (int)obj.y, (int)obj.width, (int)obj.height);
                _triggers.Add(new TriggerZone(obj.name, rect));
            }
        }
    }
}

public record TriggerZone(string Name, Rectangle Bounds);
```

Source: mirrors `LoadCollisionObjects` pattern at `World/TileMap.cs:70-92`. [VERIFIED: TileMap.cs behavior]

### Anti-Patterns to Avoid

- **Hard-coding door coordinates in C#:** Frágil a edições no Tiled. Use Trigger object group no TMX.
- **Diálogo como `Console.WriteLine`:** NPC-01 exige caixa visual com portrait. Criar `DialogueScene` desde o começo.
- **Desenhar shop no FarmScene sem overlay:** Viola isolamento. Use Scene overlay (mesmo padrão do InventoryScene).
- **Mutar `GameState.QuestState` diretamente:** Passa por `MainQuest.Activate()` para disparar o event.
- **Guardar preço em `ItemDefinition`:** Item pode ter preço diferente em shop futuro. Use table separada (`ShopStock`) ou adicionar `BasePrice` ao `ItemDefinition` (escolha arquitetural — planner decide). **Recomendação: tabela separada agora, preço base no item depois se multi-shop aparecer.**
- **Não bumpar `CURRENT_SAVE_VERSION` ao adicionar `MainQuestState`:** Saves da Phase 3 quebrarão. Sempre bump + migration branch.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Fade transition entre scenes | Custom fade state machine | `SceneManager.TransitionTo` | Já existe, 2-phase fade out→action→fade in. (SceneManager.cs:103-135) |
| Scene stack management | Cascade manual de Update/Draw | `SceneManager.Push`/`PushImmediate` | Top-only update, bottom-up draw ready. |
| Polygon collision / rectangle overlap | Código manual | `Rectangle.Intersects` + `TileMap.CheckCollision` | `Rectangle.Intersects` é BCL; TileMap já ray-casts polígonos. |
| JSON save/load | Custom serializer | `System.Text.Json` via `SaveManager` | Já em uso. Apenas adicionar campos ao GameState + migration. |
| Input edge detection | Manual current/previous state | `InputManager.IsKeyPressed` | Padrão já estabelecido. (InputManager.cs:54) |
| Grid inventory rendering no Sell tab | Novo grid renderer | `InventoryGridRenderer` | Já pronto; aceita position/layout. |
| Sprite sheet atlasing para ícones de item | Recarregar texturas | `SpriteAtlas` existente | Já carregado em FarmScene; pass via ServiceContainer ou construtor. |
| Save migration | Ad-hoc fallback | `SaveManager.MigrateIfNeeded` pattern | Branch v4→v5 seguindo o mesmo formato dos branches existentes. |

**Key insight:** A fase 4 é 80% composition work. O único código estruturalmente novo é `NpcEntity`, `DialogueScene`, `ShopScene`, e o `MainQuest` event container. O resto é wiring + content.

## Runtime State Inventory

**Not applicable** — Phase 4 é greenfield feature development (não rename/refactor). No entanto, algumas considerações adjacentes:

| Category | Items | Action |
|----------|-------|--------|
| Stored data | Existing saves (v4) precisam migrar para v5 ao adicionar `MainQuestState` e possivelmente um spawn-point-per-scene hint | Bump `CURRENT_SAVE_VERSION`; adicionar `if (state.SaveVersion < 5)` branch em `MigrateIfNeeded` |
| Live service config | None — projeto local | — |
| OS-registered state | None | — |
| Secrets/env vars | None | — |
| Build artifacts | Nova content (maps, portraits) deve ser copiada via `<Content Include="...">` no `.csproj` (ou MGCB se for processada) | Checar `Content/Content.mgcb` para novos PNGs se usar pipeline; .tmx files são lidos direto via File.OpenRead (FarmScene.cs:59), sem MGCB |

## Common Pitfalls

### Pitfall 1: Player spawn point wrong after scene transition
**What goes wrong:** Player entra em `CastleScene` no canto do mapa ou dentro de uma parede.
**Why it happens:** Cada scene tem sua própria origem; sem entry points explícitos, o player aparece em (0,0) ou na posição salva de OUTRA scene.
**How to avoid:** Constructor de Scene recebe um `string entryPoint` e a scene lookup'a coordenadas desse spawn (em um dict static ou em um Tiled object group `Spawns`). Ex: `new CastleScene(Services, "from_village_door")` → spawn no centro-sul do castle map.
**Warning signs:** Player spawna em cima da porta e instantly trigga volta; ou spawna fora do mapa e aparece preso.

### Pitfall 2: Trigger zone fires every frame while inside
**What goes wrong:** Transição é chamada infinitamente enquanto player overlaps com trigger zone, ou duplicata de fade loop.
**Why it happens:** `SceneManager.TransitionTo` já tem guard (`if (_state != TransitionState.None) return;`) — mas o player retorna no mapa de destino ainda dentro de um trigger e volta.
**How to avoid:** (1) SceneManager já ignora durante transition (SceneManager.cs:39). (2) Spawn do player no destination deve estar *fora* do trigger de retorno. (3) Use trigger zones apenas como "edge" zones; entry points ficam para dentro.
**Warning signs:** "[SceneManager] Transition started" log repetindo; player teleporta infinitamente.

### Pitfall 3: Typewriter + skip logic off-by-one
**What goes wrong:** Press E completa linha atual; mas se press E de novo no mesmo frame, ou se a linha já tinha completado sozinha, pula duas linhas.
**Why it happens:** Condição ambígua entre "linha ainda revelando" e "linha completa aguardando advance".
**How to avoid:** Dois estados explícitos: `Revealing` (chars < total → E completa) e `WaitingAdvance` (E avança ou fecha). Use `InputManager.IsKeyPressed` (edge-triggered, InputManager.cs:54), não `IsKeyDown`.
**Warning signs:** Linhas são puladas; ou caixa fecha sem mostrar todo o texto.

### Pitfall 4: Gold not accessible where Shop UI needs it
**What goes wrong:** `ShopScene` precisa de gold check + debit; `InventoryManager` atualmente não tem Gold — está em `GameState`.
**Why it happens:** Phase 2 decidiu manter Gold em GameState (verified: GameState.cs:25 `public int Gold`).
**How to avoid:** Duas opções: (A) expor `Gold` em `InventoryManager` (wraps GameState ref), (B) passar GameState ref ao `ShopScene`. **Recomendação (A):** adicionar `public int Gold { get; set; }` + `TrySpendGold(int)` em `InventoryManager` para manter uma única porta de entrada para operações econômicas; `SaveToState`/`LoadFromState` já vão carregar/persistir.
**Warning signs:** Compras "funcionam" mas gold nunca decrementa na HUD; save não persiste compras.

### Pitfall 5: Inventory full on purchase — silent drop
**What goes wrong:** Player compra item, gold debitado, mas `InventoryManager.TryAdd` retorna `remaining > 0` (inventário cheio) e item some.
**Why it happens:** `TryAdd` retorna leftover mas não é checado antes do débito.
**How to avoid:** D-10 já cobre isso — Buy button deve estar *disabled* quando `gold < price || inventory full`. Planner deve especificar check ANTES do débito: `if (def.StackLimit == 1 ? inventory cheio : space suficiente)`. Fluxo correto: (1) check gold, (2) check space via helper `CanAdd(itemId, 1)`, (3) se ambos ok: debit gold + TryAdd.
**Warning signs:** Gold desaparece sem item aparecer; player reclama de "bug".

### Pitfall 6: Save migration chain break
**What goes wrong:** Adicionar v5 sem o `if (state.SaveVersion < 5)` branch. Saves antigos carregam com `MainQuestState = NotStarted` por default mas o enum não é seteado explicitamente.
**How to avoid:** Seguir o pattern exato em `SaveManager.cs:67-99`. Cada branch seta valores default para campos novos. Bump `CURRENT_SAVE_VERSION` para 5.

### Pitfall 7: Dialogue portrait pixelation
**What goes wrong:** Portrait PNG 64×64 renderizado em um box 80×80 vira blurry porque SpriteBatch usa default bilinear sampling.
**How to avoid:** Todo render de sprite no jogo usa `SamplerState.PointClamp` (verified: InventoryScene.cs:104, FarmScene.cs:481). DialogueScene deve fazer o mesmo.

## Code Examples

### Example 1: NpcEntity base class

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Core;

namespace stardew_medieval_v3.Entities;

/// <summary>
/// Interactive NPC with portrait and dialogue branches keyed by quest state.
/// </summary>
public class NpcEntity : Entity
{
    public string NpcId { get; }
    public Texture2D? Portrait { get; }
    private const float InteractRange = 28f;

    public NpcEntity(string npcId, Texture2D sprite, Texture2D? portrait, Vector2 position)
    {
        NpcId = npcId;
        SpriteSheet = sprite;
        Portrait = portrait;
        Position = position;
        FrameWidth = 16;
        FrameHeight = 24;
        Console.WriteLine($"[NpcEntity] Spawned {npcId} at ({position.X}, {position.Y})");
    }

    public bool IsInInteractRange(Vector2 playerPos)
        => Vector2.Distance(Position, playerPos) <= InteractRange;

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (SpriteSheet == null) return;
        spriteBatch.Draw(SpriteSheet,
            new Rectangle((int)Position.X - FrameWidth / 2,
                          (int)Position.Y - FrameHeight / 2,
                          FrameWidth, FrameHeight),
            Color.White);
    }
}
```

Pattern source: `Entities/DummyNpc.cs` (verified). [VERIFIED: codebase read]

### Example 2: DialogueScene skeleton

```csharp
public class DialogueScene : Scene
{
    private readonly NpcEntity _npc;
    private readonly string[] _lines;
    private int _lineIndex;
    private int _charsRevealed;
    private float _charTimer;
    private const float CharInterval = 0.025f; // ~40 cps, per D-05

    public DialogueScene(ServiceContainer services, NpcEntity npc, string[] lines)
        : base(services)
    {
        _npc = npc;
        _lines = lines;
    }

    public override void Update(float deltaTime)
    {
        var input = Services.Input;
        bool advance = input.IsKeyPressed(Keys.E) || input.IsKeyPressed(Keys.Space);

        if (_charsRevealed < _lines[_lineIndex].Length)
        {
            _charTimer += deltaTime;
            while (_charTimer >= CharInterval && _charsRevealed < _lines[_lineIndex].Length)
            {
                _charTimer -= CharInterval;
                _charsRevealed++;
            }
            if (advance) _charsRevealed = _lines[_lineIndex].Length; // skip to full
        }
        else if (advance)
        {
            _lineIndex++;
            if (_lineIndex >= _lines.Length)
            {
                Services.SceneManager.PopImmediate();
                return;
            }
            _charsRevealed = 0;
            _charTimer = 0;
        }
    }
}
```

### Example 3: Shop purchase flow

```csharp
public bool TryBuy(ShopItem item)
{
    var inv = Services.Inventory!;
    if (inv.Gold < item.Price) { ShowToast("Not enough gold"); return false; }
    if (!CanAddOne(inv, item.ItemId))  { ShowToast("Inventory full"); return false; }

    inv.Gold -= item.Price;
    inv.TryAdd(item.ItemId, 1);
    ShowToast($"Purchased {item.ItemId}");
    Console.WriteLine($"[ShopScene] Bought {item.ItemId} for {item.Price}g");
    return true;
}

private static bool CanAddOne(InventoryManager inv, string itemId)
{
    var def = ItemRegistry.Get(itemId);
    if (def == null) return false;
    // Stackable: fits in existing stack OR empty slot available
    for (int i = 0; i < InventoryManager.SlotCount; i++)
    {
        var s = inv.GetSlot(i);
        if (s == null) return true;
        if (s.ItemId == itemId && s.Quantity < def.StackLimit) return true;
    }
    return false;
}
```

### Example 4: Save migration v4 → v5

```csharp
// In SaveManager.cs, update CURRENT_SAVE_VERSION to 5 and add:
if (state.SaveVersion < 5)
{
    // v4 -> v5: main quest state persisted as enum-compatible int
    // Existing state.QuestState (int) maps: 0 -> NotStarted, 1 -> Active, 2 -> Complete
    // No data transform needed — field already exists from v3
    state.SaveVersion = 5;
    Console.WriteLine("[SaveManager] Migrated save from v4 to v5");
}
```

Note: `GameState.QuestState` (int) já existe (GameState.cs:27). Migration apenas bumpa versão; semantic enum pode viver em `Quest/MainQuestState.cs` com conversão int ↔ enum nas bordas.

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Monolithic Game1 com tudo | Scene-based com SceneManager | Phase 1 (ARCH-01) | Phase 4 simply adds more scenes |
| WeaponId/ArmorId legacy fields | `Equipment` dict per EquipSlot | Phase 2 | Shop vende equipamentos que aceitam EquipSlot direto |
| `QuestState` armazenado como int | Enum `MainQuestState` semântico (Phase 4) | NOW | Compat: serializa como int via cast; readable em código |

**Deprecated/outdated:**
- Nenhum sistema de Phase 4 existe pré-atualmente — greenfield.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Container de preços fica separado de ItemDefinition (tabela `ShopStock`) | Architecture → Don't Hand-Roll | Low — planner pode inverter facilmente se user preferir `BasePrice` no item. |
| A2 | Village 960×540 significa tela única sem scroll; usar tile count ~60×34 a 16px | Project Constraints | Low — mapa Tiled pode ser qualquer size; 60×34 é só sugestão para caber sem scroll. User/planner confirma no Tiled. |
| A3 | Portrait 64×64 ou 80×80 placeholder é aceitável | Architecture | Low — D-Discretion menciona placeholder pixel art OK. |
| A4 | Typewriter ~40cps (0.025s/char) matches Stardew feel | Example 2 | Low — é tunable em runtime; specifics section menciona "30–60 chars/sec". |
| A5 | Interact range 28px (1.75 tiles) é adequado | NpcEntity pattern | Low — tunable, segue convenção top-down RPG. |
| A6 | Gold pertence em `InventoryManager` (não em um EconomyManager separado) | Pitfall 4 | Medium — planner deve confirmar; alternativa (B) passa GameState ref direto ao ShopScene. User no CONTEXT.md não locked essa decisão. |
| A7 | Trigger zones ficam num object group "Triggers" no TMX | Pattern 5 | Low — direct analog a "Collision" que já funciona. |
| A8 | Dialogue lines é `string[]` linear (não struct com speaker/emote) | Example 2 | Low — D-06 exclui branching, speaker é sempre o NPC da scene. |
| A9 | Quest complete trigger em Phase 4 é debug key (F4?) — não wired ao boss | D-12 | Already locked in CONTEXT.md, no risk. |
| A10 | `ShopScene` é overlay Scene (não inline em VillageScene nem própria scene via TransitionTo) | Pattern 2 | Low — consistente com InventoryScene. |

## Open Questions

1. **Gold ownership: InventoryManager vs GameState direct?**
   - What we know: Gold está em `GameState.Gold`. ShopScene precisa ler+mutar.
   - What's unclear: Adicionar Gold prop em `InventoryManager` (wrapping) vs passar GameState ref ao ShopScene.
   - Recommendation: **Option A — expose `Gold` via InventoryManager** (single econ entrypoint). Planner can reverse if user objects.

2. **ShopStock: separate table OR BasePrice field on ItemDefinition?**
   - What we know: `ItemDefinition` não tem price. `items.json` would need schema update if we add one.
   - What's unclear: Phase 6 (death penalty) precisa sell values também — se pricing fica em ShopStock, perde reusabilidade.
   - Recommendation: **Add `BasePrice` to ItemDefinition** (single source of truth), com `ShopStock` tendo apenas a *lista curada* de item IDs vendidos. Sell price = BasePrice × 0.5 default.

3. **CurrentScene restoration on load: where does player respawn?**
   - What we know: `GameState.CurrentScene` já existe mas `FarmScene` sempre é entrada (Game1 hardcoded, verified at FarmScene.cs:62 `Position = TileMap.TileCenterWorld(10, 10)`).
   - What's unclear: Se player salvou no castelo, loadar direto em CastleScene ou sempre voltar para Farm?
   - Recommendation: Phase 4 **sempre volta para Farm no load** (simpler); proper scene restoration fica para Phase 6 (SAV-01/SAV-02). Log it as known limitation.

4. **Single main.tmx village.tmx or tile-set per interior?**
   - What we know: `test_farm.tmx` uses shared `farm_tileset.tsx`.
   - What's unclear: Castle interior precisa de tileset próprio (stone walls, throne) ou reusa farm tileset?
   - Recommendation: Criar `castle_tileset.tsx` e `shop_tileset.tsx` leves. Disponibilidade de assets é o limitador (STATE.md blocker #1). **Placeholder**: reuse farm_tileset com tiles sólidos como "walls" é ok para MVP.

5. **Portraits: where to load?**
   - Recommendation: `Content/Sprites/Portraits/king.png`, `shopkeeper.png`, carregados direto via `Texture2D.FromStream` em scene `LoadContent` (match FarmScene.cs:652 pattern).

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| MonoGame 3.8 DesktopGL | All rendering/input | ✓ | 3.8.* (.csproj locked) | — |
| .NET 8.0 SDK | Build | ✓ | 8.0 (assumed — project builds) | — |
| TiledCS | TMX parsing | ✓ | 3.3.3 | — |
| dotnet-mgcb | Content pipeline | ✓ | 3.8.4.1 | Raw `Texture2D.FromStream` works for uncompiled PNGs (FarmScene.cs:652) |
| Tiled editor | Author new village/castle/shop TMX | ⚠ User-side | — | Planner can hand-write XML following `test_farm.tmx` format |
| Portrait art assets | NPC-01 | ✗ | — | Placeholder: solid-color 64×64 PNG with letter ("K", "S") drawn |
| Interior tilesets (castle/shop) | WLD-02 | ✗ | — | Reuse `farm_tileset.tsx`; "walls" = solid colored tile |

**Missing dependencies with no fallback:** None.

**Missing dependencies with fallback:**
- Portrait art → placeholder colored rectangles with initials (acceptable per CONTEXT.md Claude's Discretion).
- Interior-specific tilesets → reuse farm tileset (user flagged assets as blocker in STATE.md).

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | **None currently** — no test project exists in the repo |
| Config file | none — see Wave 0 |
| Quick run command | `dotnet build stardew_medieval_v3.csproj -c Debug` (compile smoke) |
| Full suite command | `dotnet run` + manual UAT script |

**State verified:** `ls` of repo shows no `tests/` or `*.Tests.csproj`. Phases 2 and 3 shipped VERIFICATION via manual UAT — same approach required here.

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Command/Method | File Exists? |
|--------|----------|-----------|----------------|-------------|
| WLD-01 | Fade transition Farm ↔ Village works | manual-only | Walk to farm edge trigger; observe fade | ❌ Wave 0: UAT script in 04-VERIFICATION.md |
| WLD-02 | Village has castle + shop accessible | manual-only | Visual inspection of village.tmx + trigger test | ❌ |
| WLD-03 | Trigger zones fire on player overlap | integration (manual) | Walk into each trigger, verify scene swap | ❌ |
| WLD-04 | State preserved across transitions | manual | Open inv in farm (gold=100), go to village, return, assert gold=100 | ❌ |
| NPC-01 | Dialogue box renders with portrait, typewriter, advance | manual-only | Talk to King, confirm visual | ❌ |
| NPC-02 | King dialogue sets MainQuest Active | integration (manual) | Talk, observe HUD tracker appear | ❌ |
| NPC-03 | Buy/Sell modifies gold + inventory | manual | Buy item: gold decreases, inventory increases | ❌ |
| NPC-04 | Dialogue differs per quest state | manual | Talk NotStarted → Active → Complete; compare text | ❌ |
| HUD-03 | Shop UI shows items/prices/buttons | manual-only | Visual inspection | ❌ |
| HUD-05 | Dialogue box styled with portrait + advance | manual-only | Visual inspection | ❌ |
| — | Save v4 loads as v5 without data loss | integration (manual) | Load pre-phase-4 savegame.json, confirm `[SaveManager] Migrated save from v4 to v5` log and no crash | ❌ |

All Phase 4 requirements are UI-/UX-heavy and therefore manual-only — same model as Phase 2 (HUD-02) and Phase 3 (UAT 15/15).

### Sampling Rate
- **Per task commit:** `dotnet build -c Debug` (compile gate)
- **Per wave merge:** `dotnet run` + spot-check scenario (e.g., Wave 1 = scene transitions work; Wave 2 = dialogue; Wave 3 = shop)
- **Phase gate:** Full UAT script from 04-VERIFICATION.md (goal-backward, each success criterion from ROADMAP)

### Wave 0 Gaps
- [ ] `04-VERIFICATION.md` UAT script — covers WLD-01..04, NPC-01..04, HUD-03, HUD-05 (author AFTER execution, as Phase 3 did)
- [ ] Save migration smoke: place a known v4 savegame.json in LocalAppData before first run and verify migration log
- [ ] No new test framework needed; follow established manual UAT pattern

*(The project has no automated test scaffold. Introducing one is out of scope per CONTEXT.md deferred ideas + CLAUDE.md constraints. Continue with manual UAT — consistent with Phases 2 & 3.)*

## Sources

### Primary (HIGH confidence — verified via codebase read)
- `Core/SceneManager.cs` — TransitionTo/Push/Pop/Immediate semantics, fade state machine (lines 37–159)
- `Core/Scene.cs` — abstract lifecycle (LoadContent/Update/Draw/UnloadContent)
- `Core/GameState.cs` — persistent fields, `QuestState` (int) already present at line 27
- `Core/SaveManager.cs` — migration pattern, CURRENT_SAVE_VERSION bump (lines 67–99)
- `Core/Entity.cs` — base class contract, HitBox/CollisionBox virtuals
- `Core/InputManager.cs` — edge-triggered input, `IsKeyPressed` for E/Space
- `Core/ServiceContainer.cs` — composition root pattern
- `Entities/DummyNpc.cs` — exact NPC entity pattern to mirror
- `Scenes/FarmScene.cs` — reference for scene lifecycle, TMX load, save/load wiring
- `Scenes/InventoryScene.cs` — overlay Scene pattern (PushImmediate + dim background + panel)
- `Scenes/PauseScene.cs` — overlay menu pattern (button grid, hover, PopImmediate)
- `Scenes/TestScene.cs` — minimal scene scaffold (matches CastleScene need)
- `World/TileMap.cs` — TMX loader, `LoadCollisionObjects` pattern at lines 70–92 to mirror for triggers
- `Inventory/InventoryManager.cs` — TryAdd/TryConsume/RemoveAt API + save/load integration
- `UI/HUD.cs` — existing HUD surface (screen-space, PointClamp)
- `UI/InventoryGridRenderer.cs` — reusable for Sell tab
- `Content/Maps/test_farm.tmx` — TMX schema reference (object groups, polygons)

### Secondary (MEDIUM — derived from CLAUDE.md/CONTEXT.md)
- `CLAUDE.md` project constraints (engine, language, Tiled pipeline, conventions)
- `.planning/phases/04-world-npcs/04-CONTEXT.md` locked decisions D-01..D-14
- `.planning/REQUIREMENTS.md` requirement traceability

### Tertiary (LOW — conventional knowledge, not verified this session)
- Stardew Valley dialogue pacing (~30–60 cps) — [ASSUMED] standard 2D RPG feel
- Portrait sizes 64×64/80×80 — [ASSUMED] pixel art convention

## Metadata

**Confidence breakdown:**
- Standard stack: **HIGH** — all pieces already in codebase, verified by file reads.
- Architecture: **HIGH** — patterns are direct mirrors of existing scenes/entities.
- Pitfalls: **HIGH** — derived from actual code behavior (SceneManager transition guard, SaveManager migration chain, InputManager edge-trigger).
- Open Questions: **MEDIUM** — 5 architectural choices that planner should confirm during planning (Gold ownership, pricing location, load-scene restoration, tileset reuse, portrait location).

**Research date:** 2026-04-12
**Valid until:** 2026-05-12 (stable codebase; 30-day window)
