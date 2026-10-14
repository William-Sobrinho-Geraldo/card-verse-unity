# RULEBOOK — Sueca

Fonte de verdade das regras implementadas pelo motor (`SuecaOnline.Domain`).
Divergencias devem ser decididas aqui e cobertas por teste.

## 1. Baralho

- **40 cartas**: baralho frances sem os `8`, `9` e `10`.
- **Naipes:** `Ouros`, `Copas`, `Espadas`, `Paus`.
- **Ordem de forca por naipe (maior → menor):** `As, 7, Rei, Valete, Dama, 6, 5, 4, 3, 2`.
- **Valor em pontos:** `As = 11`, `7 = 10`, `Rei = 4`, `Valete = 3`, `Dama = 2`, demais `0`.
- **Total do baralho = 120 pontos** (cada naipe soma exatamente 30).
- O `7` (manilha/bisca) e a segunda carta mais forte e vence o Rei.
- O `Valete` vence a `Dama` (particularidade da Sueca).

Implementacao: `CardPoints` (pontos), `RankStrength` (forca), `Deck` (validacao).

## 2. Formacao e distribuicao

- **4 jogadores** em **2 parcerias fixas**; parceiros sentam-se frente a frente
  (`NorthSouth` vs `EastWest`).
- Cada jogador recebe **10 cartas** (todo o baralho e distribuido, sem monte).
- O turno de dar passa para a direita a cada mao (`Dealer = Seats.Next(Dealer)`).
- A **ultima carta do dealer** (fundo do baralho) e virada: seu naipe define o
  **trunfo**. A carta permanece na mao do dealer e e conhecida por todos.

## 3. Jogada (vazas)

- A mao tem **10 vazas**. O jogador a direita do dealer inicia a primeira.
- **Obrigacao de assistir ao naipe:** quem possui carta do naipe puxado **deve**
  joga-la. Sem carta do naipe, pode jogar **qualquer** carta — o trunfo e
  **opcional**, nao obrigatorio.
- **Vencedor da vaza:**
  1. Se houver trunfo(s), vence o **trunfo de maior forca**.
  2. Caso contrario, vence a **carta de maior forca do naipe puxado**.
  3. Carta descartada de outro naipe **nunca** vence.
- O **vencedor lidera a proxima vaza**.
- Sem sinais combinados entre parceiros; "engordar" (colocar carta de valor sobre
  vaza ja vencida pelo parceiro) e legitimo.

Implementacao: `FollowSuitRule`, `TrickRules`.

## 4. Pontuacao da mao

Total disponivel = 120 pontos. Cada parceria soma os pontos das cartas que capturou.

| Pontos | Resultado |
|---|---|
| `61 a 90` | **1 jogo** |
| `91 a 119` | **2 jogos** (risca) |
| `120` com **todas as 10 vazas** | **4 jogos** (bandeira) — vitoria imediata |
| `120` **sem** todas as vazas | **2 jogos** (nao e bandeira) |
| `60 x 60` (empate) | ninguem pontua; proxima mao decidida vale **1 jogo extra** (acumulavel) |

- Vence a mao quem tiver **mais de 60** pontos.
- O bonus de empate acumula a cada novo empate e e consumido na proxima mao decidida.

Implementacao: `RoundScoring.Calculate`.

## 5. Partida

- Vence a partida quem chegar primeiro a **4 jogos**.
- **Bandeira** encerra a partida imediatamente (4 jogos).
- Empates 60-60 nao concedem jogos; apenas acumulam bonus.

Implementacao: `MatchState` (limiar `GamesToWinMatch = 4`).

## 6. Glossario

| Termo | Significado |
|---|---|
| `Manilha` / `Bisca` | O 7, segunda carta mais forte do naipe |
| `Trunfo` | Naipe definido pela carta virada do dealer |
| `Vaza` (trick) | Rodada com uma carta de cada jogador |
| `Parceria` | Dupla fixa de jogadores |
| `Mao` (hand) | Conjunto de 10 vazas distribuidas |
| `Engordar` | Colocar carta de valor sobre vaza que o parceiro ja vence |
| `Bandeira` | Vencer todas as 10 vazas (4 jogos) |
| `Risca` | Fazer 91-119 pontos (2 jogos) |
| `Palha` | Cartas sem valor (6, 5, 4, 3, 2) |

## 7. Mapa regra → teste

| Regra | Teste |
|---|---|
| Baralho 40 cartas / 120 pontos | `DeckTests` |
| Hierarquia de forca / trunfo | `CardHierarchyTests`, `TrickResolutionTests` |
| Assistir ao naipe | `FollowSuitRuleTests`, `MatchStateTests` |
| Turno / posse da carta | `MatchStateTests` |
| Limiares de pontuacao / bandeira / empate | `RoundScoringTests` |
| 10 vazas somando 120 pontos | `MatchStateTests.FullRound_TenTricks_TotalExactly120Points` |
| Determinismo por seed | `ShuffleDeterminismTests`, `MatchStateTests` |
