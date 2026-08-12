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
감쌌다 — 버튼 주변 랜덤 오프셋(`fireworksSpreadRadius`, 기본 반경 70)에 `fireworksBurstCount`(기본 4)번을
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

이번 세션은 스크립트만 작성했고 **씬에는 아직 아무것도 배치하지 않았다** — 이 프로젝트는 UI GameObject를
사용자가 Figma/목업 기준으로 직접 만들어 스크립트 필드에 연결하는 방식으로 진행 중이라, 아래는 사용자가
씬에서 해야 할 일이다.

- `AssetGoalNotifier` 컴포넌트를 아무 상시 존재하는 오브젝트(예: `DoubtMidSfxController`가 붙어있는
  `/GameStarter`)에 추가.
- `AssetGoalNotificationUI` 오브젝트(항상 활성 유지) + 그 밑에 목업 배너 `panel`(기본 비활성) 배치, X 버튼을
  `closeButton`에 연결, 배너 텍스트("엑시트 목표 금액을 달성하였습니다...!")는 목업 그대로 정적 텍스트로 배치.
- `EventLogButton`에 `Outline` 컴포넌트 추가 후 `achievedOutline`에 연결, `achievedParticleColor`(기본
  노랑)/`achievedOutline` 색상을 목업에 맞게 조정.
- `CoinPriceHeaderUI.achievedColor`(기본 노랑)를 목업 색상에 맞게 Inspector에서 조정.

## 알려진 이슈 / 주의점

- **배너 자체엔 VFX 없음** : 폭죽 연출은 `EventLogButton`(테두리+파티클)에만 적용했다. 배너가 뜨는 순간은
  효과음(`openSfx`)만 재생되고 별도 버스트는 없다 — 필요하면 추가 요청 시 반영.
- **Play 모드 검증 못 함** : 씬 배치가 안 끝나서 실제 동작(배너 위치/타이밍, 폭죽 좌표, 색상)은 코드 리뷰
  수준으로만 확인했다. 사용자가 씬 배치를 마친 뒤 실제 플레이로 확인 필요.
- **트리거 기준 변경 이력** : "총자산 기준" → "현금 5억 기준(`CanExit`)"으로 한 번 뒤집힌 결정이다. 나중에
  다시 총자산 기준으로 바꾸고 싶으면 `PlayerManager.GetTotalAsset()`은 이미 있으니
  `AssetGoalNotifier`/`CoinPriceHeaderUI`/`EventLogButton` 3곳의 `CanExit` 참조만 바꾸면 된다.
- **폭죽 파라미터는 임시값** : `fireworksBurstCount`(4)/`fireworksSpreadRadius`(70)/`fireworksStaggerSeconds`
  (0.08초)/`particleIntervalSeconds`(2초) 전부 체감 없이 잡은 값 — 실제 플레이 후 조정 가능.
