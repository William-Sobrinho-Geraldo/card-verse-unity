using System.Collections;
using System.Collections.Generic;
using SuecaOnline.Domain.Sueca.Cards;
using SuecaOnline.Domain.Sueca.Match;
using SuecaOnline.Presentation.Animation;
using SuecaOnline.Presentation.Cards;
using UnityEngine;

namespace SuecaOnline.Presentation.UI
{
    /// <summary>
    /// Gerencia visualmente as cartas jogadas no centro da mesa, uma por assento.
    /// Nao decide regras: apenas apresenta e limpa a vaza.
    /// </summary>
    public sealed class TableView : MonoBehaviour
    {
        [SerializeField, Tooltip("Prefab da carta usado no centro da mesa.")]
        private CardView _cardViewPrefab;

        [SerializeField, Tooltip("Ancora da carta do assento Norte.")]
        private RectTransform _northSlot;

        [SerializeField, Tooltip("Ancora da carta do assento Leste.")]
        private RectTransform _eastSlot;

        [SerializeField, Tooltip("Ancora da carta do assento Sul.")]
        private RectTransform _southSlot;

        [SerializeField, Tooltip("Ancora da carta do assento Oeste.")]
        private RectTransform _westSlot;

        [SerializeField, Tooltip("Escala aplicada a carta vencedora da vaza.")]
        private float _winnerScale = 1.12f;

        private readonly Dictionary<Seat, RectTransform> _slots = new Dictionary<Seat, RectTransform>();
        private readonly Dictionary<Seat, CardView> _shown = new Dictionary<Seat, CardView>();

        public int ShownCount
        {
            get { return _shown.Count; }
        }

        public bool HasCard(Seat seat)
        {
            return _shown.ContainsKey(seat);
        }

        /// <summary>Carta atualmente exibida no centro da mesa para o assento (ou nula).</summary>
        public CardView GetShownCard(Seat seat)
        {
            return _shown.TryGetValue(seat, out CardView view) ? view : null;
        }

        /// <summary>Slot central associado ao assento (ou nulo).</summary>
        public RectTransform SlotFor(Seat seat)
        {
            RebuildSlots();
            return _slots.TryGetValue(seat, out RectTransform slot) ? slot : null;
        }

        /// <summary>Configuracao programatica (usada por ferramentas Editor e testes).</summary>
        public void Configure(
            CardView cardViewPrefab,
            RectTransform northSlot,
            RectTransform eastSlot,
            RectTransform southSlot,
            RectTransform westSlot)
        {
            _cardViewPrefab = cardViewPrefab;
            _northSlot = northSlot;
            _eastSlot = eastSlot;
            _southSlot = southSlot;
            _westSlot = westSlot;
            RebuildSlots();
        }

        public void ShowCard(Seat seat, Card card)
        {
            CreateCardView(seat, card);
        }

        /// <summary>
        /// Exibe a carta no centro da mesa com um movimento fluido a partir de uma
        /// posicao de mundo (tipicamente a carta na mao), rotacionando ate a posicao final.
        /// Puramente visual: nao altera o estado da partida.
        /// </summary>
        public CardView ShowCardAnimated(Seat seat, Card card, Vector3 fromWorld, float duration, float fromRotationDegrees)
        {
            CardView view = CreateCardView(seat, card);
            if (view == null)
            {
                return null;
            }

            RectTransform rect = view.Rect;
            Vector2 target = rect.anchoredPosition;
            Vector2 start = rect.parent != null
                ? (Vector2)rect.parent.InverseTransformPoint(fromWorld)
                : target;

            rect.anchoredPosition = start;
            rect.localRotation = Quaternion.Euler(0f, 0f, fromRotationDegrees);
            rect.localScale = Vector3.one * 0.86f;

            if (isActiveAndEnabled && duration > 0f)
            {
                StartCoroutine(AnimateIn(rect, start, target, fromRotationDegrees, duration));
            }
            else
            {
                rect.anchoredPosition = target;
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
            }

            return view;
        }

        private CardView CreateCardView(Seat seat, Card card)
        {
            RebuildSlots();

            if (!_slots.TryGetValue(seat, out RectTransform slot) || slot == null)
            {
                return null;
            }

            if (_shown.TryGetValue(seat, out CardView existing) && existing != null)
            {
                Destroy(existing.gameObject);
                _shown.Remove(seat);
            }

            if (_cardViewPrefab == null)
            {
                return null;
            }

            CardView view = Instantiate(_cardViewPrefab, slot);
            view.name = "TableCard_" + seat;

            RectTransform rect = view.Rect;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            view.SetCard(card, true);
            view.IsInteractable = false;
            _shown[seat] = view;
            return view;
        }

        private static IEnumerator AnimateIn(RectTransform rect, Vector2 start, Vector2 target, float fromRotation, float duration)
        {
            yield return CardAnimator.Tween(duration, Ease.EaseOutCubic, t =>
            {
                if (rect == null)
                {
                    return;
                }

                rect.anchoredPosition = Vector2.LerpUnclamped(start, target, t);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(fromRotation, 0f, t));
                rect.localScale = Vector3.one * Mathf.LerpUnclamped(0.86f, 1f, t);
            });
        }

        public void HighlightWinner(Seat seat)
        {
            foreach (KeyValuePair<Seat, CardView> pair in _shown)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                float scale = pair.Key == seat ? _winnerScale : 1f;
                pair.Value.transform.localScale = Vector3.one * scale;
            }
        }

        public void ClearTrick()
        {
            foreach (KeyValuePair<Seat, CardView> pair in _shown)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value.gameObject);
                }
            }

            _shown.Clear();
        }

        private void RebuildSlots()
        {
            _slots.Clear();

            if (_northSlot != null)
            {
                _slots[Seat.North] = _northSlot;
            }

            if (_eastSlot != null)
            {
                _slots[Seat.East] = _eastSlot;
            }

            if (_southSlot != null)
            {
                _slots[Seat.South] = _southSlot;
            }

            if (_westSlot != null)
            {
                _slots[Seat.West] = _westSlot;
            }
        }
    }
}
