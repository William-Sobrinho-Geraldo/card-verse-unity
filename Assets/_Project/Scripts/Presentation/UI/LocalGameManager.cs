using System;
using System.Collections;
using System.Collections.Generic;
using SuecaOnline.Application.Sueca;
using SuecaOnline.Domain.Sueca.Cards;
using SuecaOnline.Domain.Sueca.Match;
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

        private const float VerticalHandSpacing = 34f;
        private const float HorizontalHandSpacing = 78f;

        private readonly Dictionary<Seat, List<CardView>> _seatViews = new Dictionary<Seat, List<CardView>>();

        private MatchState _match;
        private Coroutine _flow;
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

        private void Start()
        {
            EnsureCardViewPrefab();
            _match.StartMatch();
            StartRound();
        }

        private void OnDestroy()
        {
            StopFlow();
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
            PumpFlow();
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
                return false;
            }

            _match.PlayCard(Seat.South, card);
            RemoveSeatCard(Seat.South, card);

            if (_tableView != null)
            {
                _tableView.ShowCard(Seat.South, card);
            }

            RefreshHud();
            PumpFlow();
            return true;
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

            _match.PlayCard(seat, card);
            RemoveSeatCard(seat, card);

            if (_tableView != null)
            {
                _tableView.ShowCard(seat, card);
            }

            RefreshHud();
            PumpFlow();
        }

        private IEnumerator ResolveTrickFlow()
        {
            yield return new WaitForSeconds(_aiDelay);
            _flow = null;

            TrickRecord record = _match.ResolveTrick();

            if (_tableView != null)
            {
                _tableView.HighlightWinner(record.Winner);
            }

            yield return new WaitForSeconds(_aiDelay);

            if (_tableView != null)
            {
                _tableView.ClearTrick();
            }

            _match.BeginNextTrick();
            RefreshHud();

            if (_match.Phase == GamePhase.AwaitingPlayerMove)
            {
                PumpFlow();
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
            TryPlaySouthCard(view.Card);
        }

        private void RemoveSeatCard(Seat seat, Card card)
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

            RelayoutSeat(seat);
        }

        private void RelayoutSeat(Seat seat)
        {
            List<CardView> views = GetSeatList(seat);
            bool vertical = seat == Seat.East || seat == Seat.West;
            float spacing = vertical ? VerticalHandSpacing : HorizontalHandSpacing;
            float total = (views.Count - 1) * spacing;

            for (int i = 0; i < views.Count; i++)
            {
                CardView view = views[i];
                if (view == null)
                {
                    continue;
                }

                float offset = (i * spacing) - (total * 0.5f);
                RectTransform rect = (RectTransform)view.transform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = vertical ? new Vector2(0f, -offset) : new Vector2(offset, 0f);
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
            bool southTurn = _match.Phase == GamePhase.AwaitingPlayerMove && _match.CurrentTurn == Seat.South;
            List<CardView> southViews = GetSeatList(Seat.South);

            for (int i = 0; i < southViews.Count; i++)
            {
                if (southViews[i] != null)
                {
                    southViews[i].IsInteractable = southTurn;
                }
            }

            if (_statusText == null || _match.Phase != GamePhase.AwaitingPlayerMove)
            {
                return;
            }

            _statusText.text = southTurn ? "Sua vez" : ("Vez de " + _match.CurrentTurn);
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
                return;
            }

            Partnership winner = result.Winner.Value;
            int points = winner == Partnership.NorthSouth ? result.NorthSouthPoints : result.EastWestPoints;
            string label = winner == Partnership.NorthSouth ? "Nos (N/S)" : "Eles (L/O)";
            string bandeira = result.IsBandeira ? " (BANDEIRA)" : string.Empty;
            _statusText.text = label + " venceram a mao com " + points + " pontos" + bandeira + ".";
        }

        private void OnMatchWon(Partnership winner)
        {
            if (_statusText != null)
            {
                _statusText.text = "Partida encerrada.";
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
