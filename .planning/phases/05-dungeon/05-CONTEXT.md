# Phase 5: Dungeon — Context

**Gathered:** 2026-04-14
**Status:** Ready for planning

<domain>
## Phase Boundary

Entregar uma dungeon completa e jogável de ponta a ponta: o jogador entra a partir da vila, progride por 7 salas conectadas (4 principais + 2 opcionais + boss), combate inimigos para abrir portas, abre baús em salas opcionais com drag-and-drop para o inventário, e derrota o boss na sala final. Morte reseta a run.

Atende: DNG-01, DNG-02, DNG-03, DNG-04.

**Fora de escopo** (pertence a outras fases):
- XP/level up, penalidade de gold/item por morte → Phase 6
- Multiplas dungeons/andares → pós-v1.0
- HUD gráfico completo (HP bar etc.) → Phase 6

</domain>

<decisions>
## Implementation Decisions

### Estrutura do mapa
- **D-01:** Um arquivo TMX por sala (7 TMXs). Transições entre salas usam `TriggerZone` e troca de cena, consistente com Farm→Village→Castle.
- **D-02:** Layout linear: 4 salas principais obrigatórias em sequência + 2 salas opcionais como desvios laterais (saem e voltam à rota principal) + 1 boss room no final. Total: 7 salas.
- **D-03:** Mapas hand-authored (Tiled), não procedurais.

### Porta / gate de progressão
- **D-04:** Cada sala principal tem uma porta com dois estados visuais: **fechada** (bloqueia collision + transição) e **aberta** (libera passagem). Sprite-swap simples, sem necessidade de animação elaborada.
- **D-05:** Porta abre automaticamente quando todos os inimigos da sala são derrotados (listener no `CombatManager`/`EnemySpawner` da sala → dispara `OnRoomCleared` → troca sprite + habilita TriggerZone de saída).
- **D-06:** Salas opcionais **não são gated** — entrada livre; o baú lá dentro é o incentivo.

### Baús e loot
- **D-07:** Um baú por sala opcional → **2 baús totais** na dungeon. Drop do boss é separado (garantido) e não usa baú.
- **D-08:** **Interação do baú:** jogador pressiona **E** perto do baú → abre overlay que **pausa o gameplay** e mostra o inventário do jogador lado-a-lado com os slots do baú.
- **D-09:** **Coleta:** drag-and-drop dos slots do baú para o inventário do jogador (reusa o sistema de inventário existente). Itens não coletados ficam no baú e persistem se o jogador reabrir (até a dungeon resetar).
- **D-10:** Baú abre 1x por run (sprite muda para "aberto"). Conteúdo gerado na entrada da dungeon via `LootTable` (reusa sistema da Phase 3).

### Entrada, boss e morte
- **D-11:** Entrada da dungeon é um `TriggerZone` em ponto visível no `village.tmx` (ex: caverna/portão). Zero diálogo — mesma UX da transição Farm↔Village.
- **D-12:** Boss room tem porta própria que só abre após todas as 4 salas principais estarem limpas. Fluxo natural do layout linear garante isso, mas a porta do boss checa o flag `room_N_cleared` para todas as principais.
- **D-13:** Morte na dungeon → respawn na fazenda com HP cheio + **dungeon reseta completamente** (inimigos respawnam, baús refecham, portas refecham, loot re-rolla). Penalidade de gold/item fica para Phase 6.
- **D-14:** Derrotar o boss = dungeon completa. Boss solta loot garantido como `ItemDropEntity` normal (não baú). Saída volta pra fazenda (ou vila — planner decide).

### Claude's Discretion
- Tileset/arte das salas (medieval/caverna — usar o que encaixar no estilo existente)
- Variedade de inimigos por sala (reusar tipos existentes da Phase 3; quantidade e mix ficam a cargo do balanceamento no planner)
- Decoração e props das salas
- Nome exato dos arquivos TMX (ex: `dungeon_r1.tmx`, `dungeon_boss.tmx`)
- Posição exata da entrada na vila (qualquer ponto visual coerente)
- Som/SFX (baú, porta, boss) — se houver tempo

</decisions>

<specifics>
## User-referenced specifics

- **Baú overlay:** inspiração estilo Minecraft/Terraria — "pausa o jogo atrás, abre o inventário ao lado da tela com os slots do baú e ele coleta o que ele quiser arrastando pro inventário".
- **Transição de cena:** padrão já estabelecido em `FarmScene`/`VillageScene`/`CastleScene` + `TriggerZone`.

</specifics>

<canonical_refs>
## Canonical References

- `.planning/ROADMAP.md` — Phase 5 success criteria
- `.planning/REQUIREMENTS.md` — DNG-01..04
- `.planning/phases/03-combat/03-CONTEXT.md` — EnemySpawner, LootTable, combat system
- `.planning/phases/04-world-npcs/04-CONTEXT.md` — scene transitions, TriggerZone pattern
- `src/Combat/EnemySpawner.cs`, `src/Combat/LootTable.cs`, `src/Combat/CombatManager.cs`
- `src/World/TriggerZone.cs`, `src/World/TileMap.cs`
- `src/Scenes/` — padrão SceneManager + overlays (ex: `ShopOverlayScene.cs` pra inspiração do ChestOverlay)
- `src/Inventory/` — sistema de inventário atual (base para chest overlay drag-and-drop)

</canonical_refs>

<deferred>
## Deferred Ideas (not this phase)

- Múltiplas dungeons / andares
- Dificuldade escalável / modo NG+
- Loot raro condicional (sem matar dano / speedrun)
- Sons e música ambiente da dungeon
- Cutscene de boss
- Penalidade de morte (gold/item) → Phase 6

</deferred>

## Next Steps

1. Run `/gsd-plan-phase 5` to generate research + PLAN.md files.
2. Planner deve produzir plans cobrindo: (a) infraestrutura de dungeon (scenes, room-clear event, door state), (b) baús + chest overlay UI, (c) boss room + vitória + death reset + entrada na vila.
