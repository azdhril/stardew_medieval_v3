# Quick Task 260424-2af: Sistema global de UI widgets — Context

**Gathered:** 2026-04-24
**Status:** Ready for research + planning

<domain>
## Task Boundary

Criar um sistema global de UI widgets (IClickable + UIManager + botões concretos) para eliminar o boilerplate de hit-test, hover feedback e cursor change que hoje é replicado em cada cena (ChestScene, ShopPanel, InventoryScene, PauseScene, DialogueScene).

Escopo confirmado: **framework completo + migração de TODAS as cenas clicáveis nesta task**. Não deixa tech debt pendurado.

</domain>

<decisions>
## Implementation Decisions

### Escopo da entrega

- **Decisão**: Framework + migrar todas as cenas agora. Uma task só.
- **Cenas a migrar**: ChestScene, ShopPanel, InventoryScene, PauseScene, DialogueScene. E qualquer outra que tenha botão clicável (auditar `Mouse.GetState`, `Rectangle.Contains(mousePos)`, `Mouse.SetCursor`).
- **Critério de pronto**: zero `Mouse.SetCursor` manual em cenas; toda lógica de hover/cursor flui pelo `UIManager`.

### Modelo de API

- **Decisão**: Interface `IClickable` mínima + classes concretas (`IconButton`, `TextButton`, `Tab`, `CloseButton` se necessário).
- **IClickable contrato mínimo**:
  - `Rectangle Bounds { get; }`
  - `bool Enabled { get; }`
  - `void OnClick()`  — callback quando clicado
  - Opcional: `void OnHoverEnter()`, `void OnHoverExit()` via default interface methods
  - Opcional: `string? Tooltip { get; }` pra suporte a tooltip global
- **Rejeitadas**: abstract Widget base class (muita herança rígida) e componente-com-delegates (menos idiomatic C#).

### Visual style / hover feedback

- **Decisão**: Embutido padrão (nudge 1px + halo 55% white) em todos os widgets concretos. Consistency é o default.
- **Override**: via subclasse ou parâmetro `HoverStyle` no construtor (ex: `HoverStyle.NudgeHalo`, `HoverStyle.BrightenOnly`, `HoverStyle.None`).
- **Widgets herdam**: `DrawSelf(hovered)` padrão; pode ser overridden.

### Extras incluídos nesta entrega

Todos os três extras foram marcados:
- **Tooltip global**: `Widget.Tooltip` string property. `UIManager` mostra após ~500ms de hover estável, desenha num spot neutro (cursor + offset). Fonte/estilo do tooltip reusa o padrão que já existe no ChestScene.
- **Keyboard navigation**: `UIManager` mantém `_focusedWidget`. Tab/Shift-Tab navega. Enter/Space aciona OnClick. Arrow keys opcional (depende do layout — pode ficar pra depois se complicar).
- **Hover/click SFX**: `UIManager` tem hooks `OnHoverSound`, `OnClickSound`. Se o projeto não tem sistema de áudio implementado ainda, os hooks ficam prontos mas no-op por default; quando o áudio vier, plugam-se sem refactor. Não bloquear a task esperando áudio.

### Claude's Discretion

- **Nomeclatura de diretório/namespace**: propor `src/UI/Widgets/` com namespace `stardew_medieval_v3.UI.Widgets`. Se já houver convenção diferente, seguir a existente.
- **UIManager lifecycle**: **scene-owned** (protected field em `Scene` base class ou campo privado em cada cena). Não services-owned. Razão: overlays em stack (FarmScene + ChestScene) — manager global deixaria widgets da cena de baixo interceptarem hit-tests do overlay. Pontuado pelo research agent. Cada cena instancia seu próprio `UIManager` em `LoadContent` e libera em `UnloadContent`.
- **Focus visual**: desenhar outline dourado 1-2px ao redor do widget focado (igual o halo mas contínuo). Keyboard-only users veem claramente.
- **Input gating**: quando `UIManager` trata um click (hit em um widget), não propaga pra scene input. Evita double-handling.
- **SetCursor cache**: só chamar `Mouse.SetCursor` quando o estado de hover muda (hovering / not hovering). Evita reports de crash por chamar per-frame em alguns drivers.
- **Tooltip font size**: `FontRole.Body 15` (tamanho "small" já existente) — compacto sem ficar ilegível.

</decisions>

<specifics>
## Specific Ideas

- Padrão de hover já aceito: nudge 1px upward + 4-direction 55%-white halo. Reutilizar o helper `DrawHoverableIcon` já existente em ChestScene + ShopPanel, deslocar pro framework e deletar as cópias locais.
- `IconButton` deve aceitar um `Texture2D` e opcionalmente um background `Texture2D` (pra casos com/sem fundo tipo BtnSlot).
- `TextButton` usa o mesmo visual do Buy/Sell atual (YellowBtnSmall 9-slice + texto centered).
- `Tab` precisa distinguir estados Active / Inactive / Hovered (3 estados) — TabOn vs TabOff sprites + nudge no hover.
- Tooltip style: reusar `DrawTooltipPanel` que já existe em ChestScene (bordas arredondadas, fill escuro, border dourada). Deslocar pro framework também.
</specifics>

<canonical_refs>
## Canonical References

- Padrão "IClickable + UIManager" é bem estabelecido em MonoGame/XNA community. A research task deve buscar exemplos/gotchas antes de finalizar a API.
- FontStashSharp já integrado (quick 260423-tu6) — widgets usam `SpriteFontBase`.
- `ServiceContainer` é o composition root atual (Game1.LoadContent) — novo `Services.UI` slot para o `UIManager`.
</canonical_refs>
