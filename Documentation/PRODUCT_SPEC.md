# PRODUCT_SPEC — SuecaOnline

## 1. Visao

`SuecaOnline` e um jogo digital de cartas baseado na **Sueca**, jogo de vazas de
origem portuguesa/brasileira. A plataforma alvo inicial e desktop (Windows), com
layout responsivo pensado para futuras versoes mobile.

## 2. Objetivo da Fase 1

Entregar a **fundacao tecnica** do produto:

1. Documentacao de produto, arquitetura, regras e maquina de estados.
2. Biblioteca de **dominio em C# puro** (`SuecaOnline.Domain`) implementando o
   motor de regras da Sueca, sem qualquer dependencia de `UnityEngine`.
3. **Testes EditMode (NUnit)** cobrindo os invariantes do motor.

A Fase 1 **nao** inclui UI, arte, audio, rede ou IA. A camada Unity apenas
consumira o dominio em fases posteriores.

## 3. Escopo entregue

### 3.1 Modelo de cartas

- `Card` — `readonly struct` imutavel (`Suit` + `Rank`), com igualdade por valor.
- `Suit` — `Ouros`, `Copas`, `Espadas`, `Paus`.
- `Rank` — `As`, `Sete`, `Rei`, `Valete`, `Dama`, `Seis`, `Cinco`, `Quatro`, `Tres`, `Dois`.
- `CardPoints` — pontos classicos (`As=11, 7=10, Rei=4, Valete=3, Dama=2`, demais `0`).
- `RankStrength` — hierarquia separada da pontuacao
  (`As > 7 > Rei > Valete > Dama > 6 > 5 > 4 > 3 > 2`).
- `Deck` — 40 cartas unicas, validando total de **120 pontos** e 10 cartas por naipe.

### 3.2 Aleatoriedade deterministica

- `IRandomSource` e `IShuffler` — contratos injetaveis.
- `SeededRandomSource` — RNG xorshift32 reproduzivel por seed.
- `FisherYatesShuffler` — embaralhamento Fisher-Yates deterministico.

### 3.3 Regras

- `FollowSuitRule` — obrigacao de assistir ao naipe (trunfo opcional).
- `TrickRules` — resolucao de vaza (trunfo > naipe puxado > descarte).

### 3.4 Partida

- `MatchState` — maquina de estados explicita (`GamePhase`).
- `RoundScoring` — pontuacao pura de mao (limiares, bandeira, empate com bonus).

## 4. Criterios de aceite (Fase 1)

- [x] Todos os arquivos C# do `Domain` compilam sem depender de componentes de UI da Unity.
- [x] Testes EditMode passam via Unity Test Runner.
- [x] Baralho com 40 cartas unicas totalizando 120 pontos.
- [x] Hierarquia de forca correta, incluindo trunfo.
- [x] Rejeicao de jogadas fora de turno ou que violem a obrigacao de assistir ao naipe.
- [x] 10 vazas contabilizando exatamente 120 pontos ao fim da partida.

## 5. Fora de escopo (proximas fases)

- Camada `Presentation` (MonoBehaviours, views, input, UI).
- Camada `Application` (casos de uso) e `Infrastructure` (persistencia, audio, rede).
- IA de jogadores, multiplayer, matchmaking e servidor autoritativo.
