using System;
using System.Collections;
using UnityEngine;

namespace SuecaOnline.Presentation.Animation
{
    /// <summary>Curvas de suavizacao usadas nas animacoes de carta.</summary>
    public enum Ease
    {
        Linear,
        EaseInQuad,
        EaseOutQuad,
        EaseInOutQuad,
        EaseOutCubic,
        EaseOutBack,
        EaseOutElastic
    }

    /// <summary>
    /// Utilitarios de tween puramente visuais (interpolacao segura baseada em tempo).
    /// Opera apenas sobre Transform/CanvasGroup: nunca toca no estado de regras, preservando
    /// o determinismo do nucleo. Todas as rotinas usam tempo nao escalado e retornam
    /// <see cref="IEnumerator"/> para serem iniciadas por um MonoBehaviour.
    /// </summary>
    public static class CardAnimator
    {
        private const float BackOvershoot = 1.70158f;
        private const float ElasticPeriod = 0.35f;

        /// <summary>Avalia a curva de suavizacao para um progresso normalizado [0,1].</summary>
        public static float Evaluate(float t, Ease ease)
        {
            t = Mathf.Clamp01(t);

            switch (ease)
            {
                case Ease.EaseInQuad:
                    return t * t;
                case Ease.EaseOutQuad:
                    return 1f - ((1f - t) * (1f - t));
                case Ease.EaseInOutQuad:
                    return t < 0.5f
                        ? 2f * t * t
                        : 1f - (Mathf.Pow((-2f * t) + 2f, 2f) * 0.5f);
                case Ease.EaseOutCubic:
                    return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.EaseOutBack:
                    float c1 = BackOvershoot;
                    float c3 = c1 + 1f;
                    return 1f + (c3 * Mathf.Pow(t - 1f, 3f)) + (c1 * Mathf.Pow(t - 1f, 2f));
                case Ease.EaseOutElastic:
                    if (t <= 0f)
                    {
                        return 0f;
                    }

                    if (t >= 1f)
                    {
                        return 1f;
                    }

                    return (Mathf.Pow(2f, -10f * t) *
                            Mathf.Sin(((t - (ElasticPeriod * 0.25f)) * (2f * Mathf.PI)) / ElasticPeriod)) + 1f;
                default:
                    return t;
            }
        }

        /// <summary>Pausa puramente visual, independente de <c>Time.timeScale</c>.</summary>
        public static IEnumerator Delay(float seconds)
        {
            if (seconds <= 0f)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        /// <summary>Interpola linearmente uma acao sobre o tempo, aplicando a curva informada.</summary>
        public static IEnumerator Tween(float duration, Ease ease, Action<float> apply)
        {
            if (apply == null)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                apply(1f);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                apply(Evaluate(elapsed / duration, ease));
                yield return null;
            }

            apply(1f);
        }

        public static IEnumerator MoveAnchored(RectTransform rect, Vector2 from, Vector2 to, float duration, Ease ease)
        {
            if (rect == null)
            {
                yield break;
            }

            yield return Tween(duration, ease, t =>
            {
                if (rect != null)
                {
                    rect.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
                }
            });
        }

        public static IEnumerator RotateZ(RectTransform rect, float fromDegrees, float toDegrees, float duration, Ease ease)
        {
            if (rect == null)
            {
                yield break;
            }

            yield return Tween(duration, ease, t =>
            {
                if (rect != null)
                {
                    rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(fromDegrees, toDegrees, t));
                }
            });
        }

        public static IEnumerator Scale(RectTransform rect, Vector3 from, Vector3 to, float duration, Ease ease)
        {
            if (rect == null)
            {
                yield break;
            }

            yield return Tween(duration, ease, t =>
            {
                if (rect != null)
                {
                    rect.localScale = Vector3.LerpUnclamped(from, to, t);
                }
            });
        }

        public static IEnumerator Fade(CanvasGroup group, float from, float to, float duration, Ease ease)
        {
            if (group == null)
            {
                yield break;
            }

            group.alpha = from;
            yield return Tween(duration, ease, t =>
            {
                if (group != null)
                {
                    group.alpha = Mathf.LerpUnclamped(from, to, t);
                }
            });
        }

        /// <summary>Sacudida horizontal amortecida, restaurando a posicao original ao final.</summary>
        public static IEnumerator Shake(RectTransform rect, float duration, float magnitude)
        {
            if (rect == null || duration <= 0f)
            {
                yield break;
            }

            Vector2 origin = rect.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float damper = 1f - progress;
                float offset = Mathf.Sin(progress * Mathf.PI * 8f) * magnitude * damper;
                rect.anchoredPosition = origin + new Vector2(offset, 0f);
                yield return null;
            }

            if (rect != null)
            {
                rect.anchoredPosition = origin;
            }
        }

        public static CanvasGroup GetOrAddCanvasGroup(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            CanvasGroup group = target.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = target.AddComponent<CanvasGroup>();
            }

            return group;
        }
    }
}
