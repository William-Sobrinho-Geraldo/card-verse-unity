namespace SuecaOnline.Domain.Sueca.Match
{
    /// <summary>Assento fixo na mesa. Parceiros ficam frente a frente (Norte/Sul e Leste/Oeste).</summary>
    public enum Seat
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    /// <summary>Parceria fixa de dois jogadores.</summary>
    public enum Partnership
    {
        NorthSouth = 0,
        EastWest = 1
    }

    /// <summary>Utilitarios de assento, turno e parceria.</summary>
    public static class Seats
    {
        /// <summary>Todos os assentos na ordem do turno (horario).</summary>
        public static readonly Seat[] All = { Seat.North, Seat.East, Seat.South, Seat.West };

        /// <summary>Assento seguinte no sentido do turno.</summary>
        public static Seat Next(Seat seat)
        {
            return (Seat)(((int)seat + 1) & 3);
        }

        /// <summary>Parceria a que pertence o assento.</summary>
        public static Partnership PartnershipOf(Seat seat)
        {
            return seat == Seat.North || seat == Seat.South
                ? Partnership.NorthSouth
                : Partnership.EastWest;
        }

        /// <summary>Parceria adversaria.</summary>
        public static Partnership OpponentOf(Partnership partnership)
        {
            return partnership == Partnership.NorthSouth
                ? Partnership.EastWest
                : Partnership.NorthSouth;
        }
    }
}
