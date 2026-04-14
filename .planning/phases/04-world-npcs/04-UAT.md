---
status: complete
phase: 04-world-npcs
source:
  - .planning/phases/04-world-npcs/04-01-SUMMARY.md
  - .planning/phases/04-world-npcs/04-02-SUMMARY.md
  - .planning/phases/04-world-npcs/04-03-SUMMARY.md
  - .planning/phases/04-world-npcs/04-04-SUMMARY.md
started: 2026-04-13T02:59:50Z
updated: 2026-04-13T03:24:13Z
---

## Current Test

[testing complete]

## Tests

### 1. Cold Start Smoke Test
expected: Feche qualquer instância rodando. Rode `dotnet run` a partir de zero. O jogo deve abrir sem erros, carregar a FarmScene com o player, HUD visível (stamina, hora, quest tracker "Quest: (none)" em cinza no canto superior direito). Saves v3/v4 existentes migram silenciosamente para v5 sem perda de Gold/QuestState.
result: pass

### 2. Farm → Village Transition
expected: Ande para leste na farm até pisar no trigger (borda leste, próximo a x=624, y=208). Fade-to-black ocorre, então spawn na VillageScene em (48, 270). Console loga `[VillageScene] Entered from Farm, spawn (48,270)`.
result: pass

### 3. Village → Farm Transition
expected: Na Village, ande para oeste na borda (trigger `exit_to_farm`). Fade → FarmScene, player reaparece em (896, 272) (lado leste da farm, fora do trigger de re-entrada).
result: pass

### 4. Village → Castle + Back
expected: Na Village, ande sobre a porta do castelo (trigger `door_castle` em ~(192, 96)). Fade → CastleScene, player spawna em (208, 128). Saindo pelo sul (x≈208, y=464) volta para Village em (208, 128) (frente da porta do castelo, sem re-trigger loop).
result: pass
note: "Retestado após adição de marcadores coloridos sobre triggers (GameplayScene.DrawTriggerMarkers)"

### 5. Village → Shop + Back
expected: Mesma mecânica: porta da Shop em ~(720, 96) leva à ShopScene (spawn 208,416). Saída sul retorna à Village em (736, 128).
result: pass

### 6. King Dialogue (NotStarted) Activates Quest
expected: Na CastleScene, aproxime do King (spawn 320,100) dentro de 28px. Prompt flutuante "Press E to talk" aparece acima do sprite. E abre DialogueScene com portrait 80x80 à esquerda e typewriter a ~40cps. 3 linhas NotStarted (última: "...clear the dungeon..."). E/Espaço avança (snap-to-full se mid-typing). Após última linha, overlay fecha e HUD quest tracker muda para "Quest: Clear the Dungeon" (em dourado/branco).
result: pass

### 7. King Dialogue (Active) After Activation
expected: Falar com o King novamente mostra 2 linhas da variante Active (ex: "The dungeon still festers with evil, hero."). Nenhuma nova alteração de estado na quest.
result: pass

### 8. F9 Dev Hook Advances Quest (Debug build)
expected: Rodando em Debug, F9 avança estado da quest (NotStarted→Active→Complete). Console loga `[DEBUG] F9 pressed, quest state -> {state}`. Em Complete, HUD mostra "Quest: Clear the Dungeon v" (check ASCII em LimeGreen). Release build não expõe F9.
result: pass

### 9. Shopkeeper Dialogue → Shop Opens
expected: Na ShopScene, aproxime do Shopkeeper (spawn 320,200). Press E abre dialogue variante do shopkeeper (estado atual da quest). Ao fechar a última linha, ShopOverlayScene empurra automaticamente mostrando painel 720x400 centralizado com tabs Buy/Sell e contador de Gold em dourado no canto.
result: pass
initial_issue: "só trava em Discount? Bah. Buy what you like, on me today. e fica travado n consigo sair desse dialogo"
fix_applied: "src/Scenes/DialogueScene.cs: PopImmediate() antes de _onClose?.Invoke() (ordem invertida). Bug: onClose pushava ShopOverlayScene, Pop derrubava o overlay recém-pushado."
retest_feedback: "Passou após fix. Feedback adicional: suporte a mouse nos tabs Buy/Sell (hoje só TAB troca)."

### 10. Shop Buy — Happy Path
expected: Com Gold suficiente, selecione um item (ex: Cabbage_Seed 25g) com ↓, preço mostrado em LimeGreen, Buy habilitado. Enter debita Gold exato, adiciona 1 item ao inventário, toast verde "Purchased Cabbage Seed" aparece 2.2s (fade-in 0.6s, hold 1.2s, fade-out 0.4s). Log `[ShopPanel] Bought {id} for {price}g`.
result: issue
reported: "falta conseguir manipular o menu do shopkeeper com o mouse"
severity: major

### 11. Shop Buy — Disabled States
expected: Item caro além do Gold → label "Not enough gold" em vermelho, Enter não debita nem adiciona. Com inventário cheio, tentar qualquer item mostra "Inventory full". Nenhum debit de Gold em qualquer estado bloqueado.
result: pass

### 12. Shop Sell — Full Stack
expected: Tab muda para Sell, lista mostra apenas slots do inventário preenchidos. Selecionar um stack de 5 Cabbages (BasePrice 40 → sell 20 cada) e Enter: Gold += 100, slot esvazia, toast dourado "Sold Cabbage for 100g". Itens não-vendáveis (Hoe/Watering_Can/rotten) rejeitam com "Cannot sell this item".
result: issue
reported: "falta barra de rolagem no menu do shope keeper, falta conseguir utilizar a roda do mouse.. falta poder escolher o tamanho do pack de venda.. e não só vender todo o pack"
severity: major

### 13. Shop Esc Closes + Save Persists
expected: Esc fecha ShopOverlayScene e retorna ao gameplay normal da ShopScene (player controlável). Saindo pela porta sul volta à Village. Avançar um dia (F1 ou dormir) salva; relançar o jogo preserva Gold atual, quest state e inventário completo. CurrentScene sempre boota em Farm (design).
result: issue
reported: "dei f1 e quando voltei pro jogo não estava na village, isso está correto? o status da quest tinha sido perdido e o dinheiro e itens tb nao salvou"
severity: blocker
note: "Boot em Farm após reload é por design (spec explícita). O bug crítico é que Gold, quest state e inventário NÃO persistem no save após F1/dormir."

## Summary

total: 13
passed: 10
issues: 3
pending: 0
skipped: 0

## Gaps

- truth: "Shop tabs Buy/Sell devem ser clicáveis com mouse"
  status: failed
  reason: "User feedback no Test 9: só TAB/teclado troca abas; esperado suporte mouse também."
  severity: minor
  test: 9
  artifacts: ["src/UI/ShopPanel.cs"]
  missing: ["Mouse hit-test + click handling para regiões dos tabs (80x32 em (136,88))"]

- truth: "Shop menu deve ser totalmente manipulável com mouse (navegação da lista, seleção de item, botão Buy/Sell, fechar)"
  status: failed
  reason: "User reported no Test 10: falta conseguir manipular o menu do shopkeeper com o mouse"
  severity: major
  test: 10
  artifacts: ["src/UI/ShopPanel.cs", "src/Scenes/ShopOverlayScene.cs"]
  missing: ["Mouse hit-test para lista de itens (hover + click seleciona)", "Click no botão Buy/Sell executa ação", "Click fora do painel ou em X fecha overlay", "Scroll wheel para lista longa"]

- truth: "Shop deve ter scrollbar visível na lista de itens"
  status: failed
  reason: "User reported no Test 12: falta barra de rolagem no menu do shop keeper"
  severity: major
  test: 12
  artifacts: ["src/UI/ShopPanel.cs"]
  missing: ["Renderizar scrollbar vertical quando a lista excede a altura visível (indicador de posição + trilho)"]

- truth: "Scroll wheel do mouse deve rolar a lista de itens do shop"
  status: failed
  reason: "User reported no Test 12: falta conseguir utilizar a roda do mouse"
  severity: major
  test: 12
  artifacts: ["src/UI/ShopPanel.cs", "src/Core/InputManager.cs"]
  missing: ["Captura de MouseState.ScrollWheelValue delta", "Aplicar scroll ao índice/offset da lista do ShopPanel quando overlay está ativo"]

- truth: "Venda deve permitir escolher quantidade do pack (não apenas vender o stack inteiro)"
  status: failed
  reason: "User reported no Test 12: falta poder escolher o tamanho do pack de venda, e não só vender todo o pack"
  severity: major
  test: 12
  artifacts: ["src/UI/ShopPanel.cs", "src/Scenes/ShopOverlayScene.cs"]
  missing: ["UI de seleção de quantidade (ex: +/- buttons, slider, ou prompt numérico)", "Lógica de venda parcial: debita N do stack, credita N * sellPrice ao Gold", "Confirmar com Enter, cancelar com Esc"]

- truth: "Save após F1/dormir deve persistir Gold, quest state e inventário"
  status: failed
  reason: "User reported no Test 13: após F1 e reload, quest state perdido, Gold e itens não salvaram (boot em Farm é por design, não é o bug)"
  severity: blocker
  test: 13
  artifacts: ["src/Core/SaveManager.cs", "src/Core/GameState.cs", "src/Scenes/FarmScene.cs"]
  missing: ["Incluir PlayerStats.Gold na serialização de GameState", "Incluir QuestTracker/quest state na serialização", "Incluir Inventory (slots + hotbar + equipment) na serialização", "Aplicar estado carregado ao reentrar nas cenas após boot", "Verificar migration v4→v5 preserva campos novos"]
