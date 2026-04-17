# Stardew Medieval

## What This Is

Um jogo top-down medieval fantasy que combina simulação de fazenda (estilo Stardew Valley) com exploração de dungeons e combate RPG (estilo Tibia). v1.0 entrega o loop completo: fazenda → combate → loot → progressão → missão do Rei completada. Feito em C# 12 / MonoGame 3.8 DesktopGL.

## Current State

**Shipped:** v1.0 MVP (2026-04-17) — 8 phases, 29 plans, 42/42 requirements satisfied.

Core loop entregue end-to-end: farm → harvest → sell → shop → dungeon (7 salas + boss) → XP/gold → level up → sleep/save → relaunch preserva progresso. Save v9 com migração cumulativa v1→v9. HUD pixel-art com NineSlice. Três fluxos E2E (core loop, death, quest) verificados por integration checker.

## Core Value

O loop central deve ser satisfatório: cuidar da fazenda → explorar/lutar → voltar com loot → evoluir → desbloquear mais conteúdo. **Confirmado em v1.0** — todos os fluxos E2E fecham.

## Next Milestone Goals (v1.1)

Foco em **polish visual e profundidade de personagem** após o loop estar provado.

- **Phase 7 — Animation System & Mana Seed Integration:** refactor Entity/animation system para animações nomeadas com timing variável por frame, paper-doll com camadas (body/hair/outfit/weapon), integração do pack Mana Seed (8×8 sheets 64×64) substituindo o layout 4×4 atual. Base para customização de personagem.
- **Tech debt de v1.0** a fechar:
  - `SlashEffect` wire-up em `DungeonScene` (CMB-01 visual polish)
  - Nyquist VALIDATION.md para phases 01/02/03.1/04/04.1/06

## Requirements

### Validated (v1.0)

Todas as 42 capacidades v1 estão implementadas e verificadas — ver [milestones/v1.0-REQUIREMENTS.md](milestones/v1.0-REQUIREMENTS.md):

- ✓ ARCH-01..05 — Architecture Foundation (SceneManager, Entity, ItemDefinition, GameState, Game1 refactor) — v1.0
- ✓ FARM-01..03 — Farming polish + inventário integrado — v1.0
- ✓ INV-01..05 — Inventory 20 slots + hotbar + equipment + rarity + drop magnetism — v1.0
- ✓ CMB-01..06 — Combate melee + magia + HP + 3 inimigos + AI + boss — v1.0
- ✓ WLD-01..04 — Transições de mapa + vila + trigger zones + state preservation — v1.0
- ✓ NPC-01..04 — Diálogo + Rei + shopkeeper + quest-state branching — v1.0
- ✓ DNG-01..04 — Dungeon 7 salas + combat-gated + chests + boss room — v1.0
- ✓ PRG-01..04 — XP + level-up + gold + death penalty — v1.0
- ✓ HUD-01..05 — HUD gráfica + inventory UI + shop UI + quest tracker + dialogue box — v1.0
- ✓ SAV-01..02 — Save estendido + migração de versão (v1→v9) — v1.0

### Active (v1.1 target)

- [ ] Animation system com timing variável por frame
- [ ] Paper-doll layered sprites (body/hair/outfit/weapon)
- [ ] Mana Seed pack integration (8×8 sheets, 64×64)
- [ ] SlashEffect em DungeonScene (v1.0 debt)
- [ ] Nyquist VALIDATION.md completion

### Out of Scope

| Feature | Reason |
|---------|--------|
| Crafting system | Loja supre a necessidade; craft é v2 |
| Procedural dungeons | Hand-crafted é suficiente e mais controlável |
| Múltiplas dungeons | 1 dungeon já valida o loop |
| Dialogue trees complexas | Branching é v2 |
| Stealth/furtividade | Não essencial pro core loop |
| Equipment visível no sprite | Será reavaliado após Phase 7 (animation system destrava isso) |
| Weather system | Estações já existem, clima é polish extra |
| Companion/pet system | IA extra, v2+ |
| Achievements | Polish, não gameplay |
| Classes/skill trees | v2 após animation system |
| Multiplayer / mobile | v2+ |
| Mineração / pesca | v2 |

## Context

### Codebase Atual (pós v1.0)

Projeto C# 12 / .NET 8.0 / MonoGame 3.8 DesktopGL. Arquitetura baseada em Scenes (SceneManager + stack-based transitions + fade), Entity base class, ServiceContainer para deps. GameState v9 serializado via JSON. ~50+ tests em xUnit cobrem progression, save migration, death penalty. 960x540 base resolution.

### Decisão MonoGame vs Unity

Unity descartado — DX de dev assistido por IA é ruim (muita interação manual com HUD do Unity). MonoGame permite tudo via código. **Validado em v1.0** — 8 phases planejadas + executadas sem fricção.

### Assets

Mix: crops + player existentes, enemies/boss/UI pixel-art adicionados em v1.0. Mana Seed pack (próxima fase) traz animação completa e base para paper-doll.

### Referências

- **Stardew Valley** — farming, atmosfera relaxante, loop de gameplay, longevidade
- **Tibia** — combate, loot com raridade, inventário, progressão de personagem

## Constraints

- **Engine:** MonoGame 3.8 DesktopGL — sem trocar engine
- **Linguagem:** C# 12 / .NET 8.0
- **Mapas:** Tiled (.tmx/.tsx) via TiledCS
- **Resolução:** 960x540 base
- **Assets:** Pixel art medieval
- **Plataforma v1:** Windows PC only

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| MonoGame ao invés de Unity | Melhor para dev assistido por IA (tudo via código) | ✓ Good — 8 phases sem fricção |
| Fazenda + 1 Dungeon como escopo v1 | Loop central completo sem overscope | ✓ Good — loop fechou em 29 plans |
| Combate melee + magia desde v1 | Variedade sem overscope | ✓ Good — CMB-01..06 todos satisfied |
| Vila mínima (Rei + loja) | Fio condutor sem overscope narrativo | ✓ Good — quest fluxo funciona |
| Sem craft no v1 (compra na loja) | Reduz complexidade | ✓ Good — shop supriu o gap |
| Phase 3.1 inserida (verification backfill) | Drift de metadata durante execução | ✓ Good — 15 reqs promovidos a satisfied |
| Phase 4.1 inserida (shop UX + save gap) | Blocker de save persistence + UX regression | ✓ Good — UX fechada |
| Gold_Coin bypass inventory slots | Evita gold consumir espaço | ✓ Good — core loop limpo |
| Toast shared via ServiceContainer | Mensagens de death survivem scene transition | ✓ Good |
| Mana Seed pack adiado p/ v1.1 | Refactor grande, não bloqueia core loop | ⏳ Pending (Phase 7) |

## Evolution

Este documento evolui em transições de fase e fronteiras de milestone.

- **Transição de fase** (`/gsd-transition`): requirements invalidados/validados, decisões novas, "What This Is" drift.
- **Fim de milestone** (`/gsd-complete-milestone`): revisão full, Core Value check, Out of Scope audit, Context update.

---
*Last updated: 2026-04-17 after v1.0 milestone*
