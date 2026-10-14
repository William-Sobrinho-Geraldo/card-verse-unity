using System.Collections.Generic;
using NUnit.Framework;
using SuecaOnline.Domain.Sueca.Cards;
using SuecaOnline.Domain.Sueca.Exceptions;
using SuecaOnline.Domain.Sueca.Match;

namespace SuecaOnline.Tests.EditMode
{
    public class MatchStateTests
    {
        private static MatchState StartDealtMatch(int seed)
        {
            MatchState match = MatchState.CreateDeterministic();
            match.StartMatch();
            match.DealRound(seed);
            return match;
        }

        private static void PlayFullRound(MatchState match)
        {
            int guard = 0;

            while (match.Phase != GamePhase.RoundCompleted && match.Phase != GamePhase.MatchCompleted)
            {
                guard++;
                Assert.Less(guard, 500, "A mao nao terminou dentro do limite de iteracoes.");

                switch (match.Phase)
                {
                    case GamePhase.AwaitingPlayerMove:
                        Seat seat = match.CurrentTurn;
                        IReadOnlyList<Card> legal = match.GetLegalMoves(seat);
                        Assert.Greater(legal.Count, 0, "Jogador sem jogada legal.");
                        match.PlayCard(seat, legal[0]);
                        break;

                    case GamePhase.ResolvingTrick:
                        match.ResolveTrick();
                        break;

                    case GamePhase.TrickCompleted:
                        match.BeginNextTrick();
                        break;

                    default:
                        Assert.Fail("Fase inesperada durante a mao: " + match.Phase);
                        break;
                }
            }
        }

        [Test]
        public void StartMatch_EntersDealingCards()
        {
            MatchState match = MatchState.CreateDeterministic();

            Assert.AreEqual(GamePhase.WaitingForPlayers, match.Phase);

            match.StartMatch();

            Assert.AreEqual(GamePhase.DealingCards, match.Phase);
        }

        [Test]
        public void DealRound_GivesTenCardsPerPlayer_AndSetsTrumpFromDealerLastCard()
        {
            MatchState match = StartDealtMatch(42);

            foreach (Seat seat in Seats.All)
            {
                Assert.AreEqual(MatchState.CardsPerPlayer, match.GetHand(seat).Count, "Mao de " + seat);
            }

            Card dealerLastCard = match.GetHand(match.Dealer)[MatchState.CardsPerPlayer - 1];
            Assert.AreEqual(dealerLastCard.Suit, match.Trump, "O trunfo e o naipe da ultima carta do dealer.");

            Assert.AreEqual(GamePhase.AwaitingPlayerMove, match.Phase);
            Assert.AreEqual(Seats.Next(match.Dealer), match.CurrentTurn, "O jogador a direita do dealer inicia.");
        }

        [Test]
        public void DealRound_ExposesTrumpCard_FromDealerLastCard()
        {
            MatchState match = StartDealtMatch(42);

            Card dealerLastCard = match.GetHand(match.Dealer)[MatchState.CardsPerPlayer - 1];

            Assert.AreEqual(dealerLastCard, match.TrumpCard, "A carta de trunfo e a ultima carta do dealer.");
            Assert.AreEqual(match.TrumpCard.Suit, match.Trump, "O naipe do trunfo vem da carta virada.");
        }

        [Test]
        public void DealRound_ExposesSameTrumpCard_ForSameSeed()
        {
            MatchState first = StartDealtMatch(2026);
            MatchState second = StartDealtMatch(2026);

            Assert.AreEqual(first.TrumpCard, second.TrumpCard);
        }

        [Test]
        public void BeginNextTrick_WinnerLeadsNextTrick()
        {
            MatchState match = StartDealtMatch(2024);

            while (match.Phase == GamePhase.AwaitingPlayerMove)
            {
                Seat seat = match.CurrentTurn;
                IReadOnlyList<Card> legal = match.GetLegalMoves(seat);
                match.PlayCard(seat, legal[0]);
            }

            Assert.AreEqual(GamePhase.ResolvingTrick, match.Phase);

            TrickRecord record = match.ResolveTrick();
            match.BeginNextTrick();

            Assert.AreEqual(record.Winner, match.CurrentTurn, "O vencedor da vaza inicia a proxima.");
            Assert.AreEqual(record.Winner, match.Leader);
        }

        [Test]
        public void DealRound_IsDeterministic_ForSameSeed()
        {
            MatchState first = StartDealtMatch(2026);
            MatchState second = StartDealtMatch(2026);

            Assert.AreEqual(first.Trump, second.Trump);

            foreach (Seat seat in Seats.All)
            {
                CollectionAssert.AreEqual(first.GetHand(seat), second.GetHand(seat), "Mao de " + seat);
            }
        }

        [Test]
        public void PlayCard_BeforeDeal_ThrowsInvalidPhase()
        {
            MatchState match = MatchState.CreateDeterministic();
            match.StartMatch();

            Assert.Throws<InvalidPhaseException>(
                () => match.PlayCard(Seat.North, new Card(Suit.Ouros, Rank.As)));
        }

        [Test]
        public void PlayCard_OutOfTurn_ThrowsInvalidMove()
        {
            MatchState match = StartDealtMatch(1);
            Seat notCurrent = Seats.Next(match.CurrentTurn);
            Card card = match.GetHand(notCurrent)[0];

            Assert.Throws<InvalidMoveException>(() => match.PlayCard(notCurrent, card));
        }

        [Test]
        public void PlayCard_WithCardNotInHand_ThrowsInvalidMove()
        {
            MatchState match = StartDealtMatch(2);
            Seat current = match.CurrentTurn;
            Card notInHand = FindCardNotInHand(match.GetHand(current));

            Assert.Throws<InvalidMoveException>(() => match.PlayCard(current, notInHand));
        }

        [Test]
        public void PlayCard_OffSuit_WhenAbleToFollow_ThrowsInvalidMove()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                MatchState match = StartDealtMatch(seed);
                Seat leader = match.CurrentTurn;
                Card leadCard = match.GetHand(leader)[0];
                match.PlayCard(leader, leadCard);

                Seat follower = match.CurrentTurn;
                IReadOnlyList<Card> hand = match.GetHand(follower);

                bool hasLeadSuit = false;
                bool hasOffSuit = false;
                Card offSuit = default;

                foreach (Card card in hand)
                {
                    if (card.Suit == leadCard.Suit)
                    {
                        hasLeadSuit = true;
                    }
                    else
                    {
                        hasOffSuit = true;
                        offSuit = card;
                    }
                }

                if (!hasLeadSuit || !hasOffSuit)
                {
                    continue;
                }

                Assert.Throws<InvalidMoveException>(() => match.PlayCard(follower, offSuit));
                return;
            }

            Assert.Inconclusive("Nenhum seed produziu cenario de violacao de naipe.");
        }

        [Test]
        public void FullRound_TenTricks_TotalExactly120Points()
        {
            MatchState match = StartDealtMatch(2024);

            PlayFullRound(match);

            int northSouthPoints = match.GetCapturedPoints(Partnership.NorthSouth);
            int eastWestPoints = match.GetCapturedPoints(Partnership.EastWest);

            Assert.AreEqual(120, northSouthPoints + eastWestPoints, "Todas as cartas somam 120 pontos.");
            Assert.AreEqual(10, match.GetTricksWon(Partnership.NorthSouth) + match.GetTricksWon(Partnership.EastWest));

            foreach (Seat seat in Seats.All)
            {
                Assert.AreEqual(0, match.GetHand(seat).Count, "Mao vazia ao fim da rodada: " + seat);
                Assert.AreEqual(0, match.GetCapturedCards(seat).Count % 4, "Vazas capturadas em multiplos de 4: " + seat);
            }

            Assert.IsTrue(
                match.Phase == GamePhase.RoundCompleted || match.Phase == GamePhase.MatchCompleted,
                "Fase final esperada RoundCompleted/MatchCompleted, obtida " + match.Phase + ".");
        }

        [Test]
        public void FullRound_CapturedCardsTotalForty()
        {
            MatchState match = StartDealtMatch(777);

            PlayFullRound(match);

            int totalCaptured = 0;
            foreach (Seat seat in Seats.All)
            {
                totalCaptured += match.GetCapturedCards(seat).Count;
            }

            Assert.AreEqual(Deck.StandardCardCount, totalCaptured);
        }

        private static Card FindCardNotInHand(IReadOnlyList<Card> hand)
        {
            foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in System.Enum.GetValues(typeof(Rank)))
                {
                    var candidate = new Card(suit, rank);
                    bool found = false;

                    for (int i = 0; i < hand.Count; i++)
                    {
                        if (hand[i] == candidate)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        return candidate;
                    }
                }
            }

            return default;
        }
    }
}
