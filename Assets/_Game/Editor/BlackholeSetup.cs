using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Blackhole.EditorTools
{
    /// <summary>
    /// 메뉴 Blackhole > 게임 만들기: 프로젝트 설정, 글꼴·재질·행성 데이터·프리팹을 만들고 Main 씬을 조립한다.
    /// 다시 실행하면 같은 경로의 에셋을 갱신하고 씬을 새로 만든다 (씬을 손으로 고쳤다면 덮어쓴다).
    /// </summary>
    public static class BlackholeSetup
    {
        public const string Root = "Assets/_Game";
        public const string ScenePath = Root + "/Scenes/Main.unity";
        const string FontPath = Root + "/Fonts/Jua-Regular.ttf";
        const string FontAssetPath = Root + "/Fonts/Jua SDF.asset";
        const string OutlineMaterialPath = Root + "/Fonts/Jua SDF - Outline.mat";
        const string SpriteMaterialPath = Root + "/Materials/SpriteUnlit.mat";
        const string ShapesMaterialPath = Root + "/Materials/Shapes.mat";
        const string PlanetDataPath = Root + "/Data/PlanetData.asset";
        const string PlanetPrefabPath = Root + "/Prefabs/Planet.prefab";
        const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        // 화면에 나오는 글자들. 동적 글꼴이지만 미리 구워 두면 첫 판에 글자를 굽느라 끊기지 않는다.
        const string Charset =
            "0123456789,.+-!?():x%/ " +
            "점수 최고 다음 행성 교체 운석 청소 광고 준비 중 도감 배너 자리 기기에서는 SDK 배너가 여기에 붙어요 " +
            "블랙홀 만들기 같은 행성끼리 부딪히면 합체 태양 두 개가 만나면 블랙홀이 태어나요 화면을 눌러 발사 방향을 정해요 " +
            "손을 떼면 행성이 날아가요 점선 밖으로 행성이 넘치면 끝나요 시작하기 최고 점수 지금까지 만든 블랙홀 개 " +
            "행성이 넘쳤어요 최고 기록 경신 이번 판 최고 행성 광고 보고 이어하기 다시 하기 점수 자랑하기 " +
            "보상형 광고 전면 광고 실제 출시 버전에서는 여기서 광고가 재생돼요 보상 바깥쪽 행성 정리 후 이어하기 " +
            "지금 행성을 다른 행성으로 바꾸기 운석과 달을 모두 치우기 판과 판 사이에 나오는 광고 자리 건너뛰기 보상 없음 " +
            "같은 행성끼리 합체시켜 보세요 새 행성 발견 블랙홀 탄생 바깥쪽 행성을 치웠어요 으로 로 바꿨어요 " +
            "운석과 달을 치웠어요 치울 운석과 달이 없어요 자랑 문구를 복사했어요 콤보 위험 " +
            "운석 달 수성 화성 금성 지구 해왕성 천왕성 토성 목성 태양 AdMob AD";

        public sealed class Assets
        {
            public TMP_FontAsset Font;
            public Material OutlineMaterial;
            public Material SpriteMaterial;
            public Material ShapesMaterial;
            public PlanetData PlanetData;
            public GameObject PlanetPrefab;
        }

        [MenuItem("Blackhole/게임 만들기 (설정 + 에셋 + 씬)", priority = 0)]
        public static void BuildAll()
        {
            SetupProject();
            if (!EnsureTmpEssentials())
            {
                EditorUtility.DisplayDialog("블랙홀 만들기", "TextMeshPro 기본 리소스를 가져오는 중입니다. 가져오기가 끝나면 메뉴를 한 번 더 실행하세요.", "확인");
                return;
            }
            var assets = CreateAssets();
            SceneBuilder.Build(assets);
        }

        [MenuItem("Blackhole/프로젝트 설정만", priority = 20)]
        public static void SetupProject()
        {
            // 웹 프로토타입과 같은 색 섞임(반투명 그라디언트, 빛무리)을 내려고 감마 색 공간을 쓴다
            if (PlayerSettings.colorSpace != ColorSpace.Gamma) PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.productName = "블랙홀 만들기";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            // 물리는 직접 계산한다 (Physics2D 는 쓰지 않음)
            Physics2D.simulationMode = SimulationMode2D.Script;
            AssetDatabase.SaveAssets();
        }

        public static bool EnsureTmpEssentials()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath) != null) return true;
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
            var package = Path.Combine(info.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            UnityEditor.AssetPackage.Package.Import(package, false);
            return false;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static Assets CreateAssets()
        {
            foreach (var folder in new[] { "Materials", "Data", "Prefabs", "Scenes" }) EnsureFolder(Root + "/" + folder);
            var a = new Assets();

            a.SpriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteMaterialPath);
            if (a.SpriteMaterial == null)
            {
                a.SpriteMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")) { name = "SpriteUnlit" };
                AssetDatabase.CreateAsset(a.SpriteMaterial, SpriteMaterialPath);
            }

            a.ShapesMaterial = AssetDatabase.LoadAssetAtPath<Material>(ShapesMaterialPath);
            if (a.ShapesMaterial == null)
            {
                a.ShapesMaterial = new Material(Shader.Find("Blackhole/Shapes")) { name = "Shapes" };
                AssetDatabase.CreateAsset(a.ShapesMaterial, ShapesMaterialPath);
            }
            a.ShapesMaterial.mainTexture = UIFactory.Art("FX/disc.png").texture;
            EditorUtility.SetDirty(a.ShapesMaterial);

            CreateFont(a);
            a.PlanetData = CreatePlanetData();
            a.PlanetPrefab = CreatePlanetPrefab(a);
            AssetDatabase.SaveAssets();
            return a;
        }

        static void CreateFont(Assets a)
        {
            a.Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (a.Font == null)
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
                // 1024 아틀라스 한 장에 화면 글자(약 170자)가 다 들어간다. 모자라면 아틀라스가 한 장 더 생긴다
                var fa = TMP_FontAsset.CreateFontAsset(font, 48, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                fa.name = "Jua SDF";
                AssetDatabase.CreateAsset(fa, FontAssetPath);
                fa.atlasTextures[0].name = "Jua SDF Atlas";
                AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
                fa.material.name = "Jua SDF Material";
                fa.material.shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
                AssetDatabase.AddObjectToAsset(fa.material, fa);
                fa.TryAddCharacters(Charset);
                EditorUtility.SetDirty(fa);
                AssetDatabase.SaveAssets();
                a.Font = fa;
            }

            // 떠오르는 점수 글자: 프로토타입 strokeText(lineWidth 4, rgba(26,20,70,.85))
            a.OutlineMaterial = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
            if (a.OutlineMaterial == null)
            {
                a.OutlineMaterial = new Material(a.Font.material) { name = "Jua SDF - Outline" };
                AssetDatabase.CreateAsset(a.OutlineMaterial, OutlineMaterialPath);
            }
            a.OutlineMaterial.shader = a.Font.material.shader;
            a.OutlineMaterial.CopyPropertiesFromMaterial(a.Font.material);
            a.OutlineMaterial.EnableKeyword("OUTLINE_ON");
            a.OutlineMaterial.SetColor("_OutlineColor", new Color32(26, 20, 70, 217));
            a.OutlineMaterial.SetFloat("_OutlineWidth", 0.3f);
            a.OutlineMaterial.SetFloat("_FaceDilate", 0.15f);
            EditorUtility.SetDirty(a.OutlineMaterial);

            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            if (settings != null)
            {
                TMP_Settings.defaultFontAsset = a.Font;
                EditorUtility.SetDirty(settings);
            }
            UIFactory.Font = a.Font;
            UIFactory.OutlineMaterial = a.OutlineMaterial;
        }

        static PlanetData CreatePlanetData()
        {
            var data = AssetDatabase.LoadAssetAtPath<PlanetData>(PlanetDataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<PlanetData>();
                AssetDatabase.CreateAsset(data, PlanetDataPath);
            }

            // 기획서 3장 표: 이름, 반지름, 밝은색, 어두운색, 발사 가중치
            (string name, float r, string light, string dark, float weight)[] table =
            {
                ("운석", 11, "#c9b8a8", "#6b5a4d", 0.30f),
                ("달", 15, "#fbf8f0", "#a39f96", 0.27f),
                ("수성", 20, "#ecd2ad", "#8f6b48", 0.22f),
                ("화성", 26, "#ff9a66", "#b8381c", 0.13f),
                ("금성", 32, "#fff0b0", "#e0a040", 0.08f),
                ("지구", 39, "#6fd3ff", "#1f5fd1", 0),
                ("해왕성", 47, "#8aa0ff", "#2a2fb8", 0),
                ("천왕성", 56, "#c8fff6", "#3fb7c9", 0),
                ("토성", 66, "#ffe9b3", "#c99645", 0),
                ("목성", 78, "#ffe0bf", "#c0784a", 0),
                ("태양", 92, "#fff8c0", "#ff9a1f", 0),
            };
            data.tiers = new PlanetData.Tier[table.Length];
            for (int i = 0; i < table.Length; i++)
            {
                var t = table[i];
                data.tiers[i] = new PlanetData.Tier
                {
                    name = t.name,
                    radius = t.r,
                    light = UIFactory.Hex(t.light),
                    dark = UIFactory.Hex(t.dark),
                    spawnWeight = t.weight,
                    sprite = UIFactory.Art($"Planets/planet_{i:00}.png"),
                };
            }
            data.faceNormal = UIFactory.Art("Faces/face_normal.png");
            data.faceBlink = UIFactory.Art("Faces/face_blink.png");
            data.faceHappy = UIFactory.Art("Faces/face_happy.png");
            data.faceScared = UIFactory.Art("Faces/face_scared.png");
            data.blackHoleName = "블랙홀";
            data.blackHoleGlow = UIFactory.Art("BlackHole/bh_glow.png");
            data.blackHoleDisk = UIFactory.Art("BlackHole/bh_disk.png");
            data.blackHoleCore = UIFactory.Art("BlackHole/bh_core.png");
            EditorUtility.SetDirty(data);
            return data;
        }

        static GameObject CreatePlanetPrefab(Assets a)
        {
            var root = new GameObject("Planet");
            var view = root.AddComponent<PlanetView>();

            var body = new GameObject("Body").AddComponent<SpriteRenderer>();
            body.transform.SetParent(root.transform, false);
            body.sharedMaterial = a.SpriteMaterial;
            body.sprite = a.PlanetData.tiers[0].sprite;

            var faceGo = new GameObject("Face");
            faceGo.transform.SetParent(root.transform, false);
            var faceRenderer = faceGo.AddComponent<SpriteRenderer>();
            faceRenderer.sharedMaterial = a.SpriteMaterial;
            faceRenderer.sprite = a.PlanetData.faceNormal;
            faceRenderer.sortingOrder = 1;
            var face = faceGo.AddComponent<PlanetFace>();
            Wire.Set(face, "face", faceRenderer);

            Wire.Set(view, "body", body);
            Wire.Set(view, "face", face);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PlanetPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}
