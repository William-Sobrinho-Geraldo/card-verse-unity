using System.Collections;
using SuecaOnline.Presentation.Animation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SuecaOnline.Presentation.UI
{
    /// <summary>Tom visual de uma mensagem de feedback.</summary>
    public enum FeedbackTone
    {
        Info,
        Warning,
        Positive
    }

    /// <summary>
    /// Camada de feedback ao jogador: indicador de turno e toasts flutuantes.
    /// Puramente visual: nao decide regras nem le estado interno do dominio.
    /// Pode montar a propria hierarquia em tempo de execucao (<see cref="EnsureBuilt"/>)
    /// ou ser materializada pelo gerador de cena no Editor.
    /// </summary>
    public sealed class GameFeedbackUI : MonoBehaviour
    {
        [Header("Indicador de turno")]
        [SerializeField]
        private RectTransform _turnPanel;

        [SerializeField]
        private Image _turnImage;

        [SerializeField]
        private TextMeshProUGUI _turnText;

        [SerializeField]
        private CanvasGroup _turnGroup;

        [Header("Toast de alerta")]
        [SerializeField]
        private RectTransform _toastPanel;

        [SerializeField]
        private Image _toastImage;

        [SerializeField]
        private TextMeshProUGUI _toastText;

        [SerializeField]
        private CanvasGroup _toastGroup;

        [Header("Estilo")]
        [SerializeField]
        private Sprite _roundedSprite;

        [SerializeField, Min(0.05f)]
        private float _toastFadeIn = 0.15f;

        [SerializeField, Min(0.1f)]
        private float _toastHold = 2.2f;

        [SerializeField, Min(0.05f)]
        private float _toastFadeOut = 0.35f;

        [SerializeField, Min(0f)]
        private float _toastRise = 22f;

        [SerializeField, Min(0.05f)]
        private float _turnFade = 0.18f;

        private Coroutine _toastRoutine;
        private Coroutine _turnRoutine;
        private bool _turnVisible;
        private bool _pulsing;
        private string _turnMessage;
        private Vector2 _toastBasePosition;

        public bool IsTurnVisible
        {
            get { return _turnVisible; }
        }

        public string TurnMessage
        {
            get { return _turnMessage; }
        }

        private void Awake()
        {
            EnsureBuilt();
            HideInstant();
        }

        private void OnEnable()
        {
            EnsureBuilt();
        }

        /// <summary>
        /// Garante que a hierarquia visual exista. Idempotente: pode ser chamada pelo
        /// Editor (monta e serializa a cena) e em runtime (fallback).
        /// </summary>
        public void EnsureBuilt()
        {
            if (_turnPanel != null && _turnText != null && _turnGroup != null &&
                _toastPanel != null && _toastText != null && _toastGroup != null)
            {
                _turnPanel.gameObject.SetActive(true);
                _toastPanel.gameObject.SetActive(true);
                _toastBasePosition = _toastPanel.anchoredPosition;
                return;
            }

            _turnPanel = CreatePanel("TurnIndicator", new Vector2(0.5f, 0f), new Vector2(0f, 332f), new Vector2(760f, 78f), PanelColor(FeedbackTone.Info));
            _turnImage = _turnPanel.GetComponent<Image>();
            _turnGroup = CardAnimator.GetOrAddCanvasGroup(_turnPanel.gameObject);
            _turnText = CreateLabel("TurnText", _turnPanel, 34f, TextColor(FeedbackTone.Info), FontStyles.Bold);

            _toastPanel = CreatePanel("Toast", new Vector2(0.5f, 1f), new Vector2(0f, -186f), new Vector2(940f, 92f), PanelColor(FeedbackTone.Warning));
            _toastImage = _toastPanel.GetComponent<Image>();
            _toastGroup = CardAnimator.GetOrAddCanvasGroup(_toastPanel.gameObject);
            _toastText = CreateLabel("ToastText", _toastPanel, 32f, TextColor(FeedbackTone.Warning), FontStyles.Normal);

            _toastBasePosition = _toastPanel.anchoredPosition;
        }

        /// <summary>Define a referencia do sprite arredondado usada pelos paineis.</summary>
        public void SetRoundedSprite(Sprite sprite)
        {
            _roundedSprite = sprite;
        }

        /// <summary>Exibe ou atualiza o indicador de turno com a mensagem informada.</summary>
        public void ShowTurn(string message, FeedbackTone tone)
        {
            EnsureBuilt();

            if (_turnPanel == null)
            {
                return;
            }

            bool changed = _turnMessage != message;
            _turnMessage = message;

            if (_turnText != null)
            {
                _turnText.text = message;
                _turnText.color = TextColor(tone);
            }

            if (_turnImage != null)
            {
                _turnImage.color = PanelColor(tone);
            }

            if (!_turnPanel.gameObject.activeSelf)
            {
                _turnPanel.gameObject.SetActive(true);
            }

            _turnVisible = true;
            RefreshTurnAnimation(changed);
        }

        /// <summary>Oculta o indicador de turno.</summary>
        public void HideTurn()
        {
            if (!_turnVisible || _turnPanel == null)
            {
                _turnVisible = false;
                _turnMessage = null;
                return;
            }

            _turnVisible = false;
            _turnMessage = null;

            if (!isActiveAndEnabled || _turnGroup == null)
            {
                HideInstant();
                return;
            }

            StopTurnRoutine();
            _turnRoutine = StartCoroutine(FadeTurnOut());
        }

        /// <summary>Exibe um toast flutuante que se desvanece automaticamente.</summary>
        public void ShowToast(string message, FeedbackTone tone)
        {
            EnsureBuilt();

            if (_toastPanel == null || _toastText == null)
            {
                return;
            }

            _toastText.text = message;
            _toastText.color = TextColor(tone);

            if (_toastImage != null)
            {
                _toastImage.color = PanelColor(tone);
            }

            _toastPanel.gameObject.SetActive(true);

            if (!isActiveAndEnabled || _toastGroup == null)
            {
                return;
            }

            if (_toastRoutine != null)
            {
                StopCoroutine(_toastRoutine);
            }

            _toastRoutine = StartCoroutine(ToastRoutine());
        }

        /// <summary>Limpa todo o feedback em exibicao.</summary>
        public void ClearAll()
        {
            if (_toastRoutine != null)
            {
                StopCoroutine(_toastRoutine);
                _toastRoutine = null;
            }

            StopTurnRoutine();
            _turnVisible = false;
            _turnMessage = null;
            HideInstant();
        }

        public static Color PanelColor(FeedbackTone tone)
        {
            switch (tone)
            {
                case FeedbackTone.Warning:
                    return new Color(0.28f, 0.04f, 0.04f, 0.94f);
                case FeedbackTone.Positive:
                    return new Color(0.04f, 0.20f, 0.11f, 0.94f);
                default:
                    return new Color(0.05f, 0.10f, 0.22f, 0.90f);
            }
        }

        public static Color TextColor(FeedbackTone tone)
        {
            switch (tone)
            {
                case FeedbackTone.Warning:
                    return new Color(1f, 0.88f, 0.82f, 1f);
                case FeedbackTone.Positive:
                    return new Color(0.86f, 1f, 0.88f, 1f);
                default:
                    return new Color(0.90f, 0.95f, 1f, 1f);
            }
        }

        private IEnumerator ToastRoutine()
        {
            RectTransform panel = _toastPanel;
            CanvasGroup group = _toastGroup;
            Vector2 basePosition = _toastBasePosition;

            panel.anchoredPosition = basePosition - new Vector2(0f, _toastRise);
            group.alpha = 0f;

            yield return CardAnimator.Tween(_toastFadeIn, Ease.EaseOutCubic, t =>
            {
                if (panel != null)
                {
                    panel.anchoredPosition = Vector2.LerpUnclamped(basePosition - new Vector2(0f, _toastRise), basePosition, t);
                }

                if (group != null)
                {
                    group.alpha = t;
                }
            });

            yield return CardAnimator.Delay(_toastHold);

            yield return CardAnimator.Tween(_toastFadeOut, Ease.EaseInOutQuad, t =>
            {
                if (group != null)
                {
                    group.alpha = 1f - t;
                }
            });

            panel.anchoredPosition = basePosition;

            if (panel != null)
            {
                panel.gameObject.SetActive(false);
            }

            _toastRoutine = null;
        }

        private void RefreshTurnAnimation(bool restart)
        {
            if (!isActiveAndEnabled || _turnGroup == null)
            {
                return;
            }

            StopTurnRoutine();
            _turnRoutine = StartCoroutine(TurnRoutine(restart));
        }

        private IEnumerator TurnRoutine(bool restart)
        {
            RectTransform panel = _turnPanel;
            CanvasGroup group = _turnGroup;

            if (restart)
            {
                panel.localScale = Vector3.one * 0.92f;
                group.alpha = 0f;

                yield return CardAnimator.Tween(_turnFade, Ease.EaseOutBack, t =>
                {
                    if (panel != null)
                    {
                        panel.localScale = Vector3.one * Mathf.LerpUnclamped(0.92f, 1f, t);
                    }

                    if (group != null)
                    {
                        group.alpha = t;
                    }
                });
            }
            else
            {
                panel.localScale = Vector3.one;
                group.alpha = 1f;
            }

            if (_pulsing)
            {
                _turnRoutine = null;
                yield break;
            }

            _pulsing = true;

            while (_turnVisible && panel != null)
            {
                yield return CardAnimator.Tween(0.7f, Ease.EaseInOutQuad, t =>
                {
                    if (panel != null && _turnVisible)
                    {
                        float scale = 1f + (Mathf.Sin(t * Mathf.PI) * 0.03f);
                        panel.localScale = Vector3.one * scale;
                    }
                });

                if (!_turnVisible)
                {
                    break;
                }

                yield return null;
            }

            _pulsing = false;

            if (panel != null)
            {
                panel.localScale = Vector3.one;
            }

            _turnRoutine = null;
        }

        private IEnumerator FadeTurnOut()
        {
            CanvasGroup group = _turnGroup;

            yield return CardAnimator.Tween(_turnFade, Ease.EaseInQuad, t =>
            {
                if (group != null)
                {
                    group.alpha = 1f - t;
                }
            });

            if (_turnPanel != null)
            {
                _turnPanel.gameObject.SetActive(false);
                _turnPanel.localScale = Vector3.one;
            }

            if (_turnGroup != null)
            {
                _turnGroup.alpha = 1f;
            }

            _pulsing = false;
            _turnRoutine = null;
        }

        private void StopTurnRoutine()
        {
            _pulsing = false;

            if (_turnRoutine != null)
            {
                StopCoroutine(_turnRoutine);
                _turnRoutine = null;
            }
        }

        private void HideInstant()
        {
            if (_turnPanel != null)
            {
                _turnPanel.gameObject.SetActive(false);
                _turnPanel.localScale = Vector3.one;
            }

            if (_turnGroup != null)
            {
                _turnGroup.alpha = 1f;
            }

            if (_toastPanel != null && _toastRoutine == null)
            {
                _toastPanel.gameObject.SetActive(false);
            }

            if (_toastGroup != null)
            {
                _toastGroup.alpha = 1f;
            }

            if (_toastPanel != null)
            {
                _toastPanel.anchoredPosition = _toastBasePosition;
            }
        }

        private RectTransform CreatePanel(string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = gameObject.AddComponent<Image>();
            image.sprite = _roundedSprite;
            image.type = _roundedSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;

            Outline outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.82f, 0.30f, 0.65f);
            outline.effectDistance = new Vector2(2f, -2f);

            return rect;
        }

        private TextMeshProUGUI CreateLabel(string name, RectTransform parent, float fontSize, Color color, FontStyles style)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(18f, 8f);
            rect.offsetMax = new Vector2(-18f, -8f);

            TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.raycastTarget = false;

            if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            return text;
        }
    }
}
