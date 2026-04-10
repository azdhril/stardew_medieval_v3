# Phase 1: Architecture Foundation - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md -- this log preserves the alternatives considered.

**Date:** 2026-04-10
**Phase:** 01-architecture-foundation
**Areas discussed:** Scene architecture, Entity design, ItemDefinition scope, Save migration

---

## Scene Architecture

### Estrutura das scenes

| Option | Description | Selected |
|--------|-------------|----------|
| Abstract class | Classe abstrata Scene com metodos virtuais. Padrao comum MonoGame. | X |
| Interface IScene | Interface pura, mais flexivel mas raramente necessario pra jogos simples. | |
| Voce decide | Claude escolhe. | |

**User's choice:** Abstract class
**Notes:** None

### Acesso a servicos compartilhados

| Option | Description | Selected |
|--------|-------------|----------|
| ServiceContainer | Objeto agrupando managers, passado via construtor. | X |
| Parametros diretos | Cada scene recebe managers individuais. Construtores longos. | |
| Voce decide | Claude escolhe. | |

**User's choice:** ServiceContainer
**Notes:** None

### Tipo de transicao

| Option | Description | Selected |
|--------|-------------|----------|
| Fade to black | Fade out -> preto -> troca -> fade in. Classico. | X |
| Corte seco | Troca instantanea sem animacao. | |
| Voce decide | Claude escolhe. | |

**User's choice:** Fade to black
**Notes:** None

### Scene stack

| Option | Description | Selected |
|--------|-------------|----------|
| Troca simples | Uma scene ativa por vez. Menus como overlays internos. | |
| Stack (push/pop) | Empilhar scenes. Menus como scenes separadas. | X |
| Voce decide | Claude escolhe. | |

**User's choice:** Stack (push/pop)
**Notes:** None

---

## Entity Design

### Estrutura do sistema de entidades

| Option | Description | Selected |
|--------|-------------|----------|
| Heranca simples | Abstract Entity com Position, Sprite, Collision. Subclasses especializadas. | X |
| Component-based (ECS lite) | Entity como container de components. Mais flexivel, overengineering pro v1. | |
| Voce decide | Claude escolhe. | |

**User's choice:** Heranca simples
**Notes:** None

### Conteudo da Entity base

| Option | Description | Selected |
|--------|-------------|----------|
| Animacao + HP + Velocity | As 3 capacidades na base. Evita refactor nas Phases 3-5. | X |
| So Animacao + HP | Velocity nas subclasses. | |
| Minimo possivel | So Position/Collision/Facing. | |

**User's choice:** Animacao + HP/IsAlive + Velocity (as 3)
**Notes:** User asked for recommendation considering scalability. Claude recommended all 3 based on known Phase 3-5 requirements (not speculation). User agreed.

---

## ItemDefinition Scope

### Detalhamento do modelo

| Option | Description | Selected |
|--------|-------------|----------|
| Estrutura completa | Todos campos do v1: Id, Name, Type, Rarity, StackLimit, SpriteId, Stats. | X |
| Placeholder minimo | So Id, Name, Type. Expande depois. | |
| Voce decide | Claude escolhe. | |

**User's choice:** Estrutura completa
**Notes:** None

### Onde definir itens

| Option | Description | Selected |
|--------|-------------|----------|
| JSON + Registry | items.json carregado via ItemRegistry estatico. CropRegistry migra. | X |
| Hardcoded | Manter padrao do CropRegistry. Tudo em codigo. | |
| Voce decide | Claude escolhe. | |

**User's choice:** JSON + Registry
**Notes:** None

---

## Save Migration

### Estrategia de compatibilidade

| Option | Description | Selected |
|--------|-------------|----------|
| Versao + defaults | Incrementar versao, MigrateIfNeeded() ja existe. Estender. | X |
| Reset saves | Quebrar compatibilidade. Saves descartados. | |
| Voce decide | Claude escolhe. | |

**User's choice:** Versao + defaults
**Notes:** None

### Campos novos no GameState

| Option | Description | Selected |
|--------|-------------|----------|
| Inventario (placeholder) | List<ItemStack> vazia. | X |
| Gold + XP + Level | Numericos zerados. | X |
| CurrentScene + QuestState | Scene string + quest enum. | X |
| Equipment slots | WeaponId/ArmorId opcionais. | X |

**User's choice:** Todos os 4 + HotbarSlots
**Notes:** User asked about hotkeys -- clarified that hotbar slots (which items are in slots 1-8) are player state (saved), while keybindings are configuration (not saved in GameState).

---

## Claude's Discretion

- Ordem interna de refatoracao
- Nomes exatos de metodos/propriedades intermediarios
- Organizacao interna do fade transition no SceneManager

## Deferred Ideas

None -- discussion stayed within phase scope
