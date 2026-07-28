#nullable enable

using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlappyTemplate
{
    [RequireComponent(typeof(RectTransform))]
    public class Toast : MonoBehaviour
    {
        readonly float offsetX = 20f;
        private ToastOptions? currentOptions;
        private Tween? autoHideTween;
        private Tween? mainTween;
        private Tween? hideTween;

        [SerializeField]
        Color errorColor = Color.red;

        [SerializeField]
        Color warningColor = Color.wheat;

        [SerializeField]
        Color successColor = Color.green;

        [SerializeField]
        private RectTransform rectTransform = null!;

        [SerializeField]
        private Image backgroundImage = null!;

        [SerializeField]
        private Button closeButton = null!;

        [SerializeField]
        private RectTransform titleParent = null!;

        [SerializeField]
        private TMP_Text titleText = null!;

        [SerializeField]
        private TMP_Text contentText = null!;

        void Awake()
        {
            if (titleParent == null)
                throw new ArgumentException("titleParent is not assigned");
            if (titleText == null)
                throw new ArgumentException("titleText is not assigned");
            if (contentText == null)
                throw new ArgumentException("contentText is not assigned");
            if (closeButton == null)
                throw new ArgumentException("closeButton is not assigned");
            if (rectTransform == null)
                throw new ArgumentException("rectTransform is not assigned");
            if (backgroundImage == null)
                throw new ArgumentException("backgroundImage is not assigned");
        }

        void OnEnable()
        {
            Reset();
        }

        private void Reset()
        {
            autoHideTween?.Kill();
            hideTween?.Kill();
            mainTween?.Kill();
            rectTransform!.anchoredPosition = new Vector2(
                rectTransform.rect.width + offsetX,
                rectTransform.anchoredPosition.y
            );
        }

        public void Show(ToastOptions options)
        {
            Reset();
            transform.gameObject.SetActive(true);
            currentOptions = options;
            float animationDuration = currentOptions.AnimationDuration.Milliseconds / 1000f;
            titleText.text = currentOptions.Title ?? string.Empty;
            if (titleText.text == string.Empty)
                titleParent.gameObject.SetActive(false);
            else
                titleParent.gameObject.SetActive(true);
            contentText.text = currentOptions.Message;
            closeButton.gameObject.SetActive(currentOptions.ShowCloseButton);
            backgroundImage.color = currentOptions.Type switch
            {
                EToastType.Error => errorColor,
                EToastType.Warning => warningColor,
                EToastType.Success => successColor,
                _ => Color.white,
            };
            mainTween = DOTween
                .To(
                    () => rectTransform.anchoredPosition,
                    x => rectTransform.anchoredPosition = x,
                    new Vector2(0, rectTransform.anchoredPosition.y),
                    animationDuration
                )
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    if (currentOptions.AutoHide && currentOptions.HideAfter.TotalMilliseconds > 0)
                        autoHideTween = DOVirtual.DelayedCall(
                            currentOptions.HideAfter.Seconds,
                            Hide
                        );
                });
        }

        public void Hide()
        {
            float animationDuration = currentOptions!.AnimationDuration.Milliseconds / 1000f;
            hideTween = DOTween
                .To(
                    () => rectTransform.anchoredPosition,
                    x => rectTransform.anchoredPosition = x,
                    new Vector2(
                        rectTransform.rect.width + offsetX,
                        rectTransform.anchoredPosition.y
                    ),
                    animationDuration
                )
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    transform.gameObject.SetActive(false);
                });
        }

        public void OnCloseClick()
        {
            Hide();
        }
    }
}
