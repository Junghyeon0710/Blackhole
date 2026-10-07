using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Blackhole
{
    /// <summary>HUD, 도구 버튼, 행성 도감, 알림, 시작 화면, 결과 창. 흐린 글자·버튼·도감 색은 지금 우주를 따른다.</summary>
    public sealed class UIManager : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text bestText;
        [SerializeField] PlanetIcon nextIcon;
        [Tooltip("점수 아래 \"두 번째 우주\" 표시. 첫 번째 우주에서는 숨긴다")]
        [SerializeField] GameObject universeRow;
        [SerializeField] Image universeDot;
        [SerializeField] TMP_Text universeLabel;

        [Header("우주 색")]
        [Tooltip("흐린 글자와 테두리: 우주 테두리 색을 따른다 (투명도는 그대로)")]
        [SerializeField] Graphic[] lineTinted;
        [Tooltip("버튼 바탕: 우주 가운데 색")]
        [SerializeField] Graphic[] panelTinted;
        [Tooltip("도감 바탕: 우주 가장자리 색")]
        [SerializeField] Graphic[] deepTinted;

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
        float[] lineAlpha, panelAlpha, deepAlpha;
        float metaLines2 = -1, metaLineStep;

        public FloatingTextLayer FloatingTexts => floatingTexts;

        public void Bind(GameManager g)
        {
            game = g;
            collection.Bind(g.Data);
            floatingTexts.Bind(g);
            lineAlpha = Alphas(lineTinted);
            panelAlpha = Alphas(panelTinted);
            deepAlpha = Alphas(deepTinted);

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

        static float[] Alphas(Graphic[] graphics)
        {
            var a = new float[graphics.Length];
            for (int i = 0; i < graphics.Length; i++) a[i] = graphics[i].color.a;
            return a;
        }

        static void Tint(Graphic[] graphics, float[] alphas, Color color)
        {
            if (alphas == null) return;
            for (int i = 0; i < graphics.Length; i++) graphics[i].color = new Color(color.r, color.g, color.b, alphas[i]);
        }

        /// <summary>흐린 글자·테두리, 버튼 바탕, 도감 바탕을 우주 색으로 (원래 투명도는 그대로).</summary>
        public void SetPalette(UniversePalette palette)
        {
            Tint(lineTinted, lineAlpha, palette.Line);
            Tint(panelTinted, panelAlpha, palette.Panel);
            Tint(deepTinted, deepAlpha, palette.Deep);
        }

        /// <summary>점수 아래 "두 번째 우주". 첫 번째 우주(0)에서는 숨긴다.</summary>
        public void SetUniverse(int universe, Color accent)
        {
            universeRow.SetActive(universe > 0);
            if (universe <= 0) return;
            universeLabel.text = $"{Korean.Ordinal(universe + 1)} 우주";
            universeDot.color = accent;
        }

        // 프로토타입 .meta b: 값만 밝은 글자색
        static string Bright(string value) => "<color=#FFF7EC>" + value + "</color>";

        public void ShowStart(int best, int blackHoles, int farthestUniverse)
        {
            string text = $"최고 점수 {Bright(Korean.Number(best))}\n지금까지 만든 블랙홀 {Bright(blackHoles.ToString())}개";
            if (farthestUniverse > 0) text += $"\n가장 멀리 간 우주 {Bright(Korean.Ordinal(farthestUniverse + 1))}";
            startMeta.text = text;
            FitMetaLines(farthestUniverse > 0 ? 3 : 2);
            startScreen.Show();
            RefreshNext();
        }

        // 시작 화면 아래 글은 기본 두 줄 높이다. 세 줄이면 한 줄만큼 늘린다 (카드는 내용에 맞춰 커진다)
        void FitMetaLines(int lines)
        {
            if (!startMeta.TryGetComponent<LayoutElement>(out var le)) return;
            if (metaLines2 < 0)
            {
                metaLines2 = le.preferredHeight;
                metaLineStep = startMeta.GetPreferredValues("가\n가\n가").y - startMeta.GetPreferredValues("가\n가").y;
            }
            le.preferredHeight = le.minHeight = metaLines2 + (lines - 2) * metaLineStep;
        }

        public void HideStart() => startScreen.Hide();

        public void ShowResult(int score, bool isNewRecord, int topTier, int best, bool canRevive, int universe)
        {
            finalScore.text = Korean.Number(score);
            newRecord.SetActive(isNewRecord);
            bool small = topTier == 8 || topTier == 10; // 고리·빛무리가 있는 토성과 태양은 작게
            topIcon.SetPlanet(game.Data, topTier, small ? 17 : 23, FaceMood.Happy);
            string top = $"이번 판 최고 행성 {Bright(game.Data.NameOf(topTier))}";
            if (universe > 0) top += $", {Bright(Korean.Ordinal(universe + 1) + " 우주")}까지"; // Jua 에는 가운뎃점(·)이 없다
            overMeta.text = $"{top}\n최고 점수 {Bright(Korean.Number(best))}";
            reviveRow.SetActive(canRevive); // 판당 1회. 숨기면 아래 간격도 같이 사라진다
            overScreen.Show();
        }

        public void HideResult() => overScreen.Hide();
    }
}
