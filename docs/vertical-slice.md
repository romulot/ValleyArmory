# Miner's Blade — vertical slice

## Escopo ativo

O catálogo continua contendo e validando dez equipamentos, mas somente `romulot.ValleyArmory_MinersBlade` é convertido e adicionado a `Data/Weapons`.

QualifiedItemId:

```text
(W)romulot.ValleyArmory_MinersBlade
```

## Mapeamento para WeaponData

| Origem | WeaponData | Valor efetivo |
|---|---|---:|
| `id` | `Name` | `romulot.ValleyArmory_MinersBlade` |
| `displayNameKey` | `DisplayName` | tradução ativa |
| `descriptionKey` | `Description` | tradução ativa |
| `type: sword` | `Type` | `3` (slashing sword) |
| `sprite.assetName` | `Texture` | `Mods/romulot.ValleyArmory/Weapons` |
| `sprite.spriteIndex` | `SpriteIndex` | `0` |
| `stats.minDamage` | `MinDamage` | `14` |
| `stats.maxDamage` | `MaxDamage` | `22` |
| `stats.knockback` | `Knockback` | `1.0` |
| `stats.speed` | `Speed` | `1` |
| `stats.defense` | `Defense` | `1` |
| `stats.critChance` | `CritChance` | `0.03` |
| `stats.critMultiplier` | `CritMultiplier` | `3.0` |
| raridade | `CustomFields["romulot.ValleyArmory/Rarity"]` | `Rare` |

### Semântica vanilla confirmada em Stardew Valley 1.6.15

`MeleeWeapon.ReloadData()` copia `WeaponData.Speed` e
`WeaponData.CritChance` diretamente para os campos de runtime da arma. Portanto,
o mapeamento da Miner's Blade não converte nem perde precisão nesses atributos.

- Para espadas, a duração-base do golpe é calculada como
  `(400 - Speed * 40 - farmer.addedSpeed * 40) * (1 - WeaponSpeedMultiplier)`
  milissegundos. Assim, `Speed: 1` reduz a duração-base de 400 ms para 360 ms
  antes de outros modificadores.
- O tooltip vanilla exibe a velocidade de espadas como divisão inteira
  `Speed / 2`. Por isso, o valor interno `1` aparece como `+0 Velocidade`, embora
  já produza efeito no ataque.
- Para espadas, `CritChance: 0.03` permanece uma chance-base de 3% no combate e
  é então afetada pelo multiplicador de chance crítica dos buffs do jogador.
- O tooltip calcula a unidade exibida como
  `round((CritChance - 0.001) / 0.02)`. Logo, `0.03` aparece como
  `+1 Chance Crítico`; esse número não é a porcentagem literal.
- Adagas recebem tratamento vanilla adicional na chance crítica —
  `(CritChance + 0.005) * 1.12` — tanto no combate quanto na apresentação. Isso
  não se aplica à Miner's Blade.

Essas fórmulas foram confirmadas por inspeção do IL do assembly instalado
`Stardew Valley.dll`, versão de arquivo `1.6.15.24356`. Elas são comportamento
interno do jogo e devem ser revalidadas ao atualizar a versão-alvo.

Defaults explícitos desta prova:

| WeaponData | Valor | Motivo |
|---|---:|---|
| `Precision` | `0` | catálogo ainda não modela precisão |
| `AreaOfEffect` | `0` | catálogo ainda não modela área adicional |
| `CanBeLostOnDeath` | `true` | preservar comportamento normal de armas |
| `MineBaseLevel` | `-1` | aquisição automática desabilitada |
| `MineMinLevel` | `-1` | drops automáticos desabilitados |

Sem equivalente direto nesta fase:

- `stats.price`: `WeaponData` não possui campo de preço; será usado quando aquisição/economia forem implementadas.
- `acquisition`: permanece apenas metadado `Unspecified`.
- `rarity.NameColor`: reservado para tooltip futuro.
- defaults e overrides de luz: reservados para iluminação futura.
- `LightIntensity`: não é traduzido para API do jogo nesta fase.

## Edição de assets

O handler `Content.AssetRequested`:

1. carrega `assets/weapons.png` somente quando o asset próprio `Mods/romulot.ValleyArmory/Weapons` é solicitado;
2. edita `Data/Weapons` de forma aditiva;
3. verifica se a chave já existe antes de adicionar;
4. em colisão, mantém a entrada existente e registra um único `Warning`;
5. não substitui o dicionário nem injeta os outros seis IDs de armas.

## Teste manual no jogo

Pré-condições:

- Stardew Valley `1.6.15`;
- SMAPI `4.5.2`;
- jogo fechado durante a instalação;
- backup normal do save recomendado antes de testar mods em desenvolvimento.

### Instalação

1. Na raiz do repositório, execute:

   ```bash
   dotnet build
   ```

2. Localize:

   ```text
   bin/Debug/net6.0/ValleyArmory 0.1.0.zip
   ```

3. Extraia o ZIP na pasta `Mods` do Stardew Valley. O resultado deve ser:

   ```text
   Mods/ValleyArmory/manifest.json
   Mods/ValleyArmory/ValleyArmory.dll
   Mods/ValleyArmory/assets/armory.json
   Mods/ValleyArmory/assets/weapons.png
   Mods/ValleyArmory/i18n/default.json
   Mods/ValleyArmory/i18n/pt-BR.json
   ```

4. Remova ou substitua manualmente qualquer instalação anterior de `Mods/ValleyArmory` para não misturar arquivos de builds diferentes.

### Execução

1. Inicie o jogo pelo SMAPI.
2. Confirme no console a mensagem de carregamento do Valley Armory sem erros de catálogo.
3. Abra qualquer save.
4. No console do SMAPI, execute exatamente:

   ```text
   va_give miners-blade
   ```

5. Confirme que aparece uma mensagem localizada de sucesso.
6. Se o inventário estiver cheio, confirme que a arma cai aos pés do jogador e não desaparece.

### Inspeção do item

Confirme no inventário:

- nome em inglês `Miner's Blade` ou pt-BR `Lâmina do Minerador`;
- descrição correspondente ao idioma ativo;
- sprite temporário próprio, sem textura ausente;
- dano `14–22`;
- velocidade exibida como `+0` (valor interno `1`, com efeito real de 40 ms);
- chance crítica exibida como `+1` (valor interno `0.03`, isto é, 3% base);
- defesa `+1`;
- knockback perceptível e sem comportamento anormal;
- arma utilizável como espada, com ataque normal.

Não espere ainda cor de raridade, linha “Rare/Raro” ou iluminação.

### Persistência

1. Mantenha a arma no inventário ou em um baú.
2. Durma para salvar.
3. Volte ao título.
4. Recarregue o mesmo save.
5. Confirme que a arma continua presente, com o mesmo sprite e atributos.
6. Execute novamente `va_give miners-blade` e confirme que uma nova instância válida é criada.

### Evidências a registrar

- trecho do log desde o carregamento do mod até o comando;
- captura do inventário com nome, descrição e atributos;
- resultado do teste com inventário cheio;
- resultado após salvar e recarregar.

Qualquer error item, textura ausente, perda após reload ou divergência de stats bloqueia as fases de tooltip e iluminação.
