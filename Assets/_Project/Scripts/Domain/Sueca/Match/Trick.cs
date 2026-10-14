using System;
using System.Collections.Generic;
using SuecaOnline.Domain.Sueca.Cards;
using SuecaOnline.Domain.Sueca.Rules;

namespace SuecaOnline.Domain.Sueca.Match
{
    /// <summary>Vaza em andamento: sequencia ordenada das cartas jogadas e seus assentos.</summary>
    public sealed class Trick
    {
        private readonly List<PlayedCard> _plays = new List<PlayedCard>(4);

        public IReadOnlyList<PlayedCard> Plays
        {
            get { return _plays; }
        }

        public int Count
        {
            get { return _plays.Count; }
        }

        public bool IsEmpty
        {
            get { return _plays.Count == 0; }
        }

        /// <summary>Naipe puxado (o da primeira carta). Requer ao menos uma carta.</summary>
        public Suit LeadSuit
        {
            get
            {
                if (_plays.Count == 0)
                {
                    throw new InvalidOperationException("A vaza ainda nao possui carta puxada.");
                }

                return _plays[0].Card.Suit;
            }
        }

        public void Add(Seat seat, Card card)
        {
            if (_plays.Count >= MatchState.PlayersPerTable)
            {
                throw new InvalidOperationException("A vaza ja esta completa.");
            }

            _plays.Add(new PlayedCard(seat, card));
        }

        /// <summary>Assento vencedor da vaza segundo <see cref="TrickRules"/>.</summary>
        public Seat ResolveWinner(Suit trump)
        {
            var cards = new Card[_plays.Count];
            for (int i = 0; i < _plays.Count; i++)
            {
                cards[i] = _plays[i].Card;
            }

            int winnerIndex = TrickRules.ResolveWinnerIndex(cards, trump);
            return _plays[winnerIndex].Seat;
        }

        /// <summary>Soma dos pontos das cartas da vaza.</summary>
        public int Points()
        {
            int total = 0;
            for (int i = 0; i < _plays.Count; i++)
            {
                total += _plays[i].Card.Points;
            }

            return total;
        }

        public PlayedCard[] ToArray()
        {
            return _plays.ToArray();
        }
    }
}
