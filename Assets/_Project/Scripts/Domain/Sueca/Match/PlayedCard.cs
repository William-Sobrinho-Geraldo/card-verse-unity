using SuecaOnline.Domain.Sueca.Cards;

namespace SuecaOnline.Domain.Sueca.Match
{
    /// <summary>Carta jogada por um assento especifico dentro de uma vaza.</summary>
    public readonly struct PlayedCard
    {
        public PlayedCard(Seat seat, Card card)
        {
            Seat = seat;
            Card = card;
        }

        public Seat Seat { get; }

        public Card Card { get; }
    }
}
