using System;
using System.Collections;
using System.Collections.Generic;
using SuecaOnline.Application.Sueca;
using SuecaOnline.Domain.Sueca.Cards;
using SuecaOnline.Domain.Sueca.Match;
using SuecaOnline.Presentation.Animation;
using SuecaOnline.Presentation.Cards;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SuecaOnline.Presentation.UI
{
    /// <summary>
    /// Integra o motor <see cref="MatchState"/> (SuecaOnline.Domain) a interface local.
    /// O jogador humano controla o assento Sul; Norte/Leste/Oeste jogam via
    /// <see cref="AiMoveSelector"/>. Nao implementa regras: apenas orquestra e apresenta.
    /// </summary>
    public sealed class LocalGameManager : MonoBehaviour
    {
        [SerializeField, Tooltip("View central da vaza.")]
        private TableView _tableView;

        [SerializeField, Tooltip("Ancora da mao do jogador (Sul).")]
        private RectTransform _southHandRoot;

        [SerializeField, Tooltip("Ancora da mao do assento Norte.")]
        private RectTransform _northHandRoot;

        [SerializeField, Tooltip("Ancora da mao do assento Leste.")]
        private RectTransform _eastHandRoot;

        [SerializeField, Tooltip("Ancora da mao do assento Oeste.")]
        private RectTransform _westHandRoot;

        [SerializeField, Tooltip("Prefab de carta usado nas maos.")]
        private CardView _cardViewPrefab;

        [SerializeField, Tooltip("Slot dedicado que exibe a carta de trunfo (carta virada do dealer).")]
        private RectTransform _trumpSlot;

        [SerializeField, Tooltip("Texto do trunfo.")]
        private TextMeshProUGUI _trumpText;

        [SerializeField, Tooltip("Texto do placar.")]
        private TextMeshProUGUI _scoreText;

        [SerializeField, Tooltip("Texto de status/turno.")]
        private TextMeshProUGUI _statusText;

        [SerializeField, Tooltip("Banner de vencedor da partida.")]
        private TextMeshProUGUI _winnerText;

        [SerializeField, Tooltip("Botao de proxima mao.")]
        private Button _nextHandButton;

        [SerializeField, Tooltip("Botao de nova partida.")]
        private Button _newMatchButton;

        [SerializeField, Tooltip("Atraso (segundos) entre jogadas automaticas.")]
        private float _aiDelay = 0.6f;

        [SerializeField, Tooltip("Semente base fixa para partidas reproduziveis. Use 0 para aleatoriedade real a cada partida.")]
        private int _fixedSeed;

        [SerializeField, Tooltip("Camada de feedback (indicador de turno e toasts). Se vazio, e criada em runtime a partir da mesa.")]
        private GameFeedbackUI _feedbackUI;

        [SerializeField, Min(0f), Tooltip("Duracao do voo de cada carta ao ser distribuida.")]
        private float _dealDuration = 0.32f;

        [SerializeField, Min(0f), Tooltip("Atraso entre cartas sucessivas ao distribuir.")]
        private float _dealStagger = 0.045f;

        [SerializeField, Min(0f), Tooltip("Duracao do movimento da carta da mao para a mesa.")]
        private float _playDuration = 0.3f;

        [SerializeField, Min(0f), Tooltip("Pausa dramatica antes de resolver a vaza completa.")]
        private float _trickPause = 0.5f;

        [SerializeField, Min(0f), Tooltip("Duracao em que as cartas da vaza deslizam para o vencedor e desvanecem.")]
        private float _collectDuration = 0.55f;

        [SerializeField, Min(0f), Tooltip("Duracao do reposicionamento das cartas na mao apos uma jogada.")]
        private float _relayoutDuration = 0.22f;

        private const float VerticalHandSpacing = 34f;
        private const float HorizontalHandSpacing = 78f;

        private readonly Dictionary<Seat, List<CardView>> _seatViews = new Dictionary<Seat, List<CardView>>();
        private readonly Dictionary<Seat, Coroutine> _relayoutRoutines = new Dictionary<Seat, Coroutine>();

        private MatchState _match;
        private Coroutine _flow;
        private Coroutine _dealRoutine;
        private bool _isDealing;
        private CardView _trumpCardView;
        private int _matchSeed;

        public MatchState Match
        {
            get { return _match; }
        }

        public TableView Table
        {
            get { return _tableView; }
        }

        /// <summary>Carta de trunfo atualmente renderizada no slot dedicado (nula se ausente).</summary>
        public CardView TrumpCardView
        {
            get { return _trumpCardView; }
        }

        /// <summary>Camada de feedback (indicador de turno e toasts).</summary>
        public GameFeedbackUI Feedback
        {
            get { return _feedbackUI; }
        }

        /// <summary>Quantidade de cartas atualmente renderizadas para um assento.</summary>
        public int GetRenderedCardCount(Seat seat)
        {
            List<CardView> views = GetSeatList(seat);
            int count = 0;

            for (int i = 0; i < views.Count; i++)
            {
                if (views[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private void Awake()
        {
            _matchSeed = GenerateMatchSeed();
            _match = MatchState.CreateDeterministic();
            EnsureFeedbackUI();
            Subscribe();

            if (_nextHandButton != null)
            {
                _nextHandButton.onClick.AddListener(StartNextRound);
                _nextHandButton.gameObject.SetActive(false);
            }

            if (_newMatchButton != null)
            {
                _newMatchButton.onClick.AddListener(StartNewMatch);
            }

            if (_winnerText != null)
            {
                _winnerText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Garante uma camada de feedback mesmo quando a cena nao a declara.
        /// Resolve a raiz do Canvas atraves da <see cref="TableView"/>, sem pesquisas globais
        /// em runtime (Find*/FindObjectOfType).
        /// </summary>
        private void EnsureFeedbackUI()
        {
            if (_feedbackUI != null)
            {
                _feedbackUI.EnsureBuilt();
                return;
            }

            Canvas canvas = null;
            if (_tableView != null)
            {
                canvas = _tableView.GetComponentInParent<Canvas>();
            }

            if (canvas == null)
            {
                return;
            }

            RectTransform rect = new GameObject("FeedbackLayer", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _feedbackUI = rect.gameObject.AddComponent<GameFeedbackUI>();
        }

        private void Start()
        {
            EnsureCardViewPrefab();
            _match.StartMatch();
            StartRound();
        }

        private void OnDestroy()
        {
            StopFlow();
            StopDeal();
            StopAllRelayouts();
            Unsubscribe();
        }

        /// <summary>Recria a partida do zero com uma nova distribuicao aleatoria.</summary>
        public void StartNewMatch()
        {
            RecreateMatch(GenerateMatchSeed());
        }

        /// <summary>
        /// Recria a partida com uma semente explicita. Destinado a testes/replay,
        /// preservando o determinismo sem afetar o fluxo aleatorio padrao.
        /// </summary>
        public void StartNewMatch(int seed)
        {
            RecreateMatch(seed);
        }

        private void RecreateMatch(int seed)
        {
            StopFlow();
            StopDeal();
            StopAllRelayouts();
            ClearAllMotion();

            if (_feedbackUI != null)
            {
                _feedbackUI.ClearAll();
            }

            Unsubscribe();
            ClearAllSeatViews();
            ClearTrumpCard();

            if (_tableView != null)
            {
                _tableView.ClearTrick();
            }

            _matchSeed = seed;
            _match = MatchState.CreateDeterministic();
            Subscribe();
            _match.StartMatch();
            StartRound();
        }

        private int GenerateMatchSeed()
        {
            if (_fixedSeed != 0)
            {
                return _fixedSeed;
            }

            int seed = Guid.NewGuid().GetHashCode();
            return seed == 0 ? 1 : seed;
        }

        private void StartNextRound()
        {
            if (_match == null || _match.IsMatchOver)
            {
                return;
            }

            StartRound();
        }

        private void StartRound()
        {
            StopFlow();
            StopDeal();
            StopAllRelayouts();
            ClearAllMotion();

            if (_tableView != null)
            {
                _tableView.ClearTrick();
            }

            ClearAllSeatViews();

            if (_nextHandButton != null)
            {
                _nextHandButton.gameObject.SetActive(false);
            }

            if (_winnerText != null)
            {
                _winnerText.gameObject.SetActive(false);
            }

            EnsureCardViewPrefab();
            _match.DealRound(_matchSeed + _match.RoundNumber);
            BuildAllSeatViews();
            ShowTrumpCard();
            RefreshHud();

            _isDealing = true;
            UpdateInteractivity();
            _dealRoutine = StartCoroutine(DealAnimation());
        }

        /// <summary>Distribuicao animada: cada carta voa do baralho central ate a mao, escalonada.</summary>
        private IEnumerator DealAnimation()
        {
            Vector3 origin = DealerOriginWorld();
            var entries = new List<DealEntry>();

            int order = 0;
            for (int index = 0; index < MatchState.CardsPerPlayer; index++)
            {
                foreach (Seat seat in Seats.All)
                {
                    List<CardView> views = GetSeatList(seat);
                    if (index >= views.Count || views[index] == null)
                    {
                        continue;
                    }

                    RectTransform rect = views[index].Rect;
                    Vector2 target = rect.anchoredPosition;
                    Vector2 start = rect.parent != null
                        ? (Vector2)rect.parent.InverseTransformPoint(origin)
                        : target;

                    rect.anchoredPosition = start;
                    rect.localScale = Vector3.one * 0.55f;
                    rect.localRotation = Quaternion.Euler(0f, 0f, ((order % 2) == 0) ? -12f : 12f);

                    entries.Add(new DealEntry
                    {
                        View = views[index],
                        Start = start,
                        Target = target,
                        StartRotation = ((order % 2) == 0) ? -12f : 12f,
                        Delay = order * _dealStagger
                    });

                    order++;
                }
            }

            float total = _dealStagger * Mathf.Max(0, entries.Count - 1) + _dealDuration;
            float elapsed = 0f;

            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;

                for (int i = 0; i < entries.Count; i++)
                {
                    DealEntry entry = entries[i];
                    if (entry.View == null)
                    {
                        continue;
                    }

                    float t = CardAnimator.Evaluate(Mathf.Clamp01((elapsed - entry.Delay) / _dealDuration), Ease.EaseOutCubic);
                    RectTransform rect = entry.View.Rect;
                    rect.anchoredPosition = Vector2.LerpUnclamped(entry.Start, entry.Target, t);
                    rect.localScale = Vector3.one * Mathf.LerpUnclamped(0.55f, 1f, t);
                    rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(entry.StartRotation, 0f, t));
                }

                yield return null;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].View == null)
                {
                    continue;
                }

                RectTransform rect = entries[i].View.Rect;
                rect.anchoredPosition = entries[i].Target;
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
            }

            _isDealing = false;
            _dealRoutine = null;
            PumpFlow();
        }

        private void StopDeal()
        {
            if (_dealRoutine != null)
            {
                StopCoroutine(_dealRoutine);
                _dealRoutine = null;
            }

            _isDealing = false;
        }

        private void StopAllRelayouts()
        {
            foreach (KeyValuePair<Seat, Coroutine> pair in _relayoutRoutines)
            {
                if (pair.Value != null)
                {
                    StopCoroutine(pair.Value);
                }
            }

            _relayoutRoutines.Clear();
        }

        /// <summary>Restaura escala/rotacao padrao das cartas apos parar animacoes em andamento.</summary>
        private void ClearAllMotion()
        {
            foreach (Seat seat in Seats.All)
            {
                List<CardView> views = GetSeatList(seat);

                for (int i = 0; i < views.Count; i++)
                {
                    if (views[i] == null)
                    {
                        continue;
                    }

                    RectTransform rect = views[i].Rect;
                    rect.localScale = Vector3.one;
                    rect.localRotation = Quaternion.identity;
                }
            }
        }

        private Vector3 DealerOriginWorld()
        {
            if (_tableView != null)
            {
                return _tableView.transform.position;
            }

            if (_trumpSlot != null)
            {
                return _trumpSlot.position;
            }

            return transform.position;
        }

        private Vector3 WinnerCornerWorld(Seat winner)
        {
            RectTransform root = RootFor(winner);
            return root != null ? root.position : transform.position;
        }

        private struct DealEntry
        {
            public CardView View;
            public Vector2 Start;
            public Vector2 Target;
            public float StartRotation;
            public float Delay;
        }

        /// <summary>
        /// Exibe a carta virada do dealer (trunfo) no slot dedicado. Chamada logo apos
        /// a distribuicao, antes de qualquer carta ser baixada na primeira vaza.
        /// </summary>
        private void ShowTrumpCard()
        {
            ClearTrumpCard();

            if (_trumpSlot == null || _cardViewPrefab == null || _match == null)
            {
                return;
            }

            CardView view = Instantiate(_cardViewPrefab, _trumpSlot);
            view.name = "TrumpCard";

            RectTransform rect = (RectTransform)view.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;

            view.SetCard(_match.TrumpCard, true);
            view.IsInteractable = false;
            _trumpCardView = view;
        }

        private void ClearTrumpCard()
        {
            if (_trumpCardView != null)
            {
                Destroy(_trumpCardView.gameObject);
                _trumpCardView = null;
            }
        }

        /// <summary>
        /// Tenta jogar uma carta pelo assento Sul. Retorna falso quando nao e o
        /// turno do Sul ou quando a carta viola as regras.
        /// </summary>
        public bool TryPlaySouthCard(Card card)
        {
            return TryPlaySouthCardCore(card, null);
        }

        /// <summary>Variante que recebe a view clicada, permitindo o feedback de "sacudida".</summary>
        public bool TryPlaySouthCard(ICardView view)
        {
            return TryPlaySouthCardCore(view != null ? view.Card : default, view);
        }

        private bool TryPlaySouthCardCore(Card card, ICardView source)
        {
            if (_match.Phase != GamePhase.AwaitingPlayerMove)
            {
                return false;
            }

            if (_match.CurrentTurn != Seat.South)
            {
                return false;
            }

            if (!IsLegalForSouth(card))
            {
                NotifyFollowSuitViolation(source);
                return false;
            }

            PlaySouthCard(card, source);
            return true;
        }

        private void PlaySouthCard(Card card, ICardView source)
        {
            Vector3 fromWorld = source != null && source is CardView handCard
                ? handCard.Rect.position
                : (_southHandRoot != null ? _southHandRoot.position : DealerOriginWorld());

            _match.PlayCard(Seat.South, card);
            RemoveSeatCard(Seat.South, card, true);

            if (_tableView != null)
            {
                _tableView.ShowCardAnimated(Seat.South, card, fromWorld, _playDuration, PlayRotation(Seat.South));
            }

            RefreshHud();
            PumpFlow();
        }

        /// <summary>
        /// Feedback claro de jogada proibida (obrigacao de seguir naipe): a carta sacode na
        /// mao e um toast explica a regra em portugues. Nenhuma regra e alterada.
        /// </summary>
        private void NotifyFollowSuitViolation(ICardView source)
        {
            if (_match.CurrentTrick.IsEmpty)
            {
                return;
            }

            Suit lead = _match.CurrentTrick.LeadSuit;
            string suitName = CardView.SuitLabel(lead);

            if (source is CardView cardView)
            {
                cardView.PlayShake();
            }

            if (_feedbackUI != null)
            {
                _feedbackUI.ShowToast(
                    "Você é obrigado a assistir ao naipe! Você possui cartas de " + suitName + " na mão.",
                    FeedbackTone.Warning);
            }
        }

        /// <summary>Inclinacao de partida, alinhada com a direcao do assento ate o centro.</summary>
        private static float PlayRotation(Seat seat)
        {
            switch (seat)
            {
                case Seat.North:
                    return -8f;
                case Seat.East:
                    return -14f;
                case Seat.West:
                    return 14f;
                case Seat.South:
                default:
                    return 8f;
            }
        }

        private static bool ContainsCard(IReadOnlyList<Card> cards, Card card)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == card)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsLegalForSouth(Card card)
        {
            return ContainsCard(_match.GetLegalMoves(Seat.South), card);
        }

        private void PumpFlow()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (_match.Phase == GamePhase.ResolvingTrick)
            {
                StopFlow();
                _flow = StartCoroutine(ResolveTrickFlow());
                return;
            }

            UpdateInteractivity();

            if (_match.Phase == GamePhase.AwaitingPlayerMove && _match.CurrentTurn != Seat.South)
            {
                StopFlow();
                _flow = StartCoroutine(AiFlow());
            }
        }

        private IEnumerator AiFlow()
        {
            yield return new WaitForSeconds(_aiDelay);
            _flow = null;

            if (_match.Phase != GamePhase.AwaitingPlayerMove)
            {
                yield break;
            }

            Seat seat = _match.CurrentTurn;
            if (seat == Seat.South)
            {
                yield break;
            }

            IReadOnlyList<Card> legal = _match.GetLegalMoves(seat);
            bool isLeading = _match.CurrentTrick.IsEmpty;
            Card currentBest = default;
            Suit leadSuit = _match.Trump;

            if (!isLeading)
            {
                currentBest = AiMoveSelector.CurrentBest(_match.CurrentTrick.Plays, _match.Trump);
                leadSuit = _match.CurrentTrick.LeadSuit;
            }

            Card card = AiMoveSelector.Select(legal, isLeading, currentBest, leadSuit, _match.Trump);
            CardView source = FindSeatCardView(seat, card);
            Vector3 fromWorld = source != null
                ? source.Rect.position
                : (RootFor(seat) != null ? RootFor(seat).position : transform.position);

            _match.PlayCard(seat, card);
            RemoveSeatCard(seat, card, true);

            if (_tableView != null)
            {
                _tableView.ShowCardAnimated(seat, card, fromWorld, _playDuration, PlayRotation(seat));
            }

            RefreshHud();
            PumpFlow();
        }

        private CardView FindSeatCardView(Seat seat, Card card)
        {
            List<CardView> views = GetSeatList(seat);

            for (int i = 0; i < views.Count; i++)
            {
                if (views[i] != null && views[i].Card.Equals(card))
                {
                    return views[i];
                }
            }

            return null;
        }

        private IEnumerator ResolveTrickFlow()
        {
            _flow = null;

            if (_feedbackUI != null)
            {
                _feedbackUI.ShowTurn("Resolvendo a vaza...", FeedbackTone.Info);
            }

            yield return CardAnimator.Delay(_trickPause);

            Seat winner = _match.CurrentTrick.ResolveWinner(_match.Trump);

            yield return AnimateCollectTrick(winner);

            _match.ResolveTrick();

            if (_tableView != null)
            {
                _tableView.ClearTrick();
            }

            RefreshHud();

            _match.BeginNextTrick();

            if (_match.Phase == GamePhase.AwaitingPlayerMove)
            {
                PumpFlow();
            }
        }

        /// <summary>
        /// Anima as quatro cartas da vaza deslizando em direcao a mao/canto do vencedor e
        /// desvanecendo. Puramente visual: o dominio ainda nao foi resolvido.
        /// </summary>
        private IEnumerator AnimateCollectTrick(Seat winner)
        {
            if (_tableView == null || _collectDuration <= 0f)
            {
                yield break;
            }

            Vector3 target = WinnerCornerWorld(winner);
            int seatCount = Seats.All.Length;

            var views = new CardView[seatCount];
            var groups = new CanvasGroup[seatCount];
            var starts = new Vector3[seatCount];
            var scales = new float[seatCount];

            for (int i = 0; i < seatCount; i++)
            {
                views[i] = _tableView.GetShownCard(Seats.All[i]);
                if (views[i] == null)
                {
                    continue;
                }

                groups[i] = CardAnimator.GetOrAddCanvasGroup(views[i].gameObject);
                groups[i].alpha = 1f;
                starts[i] = views[i].Rect.position;
                scales[i] = views[i].Rect.localScale.x;
            }

            float elapsed = 0f;
            while (elapsed < _collectDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = CardAnimator.Evaluate(elapsed / _collectDuration, Ease.EaseInOutQuad);
                float fade = Mathf.Clamp01((t - 0.15f) / 0.85f);

                for (int i = 0; i < seatCount; i++)
                {
                    if (views[i] == null)
                    {
                        continue;
                    }

                    views[i].Rect.position = Vector3.LerpUnclamped(starts[i], target, t);

                    if (groups[i] != null)
                    {
                        groups[i].alpha = 1f - fade;
                    }

                    float boost = Seats.All[i] == winner
                        ? Mathf.LerpUnclamped(scales[i], scales[i] * 1.25f, t)
                        : Mathf.LerpUnclamped(scales[i], scales[i] * 0.85f, t);

                    views[i].Rect.localScale = Vector3.one * boost;
                }

                yield return null;
            }
        }

        private void BuildAllSeatViews()
        {
            foreach (Seat seat in Seats.All)
            {
                BuildSeatViews(seat);
            }
        }

        private void EnsureCardViewPrefab()
        {
            if (_cardViewPrefab != null)
            {
                return;
            }

            CardView loadedPrefab = null;
#if UNITY_EDITOR
            loadedPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<CardView>("Assets/_Project/Prefabs/CardPrefab.prefab");
#endif
            _cardViewPrefab = loadedPrefab;
        }

        private void BuildSeatViews(Seat seat)
        {
            EnsureCardViewPrefab();

            List<CardView> views = GetSeatList(seat);
            views.Clear();

            if (_cardViewPrefab == null)
            {
                return;
            }

            RectTransform root = RootFor(seat);
            IReadOnlyList<Card> hand = _match.GetHand(seat);
            bool faceUp = seat == Seat.South;

            for (int i = 0; i < hand.Count; i++)
            {
                CardView view = Instantiate(_cardViewPrefab, root);
                view.name = "HandCard_" + seat + "_" + i;
                view.SetCard(hand[i], faceUp);
                view.IsInteractable = false;

                if (faceUp)
                {
                    view.Clicked += OnCardClicked;
                }

                views.Add(view);
            }

            RelayoutSeat(seat);
        }

        private void OnCardClicked(ICardView view)
        {
            TryPlaySouthCard(view);
        }

        private void RemoveSeatCard(Seat seat, Card card, bool animate = false)
        {
            List<CardView> views = GetSeatList(seat);

            for (int i = 0; i < views.Count; i++)
            {
                CardView view = views[i];
                if (view == null || !view.Card.Equals(card))
                {
                    continue;
                }

                views.RemoveAt(i);
                Destroy(view.gameObject);
                break;
            }

            if (animate)
            {
                StartRelayout(seat);
            }
            else
            {
                RelayoutSeat(seat);
            }
        }

        private void StartRelayout(Seat seat)
        {
            StopRelayout(seat);

            if (!isActiveAndEnabled)
            {
                RelayoutSeat(seat);
                return;
            }

            _relayoutRoutines[seat] = StartCoroutine(RelayoutSeatAnimated(seat));
        }

        private void StopRelayout(Seat seat)
        {
            if (_relayoutRoutines.TryGetValue(seat, out Coroutine routine) && routine != null)
            {
                StopCoroutine(routine);
            }

            _relayoutRoutines.Remove(seat);
        }

        private IEnumerator RelayoutSeatAnimated(Seat seat)
        {
            List<CardView> views = GetSeatList(seat);
            bool vertical = IsVerticalSeat(seat);
            int count = views.Count;

            var from = new Vector2[count];
            var to = new Vector2[count];

            for (int i = 0; i < count; i++)
            {
                if (views[i] == null)
                {
                    continue;
                }

                EnsureCenteredAnchors(views[i].Rect);
                from[i] = views[i].Rect.anchoredPosition;
                to[i] = ComputeHandPosition(i, count, vertical);
            }

            yield return CardAnimator.Tween(_relayoutDuration, Ease.EaseOutQuad, t =>
            {
                List<CardView> current = GetSeatList(seat);
                int limit = Mathf.Min(count, current.Count);

                for (int i = 0; i < limit; i++)
                {
                    if (current[i] == null)
                    {
                        continue;
                    }

                    current[i].Rect.anchoredPosition = Vector2.LerpUnclamped(from[i], to[i], t);
                }
            });

            _relayoutRoutines.Remove(seat);
        }

        private static bool IsVerticalSeat(Seat seat)
        {
            return seat == Seat.East || seat == Seat.West;
        }

        private static Vector2 ComputeHandPosition(int index, int count, bool vertical)
        {
            float spacing = vertical ? VerticalHandSpacing : HorizontalHandSpacing;
            float total = (count - 1) * spacing;
            float offset = (index * spacing) - (total * 0.5f);
            return vertical ? new Vector2(0f, -offset) : new Vector2(offset, 0f);
        }

        private static void EnsureCenteredAnchors(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
        }

        private void RelayoutSeat(Seat seat)
        {
            StopRelayout(seat);

            List<CardView> views = GetSeatList(seat);
            bool vertical = IsVerticalSeat(seat);

            for (int i = 0; i < views.Count; i++)
            {
                CardView view = views[i];
                if (view == null)
                {
                    continue;
                }

                EnsureCenteredAnchors(view.Rect);
                view.Rect.anchoredPosition = ComputeHandPosition(i, views.Count, vertical);
            }
        }

        private List<CardView> GetSeatList(Seat seat)
        {
            if (_seatViews.TryGetValue(seat, out List<CardView> views))
            {
                return views;
            }

            views = new List<CardView>(MatchState.CardsPerPlayer);
            _seatViews[seat] = views;
            return views;
        }

        private RectTransform RootFor(Seat seat)
        {
            switch (seat)
            {
                case Seat.North:
                    return _northHandRoot;
                case Seat.East:
                    return _eastHandRoot;
                case Seat.West:
                    return _westHandRoot;
                case Seat.South:
                default:
                    return _southHandRoot;
            }
        }

        private void ClearAllSeatViews()
        {
            foreach (Seat seat in Seats.All)
            {
                StopRelayout(seat);
                List<CardView> views = GetSeatList(seat);

                for (int i = 0; i < views.Count; i++)
                {
                    if (views[i] != null)
                    {
                        Destroy(views[i].gameObject);
                    }
                }

                views.Clear();
            }
        }

        private void UpdateInteractivity()
        {
            bool southTurn = !_isDealing &&
                             _match.Phase == GamePhase.AwaitingPlayerMove &&
                             _match.CurrentTurn == Seat.South;

            List<CardView> southViews = GetSeatList(Seat.South);

            for (int i = 0; i < southViews.Count; i++)
            {
                if (southViews[i] != null)
                {
                    southViews[i].IsInteractable = southTurn;
                }
            }

            RefreshTurnIndicator();

            if (_statusText == null || _match.Phase != GamePhase.AwaitingPlayerMove)
            {
                return;
            }

            _statusText.text = southTurn ? "Sua vez" : ("Vez de " + SeatLabel(_match.CurrentTurn));
        }

        /// <summary>Indicador de turno elegante, explicito e sempre sincronizado com a fase atual.</summary>
        private void RefreshTurnIndicator()
        {
            if (_feedbackUI == null)
            {
                return;
            }

            if (_isDealing)
            {
                _feedbackUI.ShowTurn("Distribuindo as cartas...", FeedbackTone.Info);
                return;
            }

            switch (_match.Phase)
            {
                case GamePhase.AwaitingPlayerMove:
                    if (_match.CurrentTurn == Seat.South)
                    {
                        _feedbackUI.ShowTurn("Sua vez de jogar!", FeedbackTone.Positive);
                    }
                    else
                    {
                        _feedbackUI.ShowTurn("Vez de " + SeatLabel(_match.CurrentTurn) + "...", FeedbackTone.Info);
                    }

                    return;

                case GamePhase.ResolvingTrick:
                case GamePhase.TrickCompleted:
                    _feedbackUI.ShowTurn("Resolvendo a vaza...", FeedbackTone.Info);
                    return;

                default:
                    _feedbackUI.HideTurn();
                    return;
            }
        }

        private static string SeatLabel(Seat seat)
        {
            switch (seat)
            {
                case Seat.North:
                    return "Norte";
                case Seat.East:
                    return "Leste";
                case Seat.South:
                    return "Sul";
                case Seat.West:
                default:
                    return "Oeste";
            }
        }

        private void RefreshHud()
        {
            if (_match == null)
            {
                return;
            }

            if (_trumpText != null)
            {
                string trump = _match.RoundNumber > 0 ? _match.Trump.ToString() : "-";
                _trumpText.text = "Trunfo: " + trump;
            }

            if (_scoreText == null)
            {
                return;
            }

            int northSouthPoints = _match.GetCapturedPoints(Partnership.NorthSouth);
            int eastWestPoints = _match.GetCapturedPoints(Partnership.EastWest);
            int northSouthGames = _match.GetGames(Partnership.NorthSouth);
            int eastWestGames = _match.GetGames(Partnership.EastWest);

            _scoreText.text =
                "Nos (N/S): " + northSouthPoints + " pts - " + northSouthGames + " jogos  |  " +
                "Eles (L/O): " + eastWestPoints + " pts - " + eastWestGames + " jogos";
        }

        private void Subscribe()
        {
            _match.TrickResolved += OnTrickResolved;
            _match.HandScored += OnHandScored;
            _match.MatchWon += OnMatchWon;
        }

        private void Unsubscribe()
        {
            if (_match == null)
            {
                return;
            }

            _match.TrickResolved -= OnTrickResolved;
            _match.HandScored -= OnHandScored;
            _match.MatchWon -= OnMatchWon;
        }

        private void OnTrickResolved(TrickRecord record)
        {
            if (_statusText != null && _match.Phase == GamePhase.TrickCompleted)
            {
                _statusText.text = "Vaza para " + record.Winner;
            }
        }

        private void OnHandScored(HandResult result)
        {
            RefreshHud();

            if (_feedbackUI != null)
            {
                _feedbackUI.HideTurn();
            }

            if (_nextHandButton != null && !_match.IsMatchOver)
            {
                _nextHandButton.gameObject.SetActive(true);
            }

            if (_statusText == null)
            {
                return;
            }

            if (!result.Winner.HasValue)
            {
                _statusText.text = "Empate 60-60. Bonus acumulado: " + _match.PendingTieBonusGames + " jogo(s).";

                if (_feedbackUI != null)
                {
                    _feedbackUI.ShowToast(_statusText.text, FeedbackTone.Info);
                }

                return;
            }

            Partnership winner = result.Winner.Value;
            int points = winner == Partnership.NorthSouth ? result.NorthSouthPoints : result.EastWestPoints;
            string label = winner == Partnership.NorthSouth ? "Nos (N/S)" : "Eles (L/O)";
            string bandeira = result.IsBandeira ? " (BANDEIRA)" : string.Empty;
            _statusText.text = label + " venceram a mao com " + points + " pontos" + bandeira + ".";

            if (_feedbackUI != null)
            {
                _feedbackUI.ShowToast(
                    _statusText.text,
                    winner == Partnership.NorthSouth ? FeedbackTone.Positive : FeedbackTone.Warning);
            }
        }

        private void OnMatchWon(Partnership winner)
        {
            if (_statusText != null)
            {
                _statusText.text = "Partida encerrada.";
            }

            if (_feedbackUI != null)
            {
                _feedbackUI.HideTurn();
            }

            if (_nextHandButton != null)
            {
                _nextHandButton.gameObject.SetActive(false);
            }

            if (_winnerText == null)
            {
                return;
            }

            string label = winner == Partnership.NorthSouth ? "Parceria Norte/Sul" : "Parceria Leste/Oeste";
            _winnerText.text = label + " venceu a partida!";
            _winnerText.gameObject.SetActive(true);

            if (_feedbackUI != null)
            {
                _feedbackUI.ShowToast(
                    winner == Partnership.NorthSouth ? "Parabens! Sua parceria venceu a partida!" : "A parceria adversaria venceu a partida.",
                    winner == Partnership.NorthSouth ? FeedbackTone.Positive : FeedbackTone.Warning);
            }
        }

        private void StopFlow()
        {
            if (_flow == null)
            {
                return;
            }

            StopCoroutine(_flow);
            _flow = null;
        }
    }
}
