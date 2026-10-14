# STATE_MACHINE — Partida de Sueca

A partida e modelada como maquina de estados explicita (`GamePhase`) em
`MatchState`. Nao ha flags dispersas: toda operacao valida a fase atual e lanca
`InvalidPhaseException` quando aplicada fora de hora.

## 1. Fases

| Fase | Significado |
|---|---|
| `WaitingForPlayers` | Estado inicial; jogadores aguardando inicio. |
| `DealingCards` | Partida iniciada; pronta para distribuir a mao. |
| `AwaitingPlayerMove` | Aguardando a jogada do `CurrentTurn`. |
| `ResolvingTrick` | A vaza tem 4 cartas e aguarda resolucao. |
| `TrickCompleted` | Vaza resolvida; aguarda confirmacao para seguir. |
| `RoundCompleted` | As 10 vazas terminaram; mao pontuada. |
| `MatchCompleted` | Uma parceria atingiu 4 jogos ou fez bandeira. |

## 2. Diagrama de transicoes

```
                 StartMatch()
WaitingForPlayers ─────────────► DealingCards
                                      │  DealRound(seed)
                                      ▼
                              ┌► AwaitingPlayerMove
                              │        │  PlayCard (1..3 cartas)
                              │        │  PlayCard (4a carta)
                              │        ▼
                              │   ResolvingTrick
                              │        │  ResolveTrick()
                              │        ▼
                              │   TrickCompleted
                              │        │  BeginNextTrick()
                              │        │
                              │        ├─ (vazas < 10) ───────────┘
                              │        │
                              │        └─ (10a vaza) ─► RoundCompleted
                              │                             │   └─ (4 jogos/bandeira) ─► MatchCompleted
                              └────────── DealRound(seed) ──┘
```

## 3. API principal (`MatchState`)

```csharp
MatchState match = MatchState.CreateDeterministic();
match.StartMatch();              // WaitingForPlayers -> DealingCards
match.DealRound(seed);           // -> AwaitingPlayerMove (10 cartas, trunfo)
while (match.Phase == GamePhase.AwaitingPlayerMove) {
    Seat seat = match.CurrentTurn;
    IReadOnlyList<Card> legal = match.GetLegalMoves(seat);
    match.PlayCard(seat, legal[0]);
}
if (match.Phase == GamePhase.ResolvingTrick) match.ResolveTrick();
if (match.Phase == GamePhase.TrickCompleted) match.BeginNextTrick();
```

### Metodos

| Metodo | Transicao | Validacao |
|---|---|---|
| `StartMatch()` | `WaitingForPlayers` → `DealingCards` | fase de origem |
| `DealRound(seed)` | `DealingCards`/`RoundCompleted` → `AwaitingPlayerMove` | nao terminada |
| `PlayCard(seat, card)` | `AwaitingPlayerMove` → (mesma) ou `ResolvingTrick` | turno, posse e naipe |
| `ResolveTrick()` | `ResolvingTrick` → `TrickCompleted` | vaza completa |
| `BeginNextTrick()` | `TrickCompleted` → `AwaitingPlayerMove`/`RoundCompleted`/`MatchCompleted` | fase de origem |

### Estado exposto

- `Phase`, `Trump`, `Dealer`, `Leader`, `CurrentTurn`, `RoundNumber`.
- `CurrentTrick`, `LastTrick`, `LastHandResult`, `MatchWinner`, `PendingTieBonusGames`.
- Consultas: `GetHand`, `GetCapturedCards`, `GetCapturedPoints`, `GetGames`, `GetTricksWon`.
- Eventos: `TrickResolved`, `HandScored`, `MatchWon`.

## 4. Validacoes de jogada

`PlayCard` rejeita, nesta ordem:

1. Fase diferente de `AwaitingPlayerMove` → `InvalidPhaseException`.
2. `seat != CurrentTurn` → `InvalidMoveException` (jogada fora de turno).
3. Carta ausente da mao → `InvalidMoveException`.
4. Naipe puxado disponivel na mao e carta jogada de outro naipe →
   `InvalidMoveException` (violacao de assistir ao naipe).

## 5. Determinismo

`CreateDeterministic()` injeta `Func<int, IShuffler>` que monta
`FisherYatesShuffler(new SeededRandomSource(seed))`. O mesmo seed produz a mesma
distribuicao, o mesmo trunfo e o mesmo desenrolar deterministico da mao.
