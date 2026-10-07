using UnityEngine;

namespace Blackhole
{
    /// <summary>
    /// 효과음. 기본은 프로토타입과 같은 합성음이고, 덮어쓰기 칸에 클립을 넣으면 그 소리를 낸다.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        const int MergePitchSteps = 19; // 단계 1~10 + 콤보 0~8

        [Header("출시용 효과음 (비워 두면 합성음)")]
        [SerializeField] AudioClip dropOverride;
        [SerializeField] AudioClip mergeOverride;
        [SerializeField] AudioClip discoverOverride;
        [SerializeField] AudioClip gameOverOverride;
        [SerializeField] AudioClip blackHoleOverride;
        [SerializeField] AudioClip clickOverride;

        [Header("배경 음악 (선택)")]
        [SerializeField] AudioClip music;
        [SerializeField, Range(0, 1)] float musicVolume = 0.35f;

        const int Voices = 8;

        readonly AudioSource[] voices = new AudioSource[Voices];
        int nextVoice;
        AudioSource bgm;
        AudioClip drop, discover, gameOver, blackHole, click;
        readonly AudioClip[] merges = new AudioClip[MergePitchSteps];

        public bool Enabled { get; private set; }

        void Awake()
        {
            for (int i = 0; i < Voices; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }
            if (music != null)
            {
                bgm = gameObject.AddComponent<AudioSource>();
                bgm.clip = music;
                bgm.loop = true;
                bgm.volume = musicVolume;
                bgm.playOnAwake = false;
            }
            BuildClips();
            SetEnabled(SaveData.Sound);
        }

        void BuildClips()
        {
            int rate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 44100;
            drop = dropOverride ? dropOverride : new SfxBuilder(rate).Tone(620, 0.09, Wave.Triangle, 0.07, 0.55).Build("drop");
            for (int k = 0; k < MergePitchSteps; k++)
            {
                double f = 300 * System.Math.Pow(1.1225, k);
                merges[k] = new SfxBuilder(rate)
                    .Tone(f, 0.16, Wave.Sine, 0.17, 1.6)
                    .Tone(f * 1.5, 0.22, Wave.Triangle, 0.05, 1.2, 0.03)
                    .Build("merge" + k);
            }
            var disc = new SfxBuilder(rate);
            for (int i = 0; i < 3; i++) disc.Tone(523 * System.Math.Pow(1.26, i), 0.22, Wave.Triangle, 0.09, 1, i * 0.09);
            discover = discoverOverride ? discoverOverride : disc.Build("discover");
            gameOver = gameOverOverride ? gameOverOverride : new SfxBuilder(rate).Tone(330, 0.7, Wave.Sawtooth, 0.06, 0.3).Build("over");
            blackHole = blackHoleOverride ? blackHoleOverride : new SfxBuilder(rate)
                .Tone(70, 1.8, Wave.Sawtooth, 0.11, 0.3)
                .Tone(140, 1.4, Wave.Sine, 0.1, 5)
                .Build("blackhole");
            click = clickOverride ? clickOverride : new SfxBuilder(rate).Tone(760, 0.05, Wave.Square, 0.03).Build("click");
        }

        public void SetEnabled(bool on)
        {
            Enabled = on;
            if (bgm == null) return;
            if (on && !bgm.isPlaying) bgm.Play();
            else if (!on) bgm.Stop();
        }

        void Play(AudioClip clip, float pitch = 1)
        {
            if (!Enabled || clip == null) return;
            var voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % Voices;
            voice.pitch = pitch;
            voice.clip = clip;
            voice.Play();
        }

        public void Drop() => Play(drop);
        public void Click() => Play(click);
        public void Discover() => Play(discover);
        public void GameOver() => Play(gameOver);
        public void BlackHole() => Play(blackHole);

        /// <summary>단계와 콤보가 오를수록 높아지는 맑은 음.</summary>
        public void Merge(int tier, int combo)
        {
            int k = Mathf.Clamp(tier + Mathf.Min(combo - 1, 8), 0, MergePitchSteps - 1);
            if (mergeOverride != null) Play(mergeOverride, Mathf.Pow(1.1225f, k - 5));
            else Play(merges[k]);
        }
    }
}
