using System;

namespace Blackhole
{
    /// <summary>
    /// 게임 코드는 광고 SDK 대신 이 인터페이스만 부른다. SDK 를 바꿔도 게임 코드는 그대로다.
    /// 에디터는 <see cref="MockAdsService"/>, 기기는 SDK 구현(예: AdMobAdsService)을 쓴다.
    /// </summary>
    public interface IAdsService
    {
        /// <summary>보상형 광고가 로드되어 바로 보여 줄 수 있다. 아니면 버튼에 "광고 준비 중".</summary>
        bool IsRewardedReady { get; }

        /// <summary>
        /// 보상형 광고. onRewarded 는 SDK 의 보상 지급 콜백에서만 부르고(중간에 닫으면 안 부름),
        /// onClosed 는 보상과 관계없이 광고가 끝나면 부른다.
        /// </summary>
        void ShowRewarded(string label, Action onRewarded, Action onClosed);

        /// <summary>전면 광고. 보여 주지 못해도 onClosed 는 꼭 부른다.</summary>
        void ShowInterstitial(Action onClosed);

        void ShowBanner();
        void HideBanner();
    }
}
