using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Blackhole
{
    /// <summary>기기 공유 시트. 공유 시트가 없는 곳(에디터, PC)에서는 false 를 돌려주고 부른 쪽이 클립보드에 복사한다.</summary>
    public static class NativeShare
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void _BH_Share(string text);
#endif

        public static bool Share(string text, string chooserTitle)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var intentClass = new AndroidJavaClass("android.content.Intent");
                using var intent = new AndroidJavaObject("android.content.Intent");
                intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND")).Dispose();
                intent.Call<AndroidJavaObject>("setType", "text/plain").Dispose();
                intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text).Dispose();
                using var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, chooserTitle);
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                activity.Call("startActivity", chooser);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("공유 시트를 열지 못했습니다: " + e.Message);
                return false;
            }
#elif UNITY_IOS && !UNITY_EDITOR
            _BH_Share(text);
            return true;
#else
            return false;
#endif
        }

        /// <summary>스토어 링크. iOS 는 앱 등록 후 숫자 앱 ID 로 바꾼다.</summary>
        public static string StoreUrl
        {
            get
            {
#if UNITY_IOS
                return "https://apps.apple.com/app/id0000000000";
#else
                return "https://play.google.com/store/apps/details?id=" + Application.identifier;
#endif
            }
        }
    }
}
