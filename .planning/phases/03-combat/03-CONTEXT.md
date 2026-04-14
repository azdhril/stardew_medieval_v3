# Phase 3: Combat - Context

**Gathered:** 2026-04-11
**Status:** Ready for planning

<domain>
## Phase Boundary

Sistema de combate completo: ataque melee com espada, magia ranged com projetil, sistema de HP com barras visiveis, 3 tipos de inimigo com IA basica, e boss fight com ataques telegrafados. Inimigos spawnam temporariamente na fazenda (relocalizados para dungeon na Phase 5).

</domain>

<decisions>
## Implementation Decisions

### Ataque Melee (referencia: Stardew Valley)
- **D-01:** Ataque melee via Left Click. Player swinga espada na direcao que esta olhando (FacingDirection). Arco de ~90 graus cobrindo 3 tiles a frente. Estilo Stardew Valley: rapido, satisfatorio, sem complexidade.
- **D-02:** Knockback ao acertar: inimigo e empurrado ~32px na direcao oposta ao player. Simples, sem fisica elaborada — setar posicao diretamente com lerp suave.
- **D-03:** Animacao de slash: sprite overlay na frente do player (arco branco/metalico). Duracao curta (~0.3s). Nao precisa de spritesheet elaborada — pode ser 2-3 frames de um arco simples gerado por codigo ou sprite basico.
- **D-04:** Cooldown de ataque baseado na arma (Iron_Sword: ~0.5s, Steel_Sword: ~0.4s, Flame_Blade: ~0.35s). Cooldown simples, sem combo system no v1.
- **D-05:** Feedback ao acertar: inimigo pisca branco por ~0.1s (tint flash). Sem hitstop, sem screenshake no v1 — manter simples como Stardew.
- **D-06:** Dano calculado: weapon.damage + equipment_attack_bonus. Defesa do inimigo subtrai do dano (minimo 1).

### Sistema de Magia (referencia: Stardew Valley + simplicidade v1)
- **D-07:** Uma magia: Fireball. Projetil que viaja na direcao que o player olha (nao segue mouse). Velocidade ~200px/s, range maximo ~300px, some ao colidir com inimigo ou ao atingir range maximo.
- **D-08:** Lancamento via Right Click (RMB). Se player tem arma equipada, LMB = melee, RMB = magia. Simples e intuitivo.
- **D-09:** Cooldown de magia: ~2s entre casts. Sem sistema de mana no v1 — cooldown only. Mana e v2 (progression system). Indicador visual de cooldown na HUD (icone escurecido ou barra pequena).
- **D-10:** Visual do projetil: sprite pequeno (8x8 ou 16x16) com glow/particula simples. Pode ser sprite estatico com rotacao ou 2-frame animacao.
- **D-11:** Dano fixo do fireball: 15 base. Nao escala com equipment no v1. Escalonamento magico e v2.

### HP e Barras de Vida
- **D-12:** Player HP: usar Entity.HP/MaxHP que ja existe. MaxHP inicial = 100. Exibir na HUD como barra vermelha (usar UI_StatusBar_Fill_HP.png que ja existe).
- **D-13:** Enemy HP bars: barra pequena acima de cada inimigo, visivel sempre que HP < MaxHP. Barra vermelha simples sobre fundo cinza. Desaparece quando HP = MaxHP.
- **D-14:** Dano do player: inimigos atacam, player perde HP. Se HP <= 0, player morre. Morte no v1: respawn na fazenda com HP cheio. Penalty de morte (gold loss) e Phase 6.
- **D-15:** Invulnerability frames apos tomar dano: ~1s onde player pisca e nao toma dano. Padrao de Stardew Valley. Previne stunlock.

### Comportamento dos Inimigos (3 tipos, referencia: Stardew Valley mines)
- **D-16:** Skeleton (melee rusher): Velocidade media (~60px/s). Detecta player a ~120px. Corre direto pro player (pathfinding simples — mover em direcao ao player, sem A*). Ataca em melee range (~24px). Dano: 10. HP: 40. Drop: bones (loot item), chance de gold.
- **D-17:** Dark Mage (ranged caster): Velocidade lenta (~30px/s). Detecta player a ~160px. Mantem distancia (~100px do player). Dispara projetil magico em direcao ao player a cada ~3s. Dano projetil: 12. HP: 30 (fragil). Drop: mana crystal, chance de gold.
- **D-18:** Golem (slow tank): Velocidade muito lenta (~20px/s). Detecta player a ~80px (miope). Alto HP: 120. Dano alto: 20. Ataque lento (cooldown ~2s). Knockback resistance (empurrado apenas ~8px). Drop: stone, chance de rare item.
- **D-19:** AI State Machine: Idle (parado/pequeno wander) -> Chase (detectou player, move em direcao) -> Attack (em range, ataca) -> Return (player saiu do range de deteccao, volta ao ponto de spawn). Transicoes simples baseadas em distancia.
- **D-20:** Spawn temporario na fazenda: 3-5 inimigos spawnam em posicoes fixas (hardcoded) na area da fazenda. Respawnam ao avancar o dia. Relocalizados para dungeon na Phase 5.

### Boss Fight (referencia: Stardew Valley mines bosses + Tibia bosses)
- **D-21:** Boss: Skeleton King (skeleton grande, ~2x sprite normal). HP: 300. Spawna em ponto fixo na fazenda (temporario ate Phase 5).
- **D-22:** Ataques telegrafados: 2 padroes. (1) Slash amplo — wind-up de ~1s (sprite muda/pisca vermelho) antes de atacar em area grande. Dano: 25. (2) Summon — spawna 2 skeletons minions a cada 30% HP perdido.
- **D-23:** Loot unico: Flame_Blade (ja existe no items.json, rare) + gold bonus. Drop garantido na primeira vez.
- **D-24:** Indicador visual de boss: HP bar maior na parte inferior da tela (estilo boss bar de RPG). Nome do boss acima da barra.

### Enemy Sprites
- **D-25:** V1 usa sprites placeholder — retangulos coloridos com outline (skeleton=branco, mage=roxo, golem=marrom, boss=vermelho grande). Mesma abordagem do DummyNpc atual. Sprites reais sao polish futuro, nao bloqueiam gameplay.

### Claude's Discretion
- Estrutura interna do CombatSystem/AttackManager
- Como organizar EnemyEntity vs tipos especificos (heranca ou composicao)
- Implementacao exata do projetil (Entity subclass vs sistema separado)
- Collision detection approach para ataques (rectangle overlap vs circle)
- Como estruturar o spawn system internamente
- Tamanho exato dos hitboxes de ataque

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Entity System (Phase 1 output)
- `src/Core/Entity.cs` — Base class com HP/MaxHP/IsAlive, Position, Velocity, CollisionBox, FacingDirection, animation support
- `src/Player/PlayerEntity.cs` — Player com movement, collision, animation. Sem ataque ainda.
- `src/Player/PlayerStats.cs` — Stamina system. HP separado na Entity base.

### Items & Equipment (Phase 2 output)
- `src/Data/items.json` — Weapons ja definidas (Iron_Sword, Steel_Sword, Flame_Blade) com damage stats
- `src/Data/ItemDefinition.cs` — Modelo unificado com Stats dictionary
- `src/Data/ItemRegistry.cs` — Registry de itens com lookup e filtering
- `src/Data/EquipmentData.cs` — Calculo de stats combinados (attack/defense)

### Input & Scenes
- `src/Core/InputManager.cs` — LMB e RMB disponiveis (IsLeftClickPressed, IsRightClickPressed)
- `src/Core/SceneManager.cs` — Stack-based scenes com transicoes
- `src/Scenes/FarmScene.cs` — Scene principal, onde inimigos serao adicionados temporariamente

### UI Assets
- `assets/Sprites/System/UI Elements/Bars/Status/UI_StatusBar_Fill_HP.png` — Barra de HP
- `assets/Sprites/System/UI Elements/Bars/Status/UI_StatusBar_Bg.png` — Background da barra

### Collision
- `src/World/TileMap.cs` — Collision com tiles (polygon-based). Entity-to-entity collision NAO existe ainda.

### Codebase Maps
- `.planning/codebase/ARCHITECTURE.md` — Arquitetura atual
- `.planning/codebase/CONVENTIONS.md` — Naming patterns, code style

### Requirements
- `.planning/REQUIREMENTS.md` — CMB-01 a CMB-06

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Entity.HP/MaxHP/IsAlive` — Sistema de HP ja na base class, so precisa de TakeDamage()
- `Entity.Velocity` — Campo existe mas nao e usado. Reutilizar para knockback.
- `Entity.CollisionBox` — Hitbox de colisao ja existe em todas entities
- `ItemRegistry.GetByType(ItemType.Weapon)` — Lookup de armas ja funciona
- `EquipmentData.GetEquipmentStats()` — Calculo de attack/defense ja existe
- `UI_StatusBar_Fill_HP.png` — Sprite de barra de HP pronto
- `DummyNpc` pattern — Referencia de como fazer entity com sprite placeholder (retangulo colorido)

### Established Patterns
- Event-driven via delegates — usar para OnEnemyDeath, OnPlayerDamaged
- Console.WriteLine com [ModuleName] — manter para [Combat], [Enemy], [Boss]
- Entity subclass pattern (PlayerEntity, DummyNpc) — enemies seguem mesmo padrao
- Try* prefix para operacoes falhaveis — TryAttack(), TryDodge()

### Integration Points
- `src/Scenes/FarmScene.cs` — Adicionar lista de enemies, update/draw loop, collision checks
- `src/Player/PlayerEntity.cs` — Adicionar Attack(), TakeDamage(), invulnerability timer
- `src/Core/Entity.cs` — Adicionar TakeDamage(), knockback handling, death event
- `src/UI/HUD.cs` — Adicionar HP bar do player e boss bar

</code_context>

<specifics>
## Specific Ideas

- Referencia principal: Stardew Valley combat (simples, rapido, satisfatorio)
- Referencia secundaria: Tibia (variedade de inimigos, loot com raridade)
- V1 e sobre fazer funcionar, nao sobre polish — sprites placeholder OK
- Cooldown-based magic (sem mana) — simplifica v1, mana e progression (v2)
- Boss na fazenda e temporario — relocar para dungeon room na Phase 5
- Sem combo system, sem dodge roll, sem parry — essas mecanicas sao polish v2+

</specifics>

<deferred>
## Deferred Ideas

- Sistema de mana para magia (v2, progression system)
- Combo system para melee (v2+)
- Dodge roll / esquiva (v2+)
- Parry / bloqueio com escudo (v2+)
- Screenshake e hitstop (polish, v2+)
- Sprites reais de inimigos (art pass futuro)
- Mais magias alem de Fireball (v2+)
- Scaling de dano magico com equipment (v2+)
- Pathfinding A* para inimigos (v2+ se necessario)

</deferred>

---

*Phase: 03-combat*
*Context gathered: 2026-04-11*
