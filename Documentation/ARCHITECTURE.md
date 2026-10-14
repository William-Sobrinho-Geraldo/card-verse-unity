# ARCHITECTURE — SuecaOnline

## 1. Principio central

O nucleo de regras e **C# puro, sem dependencia de `UnityEngine`**. Isso garante
testabilidade rapida, determinismo, portabilidade (simulacoes, IA, servidor) e
permite rodar a logica fora do Editor.

```
Presentation  ──►  Application  ──►  Domain  ◄──  Infrastructure (adapters)
```

O `Domain` nao conhece ninguem. Nada aponta de volta para `UnityEngine`.

> Fase 1 implementa apenas o `Domain` e seus testes.

## 2. Assemblies (asmdef)

| Assembly | Referencias | UnityEngine | `noEngineReferences` |
|---|---|---|---|
| `SuecaOnline.Domain` | nenhuma | **Nao** | `true` |
| `SuecaOnline.Tests.EditMode` | `SuecaOnline.Domain`, TestAssemblies | Sim (restrito ao Editor) | `false` |

Localizacao:

```
Assets/_Project/Scripts/Domain/Sueca/SuecaOnline.Domain.asmdef
Assets/_Project/Tests/EditMode/SuecaOnline.Tests.EditMode.asmdef
```

O `Domain` e marcado com `autoReferenced: false`, exigindo referencia explicita,
o que impede vazamento acidental de dependencia.

## 3. Organizacao do codigo

```
Domain/Sueca/
  Cards/      Suit, Rank, Card, CardPoints, RankStrength, Deck
  Random/     IRandomSource, IShuffler, SeededRandomSource, FisherYatesShuffler
  Rules/      FollowSuitRule, TrickRules
  Match/      GamePhase, Seat, Partnership, Seats, PlayedCard, Trick,
              TrickRecord, HandResult, RoundScoring, MatchState
  Exceptions/ InvalidMoveException, InvalidPhaseException
```

Namespaces acompanham as pastas, com raiz `SuecaOnline.Domain`.

## 4. Padroes adotados

- **Imutabilidade:** `Card`, `PlayedCard`, `TrickRecord`, `HandResult` sao
  `readonly struct`; o estado completo fica exposto como snapshots somente-leitura.
- **Determinismo:** toda aleatoriedade passa por `Func<int, IShuffler>`, indexada
  por seed. Nao ha uso de `System.Random` nem `UnityEngine.Random` no dominio.
- **Maquina de estados explicita:** `GamePhase` substitui flags dispersas.
- **Eventos, nao polling:** `MatchState` expoe `TrickResolved`, `HandScored` e
  `MatchWon` para a camada de apresentacao reagir.
- **Sem LINQ em hot paths** e sem alocacao por jogada nos caminhos principais.
- **Erros:** violacao de regra lanca `InvalidMoveException`; operacao fora de fase
  lanca `InvalidPhaseException`; baralho invalido lanca `InvalidDeckException`.

## 5. Separacao forca x pontuacao

`Rank` representa apenas o valor nominal do baralho comum. A forca de jogo mora em
`RankStrength` e os pontos em `CardPoints`. **Nunca** comparar `Rank` por valor
numerico: o 7 e o Valete, por exemplo, sao mais fortes que cartas de valor numerico
maior (Rei e Dama). Os testes `CardHierarchyTests` guardam essa invariante.

## 6. Reprodutibilidade

`MatchState.CreateDeterministic()` cria uma partida cujo embaralhamento e
deterministico. Com o mesmo seed:

- a mesma distribuicao de maos e obtida;
- o mesmo trunfo (naipe da ultima carta do dealer) e definido;
- a mesma sequencia de vazas pode ser reproduzida.

## 7. Fluxo de uma mao

1. `StartMatch()` — `WaitingForPlayers` → `DealingCards`.
2. `DealRound(seed)` — distribui 10 cartas por jogador, define trunfo, `AwaitingPlayerMove`.
3. `PlayCard(seat, card)` x4 — valida turno, posse e naipe; ao 4o, `ResolvingTrick`.
4. `ResolveTrick()` — `TrickCompleted`.
5. `BeginNextTrick()` — vencedor lidera; ou `RoundCompleted`/`MatchCompleted` na 10a vaza.

Detalhes em [STATE_MACHINE.md](STATE_MACHINE.md).
