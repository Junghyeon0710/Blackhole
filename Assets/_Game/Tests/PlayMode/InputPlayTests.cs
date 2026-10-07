using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Blackhole.Tests
{
    /// <summary>
    /// Main 씬을 열고 가상 마우스·터치·키보드로 실제 입력 경로를 확인한다.
    /// 시작 버튼, 눌러서 조준 → 떼서 발사, 스페이스 발사, UI 버튼 위 터치는 발사하지 않음.
    /// </summary>
    public class InputPlayTests : InputTestFixture
    {
        Mouse mouse;
        Touchscreen touch;
        Keyboard keyboard;
        GameManager game;

        public override void Setup()
        {
            base.Setup();
            mouse = InputSystem.AddDevice<Mouse>();
            touch = InputSystem.AddDevice<Touchscreen>();
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        static Vector2 ScreenCenterOf(Component c)
        {
            var rt = (RectTransform)c.transform;
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners); // 오버레이 캔버스라 화면 좌표
            return (corners[0] + corners[2]) / 2;
        }

        static Vector2 ArenaCenterOnScreen() => ViewMetrics.StageRect.center;

        IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++) yield return null;
        }

        IEnumerator Click(Vector2 position)
        {
            Set(mouse.position, position);
            yield return null;
            Press(mouse.leftButton);
            yield return null;
            Release(mouse.leftButton);
            yield return null;
        }

        IEnumerator WaitReady()
        {
            float until = Time.realtimeSinceStartup + 3;
            while (!game.Session.CanLaunch && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsTrue(game.Session.CanLaunch, "발사 준비가 끝나지 않았습니다");
        }

        [UnityTest]
        public IEnumerator PointerTouchAndKeyboard_AimAndLaunch()
        {
            SceneManager.LoadScene("Main");
            yield return Frames(3);
            game = Object.FindAnyObjectByType<GameManager>();
            Assert.NotNull(game);
            Assert.AreEqual(GameState.Menu, game.State);

            // 시작하기 버튼
            var start = GameObject.Find("StartButton").GetComponent<Button>();
            yield return Click(ScreenCenterOf(start));
            Assert.AreEqual(GameState.Playing, game.State, "시작 버튼을 눌러도 판이 시작되지 않았습니다");
            Assert.AreEqual(0, game.Session.World.Bodies.Count, "시작 버튼을 뗀 것이 발사로 처리되었습니다");
            yield return Frames(2);

            // 마우스: 경기장 오른쪽을 눌렀다 떼면 오른쪽(각도 0)에서 발사
            var center = ArenaCenterOnScreen();
            Set(mouse.position, center + new Vector2(200, 0));
            yield return null;
            Press(mouse.leftButton);
            yield return null;
            Release(mouse.leftButton);
            yield return null;
            Assert.AreEqual(1, game.Session.World.Bodies.Count, "마우스로 발사되지 않았습니다");
            var first = game.Session.World.Bodies[0];
            Assert.Greater(first.X, 200, "오른쪽으로 조준하지 않았습니다");
            Assert.Less(System.Math.Abs(first.Y), 5);

            // 터치: 경기장 위쪽을 누르고 왼쪽으로 끌어 뗀다 → 마지막 손가락 방향(왼쪽)에서 발사
            yield return WaitReady();
            int before = game.Session.World.Bodies.Count;
            BeginTouch(1, center + new Vector2(0, 250));
            yield return null;
            MoveTouch(1, center + new Vector2(-250, 0));
            yield return null;
            EndTouch(1, center + new Vector2(-250, 0));
            yield return null;
            Assert.AreEqual(before + 1, game.Session.World.Bodies.Count, "터치로 발사되지 않았습니다");
            var second = game.Session.World.Bodies[game.Session.World.Bodies.Count - 1];
            Assert.Less(second.X, -200, "끌어서 바꾼 방향으로 발사되지 않았습니다");

            // UI 버튼 위 터치는 조준·발사가 아니다 (소리 버튼은 눌린다)
            yield return WaitReady();
            before = game.Session.World.Bodies.Count;
            bool soundBefore = game.SoundOn;
            var soundButton = GameObject.Find("Sound").GetComponent<Button>();
            var soundPos = ScreenCenterOf(soundButton);
            BeginTouch(2, soundPos);
            yield return null;
            EndTouch(2, soundPos);
            yield return Frames(2);
            Assert.AreEqual(before, game.Session.World.Bodies.Count, "UI 버튼을 누른 것이 발사로 처리되었습니다");
            Assert.AreNotEqual(soundBefore, game.SoundOn, "소리 버튼이 눌리지 않았습니다");
            game.ToggleSound(); // 저장된 설정을 되돌린다

            // 키보드: ← 로 각도를 돌리고 스페이스로 발사
            yield return WaitReady();
            before = game.Session.World.Bodies.Count;
            double angle = game.Session.AimAngle;
            Press(keyboard.leftArrowKey);
            yield return null;
            Release(keyboard.leftArrowKey);
            yield return null;
            Assert.AreEqual(angle + 0.08, game.Session.AimAngle, 1e-6, "← 키가 조준을 0.08 돌리지 않았습니다");
            Press(keyboard.spaceKey);
            yield return null;
            Release(keyboard.spaceKey);
            yield return null;
            Assert.AreEqual(before + 1, game.Session.World.Bodies.Count, "스페이스로 발사되지 않았습니다");
        }
    }
}
