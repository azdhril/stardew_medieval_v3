# Requirements: Stardew Medieval

**Defined:** 2026-04-10
**Core Value:** O loop central deve ser satisfatorio: cuidar da fazenda -> explorar/lutar -> voltar com loot -> evoluir -> desbloquear mais conteudo.

## v1 Requirements

Requirements para o v1 jogavel. Cada um mapeia para fases do roadmap.

### Architecture

- [x] **ARCH-01**: SceneManager gerencia transicao entre scenes (Farm, Village, Dungeon) com fade in/out
- [x] **ARCH-02**: Entity base class com posicao, sprite, colisao, compartilhada por Player/Enemy/NPC
- [x] **ARCH-03**: Unified ItemDefinition model para crops, tools, weapons, armor, consumables e loot
- [x] **ARCH-04**: GameState reestruturado para suportar inventario, XP, quest state, gold, scene atual
- [x] **ARCH-05**: Game1.cs refatorado para delegar logica para scenes ao inves de coordenar diretamente

### Farming (Polish)

- [x] **FARM-01**: Posicao correta do player ao arar, semear e regar (fix visual do crop system)
- [x] **FARM-02**: Sprites adequados para colheita (substituir overlays coloridos por sprites reais)
- [x] **FARM-03**: Farming integrado ao novo sistema de inventario (sementes/colheita vao para o inventario)

### Inventory & Items

- [x] **INV-01**: Inventory grid com 20 slots, suporte a stacking de consumiveis
- [x] **INV-02**: Hotbar com 8 slots acessiveis por number keys (1-8)
- [x] **INV-03**: Equipment slots separados (arma, armadura) que afetam combat stats
- [x] **INV-04**: Sistema de raridade de itens (common/uncommon/rare) com cores distintas e stat multipliers
- [x] **INV-05**: Itens dropados no chao com magnetismo -- puxa para o player a partir de certa distancia (estilo Stardew)

### Combat

- [x] **CMB-01**: Ataque melee direcional com espada na direcao que o player olha, com knockback ao acertar
- [x] **CMB-02**: Pelo menos 1 magia ranged (projectile com velocidade, colisao com inimigos, cooldown/mana)
- [x] **CMB-03**: Sistema de HP para player e inimigos com barras de vida visiveis
- [x] **CMB-04**: 3 tipos de inimigo com IA basica: melee rusher (skeleton), ranged caster (mage), slow tank (golem)
- [x] **CMB-05**: Enemy AI com estados: Idle -> Patrol -> Chase -> Attack -> Return
- [x] **CMB-06**: Boss fight com ataques telegrafados (wind-up antes do strike) e loot unico

### Dungeon

- [ ] **DNG-01**: 1 dungeon completa com 5-8 salas conectadas (linear com salas opcionais)
- [ ] **DNG-02**: Progressao de sala: matar todos inimigos para abrir porta para proxima sala
- [ ] **DNG-03**: Baus de tesouro em salas opcionais com loot aleatorio
- [ ] **DNG-04**: Boss room como sala final da dungeon

### World & Navigation

- [x] **WLD-01**: Map transitions entre Farm <-> Village <-> Dungeon com fade to black
- [x] **WLD-02**: Vila minima: mapa com castelo do Rei e 1 loja
- [x] **WLD-03**: Trigger zones em bordas/portas para ativar transicoes de mapa
- [x] **WLD-04**: Estado do player preservado entre transicoes de mapa

### NPCs & Dialogue

- [x] **NPC-01**: Sistema de dialogo com caixa de texto e retrato do NPC, avanca com botao
- [x] **NPC-02**: Rei NPC no castelo que da a missao principal ("limpar a dungeon")
- [ ] **NPC-03**: Shopkeeper NPC com UI de compra/venda (sementes, pocoes, equipamento basico)
- [x] **NPC-04**: NPCs com estado de quest (sem missao, missao ativa, missao completa) que altera dialogo

### Progression

- [ ] **PRG-01**: Sistema de XP -- matar inimigos da XP, threshold crescente por level
- [ ] **PRG-02**: Level up concede +HP, +damage, +stamina (10-15 levels para conteudo v1)
- [ ] **PRG-03**: Sistema de gold -- moeda dropa de inimigos e vem de venda de crops/itens
- [ ] **PRG-04**: Consequencia de morte: perde 10% do gold + chance aleatoria de perder 1 item do inventario/equipamento, respawn na fazenda

### UI/HUD

- [ ] **HUD-01**: HUD grafica com sprites: barra de HP, barra de stamina, hotbar com icones, relogio/dia
- [x] **HUD-02**: Inventory UI abrivel (tecla I ou similar) mostrando grid + equipment slots
- [ ] **HUD-03**: Shop UI com lista de itens, precos, botoes comprar/vender
- [ ] **HUD-04**: Quest tracker simples mostrando missao ativa e objetivo atual
- [x] **HUD-05**: Caixa de dialogo estilizada com retrato do NPC e texto com avanco

### Save System

- [ ] **SAV-01**: Save/load estendido para inventario, equipment, XP/level, gold, quest state, scene atual
- [ ] **SAV-02**: Migracao de versao do save file para nao quebrar saves existentes

## v2 Requirements

Diferidos para futuras releases. Nao estao no roadmap atual.

### Classes & Especializacao

- **CLS-01**: Escolha de classe (swordsman, mage, archer) com skill trees diferentes
- **CLS-02**: Skills especificas por classe desbloqueáveis por level/uso

### Craft & Mineracao

- **CRF-01**: Sistema de craft com receitas desbloqueáveis
- **CRF-02**: Sistema de mineracao com minerio para craft
- **CRF-03**: Forja/bancada de craft como estacao na fazenda

### Social & Narrativa

- **SOC-01**: Sistema de relacionamento com NPCs (amizade, casamento)
- **SOC-02**: Multiplos reinos com migracao ao casar com principe/princesa
- **SOC-03**: Sistema de honra/reputacao perante o povo

### Automacao

- **AUT-01**: Pupilos de treinamento como ajudantes da fazenda (mid/late game)

### Mundo Expandido

- **WLD-05**: Pesca com minigame por estacao
- **WLD-06**: Montarias para locomocao e batalhas
- **WLD-07**: Eventos do reino com acesso a NPCs/itens especiais
- **WLD-08**: Consequencias de guerra (fazenda destruida, NPCs mortos)

### Multiplayer

- **MUL-01**: Co-op para ate 4 jogadores (PC)
- **MUL-02**: Port para mobile (single player)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Crafting system | Scope trap #1 para o genero -- loja supre a necessidade no v1 |
| Procedural dungeons | Dungeon hand-crafted e mais controlavel e suficiente pro v1 |
| Multiplas dungeons | 1 dungeon completa e suficiente para validar o loop |
| Dialogue trees complexas | Dialogo linear atende v1, branching e v2 |
| Stealth/furtividade | Nice to have, nao essencial pro core loop |
| Equipment visivel no sprite | Complexidade de arte alta, stats invisiveis funcionam pro v1 |
| Clima/weather system | Estacoes ja existem, clima e polish extra |
| Companion/pet system | Complexidade de IA extra, v2+ |
| Achievements | Polish, nao gameplay |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| ARCH-01 | Phase 1 | Satisfied |
| ARCH-02 | Phase 1 | Satisfied |
| ARCH-03 | Phase 1 | Satisfied |
| ARCH-04 | Phase 1 | Satisfied |
| ARCH-05 | Phase 1 | Satisfied |
| FARM-01 | Phase 2 | Satisfied |
| FARM-02 | Phase 2 | Satisfied |
| FARM-03 | Phase 2 | Satisfied |
| INV-01 | Phase 2 | Satisfied |
| INV-02 | Phase 2 | Satisfied |
| INV-03 | Phase 2 | Satisfied |
| INV-04 | Phase 2 | Satisfied |
| INV-05 | Phase 2 | Satisfied |
| HUD-02 | Phase 2 | Satisfied |
| CMB-01 | Phase 3 | Satisfied |
| CMB-02 | Phase 3 | Satisfied |
| CMB-03 | Phase 3 | Satisfied |
| CMB-04 | Phase 3 | Satisfied |
| CMB-05 | Phase 3 | Satisfied |
| CMB-06 | Phase 3 | Satisfied |
| WLD-01 | Phase 4 | Complete |
| WLD-02 | Phase 4 | Complete |
| WLD-03 | Phase 4 | Complete |
| WLD-04 | Phase 4 | Complete |
| NPC-01 | Phase 4 | Complete |
| NPC-02 | Phase 4 | Complete |
| NPC-03 | Phase 4 | Pending |
| NPC-04 | Phase 4 | Complete |
| HUD-03 | Phase 4 | Pending |
| HUD-05 | Phase 4 | Complete |
| DNG-01 | Phase 5 | Pending |
| DNG-02 | Phase 5 | Pending |
| DNG-03 | Phase 5 | Pending |
| DNG-04 | Phase 5 | Pending |
| PRG-01 | Phase 6 | Pending |
| PRG-02 | Phase 6 | Pending |
| PRG-03 | Phase 6 | Pending |
| PRG-04 | Phase 6 | Pending |
| HUD-01 | Phase 6 | Pending |
| HUD-04 | Phase 6 | Pending |
| SAV-01 | Phase 6 | Pending |
| SAV-02 | Phase 6 | Pending |

**Coverage:**
- v1 requirements: 42 total
- Mapped to phases: 42
- Unmapped: 0

---
*Requirements defined: 2026-04-10*
*Last updated: 2026-04-12 after Phase 3.1 verification backfill*
