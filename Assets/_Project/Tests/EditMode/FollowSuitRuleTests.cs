using System.Collections.Generic;
using NUnit.Framework;
using SuecaOnline.Domain.Sueca.Cards;
using SuecaOnline.Domain.Sueca.Rules;

namespace SuecaOnline.Tests.EditMode
{
    public class FollowSuitRuleTests
    {
        private static List<Card> Hand(params Card[] cards)
        {
            return new List<Card>(cards);
        }

        [Test]
        public void HasSuit_True_WhenSuitIsPresent()
        {
            List<Card> hand = Hand(
                new Card(Suit.Copas, Rank.As),
                new Card(Suit.Espadas, Rank.Rei));

            Assert.IsTrue(FollowSuitRule.HasSuit(hand, Suit.Copas));
        }

        [Test]
        public void HasSuit_False_WhenSuitIsAbsent()
        {
            List<Card> hand = Hand(
                new Card(Suit.Copas, Rank.As),
                new Card(Suit.Espadas, Rank.Rei));

            Assert.IsFalse(FollowSuitRule.HasSuit(hand, Suit.Paus));
        }

        [Test]
        public void IsLegalPlay_False_WhenPlayerHasLeadSuitButPlaysAnother()
        {
            List<Card> hand = Hand(
                new Card(Suit.Ouros, Rank.Dois),
                new Card(Suit.Espadas, Rank.As));

            var offSuit = new Card(Suit.Espadas, Rank.As);

            Assert.IsFalse(FollowSuitRule.IsLegalPlay(hand, offSuit, Suit.Ouros));
        }

        [Test]
        public void IsLegalPlay_True_WhenPlayerHasNoLeadSuit()
        {
            List<Card> hand = Hand(
                new Card(Suit.Espadas, Rank.As),
                new Card(Suit.Paus, Rank.Rei));

            var offSuitTrump = new Card(Suit.Espadas, Rank.As);

            Assert.IsTrue(FollowSuitRule.IsLegalPlay(hand, offSuitTrump, Suit.Ouros));
        }

        [Test]
        public void GetLegalPlays_ReturnsOnlyLeadSuit_WhenPresent()
        {
            List<Card> hand = Hand(
                new Card(Suit.Ouros, Rank.Dois),
                new Card(Suit.Ouros, Rank.As),
                new Card(Suit.Espadas, Rank.Rei));

            IReadOnlyList<Card> legal = FollowSuitRule.GetLegalPlays(hand, Suit.Ouros);

            Assert.AreEqual(2, legal.Count);
            foreach (Card card in legal)
            {
                Assert.AreEqual(Suit.Ouros, card.Suit);
            }
        }

        [Test]
        public void GetLegalPlays_ReturnsWholeHand_WhenLeadSuitAbsent()
        {
            List<Card> hand = Hand(
                new Card(Suit.Espadas, Rank.Dois),
                new Card(Suit.Paus, Rank.As));

            IReadOnlyList<Card> legal = FollowSuitRule.GetLegalPlays(hand, Suit.Ouros);

            Assert.AreEqual(hand.Count, legal.Count);
        }
    }
}
