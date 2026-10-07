using UnityEngine;

namespace Blackhole
{
    /// <summary>판이 끝나도 남는 값. PlayerPrefs 키는 기획서 8장 (best, disc, bh, sound) + 진동 설정(vib).</summary>
    public static class SaveData
    {
        const string BestKey = "best";
        const string DiscoveredKey = "disc";
        const string BlackHolesKey = "bh";
        const string SoundKey = "sound";
        const string VibrationKey = "vib";

        public static int Best
        {
            get => PlayerPrefs.GetInt(BestKey, 0);
            set { PlayerPrefs.SetInt(BestKey, value); PlayerPrefs.Save(); }
        }

        /// <summary>지금까지 만든 가장 높은 단계. 블랙홀은 11.</summary>
        public static int Discovered
        {
            get => PlayerPrefs.GetInt(DiscoveredKey, 0);
            set { PlayerPrefs.SetInt(DiscoveredKey, value); PlayerPrefs.Save(); }
        }

        public static int BlackHoles
        {
            get => PlayerPrefs.GetInt(BlackHolesKey, 0);
            set { PlayerPrefs.SetInt(BlackHolesKey, value); PlayerPrefs.Save(); }
        }

        public static bool Sound
        {
            get => PlayerPrefs.GetInt(SoundKey, 1) != 0;
            set { PlayerPrefs.SetInt(SoundKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool Vibration
        {
            get => PlayerPrefs.GetInt(VibrationKey, 1) != 0;
            set { PlayerPrefs.SetInt(VibrationKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }
    }
}
