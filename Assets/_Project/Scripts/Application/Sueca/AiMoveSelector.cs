using System;
using System.Collections.Generic;
using SuecaOnline.Domain.Sueca.Cards;
using SuecaOnline.Domain.Sueca.Match;

namespace SuecaOnline.Application.Sueca
{
    /// <summary>
    /// Heuristica deterministica de selecao de jogada para jogadores controlados
    /// pelo computador. C# puro: sem qualquer dependencia de UnityEngine.
    /// A forca e os pontos sempre vem das tabelas do dominio (RankStrength/CardPoints).
    /// </summary>
    public static class AiMoveSelector
    {
        /// <summary>
        /// Escolhe uma carta entre as jogadas legais.
        /// Ao liderar, joga a carta mais barata disponivel; ao seguir, tenta vencer
        /// com a menor forca possivel e, se nao puder, descarta a de menor valor.
        /// </summary>
        public static Card Select(
            IReadOnlyList<Card> legalMoves,
            bool isLeading,
            Card currentBest,
            Suit leadSuit,
            Suit trump)
        {
            if (legalMoves == null)
            {
                throw new ArgumentNullException(nameof(legalMoves));
            }

            if (legalMoves.Count == 0)
            {
                throw new ArgumentException("Nao ha jogadas legais disponiveis.", nameof(legalMoves));
            }

            if (isLeading)
            {
                return SelectLead(legalMoves, trump);
            }

            return SelectFollow(legalMoves, currentBest, leadSuit, trump);
        }

        /// <summary>Carta atualmente vencendo a vaza (a mais forte entre as ja jogadas).</summary>
        public static Card CurrentBest(IReadOnlyList<PlayedCard> plays, Suit trump)
        {
            if (plays == null)
            {
                throw new ArgumentNullException(nameof(plays));
            }

            if (plays.Count == 0)
            {
                throw new ArgumentException("A vaza ainda nao possui cartas.", nameof(plays));
            }

            Suit leadSuit = plays[0].Card.Suit;
            Card best = plays[0].Card;

            for (int i = 1; i < plays.Count; i++)
            {
                if (WinsAgainst(plays[i].Card, best, leadSuit, trump))
                {
                    best = plays[i].Card;
                }
            }

            return best;
        }

        private static Card SelectLead(IReadOnlyList<Card> legal, Suit trump)
        {
            Card best = legal[0];
            int bestCost = LeadCost(legal[0], trump);

            for (int i = 1; i < legal.Count; i++)
            {
                int cost = LeadCost(legal[i], trump);
                if (cost < bestCost)
                {
                    best = legal[i];
                    bestCost = cost;
                }
            }

            return best;
        }

        private static Card SelectFollow(IReadOnlyList<Card> legal, Card currentBest, Suit leadSuit, Suit trump)
        {
            Card cheapestWinner = default;
            bool hasWinner = false;
            int winnerStrength = int.MaxValue;

            for (int i = 0; i < legal.Count; i++)
            {
                Card candidate = legal[i];
                if (!WinsAgainst(candidate, currentBest, leadSuit, trump))
                {
                    continue;
                }

                int strength = RankStrength.Get(candidate.Rank);
                if (!hasWinner || strength < winnerStrength)
                {
                    cheapestWinner = candidate;
                    winnerStrength = strength;
                    hasWinner = true;
                }
            }

            if (hasWinner)
            {
                return cheapestWinner;
            }

            return SelectDiscard(legal);
        }

        private static Card SelectDiscard(IReadOnlyList<Card> legal)
        {
            Card best = legal[0];
            int bestCost = DiscardCost(legal[0]);

            for (int i = 1; i < legal.Count; i++)
            {
                int cost = DiscardCost(legal[i]);
                if (cost < bestCost)
                {
                    best = legal[i];
                    bestCost = cost;
                }
            }

            return best;
        }

        private static int LeadCost(Card card, Suit trump)
        {
            int trumpPenalty = card.Suit == trump ? 100 : 0;
            return (card.Points * 10) + RankStrength.Get(card.Rank) + trumpPenalty;
        }

        private static int DiscardCost(Card card)
        {
            return (card.Points * 10) + RankStrength.Get(card.Rank);
        }

        /// <summary>Verdadeiro se <paramref name="candidate"/> vence <paramref name="incumbent"/> nesta vaza.</summary>
        private static bool WinsAgainst(Card candidate, Card incumbent, Suit leadSuit, Suit trump)
        {
            bool candidateIsTrump = candidate.Suit == trump;
            bool incumbentIsTrump = incumbent.Suit == trump;

            if (candidateIsTrump && !incumbentIsTrump)
            {
                return true;
            }

            if (candidateIsTrump != incumbentIsTrump)
            {
                return false;
            }

            if (candidateIsTrump)
            {
                return RankStrength.Get(candidate.Rank) > RankStrength.Get(incumbent.Rank);
            }

            bool candidateIsLead = candidate.Suit == leadSuit;
            bool incumbentIsLead = incumbent.Suit == leadSuit;

            if (candidateIsLead && !incumbentIsLead)
            {
                return true;
            }

            if (!candidateIsLead)
            {
                return false;
            }

            if (!incumbentIsLead)
            {
                return true;
            }

            return RankStrength.Get(candidate.Rank) > RankStrength.Get(incumbent.Rank);
        }
    }
}
