using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blackhole
{
    /// <summary>인게임 광고 버튼 (행성 교체, 운석 청소). 못 누를 때는 흐리게, 광고가 아직 없으면 "광고 준비 중".</summary>
    public sealed class ToolButton : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text label;
        [SerializeField] GameObject icon;
        [SerializeField] GameObject adBadge;
        [SerializeField] string title = "";

        int state = -1;

        public Button Button => button;

        /// <param name="available">이번 판에 아직 쓸 수 있고 지금 누를 수 있는 상황</param>
        /// <param name="adReady">보상형 광고가 로드됨</param>
        public void Refresh(bool available, bool adReady)
        {
            bool loading = available && !adReady;
            int next = available && adReady ? 0 : loading ? 1 : 2;
            if (next == state) return;
            state = next;
            button.interactable = next == 0;
            group.alpha = next == 0 ? 1 : 0.4f;
            label.text = loading ? "광고 준비 중" : title;
            icon.SetActive(!loading);
            adBadge.SetActive(!loading);
        }
    }
}
