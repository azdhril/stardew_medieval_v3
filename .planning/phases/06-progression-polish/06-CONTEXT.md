# Phase 6: Progression & Polish — Context

**Gathered:** 2026-04-16
**Status:** Ready for planning

<domain>
## Phase Boundary

Fechar o core loop do v1.0: matar inimigo → ganhar XP + gold → subir de level → comprar gear → voltar pra dungeon → morrer tem consequência. HUD comunica tudo isso graficamente, save guarda tudo e o jogo se comporta como um jogo "de verdade" (fecha/reabre sem perder progresso).

Atende: **PRG-01, PRG-02, PRG-03, PRG-04, HUD-01, HUD-04, SAV-01, SAV-02**.

**Fora de escopo** (não pertence a Phase 6):
- Sistema de mana (v2 — hoje Mana bar é placeholder full)
- Skill trees / classes (v2)
- Múltiplas quests (v2 — `MainQuest` é único por design em Phase 4)
- Áudio / SFX de level-up (não há sistema de som no projeto)
- Refactor de animação (Phase 7 separada)

</domain>

<decisions>
## Implementation Decisions

### XP & Leveling (PRG-01, PRG-02)
- **D-01:** XP por tier de inimigo (flat por tipo): **Skeleton 10, Dark Mage 15, Golem 25, Skeleton King 150**. Alinha com os HPs existentes (40/30/120/300 — Phase 3 D-16..21) e faz o primeiro boss kill sentir-se como um milestone.
- **D-02:** **Level cap = 100** (teto teórico). Intenção: jogador normal termina dungeon em ~level 10–15. Level 100 é "fim de tudo" pós-v1 — não existe conteúdo v1 que exija níveis altos, é só headroom narrativo.
- **D-03:** **Curva de XP exponencial** desenhada para que o boss (150 XP) seja derrotável em ~level 8–12. Planner decide a fórmula exata (ex.: `XP_to_next(n) = 50 * 1.25^(n-1)`), desde que: (a) level 2 custa ~50 XP, (b) level 10 seja atingível fazendo dungeon + farming casual, (c) level 100 seja inalcançável em v1.
- **D-04:** **Stats por level:** `+10 HP`, `+1 damage bonus`, `+5 Max Stamina`. Bônus de damage soma ao damage da arma equipada. HP/Stamina enchem até o novo máximo ao subir.
- **D-05:** **Fonte de XP:** apenas morte de inimigos (kill attribution no `CombatManager` / `BossEntity`). Crops e quest NÃO dão XP no v1 (deferred pra v2).

### Level-Up Feedback
- **D-06:** **Banner "LEVEL UP! Lv X"** top-center, ~1.5s, fade in/out. Texto grande, dourado.
- **D-07:** **Particle burst dourado** no sprite do player no momento do level-up (partículas simples, reaproveita pixel textura + fade).
- **D-08:** **Sem pause** — gameplay continua. Sem SFX (não há sistema de áudio). HP/Stamina enchem visivelmente nos bars (feedback implícito).

### Gold Drops (PRG-03)
- **D-09:** **Gold drop = `ItemDropEntity`** (coin), mesmo sistema de magnetismo da Phase 2 (D-08..10). Coin item novo em `items.json` (ex.: `Gold_Coin`) com sprite de moeda do kit UI. Consistência com pickup de itens.
- **D-10:** **Gold amounts por tier:** Skeleton 5g, Dark Mage 8g, Golem 15g, Skeleton King 100g. Cada kill rola variance de **±30%** (ex.: skeleton 3–7). Dungeon run completa rende ~100–250g.
- **D-11:** **Todo inimigo garante drop** (100%). Boss garante pilha grande. Sem chance de "nada" — economy previsível.
- **D-12:** **Pickup:** coin toca no player (magnetismo) → `Inventory.AddGold(amount)` + floating text "+N gold" leve no HUD ou no player. Coin é consumível (não ocupa slot de inventário).

### Death Penalty (PRG-04)
- **D-13:** **Todas as mortes penalizam** — farm e dungeon. Dungeon continua resetando a run (Phase 5 D-13); a penalidade de gold/item acontece ANTES do reset/respawn.
- **D-14:** **Gold:** perde **10% do gold atual** (floor, mínimo 0, sem dívida).
- **D-15:** **Item loss rolls:** rola uma vez — **25% chance: perde 1 item aleatório**; **15% chance: perde 2 itens aleatórios**; 60% não perde item. Pool inclui **qualquer slot** (inventory + hotbar + equipment). Itens de quest (se existirem no v1) ficam protegidos — por enquanto nenhum item tem a flag "quest", então n/a.
- **D-16:** **Respawn** = farm center (tile 10,10), HP cheio, stamina cheia. Dungeon já tinha esse fluxo — agora passa pela penalty-and-save pipeline primeiro.
- **D-17:** **UX de perda:** banner vermelho "You died" (~1.5s) + Toast(s) listando perdas: "Lost 12 gold", "Lost: Iron Sword", "Lost: Mana Potion". Reusa `Toast.cs` existente.

### HUD Graphical Polish (HUD-01, HUD-04)
- **D-18:** Elementos que ganham pass gráfico nesta fase:
  - **XP bar + level number (novo)** — thin bar acima do hotbar panel, full-width dentro do painel. "Lv X" à esquerda.
  - **Clock/day panel** — substituir "Day 5  12PM" texto puro por um painel estilo NineSlice com ícone de relógio + label. Posição atual (top-left) mantida.
  - **Gold label** — substituir texto dourado por painel pequeno com ícone de moeda (`UI_Icon_Sys_Gold.png`) + número.
  - **Quest tracker** — aplicar NineSlice panel + ícone de pergaminho/quest à esquerda do texto. Cores/estados (NotStarted/Active/Complete) preservados.
  - **HP/MP/STA labels in-bar** — texto sobre as barras está feio; remover o texto ou substituir por tooltip on-hover / label mais discreto (NineSlice mini-panel). **Claude's Discretion** entre as três opções — priorizar legibilidade pixel-art.
- **D-19:** **Reuso obrigatório:** `UI/NineSlice.cs`, `UI/UITheme.cs`, `assets/Sprites/System/UI Elements/Bars/Progress/UI_Progress_Style1_*` (para XP bar — sugestão Style1_Fill_Yellow ou Purple), `assets/Sprites/System/Icons/System/UI_Icon_Sys_Gold.png`. Consistência com o visual das HUDs de Inventory/Shop/Chest (já polidas em quick tasks 260416-10o).

### Save Scope & Migration (SAV-01, SAV-02)
- **D-20:** **Periodic auto-save** — novo trigger: `GameStateSnapshot.SaveNow()` a cada **~30 segundos de gameplay** (tempo real, não game-time). Implementação via timer simples em `Game1`/`SceneManager`. Objetivo: comportamento "jogo normal" — fechar a janela a qualquer momento não perde mais que 30s de progresso.
- **D-21:** **Perf guard:** se o save tomar >50ms em profiling, planner adiciona (a) write-on-dirty (só salva se `GameState` mudou desde último save), ou (b) async file write. Monitor via `[GameStateSnapshot]` log timing.
- **D-22:** **Triggers existentes mantidos:** day-advance, shop close, boss victory, pre-death, F5 manual. **Adicionais novos:** level-up, post-death (após aplicar penalty e respawn).
- **D-23:** **Save version v8 → v9** para campos novos:
  - `MaxHP` (hoje está na `PlayerEntity`/`Entity` em runtime, mas GameState não persiste — precisa persistir agora pra level scaling sobreviver load)
  - `MaxStamina` (mesmo motivo)
  - `BaseDamageBonus` (do leveling; `+1 per level`)
  - Migração v8→v9: derive `MaxHP/MaxStamina/BaseDamageBonus` do `Level` existente (saves v8 têm Level=1 → defaults iniciais).
- **D-24:** **Respawn location = farm center fixo**. Não salva "última posição segura". Simples e previsível.

### Claude's Discretion
- Fórmula exata da curva de XP (desde que respeite D-03).
- Posição exata do banner "LEVEL UP!" (top-center sugerido, mas afinar).
- Visual do particle burst (partículas spawned por Entity ou overlay em `src/Combat/SlashEffect.cs` pattern — qualquer um serve).
- Cor específica da XP bar (Yellow/Purple/Orange do kit Progress).
- Como lidar com texto HP/MP/STA "feio" sobre os bars (D-18 tail): remover completamente, reduzir tamanho, mover para dentro de mini-panel, ou tooltip — Claude escolhe o que ficar mais limpo.
- Interval exato do periodic save (30s é target — pode afinar 20–60s).
- Coin sprite: qual das moedas do kit UI (`assets/Sprites/System/Icons/System/UI_Icon_Sys_Gold.png` ou variante).
- Variance exata do gold drop (±30% é target, pode ser ±25% ou ±40%).
- Design do item loss rolling (1 roll com buckets 60/25/15 vs 2 rolls independentes) — desde que as probabilidades batam.
- Strings exatas dos Toasts ("Lost 12 gold" vs "-12 gold" etc.).

### Folded Todos
Nenhum — nenhum todo pendente mapeou para Phase 6.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project-Level
- `CLAUDE.md` — C# 12, MonoGame 3.8, 960×540, pixel-art medieval.
- `.planning/PROJECT.md` — core loop (farm → fight → loot → evolve → unlock).
- `.planning/REQUIREMENTS.md` — PRG-01..04, HUD-01, HUD-04, SAV-01, SAV-02.
- `.planning/ROADMAP.md` §"Phase 6: Progression & Polish" — 5 success criteria.

### Prior Phase Contracts
- `.planning/phases/02-items-inventory/02-CONTEXT.md` — item drops + magnetism (D-08..10) — usado para Gold_Coin.
- `.planning/phases/02-items-inventory/02-VERIFICATION.md` — inventory contract.
- `.planning/phases/03-combat/03-CONTEXT.md` — death respawn contract (D-14), enemy HP tiers (D-16..21), loot table pattern.
- `.planning/phases/03-combat/03-VERIFICATION.md` — combat contract relevante para damage bonus.
- `.planning/phases/04-world-npcs/04-CONTEXT.md` — Gold via `InventoryManager`, quest tracker placeholder (D-13), HUD-04 deferred aqui.
- `.planning/phases/05-dungeon/05-CONTEXT.md` — dungeon death reset fluxo (D-13) — agora com penalty stacked.

### Codebase Maps
- `.planning/codebase/ARCHITECTURE.md`
- `.planning/codebase/CONVENTIONS.md`
- `.planning/codebase/STRUCTURE.md`

### Core Code Touchpoints (reusable/extend)
- `src/Core/GameState.cs` — já tem `XP`, `Level`, `Gold`; adicionar `MaxHP/MaxStamina/BaseDamageBonus` em v9.
- `src/Core/SaveManager.cs` — migration chain v1→v8; adicionar case v8→v9. `CURRENT_SAVE_VERSION = 9`.
- `src/Core/GameStateSnapshot.cs` — `SaveNow()` centralizado — novo chamador: periodic timer + level-up + post-death.
- `src/Inventory/InventoryManager.cs` — `AddGold/TrySpendGold/OnGoldChanged` — consumido pela pickup de coin e pela penalty.
- `src/Player/PlayerEntity.cs` + `src/Core/Entity.cs` — `HP/MaxHP/IsAlive`; adicionar hook pra level stat push.
- `src/Player/PlayerStats.cs` — `MaxStamina`; adicionar hook pra level stat push + event `OnMaxStaminaChanged` (consumer HUD).
- `src/Combat/CombatManager.cs` + `src/Combat/EnemyEntity.cs` + `src/Combat/BossEntity.cs` — `OnEnemyDeath` hook pra awardXP + gold drop.
- `src/Combat/LootTable.cs` — pode estender com `GoldMin/GoldMax` OU coin vira um `LootDrop("Gold_Coin", 1.0f)` especial. Planner decide.
- `src/Scenes/FarmScene.cs` L378–391 — player death path (hoje só heal+recenter); adicionar penalty+save+banner.
- `src/Scenes/DungeonScene.cs` L297–310 — dungeon death path (reset run); adicionar penalty ANTES do `BeginRun()`.
- `src/UI/HUD.cs` — HP/Mana/Stamina bars + gold label + quest tracker draw; adicionar XP bar + level + novos painéis NineSlice.
- `src/UI/NineSlice.cs` + `src/UI/UITheme.cs` — padrões de panel para clock/gold/quest/XP.
- `src/UI/Toast.cs` — reuso para mensagens de morte (Lost N gold, Lost item X).
- `src/Quest/MainQuest.cs` — `OnQuestStateChanged` já existe; tracker gráfico escuta.
- `src/Data/items.json` — adicionar `Gold_Coin` item (ItemType.Currency ou similar).

### Asset Paths (reusable)
- `assets/Sprites/System/UI Elements/Bars/Progress/UI_Progress_Style1_*.png` — XP bar fills.
- `assets/Sprites/System/Icons/System/UI_Icon_Sys_Gold.png` — gold icon (gold label + coin sprite).
- `assets/Sprites/System/UI Elements/Panel/` — NineSlice panels para clock/day/gold painéis.
- `assets/Sprites/System/UI Elements/Bars/Status/UI_StatusBar_*.png` — já consumidos em HUD (HP/Mana/Stamina).

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `InventoryManager.AddGold / TrySpendGold / OnGoldChanged` (src/Inventory/InventoryManager.cs:34–68): Gold API completa. Coin pickup e death penalty consomem direto.
- `GameStateSnapshot.SaveNow()` (src/Core/GameStateSnapshot.cs): ponto único de save, já chamado de múltiplas scenes. Periodic timer só precisa chamar isso.
- `SaveManager.MigrateIfNeeded` (src/Core/SaveManager.cs:67): chain de migração; v8→v9 segue o mesmo padrão dos v3→v8.
- `ItemDropEntity` + magnetismo (Phase 2): coin reusa esse pipeline — zero novo código de pickup.
- `LootTable.Roll` + `EnemyEntity.Loot` (Phase 3): hook de morte já existe; gold adiciona nele.
- `HUD` (src/UI/HUD.cs): sprite bars já funcionais; gold label e quest tracker são os "text panels" que precisam upgrade.
- `NineSlice` + `UITheme` (src/UI/): sistema de painel já estabelecido; XP bar + clock panel + gold panel usam.
- `MainQuest.OnQuestStateChanged` event (src/Quest/MainQuest.cs:17): tracker gráfico escuta esse evento.
- `Toast.cs` (src/UI/Toast.cs): já usado para "Purchased X" / "Not enough gold"; reusa para "Lost 12 gold" / "Lost: Iron Sword".
- Death check `if (!Player.IsAlive)` pattern já em FarmScene L378 e DungeonScene L298: hook point claro para penalty+save.
- Progress bar sprites em `UI Elements/Bars/Progress/` (Style1 + Style2, 10 cores): XP bar escolhe uma variante sem precisar de novo asset.

### Established Patterns
- Event-driven: `OnHourPassed`, `OnDayAdvanced`, `OnStaminaChanged`, `OnGoldChanged`, `OnQuestStateChanged` → adicionar `OnXPGained`, `OnLevelUp`, `OnMaxHPChanged`, `OnMaxStaminaChanged`.
- Try-prefix para ops falhaveis (`TryTill`, `TrySpendGold`, `TryBuy`) → `TryLevelUp` se check-and-apply fizer sentido.
- `[ModuleName]` Console.WriteLine logging → `[Progression]`, `[Save]`, `[DeathPenalty]`.
- Save migration chain: cada case bump só toca fields novos, nunca valida tudo.
- NineSlice + UITheme para todos painéis estilizados (padrão da Phase 4+Inventory/Shop redesigns).
- Sprite bars já abstraído em `HUD.DrawSpriteBar` — XP bar reusa o helper.

### Integration Points
- `src/Combat/EnemyEntity` / `BossEntity` death event → awards XP + spawns coin drop (ItemDropEntity).
- `src/Scenes/FarmScene` + `src/Scenes/DungeonScene` death paths → chama novo `DeathPenalty.Apply()` + `GameStateSnapshot.SaveNow()`.
- `Game1` (ou `SceneManager`) tick loop → periodic save timer (30s).
- `HUD` construção em `FarmScene` / `DungeonScene` / `VillageScene` / `CastleScene` / `ShopScene` — novo XP bar + painéis precisam de mesma inicialização em todas cenas com HUD.

</code_context>

<specifics>
## Specific Ideas

- **Cap 100 é narrativo, não gameplay** — v1 não tem conteúdo além do boss; level 100 seria "100% completion + side grinding". Planner deve respeitar isso: curva que leva ~50h pra chegar em 100, mas ~5–10h pra chegar no boss level.
- **Save "normal"** — usuário quer que seja como Minecraft / jogos modernos: fechou, reabriu, tá ali. Perder >30s de progresso é frustrante.
- **HP/MP/STA texto está feio** — prioridade de polish visual, não de função. Solução mínima aceitável: remover o texto. Preferível: mini-panel ou tooltip.
- **Gold drop = moedinha no chão** — consistência com Phase 2 "item no chão + magnetismo". Não é um número abstrato que pula pro inventário.
- **Boss kill = sentimento de progresso** — 150 XP + 100g é desenhado pra ser um milestone visível: ~1–2 levels garantidos + compra significativa na loja.
- **Death penalty "qualquer coisa"** — inclui arma/armadura equipada. Usuário foi explícito. Planner deve deixar isso claro na UX pra não virar "bug" reportado.

</specifics>

<deferred>
## Deferred Ideas

- XP de crops colhidos / quest completa → v2 (expande economy de XP).
- Skill trees / classes → v2 (CLS-01/02).
- Sistema de mana real (substituir placeholder bar) → v2.
- SFX / música de level-up / morte → futuro (sem audio system hoje).
- "Última posição segura" / save-and-quit em qualquer lugar → v2 (v1 usa farm-center-respawn).
- Quest items protegidos da death penalty → quando houver múltiplas quests com itens (Phase 4 deferred "full quest list").
- Rest/sleep-to-save NPC (estalagem) → deferred em Phase 4 pra cá, agora pra v2 (periodic save cobre o caso).
- Leveling visual no sprite do player (escala/aura) → polish v2.
- Achievement / milestone tracking → out of scope (REQUIREMENTS §Out of Scope).

### Reviewed Todos (not folded)
Nenhum — `todo match-phase` não surgiu matches.

</deferred>

---

*Phase: 06-progression-polish*
*Context gathered: 2026-04-16*
