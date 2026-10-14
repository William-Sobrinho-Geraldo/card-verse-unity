# card-verse-unity (SuecaOnline)

Jogo digital de cartas baseado na **Sueca** (jogo de vazas portugues/brasileiro),
desenvolvido em **Unity 6000.6.5f1** com pipeline **URP 2D**.

> **Estado atual: Fase 1 — Fundacao, Arquitetura C# e Motor de Regras.**
> O nucleo de regras (`SuecaOnline.Domain`) esta implementado como C# puro, sem
> qualquer dependencia de `UnityEngine`, e coberto por testes EditMode (NUnit).

## Estrutura

```
Assets/_Project/
  Scripts/Domain/Sueca/     # Nucleo C# puro (sem UnityEngine) - Fase 1
    Cards/                  # Suit, Rank, Card, CardPoints, RankStrength, Deck
    Random/                 # IRandomSource, IShuffler, SeededRandomSource, FisherYatesShuffler
    Rules/                  # FollowSuitRule, TrickRules
    Match/                  # GamePhase, Seat, Partnership, Trick, MatchState, RoundScoring
    Exceptions/             # InvalidMoveException, InvalidPhaseException
  Tests/EditMode/           # Testes NUnit do dominio (sem cena)
Documentation/              # Especificacao, arquitetura, regras e maquina de estados
```

## Documentacao

| Documento | Conteudo |
|---|---|
| [Documentation/PRODUCT_SPEC.md](Documentation/PRODUCT_SPEC.md) | Visao de produto, escopo da Fase 1, criterios de aceite |
| [Documentation/ARCHITECTURE.md](Documentation/ARCHITECTURE.md) | Camadas, asmdefs, padroes e determinismo |
| [Documentation/RULEBOOK.md](Documentation/RULEBOOK.md) | Regras oficiais da Sueca implementadas pelo motor |
| [Documentation/STATE_MACHINE.md](Documentation/STATE_MACHINE.md) | Fases, transicoes e API do `MatchState` |

## Como rodar os testes

### Unity Test Runner

1. Abra o projeto no Unity 6000.6.5f1.
2. `Window > General > Test Runner > EditMode > Run All`.

### Linha de comando (batchmode)

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe" `
  -batchmode -nographics `
  -projectPath "C:\New_repos\card-verse-unity" `
  -runTests -testPlatform EditMode `
  -testResults "editmode-results.xml" `
  -logFile "unity-editmode.log"
```

## Principios inegociaveis

- `Domain` e `Application` **nao** referenciam `UnityEngine` (asmdef com `noEngineReferences`).
- Toda aleatoriedade passa por `IRandomSource`/`IShuffler` injetados — partidas reproduziveis por seed.
- Forca de carta usa tabela dedicada (`RankStrength`), **nunca** comparacao numerica de enum.
- Pontos de carta usam tabela dedicada (`CardPoints`), **nunca** aritmetica de `Rank`.
