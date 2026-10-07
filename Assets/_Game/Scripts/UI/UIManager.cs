using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Blackhole
{
    /// <summary>HUD, 도구 버튼, 행성 도감, 알림, 시작 화면, 결과 창.</summary>
    public sealed class UIManager : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text bestText;
        [SerializeField] PlanetIcon nextIcon;

        [Header("도구")]
        [SerializeField] ToolButton swapTool;
        [SerializeField] ToolButton cleanTool;
        [SerializeField] Button soundButton;
        [SerializeField] Image soundIcon;
        [SerializeField] Sprite soundOnSprite;
        [SerializeField] Sprite soundOffSprite;
        [SerializeField] Button vibrationButton;
        [SerializeField] Image vibrationIcon;
        [SerializeField] Sprite vibrationOnSprite;
        [SerializeField] Sprite vibrationOffSprite;
        [SerializeField] CollectionStrip collection;

        [Header("알림")]
        [SerializeField] Toast toast;
        [SerializeField] FloatingTextLayer floatingTexts;

        [Header("시작 화면")]
        [SerializeField] Overlay startScreen;
        [SerializeField] Button startButton;
        [SerializeField] TMP_Text startMeta;
        [SerializeField] PlanetIcon[] logo = new PlanetIcon[3];

        [Header("결과 창")]
        [SerializeField] Overlay overScreen;
        [SerializeField] TMP_Text finalScore;
        [SerializeField] GameObject newRecord;
        [SerializeField] PlanetIcon topIcon;
        [SerializeField] TMP_Text overMeta;
        [SerializeField] GameObject reviveRow;
        [SerializeField] Button reviveButton;
        [SerializeField] Button retryButton;
        [SerializeField] Button shareButton;

        GameManager game;

        public FloatingTextLayer FloatingTexts => floatingTexts;

        public void Bind(GameManager g)
        {
            game = g;
            collection.Bind(g.Data);
            floatingTexts.Bind(g);

            startButton.onClick.AddListener(() => Click(g.StartGame));
            retryButton.onClick.AddListener(() => Click(g.Retry));
            reviveButton.onClick.AddListener(() => Click(g.Revive));
            shareButton.onClick.AddListener(() => Click(g.Share));
            swapTool.Button.onClick.AddListener(() => Click(g.SwapPlanet));
            cleanTool.Button.onClick.AddListener(() => Click(g.CleanSmallPlanets));
            soundButton.onClick.AddListener(() => Click(g.ToggleSound));
            vibrationButton.onClick.AddListener(() => Click(g.ToggleVibration));

            // 시작 화면 로고: 화성, 웃는 태양, 지구
            logo[0].SetPlanet(g.Data, 3, 17, FaceMood.Normal);
            logo[1].SetPlanet(g.Data, 10, 31, FaceMood.Happy);
            logo[2].SetPlanet(g.Data, 5, 19, FaceMood.Normal);
        }

        // 클릭한 버튼이 선택된 채로 남으면 스페이스·엔터(발사 키)가 그 버튼을 또 누른다
        static void Click(Action action)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            action();
        }

        void Update()
        {
            if (game == null) return;
            bool playing = game.State == GameState.Playing && !game.Paused;
            bool adReady = game.Ads.IsRewardedReady;
            swapTool.Refresh(playing && !game.Session.SwapUsed, adReady);
            cleanTool.Refresh(playing && !game.Session.CleanUsed, adReady);
            soundIcon.sprite = game.SoundOn ? soundOnSprite : soundOffSprite;
            vibrationIcon.sprite = Haptics.Enabled ? vibrationOnSprite : vibrationOffSprite;
        }

        public void SetScore(int score, int best)
        {
            scoreText.text = Korean.Number(score);
            bestText.text = Korean.Number(Mathf.Max(best, score));
        }

        public void RefreshNext()
        {
            bool show = game.State != GameState.Menu;
            nextIcon.gameObject.SetActive(show);
            if (show)
            {
                int next = game.Session.Spawner.Next;
                nextIcon.SetPlanet(game.Data, next, 9 + next * 3, FaceMood.Normal);
            }
        }

        public void RefreshCollection(int discovered) => collection.Refresh(discovered);

        public void Toast(string message, bool big = false) => toast.Show(message, big);

        // 프로토타입 .meta b: 값만 밝은 글자색
        static string Bright(string value) => "<color=#FFF7EC>" + value + "</color>";

        public void ShowStart(int best, int blackHoles)
        {
            startMeta.text = $"최고 점수 {Bright(Korean.Number(best))}\n지금까지 만든 블랙홀 {Bright(blackHoles.ToString())}개";
            startScreen.Show();
            RefreshNext();
        }

        public void HideStart() => startScreen.Hide();

        public void ShowResult(int score, bool isNewRecord, int topTier, int best, bool canRevive)
        {
            finalScore.text = Korean.Number(score);
            newRecord.SetActive(isNewRecord);
            bool small = topTier == 8 || topTier == 10; // 고리·빛무리가 있는 토성과 태양은 작게
            topIcon.SetPlanet(game.Data, topTier, small ? 17 : 23, FaceMood.Happy);
            overMeta.text = $"이번 판 최고 행성 {Bright(game.Data.NameOf(topTier))}\n최고 점수 {Bright(Korean.Number(best))}";
            reviveRow.SetActive(canRevive); // 판당 1회. 숨기면 아래 간격도 같이 사라진다
            overScreen.Show();
        }

        public void HideResult() => overScreen.Hide();
    }
}
