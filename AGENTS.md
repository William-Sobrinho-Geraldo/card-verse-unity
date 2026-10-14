# AGENTS.md - card-verse-unity

Diretrizes para agentes de IA e desenvolvedores que trabalharem neste repositório.
Leia este arquivo antes de propor qualquer mudança de código.

## 1. Visão geral do projeto

- **Produto:** `card-verse-unity` - jogo digital de cartas baseado na **Sueca** (jogo de vazas português/brasileiro).
- **Motor:** Unity **6000.6.5f1** (Unity 6), pipeline **URP 2D**.
- **Plataforma alvo inicial:** desktop (Windows), com layout responsivo para futuras versões mobile.
- **Pacotes relevantes:** Input System (`com.unity.inputsystem`), URP, uGUI, Test Framework, Timeline.
- **Idioma do domínio:** português (nomes de cartas, naipes e conceitos da Sueca). Código e API seguem inglês para tipos e métodos, salvo termos consagrados do jogo (ex.: `Manilha`).

## 2. Arquitetura

### 2.1 Princípio central

O **núcleo do jogo (regras) deve ser código C# puro, sem qualquer dependência de `UnityEngine`**. Isso garante testabilidade, determinismo e portabilidade (simulações, servidor autoritativo, IA). A camada Unity apenas **apresenta** estado e **captura** entrada.

Fluxo de dependência (permitido):

```
Presentation  ──►  Application  ──►  Domain  ◄──  Infrastructure (adapters)
```

O `Domain` não conhece ninguém. Nada aponta de volta para `UnityEngine`.

### 2.2 Estrutura de pastas recomendada (em `Assets/`)

```
Assets/
  _Project/                     # prefixo "_" mantém a pasta no topo do Project view
    Scripts/
      Domain/                   # C# puro (sem UnityEngine)
        Sueca/
          Cards/                # Card, Suit, Rank, Deck, CardPoints
          Rules/                # TrickRules, FollowSuit, TrumpRules
          Scoring/              # HandScoring, RubberScoring
          Match/                # Deal, Trick, MatchState, GamePhase
          Random/               # IShuffler, IRandomSource (seed)
      Application/              # casos de uso / orquestração (C# puro)
        Sueca/
          StartMatchUseCase
          PlayCardUseCase
          ResolveTrickUseCase
          MatchController
      Presentation/             # MonoBehaviours, views, input, UI
        Board/
        Cards/
        UI/
        Input/
      Infrastructure/           # persistência, áudio, rede, config
    ScriptableObjects/          # dados de configuração e catálogos (assets)
    Prefabs/
    Art/
    Scenes/
    Tests/
      EditMode/                 # testes de domínio (rápidos, sem cena)
      PlayMode/                 # testes de integração com Unity
```

### 2.3 Assembly Definitions (asmdef)

Separar o projeto em assemblies por camada. Nomes de assembly com prefixo `CardVerse.`:

| Assembly | Referencia | Pode usar UnityEngine |
|---|---|---|
| `CardVerse.Domain` | nenhuma | **NÃO** |
| `CardVerse.Application` | `CardVerse.Domain` | **NÃO** |
| `CardVerse.Presentation` | `CardVerse.Domain`, `CardVerse.Application` | Sim |
| `CardVerse.Infrastructure` | `CardVerse.Domain`, `CardVerse.Application` | Sim |
| `CardVerse.Tests.EditMode` | `CardVerse.Domain`, `CardVerse.Application`, TestAssemblies | Sim (uso restrito) |
| `CardVerse.Tests.PlayMode` | todos + TestAssemblies | Sim |

- Ativar `Auto Referenced = false` e `Override References` conforme necessário para impedir vazamento de dependências.
- O asmdef de exemplo `Unity.2D.Welcome` é conteúdo do template e **não** deve ser usado como referência de estilo.

### 2.4 Padrões de arquitetura

- **Estado imutável onde possível:** `Card` deve ser `readonly struct`; estados de partida expostos como snapshots imutáveis.
- **Determinismo:** toda aleatoriedade (embaralhar, cortar) passa por `IRandomSource`/`IShuffler` injetado. Nunca chamar `UnityEngine.Random` ou `System.Random` diretamente no domínio. Toda partida deve ser reproduzível a partir de uma **seed**.
- **Separação de fases:** modelar o jogo como máquina de estados explícita (`GamePhase`: `Dealing`, `Playing`, `Scoring`, `Finished`) em vez de flags espalhadas.
- **Eventos, não acoplamento direto:** a `Presentation` reage a eventos do `Application`/`Domain` (ex.: `TrickResolved`, `HandScored`). Evitar que views leiam estado interno por polling em `Update`.
- **ScriptableObjects** apenas para **dados de configuração/arte** (catálogo de cartas, sprites, balanceamento). Regras de jogo **não** ficam em ScriptableObjects.
- **Sem lógica de jogo em MonoBehaviours.** MonoBehaviour só: receber input, orquestrar chamadas e atualizar visual.

## 3. Padrões C#

- **Versão de linguagem:** acompanhar o compilador do Unity (atualmente **C# 9**). Não usar recursos de C# 10+ (ex.: file-scoped namespaces, `record struct`, `required`).
- **Namespaces:** bloco (`namespace X { }`), conforme o template. Root namespace `CardVerse`.
- **Naming:**
  - `PascalCase`: tipos, métodos, propriedades, eventos, constantes públicas.
  - `camelCase`: parâmetros e variáveis locais.
  - `_camelCase`: campos privados/instância.
  - `s_camelCase`: campos estáticos privados. `k_PascalCase` para constantes privadas de classe.
  - Interfaces com prefixo `I` (`IShuffler`).
  - Sufixos claros: `Controller`, `View`, `UseCase`, `Service`, `Factory`, `Provider`.
- **Campos:** nunca campos públicos mutáveis expostos. Em MonoBehaviours usar `[SerializeField] private` + propriedade somente-leitura quando precisar expor.
  ```csharp
  [SerializeField, Tooltip("Tempo entre vazas em segundos.")]
  private float _trickDuration = 0.4f;
  ```
- **Imutabilidade:** aplicar `readonly`, `const` e `in`/`readonly struct` sempre que possível. Preferir tipos de valor pequenos para `Card`/`Rank`/`Suit`.
- **Comparações de rank:** **NUNCA** comparar `enum` de `Rank` por valor numérico para decidir quem vence a vaza. A força na Sueca (`A, 7, K, J, Q, 6, 5, 4, 3, 2`) não coincide com a ordem numérica. Use uma tabela/`RankStrength` dedicada.
- **Pontos de carta:** consultar uma tabela de valores (`CardPoints`), não calcular por aritmética de `Rank`.
- **Nullabilidade:** evitar `null` retornado; preferir `bool TryX(out ...)`. Não introduzir `null` em coleções do domínio. Ativar `#nullable` apenas em assemblies de C# puro quando o time concordar.
- **Tratamento de erro:** argumentos inválidos lançam `ArgumentException`; violações de regra de jogo usam exceções de domínio específicas (ex.: `InvalidMoveException`) ou resultado tipado, nunca `Debug.Log` como controle de fluxo.
- **Allocations:** evitar alocação em `Update`/loop de jogo. Sem LINQ em hot paths do domínio; cachear arrays/listas reutilizáveis.
- **Comentários:** código autoexplicativo. Comentar apenas o "porquê" não óbvio. XML docs em APIs públicas do domínio.

## 4. Regras de domínio da Sueca (fonte de verdade)

Regras que o motor **deve** implementar. Divergências devem ser decididas aqui e cobertas por testes.

### 4.1 Baralho

- **40 cartas**: baralho francês sem os `8`, `9` e `10`.
- **Naipes:** `Ouros`, `Copas`, `Espadas`, `Paus`.
- **Ordem de força por naipe (maior → menor):** `Ás, 7, Rei, Valete, Dama, 6, 5, 4, 3, 2`.
- **Valor em pontos:** `Ás = 11`, `7 = 10`, `Rei = 4`, `Valete = 3`, `Dama = 2`, demais = `0`.
- **Total do baralho = 120 pontos** (cada naipe soma exatamente 30).
- O `7` é a **segunda carta mais forte** (a "manilha"/"bisca") e vence o Rei.
- O **Valete vence a Dama** (particularidade da Sueca).

### 4.2 Formação e distribuição

- **4 jogadores**, em **2 parcerias fixas** de 2; parceiros sentam-se **frente a frente** (Norte/Sul vs. Leste/Oeste).
- Cada jogador recebe **10 cartas** (todo o baralho é distribuído, sem monte/deadstock).
- O **turno de dar passa para a direita** (sentido anti-horário em Portugal; **horário no Brasil** - parametrizar a direção da mesa).
- A **última carta do dealer** (fundo do baralho) é **virada para cima**: seu naipe define o **trunfo** da mão. A carta permanece na mão do dealer e é conhecida por todos.

### 4.3 Jogada (vazas)

- O jogador **à direita do dealer** (ou o que embaralhou, conforme a variante) começa a primeira vaza.
- **Vazas são resolvidas em 10 rodadas** (10 vazas).
- **Obrigação de seguir naipe (assistir):** se o jogador possui carta do naipe puxado, **deve** jogá-la. Sem carta do naipe, pode jogar **qualquer** carta - o trunfo é **opcional**, não obrigatório.
- **Quem vence a vaza:**
  1. Se houver trunfos na vaza, vence o **trunfo de maior força**.
  2. Caso contrário, vence a **carta de maior força do naipe puxado**.
  3. Carta descartada de outro naipe **nunca** vence.
- O **vencedor da vaza lidera a próxima**.
- Sem sinais combinados entre parceiros (porém jogar carta de valor sobre vaza do parceiro, "**engordar**", é legítimo).

### 4.4 Pontuação da mão

- Cada parceria soma os pontos das cartas que capturou em vazas. Total = 120.
- **Mais de 60 pontos vence a mão:**
  - `61 a 90` pontos → **1 jogo (game)**.
  - `91 a 119` pontos → **2 jogos** ("risca").
  - **Todas as 10 vazas** → **4 jogos** ("**bandeira**") e vitória imediata da partida.
  - Observação: capturar 120 pontos **sem** levar todas as vazas (perdendo uma vaza sem pontos) vale **2 jogos**, **não** bandeira.
- **Empate 60-60:** ninguém pontua; a próxima mão decidida vale **1 jogo extra** (bônus acumulável a cada novo empate).

### 4.5 Partida (rubber / "partida")

- Vence a partida quem chegar primeiro a **4 jogos**.
- `bandeira` (todas as vazas) encerra a partida imediatamente.

### 4.6 Glossário do domínio

| Termo | Significado |
|---|---|
| `Manilha` / `Bisca` | O 7, segunda carta mais forte do naipe |
| `Trunfo` | Naipe definido pela carta virada do dealer |
| `Vaza` (trick) | Rodada com uma carta de cada jogador |
| `Parceria` (partnership) | Dupla fixa de jogadores |
| `Mão` (hand) | Conjunto de 10 vazas distribuídas |
| `Engordar` | Colocar carta de valor sobre vaza que o parceiro já vence |
| `Bandeira` | Vencer todas as 10 vazas (4 jogos) |
| `Risca` | Fazer 91-119 pontos (2 jogos) |
| `Palha` | Cartas sem valor (6, 5, 4, 3, 2) |

## 5. Convenções de código

### 5.1 Unity / MonoBehaviour

- Preferir `[SerializeField] private` a campos públicos.
- Anotar componentes obrigatórios com `[RequireComponent]`.
- Usar `TryGetComponent<T>(out var x)` em vez de `GetComponent` + null check.
- **Proibido** `GameObject.Find*` / `FindObjectOfType*` em runtime de jogo. Injetar referências via serialização, `ScriptableObject` ou um `ServiceLocator`/DI explícito.
- Cachear `Transform`/componentes em `Awake`.
- Evitar `Update` onde eventos/`Coroutine`/`UniTask` bastarem.
- Input via **Input System** (actions), não `Input.GetKey` legado.
- Assets novos: manter o `.meta` correspondente versionado; não renomear/mover assets fora do Unity Editor para não corromper GUIDs.

### 5.2 Testes

- **Toda regra de domínio em `Domain` deve ter teste em `Tests/EditMode`** (rápidos, sem carregar cena).
- Cobrir obrigatoriamente: ordem de força, obrigação de seguir naipe, resolução de vaza com/sem trunfo, cálculo de pontos, limiares de pontuação (60/61/90/91/120), bandeira, empate com bônus acumulado.
- Usar **NUnit** (`[Test]`, `[TestCase]`) do Unity Test Framework.
- Testes de determinismo: mesmo `seed` ⇒ mesma distribuição/partida.
- Integrações que dependem de cena ficam em `Tests/PlayMode`.

### 5.3 Estilo de código

- Seguir o `.editorconfig` do repositório (quando presente) e o formato do Unity/Rider.
- Indentação de **4 espaços**; sem tabs em `.cs`.
- `using` fora do namespace, ordenados (System primeiro).
- Membros: campos, construtores, propriedades, métodos públicos, privados.
- Uma classe pública por arquivo; nome do arquivo = nome do tipo.
- Evitar regions, `#region`, e código morto/comentado.

### 5.4 Git

- Branch principal: `main`.
- Mensagens de commit no imperativo e curtas (ex.: `Add trick resolution rules`). Evitar commits gigantes.
- **Não commitar artefatos gerados:** `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Build/`, `*.csproj`, `*.sln`, `*.slnx` (já cobertos pelo `.gitignore`).
- Sempre manter `.meta` junto do asset correspondente.

## 6. O que evitar (checklist de revisão)

- [ ] Lógica de regras dentro de `MonoBehaviour` ou `ScriptableObject`.
- [ ] Referência a `UnityEngine` em `Domain`/`Application`.
- [ ] Comparar força de cartas por valor de enum ou por `>` numérico.
- [ ] Uso de `UnityEngine.Random` no domínio (quebra determinismo).
- [ ] `Find*`, LINQ ou alocações em hot path.
- [ ] Campos públicos mutáveis / singletons globais escondidos.
- [ ] Commit de `Library/`, `.csproj`, `.sln` ou assets sem `.meta`.
- [ ] Nova regra de Sueca sem teste correspondente.

## 7. Como validar mudanças

1. Abrir o projeto no Unity 6000.6.5f1 e garantir compilação sem erros/warnings novos.
2. Rodar os testes pelo **Test Runner**:
   - EditMode: `Window > General > Test Runner > EditMode > Run All`.
   - PlayMode: aba `PlayMode`.
3. Para passos de build/CI, descrever o comando usado (ainda não há pipeline de CI configurado neste repositório).
