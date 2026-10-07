using System;
using System.Collections;
using UnityEngine;

namespace Blackhole
{
    public enum GameState
    {
        Menu,
        Playing,
        GameOver,
    }

    /// <summary>
    /// 상태 전환(메뉴 → 플레이 → 게임 오버)과 판 시작·종료. 규칙은 <see cref="GameSession"/>,
    /// 그리기는 View, 화면은 UIManager 가 맡고 여기서는 이벤트를 이어 준다.
    /// 블랙홀 이벤트는 Session.BlackHole.Active, 광고 중은 <see cref="Paused"/> 로 나타낸다.
    /// 블랙홀이 판을 다 삼키면 다음 우주로 넘어간다 (<see cref="Universe"/>, 판마다 첫 번째 우주부터).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] PlanetData planetData;
        [SerializeField] PlanetRenderer planetRenderer;
        [SerializeField] Launcher launcher;
        [SerializeField] ArenaView arena;
        [SerializeField] BlackHoleView blackHoleView;
        [SerializeField] UniverseView universeView;
        [SerializeField] EffectsManager effects;
        [SerializeField] UIManager ui;
        [SerializeField] AudioManager sound;
        [SerializeField] MockAdsService mockAds;

        static readonly Color Ink = new Color32(0xff, 0xf7, 0xec, 255);
        static readonly Color Butter = new Color32(0xff, 0xd8, 0x5e, 255);

        readonly System.Random random = new System.Random();
        int retries;
        Coroutine resultRoutine;
        bool blackHoleWasDone;

        public GameSession Session { get; private set; }
        public PlanetData Data => planetData;
        public GameState State { get; private set; } = GameState.Menu;
        /// <summary>광고가 재생되는 동안 물리와 타이머를 멈춘다.</summary>
        public bool Paused { get; private set; }
        public IAdsService Ads { get; private set; }
        public int Discovered { get; private set; }
        /// <summary>이번 판에서 지금 있는 우주 (0 = 첫 번째 우주). 블랙홀이 판을 다 삼킬 때마다 하나씩 오른다.</summary>
        public int Universe { get; private set; }
        public bool SoundOn => sound.Enabled;
        public bool AcceptsAimInput => State == GameState.Playing && !Paused;

        void Awake()
        {
            Application.targetFrameRate = 60;
            Time.fixedDeltaTime = (float)Tuning.TickDt;
            Time.maximumDeltaTime = 0.05f; // 프레임이 밀려도 한 번에 몇 틱만 따라잡아 시간이 튀지 않는다

            Session = new GameSession(planetData.ToTierTable(), random.NextDouble);
            Session.Score.Best = SaveData.Best;
            Discovered = SaveData.Discovered;
            Haptics.Enabled = SaveData.Vibration;
            Ads = CreateAds();

            Session.Launched += OnLaunched;
            Session.Merged += OnMerged;
            Session.BlackHoleStarted += OnBlackHoleStarted;
            Session.Absorbed += OnBodyRemovedWithBurst;
            Session.Cleared += OnBodyRemovedWithBurst;
            Session.Ended += OnEnded;
            Session.Score.Changed += score => ui.SetScore(score, Session.Score.Best);
            Session.Spawner.Changed += () => ui.RefreshNext();
            universeView.PaletteChanged += ui.SetPalette;
            universeView.Arrived += OnUniverseArrived;

            planetRenderer.Bind(Session, planetData);
            launcher.Bind(this);
            arena.Bind(Session);
            blackHoleView.Bind(Session);
            ui.Bind(this);
        }

        IAdsService CreateAds()
        {
#if BLACKHOLE_ADMOB && !UNITY_EDITOR
            mockAds.HideBanner();
            return new AdMobAdsService();
#else
            return mockAds;
#endif
        }

        void Start()
        {
            ui.SetScore(0, Session.Score.Best);
            ui.RefreshCollection(Discovered);
            ui.SetUniverse(0, default);
            ui.ShowStart(Session.Score.Best, SaveData.BlackHoles, SaveData.FarthestUniverse);
            Ads.ShowBanner();
        }

        void FixedUpdate()
        {
            if (Paused) return;
            float dt = (float)Tuning.TickDt;
            Session.Tick(Tuning.TickDt);
            CheckBlackHoleFinished();
            effects.Tick(dt);
            ui.FloatingTexts.Tick(dt);
        }

        // 블랙홀이 판을 다 삼킨 순간(사라지기 시작할 때) 그 자리에서 다음 우주가 퍼져 나온다
        void CheckBlackHoleFinished()
        {
            var bh = Session.BlackHole;
            bool done = bh.Active && bh.Done;
            if (done && !blackHoleWasDone)
            {
                Universe++;
                if (Universe > SaveData.FarthestUniverse) SaveData.FarthestUniverse = Universe;
                universeView.Reveal(Universe, ViewMetrics.WorldToUnity(bh.X, bh.Y));
            }
            blackHoleWasDone = done;
        }

        // ───────────────────────── 판 흐름 ─────────────────────────

        public void StartGame()
        {
            sound.Click();
            ui.HideStart();
            ResetGame();
            ui.Toast("같은 행성끼리 합체시켜 보세요!");
        }

        void CancelPendingResult()
        {
            if (resultRoutine == null) return;
            StopCoroutine(resultRoutine);
            resultRoutine = null;
        }

        void ResetGame()
        {
            CancelPendingResult();
            ui.HideResult();
            State = GameState.Playing;
            Session.Reset();
            effects.Clear();
            ui.FloatingTexts.Clear();
            ui.SetScore(0, Session.Score.Best);
            ui.RefreshNext();
            Universe = 0;
            blackHoleWasDone = false;
            universeView.Show(0);
            ui.SetUniverse(0, default);
        }

        public void Launch()
        {
            if (!AcceptsAimInput) return;
            Session.Launch();
        }

        public void Retry()
        {
            sound.Click();
            retries++;
            CancelPendingResult(); // 전면 광고가 도는 동안 지난 판 결과 창이 뜨지 않게
            ui.HideResult();
            // 3판마다 한 번은 시작 전에 전면 광고
            if (retries % Tuning.InterstitialEvery == 0) ShowInterstitial(ResetGame);
            else ResetGame();
        }

        public void Revive()
        {
            sound.Click();
            if (Session.ReviveUsed) return;
            ShowRewarded("보상: 바깥쪽 행성 정리 후 이어하기", () =>
            {
                Session.Revive();
                ui.HideResult();
                State = GameState.Playing;
                ui.Toast("바깥쪽 행성을 치웠어요");
            });
        }

        public void Share()
        {
            string reached = Universe > 0
                ? $"{Korean.Ordinal(Universe + 1)} 우주까지 갔어"
                : $"{planetData.NameOf(Session.TopTier)}까지 만들었어";
            string text = $"블랙홀 만들기에서 {Korean.Number(Session.Score.Score)}점! {reached}. 나보다 잘할 수 있어? {NativeShare.StoreUrl}";
            if (NativeShare.Share(text, "점수 자랑하기")) return;
            GUIUtility.systemCopyBuffer = text;
            ui.Toast("자랑 문구를 복사했어요");
        }

        public void SwapPlanet()
        {
            if (Session.SwapUsed || !AcceptsAimInput) return;
            ShowRewarded("보상: 지금 행성을 다른 행성으로 바꾸기", () =>
            {
                int tier = Session.SwapCurrent();
                ui.Toast($"{Korean.WithRo(planetData.NameOf(tier))} 바꿨어요");
            });
        }

        public void CleanSmallPlanets()
        {
            if (Session.CleanUsed || !AcceptsAimInput) return;
            if (!Session.HasSmallBodies()) { ui.Toast("치울 운석과 달이 없어요"); return; }
            ShowRewarded("보상: 운석과 달을 모두 치우기", () =>
            {
                Session.CleanSmallBodies();
                ui.Toast("운석과 달을 치웠어요");
            });
        }

        public void ToggleSound()
        {
            sound.SetEnabled(!sound.Enabled);
            SaveData.Sound = sound.Enabled;
        }

        public void ToggleVibration()
        {
            Haptics.Enabled = !Haptics.Enabled;
            SaveData.Vibration = Haptics.Enabled;
            Haptics.Vibrate(20);
        }

        // ───────────────────────── 광고 ─────────────────────────

        void ShowRewarded(string label, Action onReward)
        {
            if (Paused || !Ads.IsRewardedReady) return;
            Paused = true;
            Ads.ShowRewarded(label, onReward, () => Paused = false);
        }

        void ShowInterstitial(Action then)
        {
            Paused = true;
            Ads.ShowInterstitial(() =>
            {
                Paused = false;
                then();
            });
        }

        // ───────────────────────── 규칙 이벤트 → 연출 ─────────────────────────

        void OnLaunched(Body body)
        {
            sound.Drop();
            Haptics.Vibrate(6);
        }

        void OnMerged(MergeEvent e)
        {
            var tier = planetData.tiers[e.Tier];
            double r = tier.radius;
            effects.Burst(e.X, e.Y, tier.light, 10 + e.Tier * 2, r);
            effects.Wave(e.X, e.Y, (float)(r * 0.8), (float)(r * 1.9), tier.light);
            ui.FloatingTexts.Add("+" + e.Points, e.X, e.Y + r * 0.3, 0.9f, 15 + e.Tier * 1.2f, Ink);
            if (e.Combo >= 2)
                ui.FloatingTexts.Add(e.Combo + "콤보!", e.X, e.Y - r * 0.5, 1f, 16 + Mathf.Min(e.Combo, 8) * 2, Butter, true);
            if (e.Tier >= 6) effects.AddShake(3 + e.Tier * 0.6f);
            sound.Merge(e.Tier, e.Combo);
            Haptics.Vibrate(e.Tier >= 7 ? 35 : 12);

            if (e.Tier > Discovered)
            {
                Discovered = e.Tier;
                SaveData.Discovered = Discovered;
                ui.RefreshCollection(Discovered);
                ui.Toast($"새 행성 발견! {tier.name}", e.Tier >= 8);
                sound.Discover();
            }
        }

        void OnBlackHoleStarted(double x, double y)
        {
            effects.SetShake(20);
            sound.BlackHole();
            Haptics.Vibrate(40, 60, 120);
            ui.Toast("블랙홀 탄생!!", true);
            SaveData.BlackHoles += 1;
            if (Discovered < planetData.MaxTier + 1)
            {
                Discovered = planetData.MaxTier + 1;
                SaveData.Discovered = Discovered;
                ui.RefreshCollection(Discovered);
            }
            ui.FloatingTexts.Add("+" + Tuning.BlackHoleBonus, x, y, 1.4f, 34, Butter);
            universeView.Prepare(Universe + 1); // 다 삼키는 동안 다음 우주 배경을 미리 굽는다
        }

        void OnUniverseArrived(int universe)
        {
            ui.SetUniverse(universe, universeView.Data.Get(universe).nebula1);
            if (universe == 0) return;
            ui.Toast($"{Korean.Ordinal(universe + 1)} 우주 도착!");
            sound.Discover();
        }

        void OnBodyRemovedWithBurst(Body b)
        {
            effects.Burst(b.X, b.Y, planetData.tiers[b.Tier].light, 8, b.R);
        }

        void OnEnded()
        {
            State = GameState.GameOver;
            sound.GameOver();
            Haptics.Vibrate(60, 40, 60);
            int score = Session.Score.Score;
            bool isNew = Session.Score.CommitBest();
            if (isNew) SaveData.Best = Session.Score.Best;
            ui.SetScore(score, Session.Score.Best);
            resultRoutine = StartCoroutine(ShowResultLater(score, isNew && score > 0));
        }

        IEnumerator ShowResultLater(int score, bool isNew)
        {
            yield return new WaitForSeconds((float)Tuning.GameOverDelay);
            resultRoutine = null;
            ui.ShowResult(score, isNew, Session.TopTier, Session.Score.Best, !Session.ReviveUsed, Universe);
        }
    }
}
