using System;

namespace SuecaOnline.Domain.Sueca.Cards
{
    /// <summary>
    /// Carta imutável da Sueca, composta por naipe e valor.
    /// Força e pontos são consultados pelas tabelas dedicadas.
    /// </summary>
    public readonly struct Card : IEquatable<Card>
    {
        public Card(Suit suit, Rank rank)
        {
            Suit = suit;
            Rank = rank;
        }

        public Suit Suit { get; }

        public Rank Rank { get; }

        /// <summary>Pontos da carta (ver <see cref="CardPoints"/>).</summary>
        public int Points
        {
            get { return CardPoints.Get(Rank); }
        }

        /// <summary>Força da carta dentro do naipe (ver <see cref="RankStrength"/>).</summary>
        public int Strength
        {
            get { return RankStrength.Get(Rank); }
        }

        public bool Equals(Card other)
        {
            return Suit == other.Suit && Rank == other.Rank;
        }

        public override bool Equals(object obj)
        {
            return obj is Card other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Suit * 397) ^ (int)Rank;
            }
        }

        public override string ToString()
        {
            return Rank + " de " + Suit;
        }

        public static bool operator ==(Card left, Card right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Card left, Card right)
        {
            return !left.Equals(right);
        }
    }
}
