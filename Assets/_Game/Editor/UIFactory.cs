using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Blackhole.EditorTools
{
    /// <summary>씬 빌더가 쓰는 uGUI 조립 도우미. 크기 단위는 프로토타입 CSS px (= 캔버스 단위).</summary>
    static class UIFactory
    {
        public static TMP_FontAsset Font;
        public static Material OutlineMaterial;

        public static Color Hex(string hex, float alpha = 1)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            c.a = alpha;
            return c;
        }

        public static Color Rgba(int r, int g, int b, float a) => new Color(r / 255f, g / 255f, b / 255f, a);

        public static Sprite Art(string relativePath)
        {
            var path = ArtImportPostprocessor.ArtRoot + relativePath;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException("스프라이트가 없습니다: " + path);
            return sprite;
        }

        public static Sprite Round(int radius) => Art($"UI/ui_round_r{radius}.png");

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>위쪽(또는 아래쪽)에 붙은 가로로 꽉 찬 띠.</summary>
        public static RectTransform Band(RectTransform rt, bool top, float offset, float height, float left = 0, float right = 0)
        {
            rt.anchorMin = new Vector2(0, top ? 1 : 0);
            rt.anchorMax = new Vector2(1, top ? 1 : 0);
            rt.pivot = new Vector2(0.5f, top ? 1 : 0);
            rt.anchoredPosition = new Vector2((left - right) / 2, top ? -offset : offset);
            rt.sizeDelta = new Vector2(-(left + right), height);
            return rt;
        }

        public static Image Img(string name, Transform parent, Sprite sprite, Color color)
        {
            var img = Node(name, parent).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        /// <summary>둥근 사각형. 스프라이트 원래 반지름과 원하는 반지름의 비율로 모서리 크기를 맞춘다.</summary>
        public static Image RoundRect(string name, Transform parent, float radius, Color color, int spriteRadius = 0)
        {
            spriteRadius = spriteRadius > 0 ? spriteRadius : NearestRound(radius);
            var img = Img(name, parent, Round(spriteRadius), color);
            img.pixelsPerUnitMultiplier = spriteRadius / Mathf.Max(0.01f, radius);
            return img;
        }

        static int NearestRound(float radius)
        {
            int[] available = { 5, 12, 14, 16, 18, 22, 28 };
            int best = available[0];
            foreach (var r in available) if (Mathf.Abs(r - radius) < Mathf.Abs(best - radius)) best = r;
            return best;
        }

        public static TextMeshProUGUI Label(string name, Transform parent, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var t = Node(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.richText = true;
            return t;
        }

        public static LayoutElement Layout(Component c, float preferredHeight = -1, float preferredWidth = -1, float flexibleWidth = -1)
        {
            if (!c.TryGetComponent<LayoutElement>(out var le)) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            le.minHeight = preferredHeight;
            le.preferredWidth = preferredWidth;
            le.flexibleWidth = flexibleWidth;
            return le;
        }

        public static RectTransform Spacer(Transform parent, float height)
        {
            var rt = Node("Space", parent);
            Layout(rt, height);
            return rt;
        }

        public static void IgnoreLayout(Component c)
        {
            if (!c.TryGetComponent<LayoutElement>(out var le)) le = c.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
        }

        /// <summary>
        /// 프로토타입 .btn: 둥근 면 + 아래로 깔린 그림자(box-shadow 0 Npx 0). 누르면 면이 4px 내려간다.
        /// 루트 사각형이 면의 크기이고 그림자는 아래로 삐져나온다.
        /// </summary>
        public static Button PushButton(string name, Transform parent, Color face, Color shadow, float radius, float depth, out RectTransform faceRect)
        {
            var root = Node(name, parent);
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = new Color(1, 1, 1, 0);
            hit.raycastTarget = true;

            var shadowImg = RoundRect("Shadow", root, radius, shadow);
            Stretch(shadowImg.rectTransform, 0, 0, depth, -depth);

            var faceImg = RoundRect("Face", root, radius, face);
            faceRect = Stretch(faceImg.rectTransform);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;

            var press = root.gameObject.AddComponent<PressableButton>();
            Wire.Set(press, "face", faceRect);
            return button;
        }

        public static T Add<T>(Component c) where T : Component => c.gameObject.AddComponent<T>();
    }

    /// <summary>[SerializeField] private 필드에 값을 넣는다.</summary>
    static class Wire
    {
        public static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field) ?? throw new InvalidOperationException($"{target.GetType().Name}.{field} 필드가 없습니다");
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Set(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field) ?? throw new InvalidOperationException($"{target.GetType().Name}.{field} 필드가 없습니다");
            p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Set(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field) ?? throw new InvalidOperationException($"{target.GetType().Name}.{field} 필드가 없습니다");
            p.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Set(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field) ?? throw new InvalidOperationException($"{target.GetType().Name}.{field} 필드가 없습니다");
            p.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field) ?? throw new InvalidOperationException($"{target.GetType().Name}.{field} 필드가 없습니다");
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
