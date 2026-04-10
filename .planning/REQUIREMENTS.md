# Requirements: Stardew Medieval

**Defined:** 2026-04-10
**Core Value:** O loop central deve ser satisfatório: cuidar da fazenda → explorar/lutar → voltar com loot → evoluir → desbloquear mais conteúdo.

## v1 Requirements

Requirements para o v1 jogável. Cada um mapeia para fases do roadmap.

### Architecture

- [ ] **ARCH-01**: SceneManager gerencia transição entre scenes (Farm, Village, Dungeon) com fade in/out
- [ ] **ARCH-02**: Entity base class com posição, sprite, colisão, compartilhada por Player/Enemy/NPC
- [ ] **ARCH-03**: Unified ItemDefinition model para crops, tools, weapons, armor, consumables e loot
- [ ] **ARCH-04**: GameState reestruturado para suportar inventário, XP, quest state, gold, scene atual
- [ ] **ARCH-05**: Game1.cs refatorado para delegar lógica para scenes ao invés de coordenar diretamente

### Farming (Polish)

- [ ] **FARM-01**: Posição correta do player ao arar, semear e regar (fix visual do crop system)
- [ ] **FARM-02**: Sprites adequados para colheita (substituir overlays coloridos por sprites reais)
- [ ] **FARM-03**: Farming integrado ao novo sistema de inventário (sementes/colheita vão para o inventário)

### Inventory & Items

- [ ] **INV-01**: Inventory grid com 20 slots, suporte a stacking de consumíveis
- [ ] **INV-02**: Hotbar com 8 slots acessíveis por number keys (1-8)
- [ ] **INV-03**: Equipment slots separados (arma, armadura) que afetam combat stats
- [ ] **INV-04**: Sistema de raridade de itens (common/uncommon/rare) com cores distintas e stat multipliers
- [ ] **INV-05**: Itens dropados no chão com magnetismo — puxa para o player a partir de certa distância (estilo Stardew)

### Combat

- [ ] **CMB-01**: Ataque melee direcional com espada na direção que o player olha, com knockback ao acertar
- [ ] **CMB-02**: Pelo menos 1 magia ranged (projectile com velocidade, colisão com inimigos, cooldown/mana)
- [ ] **CMB-03**: Sistema de HP para player e inimigos com barras de vida visíveis
- [ ] **CMB-04**: 3 tipos de inimigo com IA básica: melee rusher (skeleton), ranged caster (mage), slow tank (golem)
- [ ] **CMB-05**: Enemy AI com estados: Idle → Patrol → Chase → Attack → Return
- [ ] **CMB-06**: Boss fight com ataques telegrafados (wind-up antes do strike) e loot único

### Dungeon

- [ ] **DNG-01**: 1 dungeon completa com 5-8 salas conectadas (linear com salas opcionais)
- [ ] **DNG-02**: Progressão de sala: matar todos inimigos para abrir porta para próxima sala
- [ ] **DNG-03**: Baús de tesouro em salas opcionais com loot aleatório
- [ ] **DNG-04**: Boss room como sala final da dungeon

### World & Navigation

- [ ] **WLD-01**: Map transitions entre Farm ↔ Village ↔ Dungeon com fade to black
- [ ] **WLD-02**: Vila mínima: mapa com castelo do Rei e 1 loja
- [ ] **WLD-03**: Trigger zones em bordas/portas para ativar transições de mapa
- [ ] **WLD-04**: Estado do player preservado entre transições de mapa

### NPCs & Dialogue

- [ ] **NPC-01**: Sistema de diálogo com caixa de texto e retrato do NPC, avança com botão
- [ ] **NPC-02**: Rei NPC no castelo que dá a missão principal ("limpar a dungeon")
- [ ] **NPC-03**: Shopkeeper NPC com UI de compra/venda (sementes, poções, equipamento básico)
- [ ] **NPC-04**: NPCs com estado de quest (sem missão, missão ativa, missão completa) que altera diálogo

### Progression

- [ ] **PRG-01**: Sistema de XP — matar inimigos dá XP, threshold crescente por level
- [ ] **PRG-02**: Level up concede +HP, +damage, +stamina (10-15 levels para conteúdo v1)
- [ ] **PRG-03**: Sistema de gold — moeda dropa de inimigos e vem de venda de crops/itens
- [ ] **PRG-04**: Consequência de morte: perde 10% do gold + chance aleatória de perder 1 item do inventário/equipamento, respawn na fazenda

### UI/HUD

- [ ] **HUD-01**: HUD gráfica com sprites: barra de HP, barra de stamina, hotbar com ícones, relógio/dia
- [ ] **HUD-02**: Inventory UI abrível (tecla I ou similar) mostrando grid + equipment slots
- [ ] **HUD-03**: Shop UI com lista de itens, preços, botões comprar/vender
- [ ] **HUD-04**: Quest tracker simples mostrando missão ativa e objetivo atual
- [ ] **HUD-05**: Caixa de diálogo estilizada com retrato do NPC e texto com avanço

### Save System

- [ ] **SAV-01**: Save/load estendido para inventário, equipment, XP/level, gold, quest state, scene atual
- [ ] **SAV-02**: Migração de versão do save file para não quebrar saves existentes

## v2 Requirements

Diferidos para futuras releases. Não estão no roadmap atual.

### Classes & Especialização

- **CLS-01**: Escolha de classe (swordsman, mage, archer) com skill trees diferentes
- **CLS-02**: Skills específicas por classe desbloqueáveis por level/uso

### Craft & Mineração

- **CRF-01**: Sistema de craft com receitas desbloqueáveis
- **CRF-02**: Sistema de mineração com minério para craft
- **CRF-03**: Forja/bancada de craft como estação na fazenda

### Social & Narrativa

- **SOC-01**: Sistema de relacionamento com NPCs (amizade, casamento)
- **SOC-02**: Múltiplos reinos com migração ao casar com príncipe/princesa
- **SOC-03**: Sistema de honra/reputação perante o povo

### Automação

- **AUT-01**: Pupilos de treinamento como ajudantes da fazenda (mid/late game)

### Mundo Expandido

- **WLD-05**: Pesca com minigame por estação
- **WLD-06**: Montarias para locomoção e batalhas
- **WLD-07**: Eventos do reino com acesso a NPCs/itens especiais
- **WLD-08**: Consequências de guerra (fazenda destruída, NPCs mortos)

### Multiplayer

- **MUL-01**: Co-op para até 4 jogadores (PC)
- **MUL-02**: Port para mobile (single player)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Crafting system | Scope trap #1 para o gênero — loja supre a necessidade no v1 |
| Procedural dungeons | Dungeon hand-crafted é mais controlável e suficiente pro v1 |
| Múltiplas dungeons | 1 dungeon completa é suficiente para validar o loop |
| Dialogue trees complexas | Diálogo linear atende v1, branching é v2 |
| Stealth/furtividade | Nice to have, não essencial pro core loop |
| Equipment visível no sprite | Complexidade de arte alta, stats invisíveis funcionam pro v1 |
| Clima/weather system | Estações já existem, clima é polish extra |
| Companion/pet system | Complexidade de IA extra, v2+ |
| Achievements | Polish, não gameplay |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| ARCH-01 | TBD | Pending |
| ARCH-02 | TBD | Pending |
| ARCH-03 | TBD | Pending |
| ARCH-04 | TBD | Pending |
| ARCH-05 | TBD | Pending |
| FARM-01 | TBD | Pending |
| FARM-02 | TBD | Pending |
| FARM-03 | TBD | Pending |
| INV-01 | TBD | Pending |
| INV-02 | TBD | Pending |
| INV-03 | TBD | Pending |
| INV-04 | TBD | Pending |
| INV-05 | TBD | Pending |
| CMB-01 | TBD | Pending |
| CMB-02 | TBD | Pending |
| CMB-03 | TBD | Pending |
| CMB-04 | TBD | Pending |
| CMB-05 | TBD | Pending |
| CMB-06 | TBD | Pending |
| DNG-01 | TBD | Pending |
| DNG-02 | TBD | Pending |
| DNG-03 | TBD | Pending |
| DNG-04 | TBD | Pending |
| WLD-01 | TBD | Pending |
| WLD-02 | TBD | Pending |
| WLD-03 | TBD | Pending |
| WLD-04 | TBD | Pending |
| NPC-01 | TBD | Pending |
| NPC-02 | TBD | Pending |
| NPC-03 | TBD | Pending |
| NPC-04 | TBD | Pending |
| PRG-01 | TBD | Pending |
| PRG-02 | TBD | Pending |
| PRG-03 | TBD | Pending |
| PRG-04 | TBD | Pending |
| HUD-01 | TBD | Pending |
| HUD-02 | TBD | Pending |
| HUD-03 | TBD | Pending |
| HUD-04 | TBD | Pending |
| HUD-05 | TBD | Pending |
| SAV-01 | TBD | Pending |
| SAV-02 | TBD | Pending |

**Coverage:**
- v1 requirements: 42 total
- Mapped to phases: 0
- Unmapped: 42 ⚠️

---
*Requirements defined: 2026-04-10*
*Last updated: 2026-04-10 after initial definition*
