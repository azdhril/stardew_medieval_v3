# Stardew Medieval

## What This Is

Um jogo top-down medieval fantasy que combina simulação de fazenda (estilo Stardew Valley) com exploração de dungeons e combate RPG (estilo Tibia). O jogador é um falso herói que precisa cuidar de sua fazenda, evoluir como guerreiro, e atender missões do Rei — tudo isso em um mundo relaxante mas desafiador. Feito em C#/MonoGame para facilitar desenvolvimento assistido por IA.

## Core Value

O loop central deve ser satisfatório: cuidar da fazenda → explorar/lutar → voltar com loot → evoluir → desbloquear mais conteúdo. Se esse ciclo não for divertido, nada mais importa.

## Requirements

### Validated

Capacidades já existentes no código atual:

- ✓ Player movement com colisão baseada em polígonos — existing
- ✓ Tilemap rendering via Tiled (.tmx/.tsx) — existing
- ✓ Sistema de crops básico (till, water, plant, grow, harvest) — existing
- ✓ Ciclo dia/noite com overlay de escuridão — existing
- ✓ Sistema de stamina com consumo por ação — existing
- ✓ Sistema de estações (spring, summer, fall, winter) — existing
- ✓ Save/load via JSON com migração de versão — existing
- ✓ Camera follow com limites do mapa — existing
- ✓ HUD básica (stamina, tempo, controles) — existing

### Active

- [ ] HUD gráfica com sprites (substituir texto puro por UI visual estilo Stardew)
- [ ] Sistema de inventário com slots e equipamento
- [ ] Hotbar para ferramentas/armas e hotbar separada para poções
- [ ] Fix visual do crop system (posição do player arando/semeando/regando, sprites de colheita)
- [ ] Sistema de combate melee (espada com ataque direcional)
- [ ] Sistema de magia básico (pelo menos 1 magia à distância)
- [ ] Criaturas com IA básica (patrol, chase, attack)
- [ ] 1 dungeon completa com múltiplas salas e progressão
- [ ] Boss fight no final da dungeon
- [ ] Sistema de loot (drop de itens e ouro das criaturas)
- [ ] Vila mínima: castelo com Rei NPC + 1 loja
- [ ] Sistema de diálogo com NPCs
- [ ] Missão do Rei como fio condutor (receber missão → completar dungeon → reportar)
- [ ] Sistema de XP/level básico para progressão do personagem
- [ ] Transição entre mapas (fazenda → vila → dungeon)

### Out of Scope

- Multiplayer — complexidade alta, v1 é single player
- Mobile — PC first, mobile é futuro distante
- Múltiplos reinos — mecânica de expansão pós-v1
- Casamento/relacionamentos — sistema social complexo, v2+
- Pupilos/ajudantes de fazenda — mid/late game, não v1
- Sistema de montarias — nice to have, não essencial pro loop central
- Sistema de honra/reputação — mecânica profunda, v2+
- Craft avançado — v1 terá compra na loja, craft é v2
- Mineração — sistema completo de mineração é v2
- Pesca — sistema completo de pesca é v2
- Classes/especialização — v1 tem combate genérico, classes são v2
- Guerra entre reinos — conteúdo narrativo pós-v1
- Eventos do reino — conteúdo endgame

## Context

### Codebase Existente
Projeto brownfield em C# 12 / .NET 8.0 / MonoGame 3.8 (DesktopGL). Já tem sistemas funcionais de farming, tilemap (TiledCS), player movement com colisão, ciclo dia/noite, stamina, e save/load. A arquitetura é layered com Game1.cs como coordenador central. O código precisa de polish visual mas a base lógica funciona.

### Decisão MonoGame vs Unity
Unity foi descartado porque a experiência de desenvolvimento assistido por IA é ruim — muita coisa depende de interação manual com a HUD do Unity. MonoGame permite que todo o desenvolvimento seja feito via código, ideal para assistência de IA.

### Assets
Mix de assets: alguns prontos, outros precisam ser buscados/criados. Pixel art medieval é o estilo visual.

### Referências
- **Stardew Valley**: farming, atmosfera relaxante, loop de gameplay, longevidade
- **Tibia**: combate, loot com raridade, inventário, progressão de personagem

### Problemas Conhecidos
- Crop system tem problemas visuais (posição incorreta de ações, overlays estranhos na colheita)
- HUD é só texto renderizado, precisa virar UI gráfica
- Não há sistema de inventário
- Não há sistema de combate
- Não há criaturas/enemies
- Não há dungeon/exploração

## Constraints

- **Engine**: MonoGame 3.8 DesktopGL — sem trocar engine
- **Linguagem**: C# 12 / .NET 8.0 — manter stack existente
- **Mapas**: Tiled (.tmx/.tsx) via TiledCS — manter pipeline de mapas
- **Resolução**: 960x540 base — manter configuração atual
- **Assets**: Pixel art medieval — estilo visual consistente
- **Plataforma v1**: Windows PC only

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| MonoGame ao invés de Unity | Melhor para dev assistido por IA (tudo via código) | — Pending |
| Fazenda + 1 Dungeon como escopo v1 | Loop central completo sem escopo excessivo | — Pending |
| Combate melee + magia | Variedade no combate desde o v1 | — Pending |
| Vila mínima (Rei + loja) | Suficiente pro fio condutor sem overscope | — Pending |
| Sem craft no v1 (compra na loja) | Reduz complexidade, craft é v2 | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-04-10 after initialization*
