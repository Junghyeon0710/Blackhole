# 블랙홀 만들기

가운데로 당기는 중력 속에서 같은 행성끼리 합쳐 태양을 만들고, **태양 두 개로 블랙홀을 터뜨리는** 한 손 물리 합체 퍼즐입니다.
웹 프로토타입(blackhole.html)을 기획서 그대로 Unity 6 으로 옮겼습니다. 물리는 프로토타입과 **비트 단위로 같은 값**이 나오는 것을 테스트로 확인합니다.

<p align="center">
  <img src="docs/media/highlight.gif" width="270" alt="하이라이트 영상">
  <br>
  <a href="docs/media/highlight.mp4">🔊 소리 있는 고화질 영상 (MP4, 26초)</a>
</p>

## 플레이 화면

| 시작 화면 | 연쇄 합체 · 콤보 | 금성 한 발로 시작된 연쇄 |
| :---: | :---: | :---: |
| <img src="docs/images/01_start.jpg" width="240"> | <img src="docs/images/02_combo.jpg" width="240"> | <img src="docs/images/03_chain.jpg" width="240"> |
| **블랙홀 탄생** | **위험! 넘치기 직전** | **결과 창** |
| <img src="docs/images/04_blackhole.jpg" width="240"> | <img src="docs/images/05_danger.jpg" width="240"> | <img src="docs/images/06_result.jpg" width="240"> |

## 게임 소개

- **연쇄 합체:** 하나를 쏘면 안쪽에서 합체가 연달아 터지며 콤보가 쌓입니다 (콤보마다 점수 ×1.5, ×2 …).
- **표정 있는 행성:** 모든 행성이 눈을 깜빡이고, 합체하면 웃고(^^), 위험하면 겁먹습니다(><).
- **블랙홀 한 방:** 태양 두 개가 만나면 블랙홀이 판 위 행성을 모두 빨아들이고 빈 판에서 다시 시작합니다.

<p align="center"><img src="docs/images/planets.png" alt="행성 11단계와 블랙홀"></p>

| 조작 | 동작 |
| --- | --- |
| 화면 누르고 끌기 | 경기장 중심 기준으로 발사 방향 조준 (점선 조준선, 착지 예상 원) |
| 손 떼기 | 발사 (0.45초 뒤 다음 행성) |
| 마우스 이동 · ←→ · 스페이스 | PC 에서 조준과 발사 |

- 발사로는 운석~금성(0~4단계)만 나오고, 그 위는 합체로만 만듭니다. 금성을 만들기 전에는 0~3단계만 나옵니다.
- 행성이 점선(위험선) 밖에 2.4초 머물면 게임 오버. 광고를 보고 한 번 이어할 수 있습니다.
- 광고 보상: 이어하기, 행성 교체, 운석 청소 (판당 1회씩). 전면 광고는 3판에 한 번.

## 실행

1. Unity **6000.6.2f1** 로 프로젝트를 엽니다.
2. `Assets/_Game/Scenes/Main.unity` 를 열고 Play.
3. Game 뷰는 메뉴 **Blackhole > Game 뷰 세로 1080×1920** 으로 세로 화면에 맞출 수 있습니다.

| Blackhole 메뉴 | 하는 일 |
| --- | --- |
| 게임 만들기 (설정 + 에셋 + 씬) | 프로젝트 설정, 글꼴·재질·행성 데이터·프리팹, Main 씬을 다시 만든다 (씬을 손으로 고쳤다면 덮어쓴다) |
| 프로젝트 설정만 | 세로 고정, 감마 색 공간, 제품 이름 |
| Game 뷰 세로 … | 테스트용 해상도 |
| 자동 플레이 켜기·끄기 | 플레이 모드에서 사람처럼 조준해 자동으로 쏜다. 연출 확인, 판 길이 재기에 쓴다 |

## 구조

```
Assets/_Game
  Scripts/Core      순수 C# 규칙: GameSession(판 진행), PhysicsWorld(버렛 물리), BlackHoleEvent,
                    ScoreManager, DangerMonitor, SpawnQueue, Tuning(기획서 수치)
  Scripts/View      CameraRig, Background, ArenaView, PlanetRenderer/PlanetView/PlanetFace,
                    Launcher(조준·발사), BlackHoleView, EffectsManager, ShapeBatch(선·고리·파티클 메시)
  Scripts/UI        UIManager, Toast, FloatingTextLayer, CollectionStrip(도감), PlanetIcon …
  Scripts/Game      GameManager(상태 전환), SaveData(PlayerPrefs: best, disc, bh, sound, vib)
  Scripts/Ads       IAdsService, MockAdsService(에디터), AdMobAdsService(BLACKHOLE_ADMOB 정의 시)
  Scripts/Audio     AudioManager + SfxSynth(프로토타입 합성음을 그대로 구움)
  Scripts/Platform  Haptics(안드로이드 길이 진동, iOS 햅틱), NativeShare(공유 시트)
  Scripts/Dev       에디터 전용: 자동 플레이, 녹화기, 장면 재현 (빌드에는 들어가지 않음)
  Art               Tools/ArtGen 이 만든 스프라이트 (행성 11, 얼굴 4, 블랙홀 3, UI·아이콘)
  Editor            씬 빌더, 가져오기 설정, 자동 플레이 메뉴
  Tests             EditMode: 프로토타입과 수치 비교 / PlayMode: 실제 입력 경로
Assets/Plugins/iOS  iOS 햅틱·공유 시트 네이티브 코드
Tools/ArtGen        artgen.html(프로토타입 그리기 코드) → node Tools/ArtGen/run.mjs (Chrome 필요)
Tools/Parity        node Tools/Parity/parity.mjs → 프로토타입 물리로 기준값 생성
Tools/Video         하이라이트 영상 편집 (효과음 합성, Blender 편집, GIF)
docs/               README 그림과 영상
```

## 테스트

Test Runner(Window > General > Test Runner)에서 실행합니다.

- **PhysicsParityTests (EditMode):** `Tools/Parity/parity.mjs` 가 프로토타입 코드를 그대로 돌려 만든 기준값과 C# 이식본을 비교합니다.
  발사·합체·콤보, 블랙홀, 판이 넘쳐 게임 오버가 나는 세 시나리오에서 행성 위치·반지름, 점수, 콤보, 위험도, 블랙홀,
  게임 오버 시점이 비트 단위로 같아야 통과합니다. C# 물리를 고쳐서 깨지면 프로토타입과 손맛이 달라졌다는 뜻입니다.
  의도한 조정이라면 `parity.mjs` 안의 프로토타입 코드도 같이 고친 뒤 기준값을 다시 만드세요.
- **InputPlayTests (PlayMode):** 가상 마우스·터치·키보드로 실제 씬을 조작합니다. 시작 버튼, 눌러 조준하고 떼서 발사,
  끌어서 방향 바꾸기, UI 버튼 위 터치는 발사하지 않음, ← 키와 스페이스를 확인합니다.

## 하이라이트 영상 다시 만들기

1. 플레이 모드에서 `Scripts/Dev` 의 `FrameRecorder`(고정 시간 간격 녹화 + 효과음 시점 기록)와 `ScenarioDirector`(정해 둔 판을 틱 단위로 재현),
   `DemoPlayer`(자동 플레이)로 장면을 `Recordings/` 에 녹화합니다.
2. `python Tools/Video/make_highlight.py` 가 `Tools/Video/edit.json` 대본(구간, 속도, 자막)대로 효과음을 합성하고,
   Blender 5.2(백그라운드)로 컷·크로스페이드·자막·엔딩을 붙여 `docs/media/highlight.mp4` 와 `highlight.gif` 를 만듭니다.

## 광고

- 에디터·SDK 없는 빌드: `MockAdsService` (보상형 3초, 전면 2초, "건너뛰기"로 보상 없이 닫기 확인 가능).
- AdMob 연결: Google Mobile Ads Unity 플러그인 설치 → Player Settings > Scripting Define Symbols 에 `BLACKHOLE_ADMOB` 추가
  → Google Mobile Ads 설정에 앱 ID 입력. 광고 단위 ID 는 `AdMobAdsService.cs` 의 구글 공개 테스트 ID 입니다. 출시 직전에만 실제 ID 로 바꾸세요.

## 출시 전에 바꿀 것

- Player Settings 의 회사 이름과 번들 ID, `NativeShare.StoreUrl` 의 iOS 앱 ID
- 실제 효과음·배경 음악: `Managers/Audio` 의 덮어쓰기 칸에 넣으면 합성음 대신 재생
- 광고 동의(UMP), iOS 추적 허용 팝업, 개인정보처리방침 (기획서 10장 체크리스트)
- 실기기 테스트: 진동, 공유 시트, iOS 네이티브 코드는 기기 빌드에서만 컴파일된다

## 크레딧

- 글꼴: [Jua](https://fonts.google.com/specimen/Jua) (SIL Open Font License 1.1, `Assets/_Game/Fonts/OFL.txt`)
- 행성·UI 그림: 프로토타입의 캔버스 그리기 코드로 직접 생성 (`Tools/ArtGen`)
