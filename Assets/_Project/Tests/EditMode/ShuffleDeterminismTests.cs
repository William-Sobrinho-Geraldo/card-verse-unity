using System.Collections.Generic;
using NUnit.Framework;
using SuecaOnline.Domain.Sueca.Cards;
using SuecaOnline.Domain.Sueca.Random;

namespace SuecaOnline.Tests.EditMode
{
    public class ShuffleDeterminismTests
    {
        [Test]
        public void SameSeed_ProducesSameOrder()
        {
            IReadOnlyList<Card> deck = Deck.CreateStandard().Cards;

            IReadOnlyList<Card> first = new FisherYatesShuffler(new SeededRandomSource(12345)).Shuffle(deck);
            IReadOnlyList<Card> second = new FisherYatesShuffler(new SeededRandomSource(12345)).Shuffle(deck);

            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void Shuffle_PreservesAllCardsAndTotalPoints()
        {
            IReadOnlyList<Card> deck = Deck.CreateStandard().Cards;

            IReadOnlyList<Card> shuffled = new FisherYatesShuffler(new SeededRandomSource(7)).Shuffle(deck);

            Assert.AreEqual(Deck.StandardCardCount, shuffled.Count);
            Assert.AreEqual(CardPoints.TotalDeckPoints, Deck.SumPoints(shuffled));
            CollectionAssert.AreEquivalent(deck, shuffled);
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentOrders()
        {
            IReadOnlyList<Card> deck = Deck.CreateStandard().Cards;

            IReadOnlyList<Card> first = new FisherYatesShuffler(new SeededRandomSource(1)).Shuffle(deck);
            IReadOnlyList<Card> second = new FisherYatesShuffler(new SeededRandomSource(2)).Shuffle(deck);

            CollectionAssert.AreNotEqual(first, second);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(100)]
        [TestCase(1000)]
        public void SeededRandomSource_Next_StaysWithinBounds(int seed)
        {
            var random = new SeededRandomSource(seed);

            for (int i = 0; i < 500; i++)
            {
                int value = random.Next(40);
                Assert.GreaterOrEqual(value, 0);
                Assert.Less(value, 40);
            }
        }

        [Test]
        public void SeededRandomSource_Next_ThrowsOnNonPositiveBound()
        {
            var random = new SeededRandomSource(1);

            Assert.Throws<System.ArgumentOutOfRangeException>(() => random.Next(0));
        }
    }
}
