using System;
using SuecaOnline.Domain.Sueca.Cards;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SuecaOnline.Presentation.Cards
{
    /// <summary>
    /// Renderiza uma carta da Sueca (valor e naipe em TextMeshPro) e traduz
    /// cliques/toques do jogador em eventos. Nao contem regras de jogo.
    /// </summary>
    public sealed class CardView : MonoBehaviour, ICardView, IPointerClickHandler
    {
        private const string OurosGlyph = "\u2666";
        private const string CopasGlyph = "\u2665";
        private const string EspadasGlyph = "\u2660";
        private const string PausGlyph = "\u2663";

        [SerializeField, Tooltip("Imagem da frente da carta.")]
        private Image _faceImage;

        [SerializeField, Tooltip("Imagem do verso da carta.")]
        private Image _backImage;

        [SerializeField, Tooltip("Texto do valor (As, 7, Rei...).")]
        private TextMeshProUGUI _rankText;

        [SerializeField, Tooltip("Texto do naipe.")]
        private TextMeshProUGUI _suitText;

        [SerializeField, Tooltip("Cor aplicada a Copas e Ouros.")]
        private Color _redSuitColor = new Color(0.72f, 0.09f, 0.11f, 1f);

        [SerializeField, Tooltip("Cor aplicada a Espadas e Paus.")]
        private Color _blackSuitColor = new Color(0.09f, 0.09f, 0.11f, 1f);

        public event Action<ICardView> Clicked;

        public Card Card { get; private set; }

        public bool IsFaceUp { get; private set; }

        public bool IsInteractable { get; set; }

        public void SetCard(Card card, bool faceUp)
        {
            Card = card;
            IsFaceUp = faceUp;

            if (_rankText != null)
            {
                _rankText.text = RankLabel(card.Rank);
                _rankText.color = IsRedSuit(card.Suit) ? _redSuitColor : _blackSuitColor;
            }

            if (_suitText != null)
            {
                _suitText.text = SuitGlyph(card.Suit);
                _suitText.color = IsRedSuit(card.Suit) ? _redSuitColor : _blackSuitColor;
            }

            ApplyOrientation();
        }

        public void SetFaceUp(bool faceUp)
        {
            IsFaceUp = faceUp;
            ApplyOrientation();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsInteractable)
            {
                return;
            }

            Clicked?.Invoke(this);
        }

        private void ApplyOrientation()
        {
            if (_faceImage != null)
            {
                _faceImage.enabled = IsFaceUp;
            }

            if (_backImage != null)
            {
                _backImage.enabled = !IsFaceUp;
            }

            if (_rankText != null)
            {
                _rankText.enabled = IsFaceUp;
            }

            if (_suitText != null)
            {
                _suitText.enabled = IsFaceUp;
            }
        }

        public static string RankLabel(Rank rank)
        {
            switch (rank)
            {
                case Rank.As:
                    return "A";
                case Rank.Rei:
                    return "K";
                case Rank.Dama:
                    return "Q";
                case Rank.Valete:
                    return "J";
                case Rank.Dois:
                    return "2";
                case Rank.Tres:
                    return "3";
                case Rank.Quatro:
                    return "4";
                case Rank.Cinco:
                    return "5";
                case Rank.Seis:
                    return "6";
                case Rank.Sete:
                    return "7";
                default:
                    throw new ArgumentOutOfRangeException(nameof(rank), rank, "Valor de carta desconhecido.");
            }
        }

        public static string SuitGlyph(Suit suit)
        {
            switch (suit)
            {
                case Suit.Ouros:
                    return OurosGlyph;
                case Suit.Copas:
                    return CopasGlyph;
                case Suit.Espadas:
                    return EspadasGlyph;
                case Suit.Paus:
                    return PausGlyph;
                default:
                    throw new ArgumentOutOfRangeException(nameof(suit), suit, "Naipe desconhecido.");
            }
        }

        private static bool IsRedSuit(Suit suit)
        {
            return suit == Suit.Ouros || suit == Suit.Copas;
        }
    }
}
