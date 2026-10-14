using System.Collections.Generic;
using NUnit.Framework;
using SuecaOnline.Domain.Sueca.Cards;
using SuecaOnline.Domain.Sueca.Rules;

namespace SuecaOnline.Tests.EditMode
{
    public class TrickResolutionTests
    {
        [Test]
        public void HighestLeadSuitWins_WhenNoTrumpPlayed()
        {
            var played = new List<Card>
            {
                new Card(Suit.Copas, Rank.Quatro),
                new Card(Suit.Copas, Rank.As),
                new Card(Suit.Espadas, Rank.As)
            };

            int winner = TrickRules.ResolveWinnerIndex(played, Suit.Ouros);

            Assert.AreEqual(1, winner, "O As de Copas (naipe puxado) deve vencer; descarte de Espadas nao vence.");
        }

        [Test]
        public void DiscardOfOtherSuit_NeverWins_EvenIfStronger()
        {
            var played = new List<Card>
            {
                new Card(Suit.Ouros, Rank.Seis),
                new Card(Suit.Copas, Rank.As),
                new Card(Suit.Paus, Rank.Rei),
                new Card(Suit.Ouros, Rank.Dois)
            };

            int winner = TrickRules.ResolveWinnerIndex(played, Suit.Espadas);

            Assert.AreEqual(0, winner, "Apenas cartas do naipe puxado (Ouros) podem vencer sem trunfo.");
        }

        [Test]
        public void AnyTrump_BeatsStrongestLeadCard()
        {
            var played = new List<Card>
            {
                new Card(Suit.Copas, Rank.As),
                new Card(Suit.Espadas, Rank.Dois),
                new Card(Suit.Paus, Rank.Rei)
            };

            int winner = TrickRules.ResolveWinnerIndex(played, Suit.Espadas);

            Assert.AreEqual(1, winner, "Qualquer trunfo vence o naipe puxado.");
        }

        [Test]
        public void HighestTrumpWins_WhenMultipleTrumps()
        {
            var played = new List<Card>
            {
                new Card(Suit.Copas, Rank.As),
                new Card(Suit.Espadas, Rank.Dois),
                new Card(Suit.Espadas, Rank.Sete),
                new Card(Suit.Paus, Rank.Rei)
            };

            int winner = TrickRules.ResolveWinnerIndex(played, Suit.Espadas);

            Assert.AreEqual(2, winner, "Entre trunfos, vence o de maior forca (7).");
        }

        [Test]
        public void Jack_BeatsQueen_InLeadSuit()
        {
            var played = new List<Card>
            {
                new Card(Suit.Copas, Rank.Dama),
                new Card(Suit.Copas, Rank.Valete)
            };

            int winner = TrickRules.ResolveWinnerIndex(played, Suit.Paus);

            Assert.AreEqual(1, winner, "O Valete vence a Dama na Sueca.");
        }

        [Test]
        public void Seven_BeatsKing_InLeadSuit()
        {
            var played = new List<Card>
            {
                new Card(Suit.Copas, Rank.Rei),
                new Card(Suit.Copas, Rank.Sete)
            };

            int winner = TrickRules.ResolveWinnerIndex(played, Suit.Paus);

            Assert.AreEqual(1, winner, "O 7 (manilha) vence o Rei.");
        }

        [Test]
        public void ResolveWinnerIndex_Throws_WhenNoCards()
        {
            Assert.Throws<System.ArgumentException>(
                () => TrickRules.ResolveWinnerIndex(new List<Card>(), Suit.Ouros));
        }
    }
}
