using System;

namespace SuecaOnline.Domain.Sueca.Cards
{
    /// <summary>
    /// Hierarquia de força da Sueca, separada da pontuação.
    /// Ordem decrescente: Ás &gt; 7 &gt; Rei &gt; Valete &gt; Dama &gt; 6 &gt; 5 &gt; 4 &gt; 3 &gt; 2.
    /// NUNCA comparar <see cref="Rank"/> por valor numérico para decidir a vaza.
    /// </summary>
    public static class RankStrength
    {
        /// <summary>Força relativa; maior vence. Intervalo de 0 (2) a 9 (Ás).</summary>
        public static int Get(Rank rank)
        {
            switch (rank)
            {
                case Rank.Dois:
                    return 0;
                case Rank.Tres:
                    return 1;
                case Rank.Quatro:
                    return 2;
                case Rank.Cinco:
                    return 3;
                case Rank.Seis:
                    return 4;
                case Rank.Dama:
                    return 5;
                case Rank.Valete:
                    return 6;
                case Rank.Rei:
                    return 7;
                case Rank.Sete:
                    return 8;
                case Rank.As:
                    return 9;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rank), rank, "Valor de carta desconhecido.");
            }
        }

        /// <summary>Verdadeiro se <paramref name="left"/> é mais forte que <paramref name="right"/>.</summary>
        public static bool Beats(Rank left, Rank right)
        {
            return Get(left) > Get(right);
        }
    }
}
