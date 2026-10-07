# 블랙홀 만들기 (Unity)

기획서 「블랙홀 만들기 게임 기획서 (Unity)」와 웹 프로토타입(blackhole.html)을 Unity 6(6000.6.2f1, URP 2D)으로 옮긴 프로젝트입니다.

## 실행

1. `Assets/_Game/Scenes/Main.unity` 를 열고 Play.
2. Game 뷰는 메뉴 **Blackhole > Game 뷰 세로 1080×1920** 으로 세로 화면에 맞출 수 있습니다.
3. 조작: 경기장을 눌러 방향을 정하고 떼면 발사. 마우스는 누르지 않아도 조준, ←→ 로 각도 조절, 스페이스·엔터로 발사.

## 메뉴 (Blackhole)

| 메뉴 | 하는 일 |
| --- | --- |
| 게임 만들기 (설정 + 에셋 + 씬) | 프로젝트 설정, 글꼴·재질·행성 데이터·프리팹, Main 씬을 다시 만든다. 씬을 손으로 고쳤다면 덮어쓴다 |
| 프로젝트 설정만 | 세로 고정, 감마 색 공간, 제품 이름 |
| Game 뷰 세로 … | 테스트용 해상도 |
| 자동 플레이 켜기·끄기 | 플레이 모드에서 자동으로 쏜다. 합체·블랙홀 연출 확인, 판 길이 재기에 쓴다 |

## 구조

```
Assets/_Game
  Scripts/Core    순수 C# 규칙: GameSession(판 진행), PhysicsWorld(버렛 물리), BlackHoleEvent,
                  ScoreManager, DangerMonitor, SpawnQueue, Tuning(기획서 수치)
  Scripts/View    CameraRig, Background, ArenaView, PlanetRenderer/PlanetView/PlanetFace, Launcher(조준·발사),
                  BlackHoleView, EffectsManager, ShapeBatch(선·고리·파티클 메시)
  Scripts/UI      UIManager, Toast, FloatingTextLayer, CollectionStrip(도감), PlanetIcon …
  Scripts/Game    GameManager(상태 전환), SaveData(PlayerPrefs: best, disc, bh, sound, vib)
  Scripts/Ads     IAdsService, MockAdsService(에디터), AdMobAdsService(BLACKHOLE_ADMOB 정의 시)
  Scripts/Audio   AudioManager + SfxSynth(프로토타입 합성음을 그대로 구움)
  Scripts/Platform Haptics(안드로이드 길이 진동, iOS 햅틱), NativeShare(공유 시트)
  Art             Tools/ArtGen 이 만든 스프라이트 (행성 11, 얼굴 4, 블랙홀 3, UI·아이콘)
  Editor          씬 빌더, 가져오기 설정, 자동 플레이
  Tests           EditMode: 프로토타입과 수치 비교 / PlayMode: 실제 입력 경로
Assets/Plugins/iOS/BlackholeNative.mm   iOS 햅틱·공유 시트
Tools/ArtGen    artgen.html(프로토타입 그리기 코드) → node Tools/ArtGen/run.mjs (Chrome 필요)
Tools/Parity    node Tools/Parity/parity.mjs → 프로토타입 물리로 기준값 생성
```

물리는 기획서 8장 권장대로 프로토타입의 직접 계산을 C# 으로 옮겼습니다. `PhysicsParityTests` 가 같은 시나리오를
프로토타입 코드(Node)와 C# 으로 돌려 행성 위치·점수·콤보·블랙홀·게임 오버가 비트 단위로 같은지 확인합니다.
C# 물리를 고쳐서 이 테스트가 깨지면 프로토타입과 손맛이 달라졌다는 뜻입니다. 의도한 조정이라면
`Tools/Parity/parity.mjs` 안의 프로토타입 코드도 같이 고친 뒤 `node Tools/Parity/parity.mjs` 로 기준값을 다시 만드세요.

## 광고

- 에디터·SDK 없는 빌드: `MockAdsService` (보상형 3초, 전면 2초, "건너뛰기"로 보상 없이 닫기 확인 가능).
- AdMob 연결: Google Mobile Ads Unity 플러그인 설치 → Project Settings > Player > Scripting Define Symbols 에
  `BLACKHOLE_ADMOB` 추가 → Google Mobile Ads 설정에 앱 ID 입력. 광고 단위 ID 는 `AdMobAdsService.cs` 의 구글 공개 테스트 ID 입니다.
  출시 직전에만 실제 ID 로 바꾸세요.

## 출시 전에 바꿀 것

- Player Settings 의 회사 이름과 번들 ID, `NativeShare.StoreUrl` 의 iOS 앱 ID
- 실제 효과음·배경 음악: `Managers/Audio` 의 덮어쓰기 칸에 넣으면 합성음 대신 재생
- 광고 동의(UMP), iOS 추적 허용 팝업, 개인정보처리방침 (기획서 10장 체크리스트)
