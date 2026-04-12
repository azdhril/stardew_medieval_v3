---
status: complete
phase: 03-combat
source:
  - .planning/phases/03-combat/03-01-SUMMARY.md
  - .planning/phases/03-combat/03-02-SUMMARY.md
  - .planning/phases/03-combat/03-03-SUMMARY.md
started: 2026-04-12T00:00:00Z
updated: 2026-04-12T00:01:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Melee Attack (LMB)
expected: Equipar espada + LMB → slash visual aparece, inimigo na frente leva dano 1x por swing, respeita cooldown da arma.
result: pass

### 2. Fireball (RMB)
expected: Clicar RMB → projétil de fogo dispara a 200px/s, viaja até 300px max, causa 15 de dano fixo. Cooldown de 2s com indicador de progresso visível no HUD.
result: pass
note: "Cooldown ajustado para 1s (spec drift intencional, confirmado pelo user)"

### 3. Player HP Bar + I-Frames
expected: HUD mostra barra de HP vermelha. Ao levar hit, player pisca (blink por i-frames de 1s) e flasha vermelho. Durante i-frames não recebe dano adicional.
result: pass

### 4. Enemy Health Bars
expected: Inimigos com HP cheio não mostram barra. Após tomar dano, uma barra aparece acima deles (world-space) e some quando voltam a 100%.
result: pass

### 5. Skeleton (Melee Rusher)
expected: Skeleton (40HP) corre em direção ao player e ataca corpo-a-corpo quando em range. Rápido.
result: pass

### 6. Dark Mage (Ranged Kiter)
expected: Dark Mage (30HP) mantém distância e dispara projéteis com cooldown de ~3s. Se aproximado, tenta se afastar (kiting).
result: pass

### 7. Golem (Tank)
expected: Golem (120HP) é lento e resiste a knockback (75% resistência — quase não é empurrado ao levar hit).
result: pass

### 8. Enemy AI FSM
expected: Inimigos começam em Idle; ao ver player entram em Chase; atacam em range (Attack); se afastados muito, retornam à posição original (Return).
result: pass

### 9. Loot Drops
expected: Ao matar inimigos, drops aparecem com probabilidade: Skeleton → Bones, Dark Mage → Mana_Crystal, Golem → Stone_Chunk.
result: pass

### 10. Player Death & Respawn
expected: Player morre quando HP chega a 0. É respawnado (posição reset, HP restaurado).
result: pass

### 11. Enemy Respawn on Day Advance
expected: Dormir/avançar o dia faz inimigos mortos voltarem às posições originais de spawn.
result: pass

### 12. Boss Telegraphed Slash
expected: Skeleton King (300HP, retângulo vermelho 32x32) faz wind-up de 1s (flash vermelho telegraph) antes do slash largo (hitbox 64x32). Dá tempo de esquivar.
result: pass

### 13. Boss Summon Phases
expected: Ao reduzir boss para 70% HP e depois 40% HP, ele invoca 2 Skeletons cada vez, perto da posição dele.
result: pass

### 14. Boss First-Kill Loot
expected: Primeira vez que mata o boss: drop garantido de Flame_Blade + 5x Stone_Chunk. Kills subsequentes: 10% chance de Flame_Blade + 5x Stone_Chunk.
result: pass

### 15. Boss Kill Persists Across Save
expected: Matar boss, dormir/salvar, reabrir o jogo. BossKilled permanece true (Flame_Blade não cai garantido de novo no próximo kill — vira 10%).
result: pass
note: "Confirmado pelo user: após fechar/reabrir e rematar, Flame_Blade não caiu (10% roll não acertou, esperado)"

## Summary

total: 15
passed: 15
issues: 0
pending: 0
skipped: 0

## Gaps

[none yet]
