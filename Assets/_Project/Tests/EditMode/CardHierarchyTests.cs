using NUnit.Framework;
using SuecaOnline.Domain.Sueca.Cards;

namespace SuecaOnline.Tests.EditMode
{
    public class CardHierarchyTests
    {
        [Test]
        public void StrengthOrder_IsAsSevenKingJackQueenSixFiveFourThreeTwo()
        {
            Rank[] descending =
            {
                Rank.As, Rank.Sete, Rank.Rei, Rank.Valete, Rank.Dama,
                Rank.Seis, Rank.Cinco, Rank.Quatro, Rank.Tres, Rank.Dois
            };

            for (int i = 0; i < descending.Length - 1; i++)
            {
                Assert.Greater(
                    RankStrength.Get(descending[i]),
                    RankStrength.Get(descending[i + 1]),
                    descending[i] + " deve ser mais forte que " + descending[i + 1]);
            }
        }

        [Test]
        public void Seven_Beats_King()
        {
            Assert.IsTrue(RankStrength.Beats(Rank.Sete, Rank.Rei));
            Assert.IsFalse(RankStrength.Beats(Rank.Rei, Rank.Sete));
        }

        [Test]
        public void Jack_Beats_Queen()
        {
            Assert.IsTrue(RankStrength.Beats(Rank.Valete, Rank.Dama));
            Assert.IsFalse(RankStrength.Beats(Rank.Dama, Rank.Valete));
        }

        [Test]
        public void NumericEnumValue_DoesNotMatchStrength()
        {
            Assert.Less((int)Rank.Sete, (int)Rank.Rei);
            Assert.IsTrue(RankStrength.Beats(Rank.Sete, Rank.Rei), "7 deve vencer o Rei apesar do valor numerico menor.");

            Assert.Less((int)Rank.Valete, (int)Rank.Dama);
            Assert.IsTrue(RankStrength.Beats(Rank.Valete, Rank.Dama), "Valete deve vencer a Dama.");
        }

        [Test]
        public void Card_EqualityAndPoints()
        {
            var ace = new Card(Suit.Copas, Rank.As);
            var same = new Card(Suit.Copas, Rank.As);
            var different = new Card(Suit.Copas, Rank.Sete);

            Assert.AreEqual(ace, same);
            Assert.IsTrue(ace == same);
            Assert.IsTrue(ace != different);
            Assert.AreEqual(11, ace.Points);
            Assert.AreEqual(9, ace.Strength);
        }
    }
}
