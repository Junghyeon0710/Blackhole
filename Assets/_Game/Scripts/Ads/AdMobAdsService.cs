// Google Mobile Ads Unity 플러그인을 설치하고 Player Settings > Scripting Define Symbols 에
// BLACKHOLE_ADMOB 을 넣으면 기기 빌드에서 이 구현을 쓴다. 넣기 전에는 컴파일되지 않는다.
// 광고 단위 ID 는 구글이 공개한 테스트 ID 다. 출시 직전에만 실제 ID 로 바꾼다 (실제 광고를 직접 누르면 계정이 정지될 수 있다).
#if BLACKHOLE_ADMOB
using System;
using System.Threading.Tasks;
using GoogleMobileAds.Api;
using UnityEngine;

namespace Blackhole
{
    public sealed class AdMobAdsService : IAdsService
    {
#if UNITY_IOS
        const string BannerId = "ca-app-pub-3940256099942544/2934735716";
        const string InterstitialId = "ca-app-pub-3940256099942544/4411468910";
        const string RewardedId = "ca-app-pub-3940256099942544/1712485313";
#else
        const string BannerId = "ca-app-pub-3940256099942544/6300978111";
        const string InterstitialId = "ca-app-pub-3940256099942544/1033173712";
        const string RewardedId = "ca-app-pub-3940256099942544/5224354917";
#endif

        RewardedAd rewarded;
        InterstitialAd interstitial;
        BannerView banner;
        bool bannerWanted;
        int rewardedRetry, interstitialRetry;

        public bool IsRewardedReady => rewarded != null && rewarded.CanShowAd();

        public AdMobAdsService()
        {
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            MobileAds.Initialize(_ =>
            {
                LoadRewarded();
                LoadInterstitial();
                if (bannerWanted) ShowBanner();
            });
        }

        void LoadRewarded()
        {
            rewarded?.Destroy();
            rewarded = null;
            RewardedAd.Load(RewardedId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null) { After(NextDelay(ref rewardedRetry), LoadRewarded); return; }
                rewardedRetry = 0;
                rewarded = ad;
            });
        }

        void LoadInterstitial()
        {
            interstitial?.Destroy();
            interstitial = null;
            InterstitialAd.Load(InterstitialId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null) { After(NextDelay(ref interstitialRetry), LoadInterstitial); return; }
                interstitialRetry = 0;
                interstitial = ad;
            });
        }

        // 실패하면 2, 4, 8 … 최대 64초 뒤 다시 로드한다 (오프라인에서는 광고만 빠지고 게임은 돈다)
        static int NextDelay(ref int attempt)
        {
            int seconds = Math.Min(64, 2 << Math.Min(attempt, 5));
            attempt++;
            return seconds;
        }

        static async void After(int seconds, Action action)
        {
            await Task.Delay(seconds * 1000);
            action();
        }

        public void ShowRewarded(string label, Action onRewarded, Action onClosed)
        {
            if (!IsRewardedReady) { onClosed?.Invoke(); return; }
            var ad = rewarded;
            ad.OnAdFullScreenContentClosed += () => { onClosed?.Invoke(); LoadRewarded(); };
            ad.OnAdFullScreenContentFailed += _ => { onClosed?.Invoke(); LoadRewarded(); };
            ad.Show(_ => onRewarded?.Invoke());
        }

        public void ShowInterstitial(Action onClosed)
        {
            var ad = interstitial;
            if (ad == null || !ad.CanShowAd()) { onClosed?.Invoke(); return; }
            ad.OnAdFullScreenContentClosed += () => { onClosed?.Invoke(); LoadInterstitial(); };
            ad.OnAdFullScreenContentFailed += _ => { onClosed?.Invoke(); LoadInterstitial(); };
            ad.Show();
        }

        public void ShowBanner()
        {
            bannerWanted = true;
            if (banner == null)
            {
                banner = new BannerView(BannerId, AdSize.Banner, AdPosition.Bottom);
                banner.LoadAd(new AdRequest());
            }
            banner.Show();
        }

        public void HideBanner()
        {
            bannerWanted = false;
            banner?.Hide();
        }
    }
}
#endif
