using System;

namespace SuecaOnline.Domain.Sueca.Exceptions
{
    /// <summary>Operacao invalida para a fase atual da maquina de estados.</summary>
    public sealed class InvalidPhaseException : Exception
    {
        public InvalidPhaseException(string message) : base(message)
        {
        }
    }
}
