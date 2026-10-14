using System;
using System.Collections.Generic;

namespace SuecaOnline.Domain.Sueca.Cards
{
    /// <summary>Violacao de invariantes do baralho (contagem, duplicidade ou pontuacao).</summary>
    public sealed class InvalidDeckException : Exception
    {
        public InvalidDeckException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Baralho da Sueca: 40 cartas unicas (baralho frances sem 8, 9 e 10),
    /// totalizando exatamente 120 pontos (30 por naipe).
    /// </summary>
    public sealed class Deck
    {
        /// <summary>Numero padrao de cartas de um baralho de Sueca.</summary>
        public const int StandardCardCount = 40;

        private readonly Card[] _cards;

        private Deck(Card[] cards)
        {
            _cards = cards;
        }

        /// <summary>Cartas que compoem o baralho, em ordem imutavel.</summary>
        public IReadOnlyList<Card> Cards
        {
            get { return _cards; }
        }

        /// <summary>Soma dos pontos de todas as cartas do baralho.</summary>
        public int TotalPoints
        {
            get { return SumPoints(_cards); }
        }

        /// <summary>Cria o baralho padrao de 40 cartas e valida suas invariantes.</summary>
        public static Deck CreateStandard()
        {
            var cards = new Card[StandardCardCount];
            int index = 0;

            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                {
                    cards[index++] = new Card(suit, rank);
                }
            }

            var deck = new Deck(cards);
            deck.ValidateStandard();
            return deck;
        }

        /// <summary>Constroi um baralho a partir de cartas arbitrarias (uso em testes).</summary>
        public static Deck FromCards(IEnumerable<Card> cards, bool validate = true)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            var deck = new Deck(new List<Card>(cards).ToArray());
            if (validate)
            {
                deck.ValidateStandard();
            }

            return deck;
        }

        /// <summary>Soma os pontos de uma colecao de cartas.</summary>
        public static int SumPoints(IEnumerable<Card> cards)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            int total = 0;
            foreach (Card card in cards)
            {
                total += card.Points;
            }

            return total;
        }

        /// <summary>Valida contagem (40), unicidade e total de pontos (120).</summary>
        public void ValidateStandard()
        {
            if (_cards.Length != StandardCardCount)
            {
                throw new InvalidDeckException(
                    "Baralho deve conter " + StandardCardCount + " cartas, mas contem " + _cards.Length + ".");
            }

            var seen = new HashSet<Card>();
            foreach (Card card in _cards)
            {
                if (!seen.Add(card))
                {
                    throw new InvalidDeckException("Carta duplicada no baralho: " + card + ".");
                }
            }

            if (seen.Count != StandardCardCount)
            {
                throw new InvalidDeckException("Baralho possui cartas faltantes ou valores invalidos.");
            }

            int expected = StandardCardCount / 4;
            var perSuit = new Dictionary<Suit, int>();
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                perSuit[suit] = 0;
            }

            foreach (Card card in _cards)
            {
                perSuit[card.Suit] = perSuit[card.Suit] + 1;
            }

            foreach (KeyValuePair<Suit, int> pair in perSuit)
            {
                if (pair.Value != expected)
                {
                    throw new InvalidDeckException(
                        "Naipe " + pair.Key + " deve conter " + expected + " cartas, mas contem " + pair.Value + ".");
                }
            }

            if (TotalPoints != CardPoints.TotalDeckPoints)
            {
                throw new InvalidDeckException(
                    "Baralho deve totalizar " + CardPoints.TotalDeckPoints + " pontos, mas totaliza " + TotalPoints + ".");
            }
        }
    }
}
