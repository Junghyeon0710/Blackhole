#if UNITY_EDITOR
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Blackhole.Dev
{
    /// <summary>
    /// 하이라이트 영상 녹화용 (에디터 전용, 빌드에는 들어가지 않는다).
    /// Time.captureDeltaTime 으로 게임 시간을 프레임마다 고정해 느린 에디터에서도 끊김 없이 JPG 를 남기고,
    /// 나중에 효과음을 맞춰 넣을 수 있게 발사·합체·발견·블랙홀·게임 오버 시점을 events.json 에 적는다.
    /// </summary>
    public sealed class FrameRecorder : MonoBehaviour
    {
        public int Fps = 30;
        public int Quality = 92;

        public string Folder { get; private set; }
        public int Frame { get; private set; }
        public bool Recording { get; private set; }

        readonly StringBuilder events = new StringBuilder();
        GameManager game;
        int discovered;
        bool hooked;

        void Awake()
        {
            Time.captureDeltaTime = 1f / Fps;
            StartCoroutine(Capture());
        }

        void OnDestroy()
        {
            Time.captureDeltaTime = 0;
            Unhook();
        }

        public void Begin(string folder)
        {
            Folder = folder;
            Directory.CreateDirectory(folder);
            foreach (var f in Directory.GetFiles(folder)) File.Delete(f);
            Frame = 0;
            events.Clear();
            game = FindAnyObjectByType<GameManager>();
            discovered = game.Discovered;
            Hook();
            Recording = true;
        }

        public void End()
        {
            Recording = false;
            Unhook();
            File.WriteAllText(Path.Combine(Folder, "events.json"), "{\"fps\":" + Fps + ",\"frames\":" + Frame + ",\"events\":[" + events + "]}");
        }

        /// <summary>코드로 누른 버튼처럼 이벤트로는 안 잡히는 소리 자리.</summary>
        public void Mark(string type) => Add(type, -1, 0);

        void Add(string type, int tier, int combo)
        {
            if (!Recording) return;
            if (events.Length > 0) events.Append(',');
            events.Append(string.Format(CultureInfo.InvariantCulture, "{{\"f\":{0},\"t\":\"{1}\",\"tier\":{2},\"combo\":{3}}}", Frame, type, tier, combo));
        }

        void Hook()
        {
            if (hooked) return;
            hooked = true;
            game.Session.Launched += OnLaunched;
            game.Session.Merged += OnMerged;
            game.Session.BlackHoleStarted += OnBlackHole;
            game.Session.Ended += OnEnded;
        }

        void Unhook()
        {
            if (!hooked || game == null) return;
            hooked = false;
            game.Session.Launched -= OnLaunched;
            game.Session.Merged -= OnMerged;
            game.Session.BlackHoleStarted -= OnBlackHole;
            game.Session.Ended -= OnEnded;
        }

        void OnLaunched(Body b) => Add("drop", b.Tier, 0);
        void OnBlackHole(double x, double y) => Add("blackhole", 11, 0);
        void OnEnded() => Add("over", -1, 0);

        void OnMerged(MergeEvent e)
        {
            Add("merge", e.Tier, e.Combo);
            if (game.Discovered > discovered)
            {
                discovered = game.Discovered;
                Add("discover", e.Tier, 0);
            }
        }

        IEnumerator Capture()
        {
            var wait = new WaitForEndOfFrame();
            while (true)
            {
                yield return wait;
                if (!Recording) continue;
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(Folder, Frame.ToString("00000") + ".jpg"), tex.EncodeToJPG(Quality));
                Destroy(tex);
                Frame++;
            }
        }
    }
}
#endif
