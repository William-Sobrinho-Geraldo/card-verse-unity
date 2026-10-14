using System.Collections.Generic;
using SuecaOnline.Domain.Sueca.Cards;
using SuecaOnline.Domain.Sueca.Match;
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
            RebuildSlots();

            if (!_slots.TryGetValue(seat, out RectTransform slot) || slot == null)
            {
                return;
            }

            if (_shown.TryGetValue(seat, out CardView existing) && existing != null)
            {
                Destroy(existing.gameObject);
                _shown.Remove(seat);
            }

            if (_cardViewPrefab == null)
            {
                return;
            }

            CardView view = Instantiate(_cardViewPrefab, slot);
            view.name = "TableCard_" + seat;

            RectTransform rect = (RectTransform)view.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;

            view.SetCard(card, true);
            view.IsInteractable = false;
            _shown[seat] = view;
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
