using System;

namespace SuecaOnline.Domain.Sueca.Exceptions
{
    /// <summary>Jogada ilegal segundo as regras da Sueca (turno, posse da carta ou assistir ao naipe).</summary>
    public sealed class InvalidMoveException : Exception
    {
        public InvalidMoveException(string message) : base(message)
        {
        }
    }
}
