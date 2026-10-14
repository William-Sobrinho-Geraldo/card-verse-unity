using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SuecaOnline.Domain.Sueca.Cards;
using SuecaOnline.Domain.Sueca.Match;
using SuecaOnline.Presentation.Cards;
using SuecaOnline.Presentation.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SuecaOnline.Tests.PlayMode
{
    public class LocalGameManagerPlayModeTests
    {
        private const string SceneName = "Mesa2D";

        [UnityTest]
        public IEnumerator Mesa2DScene_RendersSouthHandWithTenCards()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null;
            yield return null;

            LocalGameManager manager = Object.FindFirstObjectByType<LocalGameManager>();
            Assert.IsNotNull(manager, "A cena Mesa2D deve conter um LocalGameManager.");

            Assert.IsNotNull(manager.Match, "O gerenciador deve criar uma partida de Sueca.");
            Assert.AreEqual(MatchState.CardsPerPlayer, manager.Match.GetHand(Seat.South).Count);
            Assert.AreEqual(MatchState.CardsPerPlayer, manager.GetRenderedCardCount(Seat.South), "A mao do jogador deve ser renderizada.");
            Assert.AreEqual(MatchState.CardsPerPlayer, manager.GetRenderedCardCount(Seat.North));
            Assert.Greater(manager.Match.RoundNumber, 0, "A rodada deve ter sido distribuida.");
        }

        [UnityTest]
        public IEnumerator Mesa2DScene_ShowsTrumpCard_BeforeFirstPlay()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null;
            yield return null;

            LocalGameManager manager = Object.FindFirstObjectByType<LocalGameManager>();
            Assert.IsNotNull(manager);
            Assert.Greater(manager.Match.RoundNumber, 0, "A rodada deve ter sido distribuida.");

            CardView trumpView = manager.TrumpCardView;
            Assert.IsNotNull(trumpView, "O slot da carta de trunfo deve estar renderizado apos a distribuicao.");
            Assert.AreEqual(manager.Match.TrumpCard, trumpView.Card, "A carta exibida deve ser a carta virada do dealer.");
            Assert.AreEqual(manager.Match.Trump, trumpView.Card.Suit, "O naipe exibido deve ser o trunfo da mao.");
        }

        [UnityTest]
        public IEnumerator TryPlaySouthCard_OutOfTurn_IsRejected()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null;
            yield return null;

            LocalGameManager manager = Object.FindFirstObjectByType<LocalGameManager>();
            Assert.IsNotNull(manager);

            if (manager.Match.CurrentTurn == Seat.South)
            {
                Assert.Pass("Nesta distribuicao o Sul ja inicia; validacao de turno coberta em EditMode.");
                yield break;
            }

            Card firstCard = manager.Match.GetHand(Seat.South)[0];
            Assert.IsFalse(
                manager.TryPlaySouthCard(firstCard),
                "Jogar fora do turno deve ser rejeitado pela apresentacao.");
        }

        [UnityTest]
        public IEnumerator StartNewMatch_WithSeed_IsReproducible()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null;
            yield return null;

            LocalGameManager manager = Object.FindFirstObjectByType<LocalGameManager>();
            Assert.IsNotNull(manager);

            const int seed = 4242;
            manager.StartNewMatch(seed);
            yield return null;

            MatchState expected = MatchState.CreateDeterministic();
            expected.StartMatch();
            expected.DealRound(seed + expected.RoundNumber);

            foreach (Seat seat in Seats.All)
            {
                CollectionAssert.AreEqual(
                    new List<Card>(expected.GetHand(seat)),
                    new List<Card>(manager.Match.GetHand(seat)),
                    "A mao de " + seat + " deve ser reproduzivel a partir da semente explicita.");
            }
        }

        [UnityTest]
        public IEnumerator StartNewMatch_Default_GeneratesDifferentDeals()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null;
            yield return null;

            LocalGameManager manager = Object.FindFirstObjectByType<LocalGameManager>();
            Assert.IsNotNull(manager);

            manager.StartNewMatch();
            yield return null;
            List<Card> firstDeal = SnapshotAllHands(manager.Match);

            manager.StartNewMatch();
            yield return null;
            List<Card> secondDeal = SnapshotAllHands(manager.Match);

            CollectionAssert.AreNotEqual(
                firstDeal,
                secondDeal,
                "Duas partidas novas nao devem distribuir a mesma sequencia de cartas.");
        }

        private static List<Card> SnapshotAllHands(MatchState match)
        {
            var cards = new List<Card>();

            foreach (Seat seat in Seats.All)
            {
                cards.AddRange(match.GetHand(seat));
            }

            return cards;
        }
    }
}
