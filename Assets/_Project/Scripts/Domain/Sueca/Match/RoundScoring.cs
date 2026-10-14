using System;
using SuecaOnline.Domain.Sueca.Cards;

namespace SuecaOnline.Domain.Sueca.Match
{
    /// <summary>
    /// Pontuacao pura de uma mao (10 vazas). Separada de <see cref="MatchState"/>
    /// para ser testavel isoladamente.
    /// Regras: &gt;60 vence; 61-90 = 1 jogo; 91-119 = 2 jogos; todas as 10 vazas = 4 jogos
    /// (bandeira); 120 pontos sem todas as vazas = 2 jogos; empate 60-60 gera bonus
    /// acumulavel de 1 jogo para a proxima mao decidida.
    /// </summary>
    public static class RoundScoring
    {
        public static HandResult Calculate(
            int northSouthPoints,
            int eastWestPoints,
            int northSouthTricks,
            int eastWestTricks,
            int pendingTieBonusGames,
            out int newPendingTieBonusGames)
        {
            if (northSouthPoints + eastWestPoints != CardPoints.TotalDeckPoints)
            {
                throw new ArgumentException(
                    "Os pontos das parcerias devem somar " + CardPoints.TotalDeckPoints + ".");
            }

            if (northSouthTricks + eastWestTricks != MatchState.TricksPerRound)
            {
                throw new ArgumentException(
                    "O total de vazas deve ser " + MatchState.TricksPerRound + ".");
            }

            if (northSouthPoints < 0 || eastWestPoints < 0)
            {
                throw new ArgumentException("Os pontos nao podem ser negativos.");
            }

            if (pendingTieBonusGames < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pendingTieBonusGames), "O bonus de empate nao pode ser negativo.");
            }

            newPendingTieBonusGames = pendingTieBonusGames;

            Partnership? winner;
            if (northSouthPoints == eastWestPoints)
            {
                winner = null;
            }
            else
            {
                winner = northSouthPoints > eastWestPoints
                    ? Partnership.NorthSouth
                    : Partnership.EastWest;
            }

            int northSouthGames = 0;
            int eastWestGames = 0;
            bool bandeira = false;

            if (!winner.HasValue)
            {
                newPendingTieBonusGames = pendingTieBonusGames + 1;
            }
            else
            {
                Partnership w = winner.Value;
                int points = w == Partnership.NorthSouth ? northSouthPoints : eastWestPoints;
                int tricks = w == Partnership.NorthSouth ? northSouthTricks : eastWestTricks;

                int games;
                if (tricks == MatchState.TricksPerRound)
                {
                    games = 4;
                    bandeira = true;
                }
                else if (points >= 91)
                {
                    games = 2;
                }
                else if (points >= 61)
                {
                    games = 1;
                }
                else
                {
                    games = 0;
                }

                games += pendingTieBonusGames;
                newPendingTieBonusGames = 0;

                if (w == Partnership.NorthSouth)
                {
                    northSouthGames = games;
                }
                else
                {
                    eastWestGames = games;
                }
            }

            return new HandResult(
                northSouthPoints,
                eastWestPoints,
                northSouthTricks,
                eastWestTricks,
                northSouthGames,
                eastWestGames,
                bandeira,
                winner);
        }
    }
}
