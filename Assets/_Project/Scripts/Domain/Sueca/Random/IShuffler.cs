using System.Collections.Generic;
using SuecaOnline.Domain.Sueca.Cards;

namespace SuecaOnline.Domain.Sueca.Random
{
    /// <summary>Contrato de embaralhamento, isolavel para testes e para injecao de comportamento.</summary>
    public interface IShuffler
    {
        /// <summary>Retorna uma nova colecao embaralhada, sem alterar a original.</summary>
        IReadOnlyList<Card> Shuffle(IReadOnlyList<Card> cards);
    }
}
