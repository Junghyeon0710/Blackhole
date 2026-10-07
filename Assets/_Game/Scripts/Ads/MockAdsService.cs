using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blackhole
{
    /// <summary>
    /// 에디터용 모의 광고 (프로토타입의 가짜 광고 화면: 보상형 3초, 전면 2초).
    /// 보상형은 매번 잠깐 "로드 중"이 되고, X 로 중간에 닫으면 보상 없이 끝나서 실제 SDK 흐름을 미리 확인할 수 있다.
    /// </summary>
    public sealed class MockAdsService : MonoBehaviour, IAdsService
    {
        [SerializeField] CanvasGroup screen;
        [SerializeField] TMP_Text kindLabel;
        [SerializeField] TMP_Text countLabel;
        [SerializeField] TMP_Text rewardLabel;
        [SerializeField] Button closeButton;
        [SerializeField] GameObject bannerPlaceholder;

        [SerializeField] float rewardedSeconds = 3;
        [SerializeField] float interstitialSeconds = 2;
        [SerializeField] float rewardedLoadSeconds = 0.8f;

        float readyAt;
        Coroutine running;
        Action onClosedPending;

        public bool IsRewardedReady => running == null && Time.unscaledTime >= readyAt;

        void Awake()
        {
            readyAt = Time.unscaledTime + rewardedLoadSeconds;
            Hide();
            if (closeButton != null) closeButton.onClick.AddListener(CloseEarly);
        }

        public void ShowRewarded(string label, Action onRewarded, Action onClosed)
        {
            if (!IsRewardedReady) { onClosed?.Invoke(); return; }
            running = StartCoroutine(Play("보상형 광고", label, rewardedSeconds, true, onRewarded, onClosed));
        }

        public void ShowInterstitial(Action onClosed)
        {
            if (running != null) { onClosed?.Invoke(); return; }
            running = StartCoroutine(Play("전면 광고", "판과 판 사이에 나오는 광고 자리", interstitialSeconds, false, null, onClosed));
        }

        public void ShowBanner() { if (bannerPlaceholder) bannerPlaceholder.SetActive(true); }
        public void HideBanner() { if (bannerPlaceholder) bannerPlaceholder.SetActive(false); }

        IEnumerator Play(string kind, string label, float seconds, bool rewarded, Action onRewarded, Action onClosed)
        {
            onClosedPending = onClosed;
            kindLabel.text = kind;
            rewardLabel.text = label;
            if (closeButton != null) closeButton.gameObject.SetActive(rewarded);
            screen.alpha = 1;
            screen.blocksRaycasts = true;
            screen.gameObject.SetActive(true);
            for (int n = Mathf.CeilToInt(seconds); n > 0; n--)
            {
                countLabel.text = n.ToString();
                yield return new WaitForSecondsRealtime(1);
            }
            countLabel.text = "0";
            Finish();
            if (rewarded) onRewarded?.Invoke();
            onClosed?.Invoke();
        }

        void CloseEarly()
        {
            if (running == null) return;
            StopCoroutine(running);
            var onClosed = onClosedPending;
            Finish();
            onClosed?.Invoke();
        }

        void Finish()
        {
            running = null;
            onClosedPending = null;
            readyAt = Time.unscaledTime + rewardedLoadSeconds;
            Hide();
        }

        void Hide()
        {
            if (screen == null) return;
            screen.alpha = 0;
            screen.blocksRaycasts = false;
            screen.gameObject.SetActive(false);
        }
    }
}
