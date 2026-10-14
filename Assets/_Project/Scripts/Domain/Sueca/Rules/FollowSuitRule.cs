using System.Collections.Generic;
using SuecaOnline.Domain.Sueca.Cards;

namespace SuecaOnline.Domain.Sueca.Rules
{
    /// <summary>
    /// Obrigacao de assistir ao naipe: se o jogador possui carta do naipe puxado,
    /// ele DEVE joga-la. Sem carta do naipe, pode jogar qualquer carta (trunfo opcional).
    /// </summary>
    public static class FollowSuitRule
    {
        /// <summary>Verdadeiro se a mao possui ao menos uma carta do naipe puxado.</summary>
        public static bool HasSuit(IReadOnlyList<Card> hand, Suit suit)
        {
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i].Suit == suit)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Verdadeiro se a carta satisfaz a obrigacao de assistir ao naipe puxado.</summary>
        public static bool IsLegalPlay(IReadOnlyList<Card> hand, Card card, Suit leadSuit)
        {
            if (card.Suit == leadSuit)
            {
                return true;
            }

            return !HasSuit(hand, leadSuit);
        }

        /// <summary>Cartas da mao que satisfazem a obrigacao de assistir ao naipe puxado.</summary>
        public static IReadOnlyList<Card> GetLegalPlays(IReadOnlyList<Card> hand, Suit leadSuit)
        {
            var legal = new List<Card>(hand.Count);
            bool hasLead = HasSuit(hand, leadSuit);

            for (int i = 0; i < hand.Count; i++)
            {
                Card card = hand[i];
                if (!hasLead || card.Suit == leadSuit)
                {
                    legal.Add(card);
                }
            }

            return legal;
        }
    }
}
