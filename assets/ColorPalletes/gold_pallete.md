# Gold Palette

Paleta dourada master do projeto. Origem: extraída do sprite-âncora dourado e
salva em `assets/Sprites/System/UI Elements/Icons/gold_pallete.gpl`.

Use pra qualquer asset com tom dourado: moedas, troféus, joias, espadas com
detalhe dourado, vassoura premium, vara de pesca tier alto, ícones de UI, etc.

## Cores (9 tons)

Ordenadas do mais escuro pro mais claro:

| Hex       | RGB              | Uso                          |
|-----------|------------------|------------------------------|
| `#000000` | 0, 0, 0          | Outline / contorno preto     |
| `#653813` | 101, 56, 19      | Marrom escuro / sombra densa |
| `#9C6332` | 156, 99, 50      | Sombra profunda              |
| `#B57D3F` | 181, 125, 63     | Sombra média                 |
| `#CB9550` | 203, 149, 80     | Dourado base                 |
| `#E9BF6B` | 233, 191, 107    | Dourado claro                |
| `#F0BB58` | 240, 187, 88     | Dourado saturado / vibrante  |
| `#FFE7BB` | 255, 231, 187    | Highlight creme              |
| `#FEEDBD` | 254, 237, 189    | Highlight bright (alt)       |

## Template de prompt pro ChatGPT / PixelLab

```
Pixel art game asset, medieval fantasy style, top-down RPG view.
Single object on a flat solid color background.
Use ONLY these 9 colors (no anti-aliasing, no gradients, no other tones):
- #000000 (outline, 1px black border around the whole object)
- #653813 (deepest shadow / dark brown)
- #9C6332 (deep shadow / dark gold)
- #B57D3F (mid shadow brown)
- #CB9550 (gold mid base)
- #E9BF6B (gold bright)
- #F0BB58 (saturated gold accent)
- #FFE7BB (highlight cream)
- #FEEDBD (highlight bright variant)
Hard pixel edges only, every pixel solid.
Subject: [ITEM]
Black 1px outline mandatory around the silhouette.
Reference: Following the gold style attached but following the limited collors and specification 
Resolution: 28x28 pixels max resolution
```

## Exemplos de Subject

- `a treasure chest with golden lock and reinforcements`
- `a magic wand with golden tip and twisted shaft`
- `a king's crown with three sharp points and ornaments`
- `a small pile of golden nuggets`
- `a golden goblet with engraved patterns`
- `a golden key with intricate bow`
- `a golden trophy cup with two handles`
- `a stack of three gold coins`

## Workflow após gerar a arte

1. ChatGPT / PixelLab gera (geralmente em ~1024×1024 mesmo pedindo resolução pequena)
2. Krita: remove fundo, recorta bordas até a silhueta
3. PixelOver: importa, define resolução-alvo, Filter = Sharp Bilinear, Mipmaps off
4. Aseprite:
   - `Sprite > Color Mode > Indexed`
   - Painel Palette > menu (≡) > **Load Palette** → escolhe `gold_pallete.gpl`
   - Confirma "Use existing palette" — força a arte nas 9 cores acima
   - Limpa pixels remanescentes manualmente
   - Centraliza no canvas final (16×16, 32×32 ou conforme uso)
5. Exporta PNG pro asset folder do projeto
