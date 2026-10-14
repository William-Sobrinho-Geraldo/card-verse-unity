using System;

namespace SuecaOnline.Domain.Sueca.Cards
{
    /// <summary>
    /// Tabela canônica de pontos das cartas da Sueca.
    /// Ás = 11, 7 = 10, Rei = 4, Valete = 3, Dama = 2, demais = 0.
    /// </summary>
    public static class CardPoints
    {
        /// <summary>Total de pontos de um baralho completo (120).</summary>
        public const int TotalDeckPoints = 120;

        /// <summary>Pontos de uma carta pelo seu valor nominal.</summary>
        public static int Get(Rank rank)
        {
            switch (rank)
            {
                case Rank.As:
                    return 11;
                case Rank.Sete:
                    return 10;
                case Rank.Rei:
                    return 4;
                case Rank.Valete:
                    return 3;
                case Rank.Dama:
                    return 2;
                case Rank.Dois:
                case Rank.Tres:
                case Rank.Quatro:
                case Rank.Cinco:
                case Rank.Seis:
                    return 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rank), rank, "Valor de carta desconhecido.");
            }
        }
    }
}
