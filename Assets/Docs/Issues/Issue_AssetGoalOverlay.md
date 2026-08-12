# 이슈 : 목표금액(엑시트 5억) 달성 알림 오버레이

작성일 : 2026-08-12

관련 구현 : `Next_Tesk.md` 23번 "3차 피드백 및 버그수정" UI개선3 항목. Notion
["3차 피드백 및 버그수정"](https://app.notion.com/p/3ba94f51302780c49627e562ce883275)에 사용자가 준 이미지
목업 2장(평상시/달성 시) 기준으로 구현.

## 관련 파일

- [PlayerManager.cs](../../Scripts/manager/PlayerManager.cs) — `GetTotalAsset()` 신규. 현금+보유 코인
  평가액(CashBonus% 포함)의 합. 헤더 "내 자산 합계" 표시 전용(아래 "트리거 기준" 참고, 달성 판정에는 안 씀).
- [MarketManager.cs](../../Scripts/manager/MarketManager.cs) — 수정 없음(기존 `CanExit` 그대로 재사용).
- [EventHub.cs](../../Scripts/manager/utils/EventHub.cs) — `OnAssetGoalAchieved` 이벤트 신규.
- [AssetGoalNotifier.cs](../../Scripts/manager/utils/AssetGoalNotifier.cs) — 신규. `CanExit`
  false→true 엣지 감지 후 `EventHub.RaiseAssetGoalAchieved()` 1회 발행.
- [AssetGoalNotificationUI.cs](../../Scripts/UI/MainModal/AssetGoalNotificationUI.cs) — 신규. 목업 배너
  표시/닫기(`EventNotificationUI`/`ModalPause` 패턴 재사용).
- [CoinPriceHeaderUI.cs](../../Scripts/UI/MainModal/CoinPriceHeaderUI.cs) — "내 자산 합계" 텍스트 색상을
  달성 여부에 따라 전환하는 로직 추가.
- [EventLogButton.cs](../../Scripts/UI/Utils/EventLogButton.cs) — `Outline` 상시 표시 + 폭죽형
  `UIBurstParticle` 버스트 추가.

## 작동 방식

**트리거 기준(현금 5억, 총자산 아님)** : 처음엔 "목표 금액 달성 여부는 내 자산합계(현금+코인평가액) 기준"이라는
요청을 그대로 받아 `MarketManager.IsAssetGoalReached`(총자산≥`TargetAsset`)를 별도 프로퍼티로 만들었다. 이후
사용자가 "엑시트 이벤트 트리거를 현금 5억 기준으로 바꾸고 기존 코드는 삭제"로 정정 — 실제 엑시트 버튼 조건과
동일하게 기존 `MarketManager.CanExit`(현금만 봄)을 그대로 재사용하도록 통일하고 `IsAssetGoalReached`는
삭제했다. 헤더의 "내 자산 합계" 표시(`GetTotalAsset()`)는 이 판정과 무관하게 그대로 유지 — 화면에 보이는
숫자는 총자산이지만, 달성 판정(배너/색상/버튼 강조)은 전부 현금 기준이다.

**엣지 감지 이유** : 배너는 "달성되는 순간 1회"만 떠야 한다(목업에 X 닫기 버튼이 있고, 상시 노출용 UI가
아님). `AssetGoalNotifier`가 매 `OnMarketUpdated`마다 이전 프레임의 `CanExit` 값을 캐시해뒀다가 false→true로
바뀌는 순간에만 이벤트를 발행한다 — 간격/구독 패턴은 `DoubtMidSfxController`와 동일. 매수/매도 직후에도
`TradeModalUI`가 `EventHub.RaiseMarketUpdated`를 호출하므로 턴 종료를 기다리지 않고 바로 감지된다.

**지속 효과는 엣지와 무관하게 매 프레임 재확인** : 헤더 텍스트 색상(`CoinPriceHeaderUI`)과 버튼 테두리
(`EventLogButton.achievedOutline`)는 `OnAssetGoalAchieved`를 구독하지 않고, 매 갱신/매 프레임 `CanExit`을
직접 읽어서 켜고 끈다. 그래야 1회성 알림 이벤트를 놓쳐도(씬 재진입 등) 항상 정확한 상태를 유지한다.

**VFX — 폭죽 연출** : 프로젝트에 파티클 프리팹/에셋이 없어(`Issue_VFXPhase1.md` 참고) `UIBurstParticle`
(코드로 uGUI Image 조각을 생성하는 간이 버스트)을 재사용했다. `UIBurstParticle.Spawn` 자체는 한 지점에서만
방사형으로 터지는데, 사용자가 "폭죽처럼 여기저기"를 요청해서 `EventLogButton.FireworksBurst()` 코루틴으로
감쌌다. 처음엔 버튼 주변 랜덤 오프셋(반경 70)에만 흩뿌렸는데, 사용자가 "메인 전체에 안 뜨고 버튼 근처에만
뜬다"고 지적해서 `ScreenShaker.Instance`(항상 `Main_Canvas` 자신에 붙는 싱글턴, `ScreenShaker.cs` 참고)의
RectTransform을 화면 전체 기준으로 재사용하도록 수정 — `ScreenShaker`가 씬에 없으면 기존처럼 버튼 주변
(`fireworksSpreadRadius`, 기본 반경 70)으로 폴백한다. `fireworksBurstCount`(기본 4)번을
`fireworksStaggerSeconds`(기본 0.08초) 시차를 두고 반복 스폰. 처음엔 달성 순간에 VFX 없이 사운드만 나가고
파티클은 `particleIntervalSeconds`(2초) 뒤에야 시작하는 공백이 있어서, `EventHub.OnAssetGoalAchieved`를
직접 구독해 즉시 1세트를 추가로 터뜨리도록 수정했다.

## 호출 스택

```
[턴 진행(NextTurn) 또는 매수/매도 확정(TradeModalUI)]
 └─ EventHub.RaiseMarketUpdated(stat)
     └─ AssetGoalNotifier.HandleMarketUpdated(stat)
         ├─ achieved = MarketManager.Instance.CanExit
         └─ achieved && !wasAchieved 일 때만:
             └─ EventHub.RaiseAssetGoalAchieved()
                 ├─ AssetGoalNotificationUI.HandleAssetGoalAchieved()
                 │   ├─ AudioManager.PlaySFX(openSfx)
                 │   └─ ModalPause.Open(panel) → EventHub.RaiseGamePaused() + panel.SetActive(true)
                 └─ EventLogButton.HandleAssetGoalAchieved()
                     └─ StartCoroutine(FireworksBurst()) — 랜덤 위치 4연발

[매 프레임]
 └─ EventLogButton.Update() → UpdateAchievedEffect()
     ├─ achievedOutline.enabled = MarketManager.Instance.CanExit
     └─ achieved 유지 상태로 particleIntervalSeconds 경과 시 → FireworksBurst() 재발동

[X 버튼 클릭]
 └─ AssetGoalNotificationUI.Close() → ModalPause.Close(panel)
```

## 씬 구조 / 남은 작업

**배너가 안 뜬다는 사용자 보고로 확인해보니(2026-08-12)** Unity MCP로 씬을 조회한 결과 `AssetGoalNotifier`/
`AssetGoalNotificationUI` 둘 다 씬에 없었다 — `EventLogButton`(`/Main_Canvas/EventLogBtn`)은 이미 씬에
있어서 테두리/폭죽(매 프레임 `CanExit` 직접 폴링, 이벤트 구독 불필요)은 동작했지만, 배너는
`EventHub.OnAssetGoalAchieved` 구독 전용이라 그 이벤트를 발행하는 `AssetGoalNotifier`가 없으면 아예 못
뜬다. `AssetGoalNotifier`는 비주얼이 없는 순수 로직 컴포넌트라(`DoubtMidSfxController.cs`가 이전에 같은
방식으로 처리된 선례, `Issue_DoubtMidSfx.md` 참고) Unity MCP로 바로 `/GameStarter`(`DoubtMidSfxController`와
같은 자리)에 추가하고 씬 저장까지 완료했다.

**"니가 연결해봐"라는 요청으로(2026-08-12) 나머지도 전부 Unity MCP로 직접 배치·연결했다** — 평소엔 UI
GameObject를 사용자가 목업 기준으로 직접 만드는 방식이지만, 이번엔 명시적으로 대행 요청을 받았다.

- `AssetGoalBanner`(`/Main_Canvas/AssetGoalBanner`, 항상 활성, `AssetGoalNotificationUI` 부착) 생성.
  그 밑에 `BannerRoot`(기본 비활성, `panel` 필드로 연결) → `Shadow2`/`Shadow1`/`FrontPanel` 3겹 골드
  패널(각각 `Nobi.UiRoundedCorners.ImageWithRoundedCorners` radius 16, 뒤로 갈수록 어둡고 (32,-32)/(16,-16)
  오프셋 — 목업의 "쌓인 카드" 느낌 재현) 구조로 만들었다. `FrontPanel` 밑에 `MessageText`(NeoDunggeunmo SDF
  Bold 34pt, 검정, "엑시트 목표 금액을 달성하였습니다...!")와 `CloseBtn`(`EventNotificationUI`의 `취소버튼.png`
  + 진회색 틴트 그대로 재사용, `closeButton` 필드로 연결)을 배치했다. 좌표는 사용자가 준 목업 스크린샷을
  1512×982 기준으로 보고, `Issue_EventNotification.md`가 썼던 것과 동일한 변환식(캔버스 실제 크기
  1821.47×1138.42에 맞춰 scaleX 1.2048/scaleY 1.1593 적용)으로 눈대중 계산했다 — `BannerRoot` anchoredPosition
  (-10, 450), sizeDelta (1300, 140).
- `EventLogButton`(`/Main_Canvas/EventLogBtn`)에 `Outline` 컴포넌트 추가(색 (1, 0.82, 0.25), distance
  (4,-4)) 후 `achievedOutline`에 연결, `achievedParticleColor`도 동일한 골드로 맞춤.
- `CoinPriceHeaderUI.achievedColor`도 동일한 골드로 맞춰 배너/버튼/헤더 텍스트 색이 하나로 통일되게 함.
- Scene 뷰 캡처(`capture_scene_view`, 임시로 `BannerRoot` 활성화 후 확인, 확인 뒤 다시 비활성화)로 배치를
  검증했다 — 목업과 비슷하게 나옴(코인 헤더 좌측 일부와 이벤트 로그 아이콘 위를 덮으며 겹침, 텍스트 가독성
  양호). Game 뷰는 기존에 알려진 환경 이슈(부트스트랩 싱글턴 없이 `SampleScene` 단독 진입 시 빈 화면)로
  여전히 확인 불가 — 실제 Play 확인은 못 함.

## 알려진 이슈 / 주의점

- **배너 자체엔 VFX 없음** : 폭죽 연출은 `EventLogButton`(테두리+파티클)에만 적용했다. 배너가 뜨는 순간은
  효과음(`openSfx`)만 재생되고 별도 버스트는 없다 — 필요하면 추가 요청 시 반영.
- **Play 모드 검증은 여전히 못 함** : 씬 배치는 끝났고 Scene 뷰 캡처로 배너 레이아웃은 확인했지만,
  `EventHub.OnAssetGoalAchieved` 발행 → 배너/폭죽/색상 전환이 실제로 이어지는 흐름과 애니메이션 타이밍은
  Play 모드로 실제 플레이(또는 `PlayerManager.currentMoney`를 5억 이상으로 만들어 트리거)해야 확인된다.
- **트리거 기준 변경 이력** : "총자산 기준" → "현금 5억 기준(`CanExit`)"으로 한 번 뒤집힌 결정이다. 나중에
  다시 총자산 기준으로 바꾸고 싶으면 `PlayerManager.GetTotalAsset()`은 이미 있으니
  `AssetGoalNotifier`/`CoinPriceHeaderUI`/`EventLogButton` 3곳의 `CanExit` 참조만 바꾸면 된다.
- **폭죽 파라미터는 임시값** : `fireworksBurstCount`(4)/`fireworksSpreadRadius`(70)/`fireworksStaggerSeconds`
  (0.08초)/`particleIntervalSeconds`(2초) 전부 체감 없이 잡은 값 — 실제 플레이 후 조정 가능.
