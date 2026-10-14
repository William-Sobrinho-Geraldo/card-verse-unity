using System;
using System.Collections.Generic;
using SuecaOnline.Domain.Sueca.Cards;

namespace SuecaOnline.Domain.Sueca.Random
{
    /// <summary>
    /// Gera um estado inicial repetivel a partir de uma semente inteira.
    /// Um mesmo seed produz sempre a mesma sequencia de numeros.
    /// </summary>
    public sealed class SeededRandomSource : IRandomSource
    {
        private uint _state;

        public SeededRandomSource(int seed)
        {
            _state = (uint)seed;
            if (_state == 0u)
            {
                _state = 0x9E3779B9u;
            }
        }

        /// <inheritdoc />
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxExclusive), maxExclusive, "O limite superior deve ser maior que zero.");
            }

            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;

            return (int)(_state % (uint)maxExclusive);
        }
    }

    /// <summary>Embaralhamento Fisher-Yates deterministico orientado por <see cref="IRandomSource"/>.</summary>
    public sealed class FisherYatesShuffler : IShuffler
    {
        private readonly IRandomSource _random;

        public FisherYatesShuffler(IRandomSource random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <inheritdoc />
        public IReadOnlyList<Card> Shuffle(IReadOnlyList<Card> cards)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            var buffer = new Card[cards.Count];
            for (int i = 0; i < cards.Count; i++)
            {
                buffer[i] = cards[i];
            }

            for (int i = buffer.Length - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                Card swap = buffer[i];
                buffer[i] = buffer[j];
                buffer[j] = swap;
            }

            return buffer;
        }
    }
}
