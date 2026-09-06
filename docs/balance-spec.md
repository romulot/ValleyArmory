# Valley Armory — especificação inicial de balanceamento

Os valores abaixo são provisórios e existem para tornar o catálogo validável. Eles devem ser comparados no jogo antes da implementação de aquisição. Raridade é apresentação e progressão; ela não aplica multiplicadores automáticos.

| Item | Tipo | Raridade | Dano | Velocidade | Defesa | Crítico | Multiplicador | Knockback | Imunidade | Preço | Referência vanilla provisória |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| Miner's Blade | Espada | Rare | 14–22 | +1 | 1 | 3% | 3,0× | 1,0 | — | 900 | Steel Smallsword |
| Black Iron Sword | Espada | Common | 20–30 | −2 | 2 | 2% | 3,0× | 1,1 | — | 1.100 | Cutlass |
| Prismatic Blade | Espada | Legendary | 55–72 | +2 | 2 | 4% | 3,2× | 1,0 | — | 6.500 | Galaxy Sword |
| Shadow Fang | Adaga | Rare | 9–15 | +4 | 0 | 8% | 3,4× | 0,7 | — | 1.000 | Crystal Dagger |
| Moon Dagger | Adaga | Epic | 20–28 | +5 | 0 | 12% | 3,5× | 0,65 | — | 2.800 | Iridium Needle |
| Stonebreaker | Martelo | Common | 24–38 | −5 | 2 | 2% | 3,0× | 1,5 | — | 1.200 | Wood Mallet |
| Abyss Hammer | Martelo | Epic | 48–68 | −6 | 4 | 2,5% | 3,1× | 1,8 | — | 4.000 | Galaxy Hammer |
| Miner's Boots | Botas | Common | — | — | 1 | — | — | — | 1 | 400 | Leather Boots |
| Obsidian Boots | Botas | Rare | — | — | 3 | — | — | — | 1 | 900 | Firewalker Boots |
| Ethereal Boots | Botas | Epic | — | — | 2 | — | — | — | 5 | 2.200 | Genie Shoes |

Antes da Fase de aquisição, cada referência e preço deverão ser revistos contra os dados efetivos de Stardew Valley 1.6.15. A Prismatic Blade deve permanecer um sidegrade do endgame, não uma substituição automática das armas Galaxy ou Infinity.

## Unidades internas e apresentação vanilla

Os números da tabela são valores internos de `WeaponData`, não os números
necessariamente mostrados pelo tooltip. A raridade não participa dessas fórmulas.

- Em espadas e adagas, cada ponto interno de velocidade reduz em 40 ms a duração
  base de 400 ms do golpe, antes dos modificadores do jogador. O tooltip mostra
  `Speed / 2` com divisão inteira; valores ímpares, portanto, são arredondados em
  direção a zero na apresentação.
- `CritChance` é uma fração (`0.03` = 3% base). Para espadas, o tooltip mostra
  `round((CritChance - 0.001) / 0.02)`, uma unidade visual vanilla que não deve
  ser interpretada como porcentagem literal.
- Adagas aplicam a transformação vanilla `(CritChance + 0.005) * 1.12` antes do
  uso em combate e da conversão para o tooltip.

Consequentemente, a Miner's Blade mantém `Speed: 1` e `CritChance: 0.03`, mas o
tooltip vanilla esperado é `+0 Velocidade` e `+1 Chance Crítico`. O balanceamento
deve ser avaliado pelos valores efetivos, não apenas pelos inteiros apresentados.
