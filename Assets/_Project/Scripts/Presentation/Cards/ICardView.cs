using System;
using SuecaOnline.Domain.Sueca.Cards;

namespace SuecaOnline.Presentation.Cards
{
    /// <summary>Contrato de uma carta renderizada, independente da implementacao de UI.</summary>
    public interface ICardView
    {
        Card Card { get; }

        bool IsFaceUp { get; }

        bool IsInteractable { get; set; }

        event Action<ICardView> Clicked;

        void SetCard(Card card, bool faceUp);
    }
}
