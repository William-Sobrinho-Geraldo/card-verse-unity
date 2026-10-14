using NUnit.Framework;
using SuecaOnline.Domain.Sueca.Match;

namespace SuecaOnline.Tests.EditMode
{
    public class RoundScoringTests
    {
        [Test]
        public void WinnerWith61To90Points_ScoresOneGame()
        {
            HandResult result = RoundScoring.Calculate(75, 45, 6, 4, 0, out int bonus);

            Assert.AreEqual(Partnership.NorthSouth, result.Winner);
            Assert.AreEqual(1, result.NorthSouthGames);
            Assert.AreEqual(0, result.EastWestGames);
            Assert.AreEqual(0, bonus);
            Assert.IsFalse(result.IsBandeira);
        }

        [Test]
        public void WinnerWith91To119Points_ScoresTwoGames()
        {
            HandResult result = RoundScoring.Calculate(20, 100, 4, 6, 0, out int bonus);

            Assert.AreEqual(Partnership.EastWest, result.Winner);
            Assert.AreEqual(2, result.EastWestGames);
            Assert.AreEqual(0, bonus);
            Assert.IsFalse(result.IsBandeira);
        }

        [Test]
        public void Bandeira_AllTenTricks_ScoresFourGames()
        {
            HandResult result = RoundScoring.Calculate(120, 0, 10, 0, 0, out int bonus);

            Assert.AreEqual(Partnership.NorthSouth, result.Winner);
            Assert.AreEqual(4, result.NorthSouthGames);
            Assert.IsTrue(result.IsBandeira);
            Assert.AreEqual(0, bonus);
        }

        [Test]
        public void AllPointsWithoutAllTricks_ScoresTwoGames_NotBandeira()
        {
            HandResult result = RoundScoring.Calculate(120, 0, 9, 1, 0, out int bonus);

            Assert.AreEqual(Partnership.NorthSouth, result.Winner);
            Assert.AreEqual(2, result.NorthSouthGames);
            Assert.IsFalse(result.IsBandeira, "120 pontos perdendo uma vaza vale 2 jogos, nao bandeira.");
        }

        [Test]
        public void TieSixtySixty_ScoresNoGames_AndAccumulatesBonus()
        {
            HandResult result = RoundScoring.Calculate(60, 60, 5, 5, 0, out int bonus);

            Assert.IsNull(result.Winner);
            Assert.AreEqual(0, result.NorthSouthGames);
            Assert.AreEqual(0, result.EastWestGames);
            Assert.AreEqual(1, bonus, "Empate 60-60 gera 1 jogo de bonus para a proxima mao.");
        }

        [Test]
        public void AccumulatedTieBonus_IsAddedToNextDecidedHand()
        {
            HandResult result = RoundScoring.Calculate(61, 59, 5, 5, 1, out int bonus);

            Assert.AreEqual(Partnership.NorthSouth, result.Winner);
            Assert.AreEqual(2, result.NorthSouthGames, "1 jogo base + 1 de bonus.");
            Assert.AreEqual(0, bonus, "Bonus consumido apos a mao decidida.");
        }

        [Test]
        public void Calculate_Throws_WhenPointsDoNotSumTo120()
        {
            Assert.Throws<System.ArgumentException>(
                () => RoundScoring.Calculate(70, 40, 6, 4, 0, out _));
        }

        [Test]
        public void Calculate_Throws_WhenTricksDoNotSumToTen()
        {
            Assert.Throws<System.ArgumentException>(
                () => RoundScoring.Calculate(70, 50, 6, 3, 0, out _));
        }
    }
}
