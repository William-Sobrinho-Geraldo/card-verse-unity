using System.Collections;
using NUnit.Framework;
using SuecaOnline.Domain.Sueca.Cards;
using SuecaOnline.Domain.Sueca.Match;
using SuecaOnline.Presentation.Cards;
using SuecaOnline.Presentation.UI;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SuecaOnline.Tests.PlayMode
{
    public class TableViewPlayModeTests
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

        private static RectTransform CreateSlot(string name)
        {
            GameObject slot = new GameObject(name, typeof(RectTransform));
            return (RectTransform)slot.transform;
        }

        [UnityTest]
        public IEnumerator ShowCard_PlacesOneCardPerSeat()
        {
            CardView prefab = LoadCardPrefab();
            Assert.IsNotNull(prefab, "CardPrefab.prefab deve existir.");

            GameObject tableObject = new GameObject("Table", typeof(RectTransform));
            TableView table = tableObject.AddComponent<TableView>();

            RectTransform north = CreateSlot("North");
            RectTransform east = CreateSlot("East");
            RectTransform south = CreateSlot("South");
            RectTransform west = CreateSlot("West");
            table.Configure(prefab, north, east, south, west);

            table.ShowCard(Seat.North, new Card(Suit.Ouros, Rank.As));
            table.ShowCard(Seat.East, new Card(Suit.Copas, Rank.Sete));
            table.ShowCard(Seat.South, new Card(Suit.Espadas, Rank.Rei));
            table.ShowCard(Seat.West, new Card(Suit.Paus, Rank.Dama));
            yield return null;

            Assert.AreEqual(4, table.ShownCount, "Apos quatro jogadas a mesa deve mostrar quatro cartas.");
            Assert.IsTrue(table.HasCard(Seat.North));
            Assert.IsTrue(table.HasCard(Seat.South));

            Object.DestroyImmediate(tableObject);
            Object.DestroyImmediate(north.gameObject);
            Object.DestroyImmediate(east.gameObject);
            Object.DestroyImmediate(south.gameObject);
            Object.DestroyImmediate(west.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClearTrick_RemovesAllShownCards()
        {
            CardView prefab = LoadCardPrefab();
            GameObject tableObject = new GameObject("Table", typeof(RectTransform));
            TableView table = tableObject.AddComponent<TableView>();

            RectTransform north = CreateSlot("North");
            RectTransform east = CreateSlot("East");
            RectTransform south = CreateSlot("South");
            RectTransform west = CreateSlot("West");
            table.Configure(prefab, north, east, south, west);

            table.ShowCard(Seat.North, new Card(Suit.Ouros, Rank.As));
            table.ShowCard(Seat.East, new Card(Suit.Copas, Rank.Sete));
            yield return null;

            Assert.AreEqual(2, table.ShownCount);

            table.ClearTrick();
            Assert.AreEqual(0, table.ShownCount, "Limpar a vaza deve remover todas as cartas exibidas.");

            Object.DestroyImmediate(tableObject);
            Object.DestroyImmediate(north.gameObject);
            Object.DestroyImmediate(east.gameObject);
            Object.DestroyImmediate(south.gameObject);
            Object.DestroyImmediate(west.gameObject);
            yield return null;
        }
    }
}
