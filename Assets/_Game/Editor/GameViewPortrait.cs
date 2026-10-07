using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Blackhole.EditorTools
{
    /// <summary>Game 뷰를 세로 폰 해상도로 바꾼다 (Unity 내부 API 를 리플렉션으로 쓴다).</summary>
    public static class GameViewPortrait
    {
        [MenuItem("Blackhole/Game 뷰 세로 1080×1920", priority = 40)]
        public static void Set1080x1920() => Set(1080, 1920);

        [MenuItem("Blackhole/Game 뷰 세로 1170×2532 (노치 폰)", priority = 41)]
        public static void Set1170x2532() => Set(1170, 2532);

        public static bool Set(int width, int height)
        {
            try
            {
                var asm = typeof(Editor).Assembly;
                var sizesType = asm.GetType("UnityEditor.GameViewSizes");
                var single = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var sizes = single.GetProperty("instance").GetValue(null);
                var groupType = (GameViewSizeGroupType)sizesType.GetProperty("currentGroupType").GetValue(sizes);
                var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { (int)groupType });

                string label = $"Blackhole {width}x{height}";
                var getTexts = group.GetType().GetMethod("GetDisplayTexts");
                var texts = (string[])getTexts.Invoke(group, null);
                int index = Array.FindIndex(texts, t => t.StartsWith(label, StringComparison.Ordinal));
                if (index < 0)
                {
                    var sizeType = asm.GetType("UnityEditor.GameViewSize");
                    var kindType = asm.GetType("UnityEditor.GameViewSizeType");
                    var ctor = sizeType.GetConstructor(new[] { kindType, typeof(int), typeof(int), typeof(string) });
                    var size = ctor.Invoke(new[] { Enum.Parse(kindType, "FixedResolution"), width, height, (object)label });
                    group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
                    texts = (string[])getTexts.Invoke(group, null);
                    index = Array.FindIndex(texts, t => t.StartsWith(label, StringComparison.Ordinal));
                }

                var gameViewType = asm.GetType("UnityEditor.GameView");
                var window = EditorWindow.GetWindow(gameViewType, false, null, false);
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var select = gameViewType.GetMethod("SizeSelectionCallback", flags);
                if (select != null) select.Invoke(window, new object[] { index, null });
                else gameViewType.GetProperty("selectedSizeIndex", flags)?.SetValue(window, index);
                window.Repaint();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Game 뷰 해상도를 바꾸지 못했습니다. Game 뷰 위쪽 해상도 메뉴에서 직접 고르세요: " + e.Message);
                return false;
            }
        }
    }
}
