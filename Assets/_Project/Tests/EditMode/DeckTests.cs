using System;
using System.Collections.Generic;
using NUnit.Framework;
using SuecaOnline.Domain.Sueca.Cards;

namespace SuecaOnline.Tests.EditMode
{
    public class DeckTests
    {
        [Test]
        public void CreateStandard_Has40UniqueCards()
        {
            Deck deck = Deck.CreateStandard();

            Assert.AreEqual(Deck.StandardCardCount, deck.Cards.Count);

            var unique = new HashSet<Card>(deck.Cards);
            Assert.AreEqual(Deck.StandardCardCount, unique.Count, "O baralho nao pode conter cartas duplicadas.");
        }

        [Test]
        public void CreateStandard_Totals120Points()
        {
            Deck deck = Deck.CreateStandard();

            Assert.AreEqual(CardPoints.TotalDeckPoints, deck.TotalPoints);
            Assert.AreEqual(120, deck.TotalPoints);
        }

        [Test]
        public void CreateStandard_EachSuitHas10CardsAnd30Points()
        {
            Deck deck = Deck.CreateStandard();

            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                int count = 0;
                int points = 0;

                foreach (Card card in deck.Cards)
                {
                    if (card.Suit == suit)
                    {
                        count++;
                        points += card.Points;
                    }
                }

                Assert.AreEqual(10, count, "Contagem do naipe " + suit);
                Assert.AreEqual(30, points, "Pontos do naipe " + suit);
            }
        }

        [TestCase(Rank.As, 11)]
        [TestCase(Rank.Sete, 10)]
        [TestCase(Rank.Rei, 4)]
        [TestCase(Rank.Valete, 3)]
        [TestCase(Rank.Dama, 2)]
        [TestCase(Rank.Seis, 0)]
        [TestCase(Rank.Cinco, 0)]
        [TestCase(Rank.Quatro, 0)]
        [TestCase(Rank.Tres, 0)]
        [TestCase(Rank.Dois, 0)]
        public void CardPoints_MatchClassicTable(Rank rank, int expectedPoints)
        {
            Assert.AreEqual(expectedPoints, CardPoints.Get(rank));
        }

        [Test]
        public void FromCards_Throws_WhenCountIsWrong()
        {
            Assert.Throws<InvalidDeckException>(() => Deck.FromCards(new List<Card>()));
        }

        [Test]
        public void FromCards_Throws_WhenDuplicateCardsExist()
        {
            var cards = new List<Card>();
            for (int i = 0; i < Deck.StandardCardCount; i++)
            {
                cards.Add(new Card(Suit.Ouros, Rank.As));
            }

            Assert.Throws<InvalidDeckException>(() => Deck.FromCards(cards));
        }
    }
}
