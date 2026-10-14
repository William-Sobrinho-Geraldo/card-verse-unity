using System;
using System.Collections;
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
    public sealed class CardView : MonoBehaviour,
        ICardView,
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
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

        [SerializeField, Tooltip("Overlay de brilho usado no hover/pressao.")]
        private Image _highlightOverlay;

        [SerializeField, Tooltip("Raiz visual escalada no feedback de interacao (padrao: esta raiz).")]
        private RectTransform _visualRoot;

        [SerializeField, Range(1f, 1.2f), Tooltip("Escala aplicada quando o ponteiro esta sobre a carta.")]
        private float _hoverScale = 1.05f;

        [SerializeField, Range(0.85f, 1f), Tooltip("Escala aplicada enquanto a carta esta pressionada.")]
        private float _pressScale = 0.97f;

        [SerializeField, Min(0.01f), Tooltip("Duracao da transicao visual, em segundos.")]
        private float _animationDuration = 0.08f;

        [SerializeField, Tooltip("Cor do brilho aplicado no hover/pressao.")]
        private Color _highlightColor = new Color(1f, 1f, 1f, 0.2f);

        [SerializeField, Tooltip("Cor aplicada a Copas e Ouros.")]
        private Color _redSuitColor = new Color(0.72f, 0.09f, 0.11f, 1f);

        [SerializeField, Tooltip("Cor aplicada a Espadas e Paus.")]
        private Color _blackSuitColor = new Color(0.09f, 0.09f, 0.11f, 1f);

        private bool _isInteractable;
        private bool _isHovered;
        private bool _isPressed;
        private Coroutine _visualRoutine;

        public event Action<ICardView> Clicked;

        public Card Card { get; private set; }

        public bool IsFaceUp { get; private set; }

        public bool IsInteractable
        {
            get { return _isInteractable; }
            set
            {
                if (_isInteractable == value)
                {
                    return;
                }

                _isInteractable = value;
                if (!value)
                {
                    ResetInteraction();
                }
            }
        }

        private RectTransform VisualRoot
        {
            get
            {
                if (_visualRoot != null)
                {
                    return _visualRoot;
                }

                _visualRoot = (RectTransform)transform;
                return _visualRoot;
            }
        }

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

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsInteractable)
            {
                return;
            }

            _isHovered = true;
            ApplyVisualState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isHovered && !_isPressed)
            {
                return;
            }

            _isHovered = false;
            _isPressed = false;
            ApplyVisualState();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable)
            {
                return;
            }

            _isPressed = true;
            ApplyVisualState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isPressed)
            {
                return;
            }

            _isPressed = false;
            ApplyVisualState();
        }

        private void OnDisable()
        {
            ResetInteraction();
        }

        private void ResetInteraction()
        {
            _isHovered = false;
            _isPressed = false;

            if (_visualRoutine != null)
            {
                StopCoroutine(_visualRoutine);
                _visualRoutine = null;
            }

            VisualRoot.localScale = Vector3.one;

            if (_highlightOverlay != null)
            {
                Color cleared = _highlightOverlay.color;
                cleared.a = 0f;
                _highlightOverlay.color = cleared;
            }
        }

        private void ApplyVisualState()
        {
            float targetScale = _isPressed ? _pressScale : (_isHovered ? _hoverScale : 1f);
            float targetHighlight = (_isHovered || _isPressed) ? _highlightColor.a : 0f;

            if (!isActiveAndEnabled)
            {
                return;
            }

            if (_visualRoutine != null)
            {
                StopCoroutine(_visualRoutine);
            }

            _visualRoutine = StartCoroutine(AnimateVisual(targetScale, targetHighlight));
        }

        private IEnumerator AnimateVisual(float targetScale, float targetHighlight)
        {
            RectTransform root = VisualRoot;
            Vector3 startScale = root.localScale;
            float startHighlight = _highlightOverlay != null ? _highlightOverlay.color.a : 0f;
            float elapsed = 0f;

            while (elapsed < _animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / _animationDuration));

                root.localScale = Vector3.Lerp(startScale, Vector3.one * targetScale, t);

                if (_highlightOverlay != null)
                {
                    Color color = _highlightOverlay.color;
                    color.a = Mathf.Lerp(startHighlight, targetHighlight, t);
                    _highlightOverlay.color = color;
                }

                yield return null;
            }

            root.localScale = Vector3.one * targetScale;

            if (_highlightOverlay != null)
            {
                Color color = _highlightOverlay.color;
                color.a = targetHighlight;
                _highlightOverlay.color = color;
            }

            _visualRoutine = null;
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
