using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static Blackhole.EditorTools.UIFactory;

namespace Blackhole.EditorTools
{
    /// <summary>
    /// Main 씬 조립 (기획서 8장 씬 구성): Camera, Background, World(Arena, Launcher, Planets, Effects, BlackHole),
    /// EventSystem, UI(HUD, Stage, 도구·도감·배너, 시작·결과·광고 창), Managers.
    /// UI 크기는 프로토타입 CSS 값을 그대로 쓴다.
    /// </summary>
    static class SceneBuilder
    {
        static readonly Color SpaceDeep = Hex("#0e0a2e");
        static readonly Color Ink = Hex("#fff7ec");
        static readonly Color InkSoft = Hex("#c9c0f2");
        static readonly Color Butter = Hex("#ffd85e");
        static readonly Color ButterDark = Hex("#e0a526");
        static readonly Color ButterInk = Hex("#3a2300");
        static readonly Color Pink = Hex("#ff8fb8");
        static readonly Color Mint = Hex("#7ef0d0");
        static readonly Color MintDark = Hex("#3fb596");
        static readonly Color MintInk = Hex("#06372b");
        static readonly Color Danger = Hex("#ff5a76");
        static readonly Color Panel = Rgba(43, 31, 107, 0.72f);
        static readonly Color PanelLine = Rgba(201, 192, 242, 0.22f);

        const float HudHeight = 89;
        const float ToolsHeight = 49;
        const float CollectionHeight = 86;
        const float BannerHeight = 50;
        const float BottomHeight = ToolsHeight + CollectionHeight + BannerHeight;

        static BlackholeSetup.Assets s_Assets;

        public static void Build(BlackholeSetup.Assets assets)
        {
            s_Assets = assets;
            UIFactory.Font = assets.Font;
            UIFactory.OutlineMaterial = assets.OutlineMaterial;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── 카메라 ──
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.position = new Vector3(0, 0, -10);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SpaceDeep;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 50;
            camGo.AddComponent<AudioListener>();
            var rig = camGo.AddComponent<CameraRig>();

            // ── 관리자 ──
            var managers = new GameObject("Managers");
            var game = managers.AddComponent<GameManager>();
            var audio = Child("Audio", managers.transform).AddComponent<AudioManager>();
            var ads = Child("Ads (Mock)", managers.transform).AddComponent<MockAdsService>();

            // ── 배경 (흔들리지 않음) ──
            var bgGo = new GameObject("Background");
            var background = bgGo.AddComponent<Background>();
            var gradient = SpriteChild("Gradient", bgGo.transform, null, -100);
            var stars = ShapeChild("Stars", bgGo.transform, -90);

            // ── 월드 (화면 흔들림을 받는 루트) ──
            var world = new GameObject("World").transform;

            var arenaGo = Child("Arena", world);
            var arena = arenaGo.AddComponent<ArenaView>();
            var arenaFill = SpriteChild("Fill", arenaGo.transform, Art("FX/arena_fill.png"), -80);
            var arenaLines = ShapeChild("Lines", arenaGo.transform, -70);
            var coreGlow = SpriteChild("CoreGlow", arenaGo.transform, Art("FX/glow.png"), -60);

            var launcherGo = Child("Launcher", world);
            var launcher = launcherGo.AddComponent<Launcher>();
            var aim = ShapeChild("Aim", launcherGo.transform, -50);
            var waitingGo = (GameObject)PrefabUtility.InstantiatePrefab(assets.PlanetPrefab, launcherGo.transform);
            waitingGo.name = "Waiting";
            var waiting = waitingGo.GetComponent<PlanetView>();
            waiting.SetSortingOrder(-40);

            var planetsGo = Child("Planets", world);
            var planets = planetsGo.AddComponent<PlanetRenderer>();
            var dangerRings = ShapeChild("DangerRings", planetsGo.transform, 900);

            var fxGo = Child("Effects", world);
            var effects = fxGo.AddComponent<EffectsManager>();
            var fxShapes = ShapeChild("Shapes", fxGo.transform, 1000);

            var bhGo = Child("BlackHole", world);
            var bhView = bhGo.AddComponent<BlackHoleView>();
            var bhVisual = Child("Visual", bhGo.transform);
            var bhGlow = SpriteChild("Glow", bhVisual.transform, assets.PlanetData.blackHoleGlow, 1100);
            var bhDisk = SpriteChild("Disk", bhVisual.transform, assets.PlanetData.blackHoleDisk, 1101);
            var bhCore = SpriteChild("Core", bhVisual.transform, assets.PlanetData.blackHoleCore, 1102);
            bhVisual.SetActive(false);

            // ── 입력 ──
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            // ── UI ──
            var ui = BuildUI(rig, world, ads);

            // ── 연결 ──
            Wire.Set(rig, "stage", ui.Stage);
            Wire.Set(rig, "canvas", ui.Canvas);

            Wire.Set(background, "rig", rig);
            Wire.Set(background, "gradient", gradient);
            Wire.Set(background, "stars", stars);

            Wire.Set(arena, "fill", arenaFill);
            Wire.Set(arena, "coreGlow", coreGlow);
            Wire.Set(arena, "lines", arenaLines);

            Wire.Set(launcher, "rig", rig);
            Wire.Set(launcher, "waiting", waiting);
            Wire.Set(launcher, "aim", aim);

            Wire.Set(planets, "prefab", assets.PlanetPrefab.GetComponent<PlanetView>());
            Wire.Set(planets, "dangerRings", dangerRings);

            Wire.Set(effects, "batch", fxShapes);
            Wire.Set(effects, "shakeRoot", world);

            Wire.Set(bhView, "glow", bhGlow);
            Wire.Set(bhView, "disk", bhDisk);
            Wire.Set(bhView, "core", bhCore);
            Wire.Set(bhView, "visual", bhVisual);

            Wire.Set(game, "planetData", assets.PlanetData);
            Wire.Set(game, "planetRenderer", planets);
            Wire.Set(game, "launcher", launcher);
            Wire.Set(game, "arena", arena);
            Wire.Set(game, "blackHoleView", bhView);
            Wire.Set(game, "effects", effects);
            Wire.Set(game, "ui", ui.Manager);
            Wire.Set(game, "sound", audio);
            Wire.Set(game, "mockAds", ads);

            // UI 레이아웃을 한 번 계산해 둔 상태로 저장한다. 안 그러면 씬을 열자마자 레이아웃이 바뀌어 "수정됨"이 된다
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)ui.Canvas.transform);
            foreach (var group in ui.Canvas.GetComponentsInChildren<LayoutGroup>(true))
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform);
            Canvas.ForceUpdateCanvases();
            EditorSceneManager.SaveScene(scene, BlackholeSetup.ScenePath);
            // 막 만든 씬은 UI 가 다시 계산되며 "수정됨" 표시가 붙는다. 저장한 파일을 다시 열어 깨끗한 상태로 둔다
            EditorSceneManager.OpenScene(BlackholeSetup.ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BlackholeSetup.ScenePath, true) };
            Debug.Log("블랙홀 만들기: Main 씬을 만들었습니다 → " + BlackholeSetup.ScenePath);
        }

        // ───────────────────────── 월드 도우미 ─────────────────────────

        static GameObject Child(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        static SpriteRenderer SpriteChild(string name, Transform parent, Sprite sprite, int order)
        {
            var sr = Child(name, parent).AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = s_Assets.SpriteMaterial;
            sr.sortingOrder = order;
            return sr;
        }

        static ShapeBatch ShapeChild(string name, Transform parent, int order)
        {
            var go = Child(name, parent);
            go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = s_Assets.ShapesMaterial;
            mr.sortingOrder = order;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            var batch = go.AddComponent<ShapeBatch>();
            Wire.Set(batch, "sortingOrder", order);
            return batch;
        }

        // ───────────────────────── UI ─────────────────────────

        sealed class UIRefs
        {
            public Canvas Canvas;
            public RectTransform Stage;
            public UIManager Manager;
        }

        static UIRefs BuildUI(CameraRig rig, Transform world, MockAdsService ads)
        {
            var canvasGo = new GameObject("UI", typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(400, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvasGo.AddComponent<GraphicRaycaster>();
            var manager = canvasGo.AddComponent<UIManager>();
            var root = (RectTransform)canvasGo.transform;

            var safe = Stretch(Node("SafeArea", root));
            safe.gameObject.AddComponent<SafeArea>();

            // ── HUD: 점수(왼쪽), 다음 행성(가운데), 최고 점수(오른쪽) ──
            var hud = Band(Node("HUD", safe), true, 0, HudHeight);
            var scoreBox = Place(Node("Score", hud), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(14, -47.5f), new Vector2(140, 46));
            Band(Label("Label", scoreBox, "점수", 13, InkSoft, TextAlignmentOptions.TopLeft).rectTransform, true, 0, 13);
            var scoreValue = Label("Value", scoreBox, "0", 30, Ink, TextAlignmentOptions.TopLeft);
            Band(scoreValue.rectTransform, true, 16, 30);

            var bestBox = Place(Node("Best", hud), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-14, -47.5f), new Vector2(140, 38));
            Band(Label("Label", bestBox, "최고", 13, InkSoft, TextAlignmentOptions.TopRight).rectTransform, true, 0, 13);
            var bestValue = Label("Value", bestBox, "0", 22, Butter, TextAlignmentOptions.TopRight);
            Band(bestValue.rectTransform, true, 16, 22);

            var pod = Place(Node("Next", hud), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(58, 75));
            var ring = Img("Ring", pod, Art("UI/ui_pod_ring.png"), Color.white);
            Place(ring.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(58, 58));
            var nextIcon = PlanetIconUI("Planet", ring.transform, Vector2.zero);
            Band(Label("Caption", pod, "다음 행성", 12, InkSoft).rectTransform, true, 60, 15);

            // ── 아래: 도구, 도감, 배너 ──
            var bottom = Band(Node("Bottom", safe), false, 0, BottomHeight);

            var banner = Band(Node("Banner", bottom), false, 0, BannerHeight);
            var stripes = Img("Stripes", banner, Art("UI/ui_banner_tile.png"), Color.white);
            stripes.type = UnityEngine.UI.Image.Type.Tiled;
            Stretch(stripes.rectTransform);
            var dash = Img("Dash", banner, Art("UI/ui_dash_h.png"), PanelLine);
            dash.type = UnityEngine.UI.Image.Type.Tiled;
            Band(dash.rectTransform, true, 0, 1.5f);
            var bannerLabel = Label("Label", banner, "배너 광고 자리 (320x50)\n기기에서는 광고 SDK 배너가 여기에 붙어요", 12, InkSoft);
            bannerLabel.lineSpacing = -8;
            Stretch(bannerLabel.rectTransform);

            Band(Label("Caption", bottom, "행성 도감", 12, InkSoft).rectTransform, false, BannerHeight + 6 + 61 + 4, 15);
            var evo = Band(Node("Collection", bottom), false, BannerHeight + 6, 61, 12, 12);
            Stretch(RoundRect("Fill", evo, 16, Rgba(14, 10, 46, 0.55f)).rectTransform);
            Stretch(Img("Border", evo, Art("UI/ui_outline_r16.png"), PanelLine).rectTransform);
            var strip = evo.gameObject.AddComponent<CollectionStrip>();
            var cells = new Object[12];
            var icons = new Object[12];
            var discs = new Object[12];
            var marks = new Object[12];
            for (int i = 0; i < 12; i++)
            {
                var cell = Place(Node("Cell " + i, evo), new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(20, 20));
                var unknown = Img("Unknown", cell, Art("FX/disc.png"), Rgba(201, 192, 242, 0.16f));
                Place(unknown.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16, 16));
                var mark = Label("Mark", cell, "?", 12, Rgba(201, 192, 242, 0.55f));
                Place(mark.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
                cells[i] = cell;
                icons[i] = PlanetIconUI("Planet", cell, Vector2.zero);
                discs[i] = unknown;
                marks[i] = mark;
            }
            Wire.Set(strip, "box", evo);
            Wire.SetArray(strip, "cells", cells);
            Wire.SetArray(strip, "icons", icons);
            Wire.SetArray(strip, "unknownDiscs", discs);
            Wire.SetArray(strip, "unknownMarks", marks);
            Wire.Set(strip, "paddingBottom", 9.5f);

            var tools = Band(Node("Tools", bottom), false, BannerHeight + CollectionHeight, ToolsHeight);
            var row = tools.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(12, 12, 4, 6);
            row.spacing = 8;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;
            var swapTool = ToolButton(tools, "Swap", Art("UI/Icons/icon_swap.png"), "행성 교체");
            var cleanTool = ToolButton(tools, "Clean", Art("UI/Icons/icon_clean.png"), "운석 청소");
            var soundButton = IconButton(tools, "Sound", Art("UI/Icons/icon_sound_on.png"), out var soundIcon);
            var vibButton = IconButton(tools, "Vibration", Art("UI/Icons/icon_vib_on.png"), out var vibIcon);

            // ── 경기장 자리: 떠오르는 글자, 알림 ──
            var stage = Stretch(Node("Stage", safe), 0, 0, HudHeight, BottomHeight);
            var floatingRoot = Stretch(Node("FloatingTexts", stage));
            var floating = floatingRoot.gameObject.AddComponent<FloatingTextLayer>();
            var template = Label("Text", floatingRoot, "+0", 15, Ink);
            template.fontSharedMaterial = OutlineMaterial;
            Place(template.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 60));
            var dangerLabel = Label("Danger", floatingRoot, "위험! 2.4", 20, Danger);
            Place(dangerLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 30));
            Wire.Set(floating, "stage", stage);
            Wire.Set(floating, "rig", rig);
            Wire.Set(floating, "worldRoot", world);
            Wire.Set(floating, "template", template);
            Wire.Set(floating, "dangerLabel", dangerLabel);

            var toastRoot = Stretch(Node("Toast", stage));
            var toastGroup = toastRoot.gameObject.AddComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false;
            toastGroup.interactable = false;
            toastGroup.alpha = 0;
            var toastBox = Place(Node("Box", toastRoot), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -28), new Vector2(220, 37));
            var toastShadow = RoundRect("Shadow", toastBox, 18, ButterDark);
            Stretch(toastShadow.rectTransform, 0, 0, 4, -4);
            var toastFill = RoundRect("Fill", toastBox, 18, Butter);
            Stretch(toastFill.rectTransform);
            var toastLabel = Label("Label", toastBox, "", 17, ButterInk);
            Stretch(toastLabel.rectTransform);
            var toast = toastRoot.gameObject.AddComponent<Toast>();
            Wire.Set(toast, "box", toastBox);
            Wire.Set(toast, "group", toastGroup);
            Wire.Set(toast, "fill", toastFill);
            Wire.Set(toast, "shadow", toastShadow);
            Wire.Set(toast, "label", toastLabel);

            // ── 창: 시작, 결과, 광고 (나중에 만든 것이 위) ──
            var start = BuildStartScreen(root, out var startButton, out var startMeta, out var logo);
            var over = BuildOverScreen(root, out var finalScore, out var newRecord, out var topIcon, out var overMeta,
                out var reviveRow, out var reviveButton, out var retryButton, out var shareButton);
            over.gameObject.SetActive(false);
            BuildAdScreen(root, ads, banner.gameObject);

            Wire.Set(manager, "scoreText", scoreValue);
            Wire.Set(manager, "bestText", bestValue);
            Wire.Set(manager, "nextIcon", nextIcon);
            Wire.Set(manager, "swapTool", swapTool);
            Wire.Set(manager, "cleanTool", cleanTool);
            Wire.Set(manager, "soundButton", soundButton);
            Wire.Set(manager, "soundIcon", soundIcon);
            Wire.Set(manager, "soundOnSprite", Art("UI/Icons/icon_sound_on.png"));
            Wire.Set(manager, "soundOffSprite", Art("UI/Icons/icon_sound_off.png"));
            Wire.Set(manager, "vibrationButton", vibButton);
            Wire.Set(manager, "vibrationIcon", vibIcon);
            Wire.Set(manager, "vibrationOnSprite", Art("UI/Icons/icon_vib_on.png"));
            Wire.Set(manager, "vibrationOffSprite", Art("UI/Icons/icon_vib_off.png"));
            Wire.Set(manager, "collection", strip);
            Wire.Set(manager, "toast", toast);
            Wire.Set(manager, "floatingTexts", floating);
            Wire.Set(manager, "startScreen", start);
            Wire.Set(manager, "startButton", startButton);
            Wire.Set(manager, "startMeta", startMeta);
            Wire.SetArray(manager, "logo", logo);
            Wire.Set(manager, "overScreen", over);
            Wire.Set(manager, "finalScore", finalScore);
            Wire.Set(manager, "newRecord", newRecord);
            Wire.Set(manager, "topIcon", topIcon);
            Wire.Set(manager, "overMeta", overMeta);
            Wire.Set(manager, "reviveRow", reviveRow);
            Wire.Set(manager, "reviveButton", reviveButton);
            Wire.Set(manager, "retryButton", retryButton);
            Wire.Set(manager, "shareButton", shareButton);

            return new UIRefs { Canvas = canvas, Stage = stage, Manager = manager };
        }

        static PlanetIcon PlanetIconUI(string name, Transform parent, Vector2 position)
        {
            var rt = Place(Node(name, parent), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, Vector2.zero);
            var icon = rt.gameObject.AddComponent<PlanetIcon>();
            var back = Img("Back", rt, null, Color.white);
            var middle = Img("Middle", rt, null, Color.white);
            var front = Img("Front", rt, null, Color.white);
            foreach (var img in new[] { back, middle, front })
                Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10));
            Wire.Set(icon, "back", back);
            Wire.Set(icon, "middle", middle);
            Wire.Set(icon, "front", front);
            return icon;
        }

        /// <summary>인게임 광고 버튼: 패널 + 테두리, 아이콘 · 글자 · AD 표시.</summary>
        static ToolButton ToolButton(Transform parent, string name, Sprite icon, string title)
        {
            var root = Node(name, parent);
            Layout(root, -1, 0, 1);
            var bg = root.gameObject.AddComponent<Image>();
            bg.sprite = Round(14);
            bg.type = UnityEngine.UI.Image.Type.Sliced;
            bg.color = Panel;
            Stretch(Img("Border", root, Art("UI/ui_outline_r14.png"), PanelLine).rectTransform);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            var content = Stretch(Node("Content", root));
            var h = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.spacing = 5;
            h.childControlWidth = true;
            h.childControlHeight = false;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            var ic = Img("Icon", content, icon, Color.white);
            ic.rectTransform.sizeDelta = new Vector2(18, 18);
            Layout(ic, 18, 18);
            var label = Label("Label", content, title, 15, Ink);
            label.rectTransform.sizeDelta = new Vector2(60, 20);
            var badge = RoundRect("AD", content, 5, Mint);
            badge.rectTransform.sizeDelta = new Vector2(20, 14);
            var bh = badge.gameObject.AddComponent<HorizontalLayoutGroup>();
            bh.padding = new RectOffset(4, 4, 1, 1);
            bh.childControlWidth = true;
            bh.childControlHeight = true;
            bh.childForceExpandWidth = false; // 켜 두면 남는 폭을 AD 표시가 다 가져간다
            bh.childForceExpandHeight = false;
            bh.childAlignment = TextAnchor.MiddleCenter;
            Label("Text", badge.transform, "AD", 10, MintInk);

            var tool = root.gameObject.AddComponent<Blackhole.ToolButton>();
            Wire.Set(tool, "button", button);
            Wire.Set(tool, "group", group);
            Wire.Set(tool, "label", label);
            Wire.Set(tool, "icon", ic.gameObject);
            Wire.Set(tool, "adBadge", badge.gameObject);
            Wire.Set(tool, "title", title);
            return tool;
        }

        static Button IconButton(Transform parent, string name, Sprite sprite, out Image icon)
        {
            var root = Node(name, parent);
            Layout(root, -1, 44);
            var bg = root.gameObject.AddComponent<Image>();
            bg.sprite = Round(14);
            bg.type = UnityEngine.UI.Image.Type.Sliced;
            bg.color = Panel;
            Stretch(Img("Border", root, Art("UI/ui_outline_r14.png"), PanelLine).rectTransform);
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            icon = Img("Icon", root, sprite, Color.white);
            Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(21, 21));
            return button;
        }

        // ───────────────────────── 창 ─────────────────────────

        static RectTransform OverlayRoot(RectTransform root, string name)
        {
            var ov = Stretch(Node(name, root));
            var dim = ov.gameObject.AddComponent<Image>();
            dim.color = Rgba(10, 7, 34, 0.72f);
            dim.raycastTarget = true; // 뒤의 경기장·버튼을 막는다
            return ov;
        }

        /// <summary>프로토타입 .card: 340px, 세로 그라디언트, 2px 테두리, 아래로 10px 그림자. 내용은 세로로 쌓인다.</summary>
        static RectTransform Card(RectTransform overlay)
        {
            var fit = Place(Node("Fit", overlay), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340, 10));
            var card = Place(Node("Card", fit), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340, 10));
            var v = card.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(20, 20, 22, 20);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            card.gameObject.AddComponent<CanvasGroup>();
            card.gameObject.AddComponent<PopIn>();
            Wire.Set(fit.gameObject.AddComponent<ScaleToFit>(), "content", card);

            var shadow = RoundRect("Shadow", card, 28, Hex("#140f3a"));
            IgnoreLayout(shadow);
            Stretch(shadow.rectTransform, 0, 0, 10, -10);
            var fill = RoundRect("Fill", card, 28, Color.white);
            IgnoreLayout(fill);
            Stretch(fill.rectTransform);
            fill.gameObject.AddComponent<UIVerticalGradient>().SetColors(Hex("#3a2a8a"), Hex("#2b1f6b"));
            var border = Img("Border", card, Art("UI/ui_outline_r28.png"), Rgba(201, 192, 242, 0.3f));
            IgnoreLayout(border);
            Stretch(border.rectTransform);
            return card;
        }

        static Button BigButton(Transform parent, string name, string label, float fontSize, float height,
            Color face, Color shadow, Color ink, Sprite icon = null)
        {
            var button = PushButton(name, parent, face, shadow, 18, 5, out var faceRect);
            Layout(button, height);
            var content = Stretch(Node("Content", faceRect));
            var h = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.spacing = 6;
            h.childControlWidth = true;
            h.childControlHeight = false;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            if (icon != null)
            {
                var ic = Img("Icon", content, icon, Color.white);
                ic.rectTransform.sizeDelta = new Vector2(fontSize + 2, fontSize + 2);
                Layout(ic, fontSize + 2, fontSize + 2);
            }
            var text = Label("Label", content, label, fontSize, ink);
            text.rectTransform.sizeDelta = new Vector2(100, fontSize + 6);
            return button;
        }

        static Overlay BuildStartScreen(RectTransform root, out Button startButton, out TMP_Text meta, out Object[] logo)
        {
            var ov = OverlayRoot(root, "StartScreen");
            var overlay = ov.gameObject.AddComponent<Overlay>();
            var card = Card(ov);

            var logoBox = Node("Logo", card);
            Layout(logoBox, 110);
            logo = new Object[]
            {
                PlanetIconUI("Mars", logoBox, new Vector2(-65, -10)),
                PlanetIconUI("Sun", logoBox, new Vector2(0, 2.5f)),
                PlanetIconUI("Earth", logoBox, new Vector2(65, -12.5f)),
            };
            Spacer(card, 4);

            var titleBox = Node("Title", card);
            Layout(titleBox, 46);
            var titleShadow = Label("Shadow", titleBox, "블랙홀 만들기", 44, Hex("#8a4e00"));
            titleShadow.characterSpacing = -2.3f;
            Stretch(titleShadow.rectTransform, 0, 0, 4, -4);
            var title = Label("Text", titleBox, "블랙홀 만들기", 44, Butter);
            title.characterSpacing = -2.3f;
            Stretch(title.rectTransform);
            Spacer(card, 6);

            var sub = Label("Sub", card, "같은 행성끼리 부딪히면 합체!\n태양 두 개가 만나면 블랙홀이 태어나요", 16, InkSoft);
            sub.lineSpacing = 8;
            Layout(sub, 47);
            Spacer(card, 14);

            var how = Node("How", card);
            var hv = how.gameObject.AddComponent<VerticalLayoutGroup>();
            hv.spacing = 7;
            hv.childControlWidth = true;
            hv.childControlHeight = true;
            hv.childForceExpandWidth = true;
            hv.childForceExpandHeight = false;
            HowRow(how, Art("UI/Icons/icon_touch.png"), "화면을 눌러 발사 방향을 정해요", 21);
            HowRow(how, s_Assets.PlanetData.tiers[8].sprite, "손을 떼면 행성이 날아가요", 6.2f * 3.26f);
            HowRow(how, Art("UI/Icons/icon_warning.png"), "점선 밖으로 행성이 넘치면 끝나요", 21);
            Spacer(card, 16);

            startButton = BigButton(card, "StartButton", "시작하기", 21, 51, Butter, ButterDark, ButterInk);
            Spacer(card, 14);

            meta = Label("Meta", card, "최고 점수 0\n지금까지 만든 블랙홀 0개", 14, InkSoft);
            meta.lineSpacing = 18;
            Layout(meta, 45);
            return overlay;
        }

        static void HowRow(Transform parent, Sprite icon, string text, float iconSize)
        {
            var row = Node("Row", parent);
            Layout(row, 36);
            Stretch(RoundRect("Bg", row, 12, Rgba(14, 10, 46, 0.4f)).rectTransform);
            var ic = Img("Icon", row, icon, Color.white);
            ic.preserveAspect = true;
            Place(ic.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(20, 0), new Vector2(iconSize, iconSize));
            Stretch(Label("Text", row, text, 15, Ink, TextAlignmentOptions.MidlineLeft).rectTransform, 40, 10, 0, 0);
        }

        static Overlay BuildOverScreen(RectTransform root, out TMP_Text finalScore, out GameObject newRecord, out PlanetIcon topIcon,
            out TMP_Text meta, out GameObject reviveRow, out Button revive, out Button retry, out Button share)
        {
            var ov = OverlayRoot(root, "OverScreen");
            var overlay = ov.gameObject.AddComponent<Overlay>();
            var card = Card(ov);

            Layout(Label("Title", card, "행성이 넘쳤어요!", 30, Pink), 36);
            Spacer(card, 8);
            finalScore = Label("Score", card, "0", 64, Ink);
            Layout(finalScore, 64);
            Spacer(card, 6);

            var rec = Node("NewRecord", card);
            Layout(rec, 30);
            rec.gameObject.AddComponent<CanvasGroup>();
            var pop = rec.gameObject.AddComponent<PopIn>();
            Wire.Set(pop, "duration", 0.5f);
            Wire.Set(pop, "delay", 0.2f);
            var recRow = Band(Node("Row", rec), true, 0, 24);
            var rh = recRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            rh.childAlignment = TextAnchor.MiddleCenter;
            rh.spacing = 5;
            rh.childControlWidth = true;
            rh.childControlHeight = false;
            rh.childForceExpandWidth = false;
            var trophy = Img("Icon", recRow, Art("UI/Icons/icon_trophy.png"), Color.white);
            trophy.rectTransform.sizeDelta = new Vector2(20, 20);
            Layout(trophy, 20, 20);
            Label("Text", recRow, "최고 기록 경신!", 18, Butter).rectTransform.sizeDelta = new Vector2(120, 24);
            newRecord = rec.gameObject;
            newRecord.SetActive(false);

            var top = Node("TopPlanet", card);
            Layout(top, 78);
            topIcon = PlanetIconUI("Planet", top, new Vector2(0, 3));

            meta = Label("Meta", card, "이번 판 최고 행성 -\n최고 점수 0", 14, InkSoft);
            meta.lineSpacing = 18;
            Layout(meta, 45);
            Spacer(card, 6);

            var reviveBox = Node("Revive", card);
            Layout(reviveBox, 63);
            reviveBox.gameObject.AddComponent<VerticalLayoutGroup>().childForceExpandHeight = false;
            var rv = reviveBox.GetComponent<VerticalLayoutGroup>();
            rv.childControlWidth = true;
            rv.childControlHeight = true;
            rv.childForceExpandWidth = true;
            revive = BigButton(reviveBox, "ReviveButton", "광고 보고 이어하기", 21, 51, Mint, MintDark, MintInk, Art("UI/Icons/icon_tv.png"));
            reviveRow = reviveBox.gameObject;

            var row = Node("Row", card);
            Layout(row, 41);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            retry = BigButton(row, "RetryButton", "다시 하기", 17, 41, Butter, ButterDark, ButterInk);
            share = BigButton(row, "ShareButton", "점수 자랑하기", 17, 41, Rgba(255, 255, 255, 0.1f), Rgba(0, 0, 0, 0.3f), Ink);
            Spacer(card, 5); // 버튼 그림자 자리
            return overlay;
        }

        static void BuildAdScreen(RectTransform root, MockAdsService ads, GameObject bannerPlaceholder)
        {
            var ov = OverlayRoot(root, "AdScreen");
            var group = ov.gameObject.AddComponent<CanvasGroup>();
            var fit = Place(Node("Fit", ov), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320, 498));
            var box = Img("Box", fit, Art("UI/ui_adbox.png"), Color.white);
            Stretch(box.rectTransform);
            Wire.Set(fit.gameObject.AddComponent<ScaleToFit>(), "content", fit);

            var tag = RoundRect("Tag", box.transform, 6, Rgba(0, 0, 0, 0.4f));
            Place(tag.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -12), new Vector2(80, 21));
            var th = tag.gameObject.AddComponent<HorizontalLayoutGroup>();
            th.padding = new RectOffset(8, 8, 3, 3);
            th.childControlWidth = true;
            th.childControlHeight = true;
            var fitter = tag.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var kind = Label("Text", tag.transform, "보상형 광고", 12, Ink);

            var count = Img("Count", box.transform, Art("FX/disc.png"), Rgba(0, 0, 0, 0.45f));
            Place(count.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12, -10), new Vector2(36, 36));
            var countText = Label("Text", count.transform, "3", 17, Ink);
            Stretch(countText.rectTransform);

            var body = Label("Body", box.transform, "실제 출시 버전에서는\n여기서 광고가 재생돼요", 18, Ink);
            body.lineSpacing = 12;
            Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(300, 60));
            var label = Label("Label", box.transform, "", 14, InkSoft);
            Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -32), new Vector2(300, 22));

            var close = PushButton("Skip", box.transform, Rgba(255, 255, 255, 0.12f), Rgba(0, 0, 0, 0.3f), 14, 3, out var closeFace);
            Place((RectTransform)close.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(190, 34));
            Stretch(Label("Label", closeFace, "건너뛰기 (보상 없음)", 13, InkSoft).rectTransform);

            Wire.Set(ads, "screen", group);
            Wire.Set(ads, "kindLabel", kind);
            Wire.Set(ads, "countLabel", countText);
            Wire.Set(ads, "rewardLabel", label);
            Wire.Set(ads, "closeButton", close);
            Wire.Set(ads, "bannerPlaceholder", bannerPlaceholder);
            ov.gameObject.SetActive(false);
        }
    }
}
