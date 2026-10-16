using System.Collections;
using NUnit.Framework;
using SuecaOnline.Domain.Sueca.Cards;
using SuecaOnline.Presentation.Cards;
using SuecaOnline.Presentation.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SuecaOnline.Tests.PlayMode
{
    /// <summary>
    /// Cobre os componentes de game feel (animacoes e feedback visual) garantindo que
    /// sejam puramente visuais: nao lancam excecoes e nao dependem do estado do dominio.
    /// </summary>
    public class AnimationFeedbackPlayModeTests
    {
        private const string SceneName = "Mesa2D";
        private const string PrefabPath = "Assets/_Project/Prefabs/CardPrefab.prefab";

        private static CardView LoadCardPrefab()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<CardView>(PrefabPath);
#else
            return null;
#endif
        }

        [UnityTest]
        public IEnumerator Mesa2DScene_ProvidesGameFeedbackLayer()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null;
            yield return null;

            LocalGameManager manager = Object.FindFirstObjectByType<LocalGameManager>();
            Assert.IsNotNull(manager);
            Assert.IsNotNull(manager.Feedback, "A cena deve prover a camada de feedback (indicador de turno e toasts).");
            Assert.IsNotNull(manager.Table, "A mesa permanece acessivel para as animacoes.");
        }

        [UnityTest]
        public IEnumerator CardView_PlayShake_RestoresPosition()
        {
            CardView view = Object.Instantiate(LoadCardPrefab());
            yield return null;

            view.SetCard(new Card(Suit.Copas, Rank.As), true);
            var rect = view.Rect;
            rect.anchoredPosition = new Vector2(40f, -20f);

            view.PlayShake();
            yield return new WaitForSecondsRealtime(0.6f);

            Assert.Less(
                Vector2.Distance(rect.anchoredPosition, new Vector2(40f, -20f)),
                0.5f,
                "A sacudida deve restaurar a posicao original da carta.");

            Object.Destroy(view.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameFeedbackUI_ShowTurnAndToast_UpdatesState()
        {
            var root = new GameObject("FeedbackTest", typeof(RectTransform));
            GameFeedbackUI feedback = root.AddComponent<GameFeedbackUI>();
            yield return null;

            feedback.ShowTurn("Sua vez de jogar!", FeedbackTone.Positive);
            feedback.ShowToast("Você é obrigado a assistir ao naipe!", FeedbackTone.Warning);

            yield return new WaitForSecondsRealtime(0.4f);

            Assert.IsTrue(feedback.IsTurnVisible, "O indicador de turno deve ficar visivel apos ShowTurn.");
            Assert.AreEqual("Sua vez de jogar!", feedback.TurnMessage);

            feedback.HideTurn();
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsFalse(feedback.IsTurnVisible, "HideTurn deve desmarcar o indicador de turno.");

            Object.Destroy(root);
            yield return null;
        }
    }
}
