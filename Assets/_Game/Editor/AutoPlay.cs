using Blackhole.Dev;
using UnityEditor;
using UnityEngine;

namespace Blackhole.EditorTools
{
    /// <summary>
    /// 플레이 모드에서 자동으로 쏘는 테스트 도구 (<see cref="DemoPlayer"/> 를 붙였다 뗀다).
    /// 합체·콤보·블랙홀 연출을 빨리 확인하거나 판 길이를 재 볼 때 쓴다.
    /// </summary>
    public static class AutoPlay
    {
        [MenuItem("Blackhole/자동 플레이 켜기·끄기 (플레이 모드)", priority = 60)]
        public static void Toggle() => SetEnabled(Find() == null);

        [MenuItem("Blackhole/자동 플레이 켜기·끄기 (플레이 모드)", true)]
        static bool CanToggle() => EditorApplication.isPlaying;

        public static DemoPlayer Find()
        {
            var game = Object.FindAnyObjectByType<GameManager>();
            return game != null ? game.GetComponent<DemoPlayer>() : null;
        }

        public static int Launches => Find() is { } p ? p.Launches : 0;

        public static DemoPlayer SetEnabled(bool on, int seed = 0, bool targeted = true)
        {
            var game = Object.FindAnyObjectByType<GameManager>();
            if (game == null) return null;
            var player = game.GetComponent<DemoPlayer>();
            if (!on)
            {
                if (player != null) Object.Destroy(player);
                return null;
            }
            if (player == null) player = game.gameObject.AddComponent<DemoPlayer>();
            if (seed != 0) player.Seed(seed);
            player.Targeted = targeted;
            return player;
        }
    }
}
