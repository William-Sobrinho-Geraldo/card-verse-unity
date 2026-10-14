using System.Collections;
using NUnit.Framework;
using SuecaOnline.Domain.Sueca.Cards;
using SuecaOnline.Domain.Sueca.Match;
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
    }
}
