using System;
using System.Collections.Generic;
using SuecaOnline.Domain.Sueca.Cards;

namespace SuecaOnline.Domain.Sueca.Rules
{
    /// <summary>
    /// Resolucao de vaza. Vence:
    /// 1) o trunfo de maior forca, se houver trunfos;
    /// 2) caso contrario, a carta de maior forca do naipe puxado (o primeiro jogado).
    /// Carta descartada de outro naipe nunca vence.
    /// </summary>
    public static class TrickRules
    {
        /// <summary>Indice (na ordem de jogada) do vencedor da vaza.</summary>
        public static int ResolveWinnerIndex(IReadOnlyList<Card> played, Suit trump)
        {
            if (played == null)
            {
                throw new ArgumentNullException(nameof(played));
            }

            if (played.Count == 0)
            {
                throw new ArgumentException("A vaza nao possui cartas.", nameof(played));
            }

            Suit leadSuit = played[0].Suit;
            int winner = 0;

            for (int i = 1; i < played.Count; i++)
            {
                Suit candidateSuit = played[i].Suit;
                Suit currentSuit = played[winner].Suit;

                bool candidateIsTrump = candidateSuit == trump;
                bool currentIsTrump = currentSuit == trump;

                if (candidateIsTrump && !currentIsTrump)
                {
                    winner = i;
                    continue;
                }

                if (candidateIsTrump != currentIsTrump)
                {
                    continue;
                }

                if (candidateIsTrump)
                {
                    if (played[i].Strength > played[winner].Strength)
                    {
                        winner = i;
                    }

                    continue;
                }

                bool candidateIsLead = candidateSuit == leadSuit;
                bool currentIsLead = currentSuit == leadSuit;

                if (candidateIsLead && !currentIsLead)
                {
                    winner = i;
                }
                else if (candidateIsLead && currentIsLead && played[i].Strength > played[winner].Strength)
                {
                    winner = i;
                }
            }

            return winner;
        }
    }
}
