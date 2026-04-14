# Phase 2: Items & Inventory - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-11
**Phase:** 02-items-inventory
**Areas discussed:** Inventario UI, Hotbar e equipamento, Item drops e magnetismo, Farming -> Inventario

---

## Inventario UI

| Option | Description | Selected |
|--------|-------------|----------|
| Grid estilo Stardew | Caixinhas com sprite UI_Slot, click para mover | ✓ |
| Lista estilo Tibia | Lista vertical com icones | |
| Grid + drag-and-drop | Grid com arrastar itens | |

**User's choice:** Grid estilo Stardew, usar sprite UI_Slot_Selected para slots
**Notes:** User apontou diretamente para o sprite em assets/Sprites/System/UI Elements/Slot/UI_Slot_Selected

---

## Hotbar e Equipamento

| Option | Description | Selected |
|--------|-------------|----------|
| Hotbar embaixo + equipment separado | Hotbar fixa na tela, equipment como aba do inventario | ✓ |
| Hotbar lateral | Slots na lateral da tela | |
| Equipment integrado no grid | Sem aba separada | |

**User's choice:** Hotbar visual embaixo da tela igual Stardew Valley. Equipment como aba do inventario, Tibia-style com slots fixos em formato de homenzinho.
**Notes:** User referenciou HUD.png e Preview.png em assets/Sprites/System/Preview/ como exemplos do kit de UI

---

## Item Drops e Magnetismo

| Option | Description | Selected |
|--------|-------------|----------|
| Sprite real do item | Item no chao usa seu proprio sprite | ✓ |
| Saquinho generico | Todos drops usam mesmo sprite de saco | |

**User's choice:** Sprite real do item. Distancia de magnetismo comeca pequena, com possibilidade futura de upgrades. Velocidade e animacao estilo Stardew Valley.
**Notes:** None

---

## Farming -> Inventario

| Option | Description | Selected |
|--------|-------------|----------|
| Colheita com ferramenta + drop no chao | Foice no crop -> item dropa -> magnetismo coleta | ✓ |
| Colheita direta para inventario | Crop some e item vai direto pro inventario | |

**User's choice:** Dropar no chao apos usar foice, magnetismo toma conta da coleta
**Notes:** None

---

## Claude's Discretion

- Drag-and-drop vs click-to-move no inventario (complexidade de implementacao)
- Layout exato de UI (margens, espacamento)
- Curva de aceleracao do magnetismo
- Organizacao interna do codigo
- Ordem das abas no inventario

## Deferred Ideas

- Aumento de distancia de magnetismo via upgrades
- Mais equipment slots (anel, amuleto)
- Tooltip detalhado
- Sort/filter no inventario
