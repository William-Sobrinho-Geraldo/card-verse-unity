using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SuecaOnline.Domain.Sueca.Cards;
using SuecaOnline.Presentation.Cards;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SuecaOnline.Tests.PlayMode
{
    public class CardViewPlayModeTests
    {
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
        public IEnumerator CardPrefab_Instantiates_WithImageAndTexts()
        {
            CardView prefab = LoadCardPrefab();
            Assert.IsNotNull(prefab, "CardPrefab.prefab deve existir e conter CardView.");

            CardView view = Object.Instantiate(prefab);
            yield return null;

            Assert.IsNotNull(view.GetComponentInChildren<Image>(true), "A carta deve conter uma Image.");

            TextMeshProUGUI[] texts = view.GetComponentsInChildren<TextMeshProUGUI>(true);
            Assert.Greater(texts.Length, 0, "A carta deve conter TextMeshPro para valor/naipe.");

            Object.Destroy(view.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SetCard_FaceUp_ShowsRankAndSuit()
        {
            CardView view = Object.Instantiate(LoadCardPrefab());
            yield return null;

            view.SetCard(new Card(Suit.Copas, Rank.As), true);

            Assert.IsTrue(view.IsFaceUp);
            Assert.AreEqual(Rank.As, view.Card.Rank);

            var contents = new List<string>();
            foreach (TextMeshProUGUI text in view.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                contents.Add(text.text);
            }

            CollectionAssert.Contains(contents, "A", "O valor da carta deve ser renderizado.");
            CollectionAssert.Contains(contents, CardView.SuitGlyph(Suit.Copas), "O naipe deve ser renderizado.");

            Object.Destroy(view.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Click_WhenInteractable_RaisesClickedEvent()
        {
            CardView view = Object.Instantiate(LoadCardPrefab());
            yield return null;

            view.SetCard(new Card(Suit.Paus, Rank.Sete), true);
            view.IsInteractable = true;

            int clickCount = 0;
            view.Clicked += _ => clickCount++;

            var eventData = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute<IPointerClickHandler>(view.gameObject, eventData, ExecuteEvents.pointerClickHandler);

            Assert.AreEqual(1, clickCount, "Clique em carta interativa deve disparar o evento.");

            Object.Destroy(view.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Click_WhenNotInteractable_DoesNotRaiseEvent()
        {
            CardView view = Object.Instantiate(LoadCardPrefab());
            yield return null;

            view.SetCard(new Card(Suit.Paus, Rank.Sete), true);
            view.IsInteractable = false;

            int clickCount = 0;
            view.Clicked += _ => clickCount++;

            var eventData = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute<IPointerClickHandler>(view.gameObject, eventData, ExecuteEvents.pointerClickHandler);

            Assert.AreEqual(0, clickCount, "Carta nao interativa nao deve disparar clique.");

            Object.Destroy(view.gameObject);
            yield return null;
        }
    }
}
