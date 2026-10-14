using SuecaOnline.Domain.Sueca.Cards;

namespace SuecaOnline.Domain.Sueca.Match
{
    /// <summary>Resultado consolidado de uma vaza resolvida.</summary>
    public readonly struct TrickRecord
    {
        public TrickRecord(Suit leadSuit, Suit trump, Seat winner, PlayedCard[] plays, int points)
        {
            LeadSuit = leadSuit;
            Trump = trump;
            Winner = winner;
            Plays = plays;
            Points = points;
        }

        public Suit LeadSuit { get; }

        public Suit Trump { get; }

        public Seat Winner { get; }

        public PlayedCard[] Plays { get; }

        public int Points { get; }
    }
}
