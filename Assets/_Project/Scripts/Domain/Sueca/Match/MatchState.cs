using System;
using System.Collections.Generic;
using SuecaOnline.Domain.Sueca.Cards;
using SuecaOnline.Domain.Sueca.Exceptions;
using SuecaOnline.Domain.Sueca.Random;
using SuecaOnline.Domain.Sueca.Rules;

namespace SuecaOnline.Domain.Sueca.Match
{
    /// <summary>
    /// Nucleo da partida de Sueca como maquina de estados explicita.
    /// Puro C#: sem qualquer dependencia de UnityEngine. Toda aleatoriedade
    /// passa por uma fabrica de <see cref="IShuffler"/> indexada por seed.
    /// </summary>
    public sealed class MatchState
    {
        public const int PlayersPerTable = 4;
        public const int CardsPerPlayer = 10;
        public const int TricksPerRound = 10;
        public const int GamesToWinMatch = 4;

        private readonly Func<int, IShuffler> _shufflerFactory;
        private readonly List<Card>[] _hands = new List<Card>[PlayersPerTable];
        private readonly List<Card>[] _captured = new List<Card>[PlayersPerTable];
        private readonly int[] _gamesByPartnership = new int[2];
        private readonly int[] _tricksByPartnership = new int[2];

        private Trick _currentTrick = new Trick();
        private int _tricksPlayedInRound;
        private int _pendingTieBonusGames;

        public MatchState(Func<int, IShuffler> shufflerFactory, Seat dealer = Seat.North)
        {
            _shufflerFactory = shufflerFactory ?? throw new ArgumentNullException(nameof(shufflerFactory));
            Dealer = dealer;

            for (int i = 0; i < PlayersPerTable; i++)
            {
                _hands[i] = new List<Card>(CardsPerPlayer);
                _captured[i] = new List<Card>(TricksPerRound);
            }

            Phase = GamePhase.WaitingForPlayers;
        }

        /// <summary>Cria uma partida reproduzivel: mesmo seed produz sempre a mesma distribuicao.</summary>
        public static MatchState CreateDeterministic(Seat dealer = Seat.North)
        {
            return new MatchState(seed => new FisherYatesShuffler(new SeededRandomSource(seed)), dealer);
        }

        public GamePhase Phase { get; private set; }

        public Suit Trump { get; private set; }

        /// <summary>
        /// Carta virada do dealer (fundo do baralho) que define o trunfo da mao.
        /// Permanece na mao do dealer e e conhecida por todos os jogadores.
        /// </summary>
        public Card TrumpCard { get; private set; }

        public Seat Dealer { get; private set; }

        public Seat Leader { get; private set; }

        public Seat CurrentTurn { get; private set; }

        public int RoundNumber { get; private set; }

        public Trick CurrentTrick
        {
            get { return _currentTrick; }
        }

        public TrickRecord? LastTrick { get; private set; }

        public HandResult? LastHandResult { get; private set; }

        public Partnership? MatchWinner { get; private set; }

        public int PendingTieBonusGames
        {
            get { return _pendingTieBonusGames; }
        }

        public bool IsMatchOver
        {
            get { return MatchWinner.HasValue; }
        }

        public event Action<TrickRecord> TrickResolved;

        public event Action<HandResult> HandScored;

        public event Action<Partnership> MatchWon;

        public IReadOnlyList<Card> GetHand(Seat seat)
        {
            return _hands[(int)seat];
        }

        public IReadOnlyList<Card> GetCapturedCards(Seat seat)
        {
            return _captured[(int)seat];
        }

        public int GetGames(Partnership partnership)
        {
            return _gamesByPartnership[(int)partnership];
        }

        public int GetTricksWon(Partnership partnership)
        {
            return _tricksByPartnership[(int)partnership];
        }

        public int GetCapturedPoints(Partnership partnership)
        {
            int total = 0;
            for (int i = 0; i < PlayersPerTable; i++)
            {
                if (Seats.PartnershipOf((Seat)i) != partnership)
                {
                    continue;
                }

                List<Card> cards = _captured[i];
                for (int c = 0; c < cards.Count; c++)
                {
                    total += cards[c].Points;
                }
            }

            return total;
        }

        /// <summary>WaitingForPlayers -&gt; DealingCards. Zera o placar da partida.</summary>
        public void StartMatch()
        {
            if (Phase != GamePhase.WaitingForPlayers)
            {
                throw new InvalidPhaseException(
                    "StartMatch so e valido em WaitingForPlayers (fase atual: " + Phase + ").");
            }

            _gamesByPartnership[(int)Partnership.NorthSouth] = 0;
            _gamesByPartnership[(int)Partnership.EastWest] = 0;
            _tricksByPartnership[(int)Partnership.NorthSouth] = 0;
            _tricksByPartnership[(int)Partnership.EastWest] = 0;
            _pendingTieBonusGames = 0;
            RoundNumber = 0;
            MatchWinner = null;
            LastHandResult = null;
            LastTrick = null;
            Phase = GamePhase.DealingCards;
        }

        /// <summary>
        /// Distribui 10 cartas para cada assento, define o trunfo (ultima carta do dealer)
        /// e passa para AwaitingPlayerMove. Valida em DealingCards ou RoundCompleted.
        /// </summary>
        public void DealRound(int seed)
        {
            if (Phase != GamePhase.DealingCards && Phase != GamePhase.RoundCompleted)
            {
                throw new InvalidPhaseException(
                    "Nao e possivel distribuir na fase " + Phase + ".");
            }

            if (IsMatchOver)
            {
                throw new InvalidPhaseException("A partida ja terminou.");
            }

            if (RoundNumber > 0)
            {
                Dealer = Seats.Next(Dealer);
            }

            Phase = GamePhase.DealingCards;
            RoundNumber++;
            _tricksPlayedInRound = 0;
            _tricksByPartnership[(int)Partnership.NorthSouth] = 0;
            _tricksByPartnership[(int)Partnership.EastWest] = 0;

            for (int i = 0; i < PlayersPerTable; i++)
            {
                _hands[i].Clear();
                _captured[i].Clear();
            }

            Deck deck = Deck.CreateStandard();
            IReadOnlyList<Card> shuffled = _shufflerFactory(seed).Shuffle(deck.Cards);

            Seat seat = Seats.Next(Dealer);
            for (int i = 0; i < shuffled.Count; i++)
            {
                _hands[(int)seat].Add(shuffled[i]);
                seat = Seats.Next(seat);
            }

            TrumpCard = _hands[(int)Dealer][CardsPerPlayer - 1];
            Trump = TrumpCard.Suit;
            Leader = Seats.Next(Dealer);
            CurrentTurn = Leader;
            _currentTrick = new Trick();
            LastTrick = null;
            Phase = GamePhase.AwaitingPlayerMove;
        }

        /// <summary>Cartas jogaveis pelo assento no estado atual (vazio fora do turno).</summary>
        public IReadOnlyList<Card> GetLegalMoves(Seat seat)
        {
            if (Phase != GamePhase.AwaitingPlayerMove || seat != CurrentTurn)
            {
                return Array.Empty<Card>();
            }

            List<Card> hand = _hands[(int)seat];
            if (_currentTrick.IsEmpty)
            {
                return hand;
            }

            return FollowSuitRule.GetLegalPlays(hand, _currentTrick.LeadSuit);
        }

        /// <summary>
        /// Joga uma carta. Rejeita jogada fora de turno, carta ausente da mao
        /// ou violacao da obrigacao de assistir ao naipe.
        /// </summary>
        public void PlayCard(Seat seat, Card card)
        {
            if (Phase != GamePhase.AwaitingPlayerMove)
            {
                throw new InvalidPhaseException("Nao e momento de jogar (fase: " + Phase + ").");
            }

            if (seat != CurrentTurn)
            {
                throw new InvalidMoveException(
                    "Nao e o turno de " + seat + " (turno atual: " + CurrentTurn + ").");
            }

            List<Card> hand = _hands[(int)seat];
            if (!hand.Contains(card))
            {
                throw new InvalidMoveException("A carta " + card + " nao esta na mao de " + seat + ".");
            }

            if (!_currentTrick.IsEmpty)
            {
                Suit lead = _currentTrick.LeadSuit;
                if (!FollowSuitRule.IsLegalPlay(hand, card, lead))
                {
                    throw new InvalidMoveException(
                        "Obrigacao de assistir ao naipe " + lead + ": o jogador possui " + lead + " e deve joga-lo.");
                }
            }

            hand.Remove(card);
            _currentTrick.Add(seat, card);

            if (_currentTrick.Count == PlayersPerTable)
            {
                Phase = GamePhase.ResolvingTrick;
            }
            else
            {
                CurrentTurn = Seats.Next(seat);
            }
        }

        /// <summary>Resolve a vaza completa (ResolvingTrick -&gt; TrickCompleted).</summary>
        public TrickRecord ResolveTrick()
        {
            if (Phase != GamePhase.ResolvingTrick)
            {
                throw new InvalidPhaseException(
                    "Nao ha vaza completa para resolver (fase: " + Phase + ").");
            }

            Suit lead = _currentTrick.LeadSuit;
            Seat winner = _currentTrick.ResolveWinner(Trump);
            int points = _currentTrick.Points();
            var record = new TrickRecord(lead, Trump, winner, _currentTrick.ToArray(), points);

            for (int i = 0; i < _currentTrick.Plays.Count; i++)
            {
                _captured[(int)winner].Add(_currentTrick.Plays[i].Card);
            }

            _tricksByPartnership[(int)Seats.PartnershipOf(winner)]++;
            _tricksPlayedInRound++;
            LastTrick = record;
            _currentTrick = new Trick();
            Phase = GamePhase.TrickCompleted;

            TrickResolved?.Invoke(record);
            return record;
        }

        /// <summary>
        /// Avanca apos a vaza confirmada: vencedor lidera a proxima ou a mao e concluida
        /// (TrickCompleted -&gt; AwaitingPlayerMove / RoundCompleted / MatchCompleted).
        /// </summary>
        public void BeginNextTrick()
        {
            if (Phase != GamePhase.TrickCompleted)
            {
                throw new InvalidPhaseException(
                    "A vaza anterior precisa ser confirmada (fase: " + Phase + ").");
            }

            if (_tricksPlayedInRound >= TricksPerRound)
            {
                CompleteRound();
                return;
            }

            CurrentTurn = LastTrick.Value.Winner;
            Leader = CurrentTurn;
            Phase = GamePhase.AwaitingPlayerMove;
        }

        private void CompleteRound()
        {
            HandResult result = ScoreRound();
            LastHandResult = result;
            Phase = GamePhase.RoundCompleted;
            HandScored?.Invoke(result);

            if (result.Winner.HasValue)
            {
                Partnership winner = result.Winner.Value;
                if (result.IsBandeira || _gamesByPartnership[(int)winner] >= GamesToWinMatch)
                {
                    MatchWinner = winner;
                    Phase = GamePhase.MatchCompleted;
                    MatchWon?.Invoke(winner);
                }
            }
        }

        private HandResult ScoreRound()
        {
            HandResult result = RoundScoring.Calculate(
                GetCapturedPoints(Partnership.NorthSouth),
                GetCapturedPoints(Partnership.EastWest),
                _tricksByPartnership[(int)Partnership.NorthSouth],
                _tricksByPartnership[(int)Partnership.EastWest],
                _pendingTieBonusGames,
                out int newPendingTieBonusGames);

            _pendingTieBonusGames = newPendingTieBonusGames;

            _gamesByPartnership[(int)Partnership.NorthSouth] += result.NorthSouthGames;
            _gamesByPartnership[(int)Partnership.EastWest] += result.EastWestGames;

            return result;
        }
    }
}
