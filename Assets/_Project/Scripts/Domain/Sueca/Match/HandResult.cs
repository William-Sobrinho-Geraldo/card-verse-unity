namespace SuecaOnline.Domain.Sueca.Match
{
    /// <summary>Resultado da pontuacao de uma mao (10 vazas).</summary>
    public readonly struct HandResult
    {
        public HandResult(
            int northSouthPoints,
            int eastWestPoints,
            int northSouthTricks,
            int eastWestTricks,
            int northSouthGames,
            int eastWestGames,
            bool isBandeira,
            Partnership? winner)
        {
            NorthSouthPoints = northSouthPoints;
            EastWestPoints = eastWestPoints;
            NorthSouthTricks = northSouthTricks;
            EastWestTricks = eastWestTricks;
            NorthSouthGames = northSouthGames;
            EastWestGames = eastWestGames;
            IsBandeira = isBandeira;
            Winner = winner;
        }

        public int NorthSouthPoints { get; }

        public int EastWestPoints { get; }

        public int NorthSouthTricks { get; }

        public int EastWestTricks { get; }

        public int NorthSouthGames { get; }

        public int EastWestGames { get; }

        /// <summary>Verdadeiro quando o vencedor levou todas as 10 vazas (4 jogos).</summary>
        public bool IsBandeira { get; }

        /// <summary>Parceria vencedora da mao; nulo em empate 60-60.</summary>
        public Partnership? Winner { get; }
    }
}
