# Phase 1: Architecture Foundation - Context

**Gathered:** 2026-04-10
**Status:** Ready for planning

<domain>
## Phase Boundary

Refatorar a base de codigo para suportar multiplas scenes, entidades compartilhadas, e estado de jogo extensivel -- desbloqueando todo o desenvolvimento de features nas Phases 2-6. O jogo deve continuar funcionando identicamente apos a refatoracao (zero regression).

</domain>

<decisions>
## Implementation Decisions

### Scene Architecture
- **D-01:** Scenes como classe abstrata (`abstract class Scene`) com metodos virtuais (LoadContent, Update, Draw, UnloadContent). Scenes herdam e sobrescrevem.
- **D-02:** Servicos compartilhados via `ServiceContainer` passado no construtor da Scene. Agrupa InputManager, TimeManager, Camera, SpriteBatch, etc.
- **D-03:** Transicao entre scenes via fade to black (fade out 0.3-0.5s -> tela preta -> troca scene -> fade in 0.3-0.5s). SceneManager controla estados: None -> FadingOut -> Loading -> FadingIn -> None.
- **D-04:** SceneManager com stack (push/pop). Uma scene ativa no topo recebe Update. Draw renderiza do fundo pro topo. Permite menus e overlays como scenes empilhadas (ex: PauseScene sobre FarmScene).

### Entity Design
- **D-05:** Heranca simples com classe abstrata `Entity`. Subclasses: PlayerEntity, EnemyEntity, NPCEntity, ItemDrop.
- **D-06:** Entity base inclui: Position, CollisionBox, Facing, Velocity/Movement, Animacao (SpriteSheet, FrameIndex, AnimationTimer), HP/IsAlive. Justificativa: Phases 3-5 precisam dessas capacidades em multiplas entidades -- colocar na base agora evita refactor garantido.

### ItemDefinition Scope
- **D-07:** Estrutura completa do ItemDefinition na Phase 1: Id, Name, Type (enum: Crop/Seed/Tool/Weapon/Armor/Consumable/Loot), Rarity (Common/Uncommon/Rare), StackLimit, SpriteId, Stats (Dictionary<string, float>). Phases seguintes so populam dados, sem mudar estrutura.
- **D-08:** Definicoes de itens em JSON (`items.json`) carregadas via `ItemRegistry` estatico. CropRegistry migra para esse sistema unificado. Facilita balanco sem recompilar.

### Save Migration
- **D-09:** Manter compatibilidade de saves via versao + defaults. Incrementar CURRENT_SAVE_VERSION. MigrateIfNeeded() ja existe no SaveManager -- estender com migracao v->v+1. Campos novos recebem defaults (inventario vazio, gold=0, etc).
- **D-10:** Novos campos no GameState nesta fase: Inventory (List<ItemStack> vazia), Gold/XP/Level (zerados), CurrentScene (string) + QuestState (enum), Equipment slots (WeaponId/ArmorId opcionais), HotbarSlots (List<string?> com 8 slots). Todos com defaults seguros.

### Claude's Discretion
- Ordem interna de refatoracao (quais arquivos primeiro)
- Nomes exatos de metodos/propriedades intermediarios
- Como organizar a logica de fade transition internamente no SceneManager

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Codebase Maps
- `.planning/codebase/ARCHITECTURE.md` -- Arquitetura atual, layers, data flow, entry points
- `.planning/codebase/STRUCTURE.md` -- Layout de diretorios, onde adicionar codigo novo
- `.planning/codebase/CONVENTIONS.md` -- Naming patterns, code style, module design

### Source Files (refatoracao direta)
- `Game1.cs` -- Coordenador central que sera decomposto em scenes
- `Core/GameState.cs` -- Modelo de estado que sera expandido
- `Core/SaveManager.cs` -- Ja tem MigrateIfNeeded(), estender
- `Player/PlayerEntity.cs` -- Referencia para extração da Entity base
- `Data/CropRegistry.cs` -- Sera migrado para o sistema ItemRegistry unificado

### Requirements
- `.planning/REQUIREMENTS.md` -- ARCH-01 a ARCH-05 definem os criterios desta fase

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `SaveManager.MigrateIfNeeded()` -- Pattern de migracao de versao ja existe, so estender
- `PlayerEntity` -- Logica de movement, animacao e colisao que sera extraida para Entity base
- `CropRegistry` -- Pattern de registry estatico que sera generalizado para ItemRegistry
- `_pixel` texture em Game1.cs -- Pode ser reusada para efeito de fade to black

### Established Patterns
- Event-driven via delegates (`OnDayAdvanced`, `OnStaminaChanged`) -- manter para comunicacao entre scenes e sistemas
- Dictionary<Point, CellData> para sparse storage -- bom pattern para inventario tambem
- `Try*` prefix para operacoes que podem falhar -- manter na Entity base
- Console.WriteLine com [ModuleName] prefix para logging -- manter consistencia

### Integration Points
- `Game1.cs` Initialize/LoadContent/Update/Draw -- ponto central da refatoracao, delegar para SceneManager
- `TimeManager.OnDayAdvanced` event -- subscribers precisam funcionar dentro do contexto de scenes
- JSON serialization via System.Text.Json -- usar mesmo serializer para items.json

</code_context>

<specifics>
## Specific Ideas

- HotbarSlots e estado do jogador (persistido em save), nao configuracao de keybindings
- CropRegistry deve migrar para ItemRegistry unificado, nao coexistir como sistema separado
- v1 e prototipo mas deve escalar -- decisoes de arquitetura consideram Phases 2-6

</specifics>

<deferred>
## Deferred Ideas

None -- discussion stayed within phase scope

</deferred>

---

*Phase: 01-architecture-foundation*
*Context gathered: 2026-04-10*
