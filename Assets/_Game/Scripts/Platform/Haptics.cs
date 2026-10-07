using System.Collections;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Blackhole
{
    /// <summary>
    /// 진동. navigator.vibrate 와 같은 형식이다: Vibrate(12) 또는 Vibrate(40, 60, 120) = 40ms 진동, 60ms 쉼, 120ms 진동.
    /// 안드로이드는 VibrationEffect 로 길이를 맞추고, iOS 는 길이 대신 세기(가벼움·보통·강함) 햅틱으로 바꾼다.
    /// </summary>
    public static class Haptics
    {
        public static bool Enabled = true;

        public static void Vibrate(int milliseconds) => Vibrate(new[] { milliseconds });

        public static void Vibrate(params int[] pattern)
        {
            if (!Enabled || pattern == null || pattern.Length == 0) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidVibrate(pattern);
#elif UNITY_IOS && !UNITY_EDITOR
            Runner.Play(pattern);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject s_Vibrator;

        static void AndroidVibrate(int[] pattern)
        {
            try
            {
                if (s_Vibrator == null)
                {
                    using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    s_Vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                using var effects = new AndroidJavaClass("android.os.VibrationEffect");
                AndroidJavaObject effect;
                if (pattern.Length == 1)
                {
                    effect = effects.CallStatic<AndroidJavaObject>("createOneShot", (long)pattern[0], -1);
                }
                else
                {
                    // 안드로이드 파형은 "쉼"부터 시작한다
                    var timings = new long[pattern.Length + 1];
                    for (int i = 0; i < pattern.Length; i++) timings[i + 1] = pattern[i];
                    effect = effects.CallStatic<AndroidJavaObject>("createWaveform", timings, -1);
                }
                using (effect) s_Vibrator.Call("vibrate", effect);
            }
            catch (System.Exception)
            {
                // 진동 장치가 없거나 막힌 기기. Handheld.Vibrate 를 코드에 두면 Unity 가 VIBRATE 권한도 넣어 준다.
                Handheld.Vibrate();
            }
        }
#endif

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void _BH_Impact(int style);

        static void Impact(int ms) => _BH_Impact(ms <= 8 ? 0 : ms <= 20 ? 1 : 2);

        sealed class Runner : MonoBehaviour
        {
            static Runner s_Instance;

            public static void Play(int[] pattern)
            {
                if (pattern.Length == 1) { Impact(pattern[0]); return; }
                if (s_Instance == null)
                {
                    var go = new GameObject("Haptics");
                    DontDestroyOnLoad(go);
                    s_Instance = go.AddComponent<Runner>();
                }
                s_Instance.StartCoroutine(PlayPattern(pattern));
            }

            static IEnumerator PlayPattern(int[] pattern)
            {
                for (int i = 0; i < pattern.Length; i++)
                {
                    if (i % 2 == 0) Impact(pattern[i]);
                    yield return new WaitForSecondsRealtime(pattern[i] / 1000f);
                }
            }
        }
#endif
    }
}
