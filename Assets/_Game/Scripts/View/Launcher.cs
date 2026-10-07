using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Blackhole
{
    /// <summary>
    /// 조준과 발사. 눌러서 방향을 정하고 떼면 발사한다 (경기장 중심에서 손가락까지의 각도).
    /// 마우스는 누르지 않아도 조준, 키보드 ←→ 는 ±0.08 rad, 스페이스·엔터는 발사.
    /// 경기장 가장자리의 대기 행성, 점선 조준선, 착지 예상 원도 여기서 그린다.
    /// </summary>
    public sealed class Launcher : MonoBehaviour
    {
        const float KeyStep = 0.08f;
        const float KeyRepeatDelay = 0.4f;
        const float KeyRepeatInterval = 1 / 30f;
        const float DeadZoneCss = 3;

        [SerializeField] CameraRig rig;
        [SerializeField] PlanetView waiting;
        [SerializeField] ShapeBatch aim;

        static readonly Color AimLine = new Color32(255, 247, 236, 128);    // rgba(255,247,236,.5)
        static readonly Color AimCircle = new Color32(255, 247, 236, 89);   // rgba(255,247,236,.35)

        GameManager game;
        GameSession session;
        bool touchAiming, mouseAiming;
        bool touchWasDown, mouseWasDown, leftWasDown, rightWasDown, fireWasDown;
        Vector2 lastMousePos;
        float repeatTimer;
        readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        PointerEventData pointerData;

        public void Bind(GameManager g)
        {
            game = g;
            session = g.Session;
            waiting.Init(g.Data);
            waiting.Face.SetMood(FaceMood.Normal);
        }

        void Update()
        {
            if (game == null) return;
            // 입력 상태는 항상 읽어 둔다. 메뉴·광고 중에 누른 손가락이 나중에 새 누름으로 잡히지 않게
            bool accept = game.AcceptsAimInput;
            if (!accept) touchAiming = mouseAiming = false;
            HandleTouch(accept);
            HandleMouse(accept);
            HandleKeyboard(accept);
        }

        // 누름·뗌은 wasPressedThisFrame 과 직접 기억한 이전 상태를 함께 본다.
        // 입력 이벤트가 처리된 업데이트와 이 Update 가 어긋나도 놓치지 않고, 한 프레임 안에 끝난 짧은 탭도 잡는다.
        static void Edges(ButtonControl button, ref bool wasDown, out bool down, out bool began, out bool ended)
        {
            down = button.isPressed;
            began = button.wasPressedThisFrame || (down && !wasDown);
            ended = button.wasReleasedThisFrame || (!down && wasDown);
            wasDown = down;
        }

        void HandleTouch(bool accept)
        {
            var screen = Touchscreen.current;
            if (screen == null) return;
            var touch = screen.primaryTouch;
            Edges(touch.press, ref touchWasDown, out bool down, out bool began, out bool ended);
            if (!accept) return;
            var pos = touch.position.ReadValue();
            if (began)
            {
                if (!IsOverUI(pos)) { touchAiming = true; AimAt(pos); }
            }
            else if (touchAiming && down)
            {
                AimAt(pos);
            }
            if (touchAiming && ended)
            {
                touchAiming = false;
                AimAt(pos);
                game.Launch();
            }
        }

        void HandleMouse(bool accept)
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            Edges(mouse.leftButton, ref mouseWasDown, out _, out bool began, out bool ended);
            var pos = mouse.position.ReadValue();
            bool moved = pos != lastMousePos;
            lastMousePos = pos;
            if (!accept) return;
            if (began)
            {
                if (!IsOverUI(pos)) { mouseAiming = true; AimAt(pos); }
            }
            else if (mouseAiming)
            {
                AimAt(pos);
            }
            else if (moved && ViewMetrics.StageRect.Contains(pos) && !IsOverUI(pos))
            {
                AimAt(pos); // 누르지 않아도 조준
            }
            if (mouseAiming && ended)
            {
                mouseAiming = false;
                AimAt(pos);
                game.Launch();
            }
        }

        void HandleKeyboard(bool accept)
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            Edges(kb.leftArrowKey, ref leftWasDown, out bool left, out bool leftBegan, out _);
            Edges(kb.rightArrowKey, ref rightWasDown, out bool right, out bool rightBegan, out _);
            bool fire = kb.spaceKey.isPressed || kb.enterKey.isPressed || kb.numpadEnterKey.isPressed;
            bool fireBegan = kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame
                || kb.numpadEnterKey.wasPressedThisFrame || (fire && !fireWasDown);
            fireWasDown = fire;
            if (!accept) return;

            if (leftBegan || rightBegan)
            {
                session.AimAngle += leftBegan ? KeyStep : -KeyStep;
                repeatTimer = KeyRepeatDelay;
            }
            else if (left != right)
            {
                // 키를 누르고 있으면 운영체제 키 반복처럼 계속 돈다
                repeatTimer -= Time.deltaTime;
                while (repeatTimer <= 0)
                {
                    session.AimAngle += left ? KeyStep : -KeyStep;
                    repeatTimer += KeyRepeatInterval;
                }
            }
            if (fireBegan) game.Launch();
        }

        bool IsOverUI(Vector2 screenPos)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            pointerData ??= new PointerEventData(es);
            pointerData.position = screenPos;
            uiHits.Clear();
            es.RaycastAll(pointerData, uiHits);
            return uiHits.Count > 0;
        }

        void AimAt(Vector2 screenPos)
        {
            var p = rig.ScreenToUnity(screenPos) / Tuning.WorldToUnity; // 월드 단위, 경기장 중심 기준
            float cssPerWorld = ViewMetrics.PxPerWorld / ViewMetrics.UiScale;
            float x = p.x * cssPerWorld, y = p.y * cssPerWorld;
            if (x * x + y * y > DeadZoneCss * DeadZoneCss) session.AimAngle = System.Math.Atan2(p.y, p.x);
        }

        /// <summary>발사 지점에서 중심 쪽으로 쏜 원(반지름 r)이 처음 닿는 거리 (기획서 8장 착지 예상 원 공식).</summary>
        double RayHit(double sx, double sy, double dx, double dy, double r, double maxT)
        {
            double best = maxT;
            var bodies = session.World.Bodies;
            for (int i = 0; i < bodies.Count; i++)
            {
                var b = bodies[i];
                double px = b.X - sx, py = b.Y - sy, t = px * dx + py * dy;
                if (t < 0) continue;
                double hx = px - t * dx, hy = py - t * dy, h2 = hx * hx + hy * hy, rr = b.R + r;
                if (h2 < rr * rr)
                {
                    double th = t - System.Math.Sqrt(rr * rr - h2);
                    if (th < best) best = System.Math.Max(0, th);
                }
            }
            return best;
        }

        void LateUpdate()
        {
            aim.Clear();
            bool show = game != null && game.State == GameState.Playing && session.IsRunning && !session.IsOver && !session.BlackHole.Active;
            if (waiting.gameObject.activeSelf != show) waiting.gameObject.SetActive(show);
            if (show)
            {
                int tier = session.Spawner.Current;
                double r = session.Tiers.Radius(tier), l = Tuning.ArenaRadius - r - 1;
                double ux = System.Math.Cos(session.AimAngle), uy = System.Math.Sin(session.AimAngle);
                double sx = ux * l, sy = uy * l;
                var start = (Vector2)ViewMetrics.WorldToUnity(sx, sy);
                float rUnity = (float)r * Tuning.WorldToUnity;

                if (session.Ready)
                {
                    double th = RayHit(sx, sy, -ux, -uy, r, l);
                    var hit = (Vector2)ViewMetrics.WorldToUnity(sx - ux * th, sy - uy * th);
                    aim.DashedLine(start, hit, ViewMetrics.CssToUnity(2), AimLine, ViewMetrics.CssToUnity(3), ViewMetrics.CssToUnity(7));
                    aim.Ring(hit, rUnity, ViewMetrics.CssToUnity(1.5f), AimCircle);
                }

                float pop = session.Ready ? 1 : Mathf.Max(0.4f, 1 - (float)(session.Cooldown / Tuning.LaunchCooldown));
                float bob = ViewMetrics.CssToUnity(Mathf.Sin(Time.time * 4) * 1.2f);
                waiting.SetTier(tier);
                waiting.SetAlpha(session.Ready ? 1 : 0.45f);
                waiting.Place(new Vector3(start.x, start.y - bob, 0), rUnity * pop);
            }
            aim.Apply();
        }
    }
}
