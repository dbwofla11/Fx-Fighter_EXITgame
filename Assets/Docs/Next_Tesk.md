# 다음 작업

완료된 작업 목록은 `Completed_Tasks.md`로 분리했다. 이 문서에는 아직 안 된 것(후보/버그)만 남긴다.

## 다음 세션 작업 후보 (요약)

| # | 작업 | 비고 |
|---|---|---|
| 2 | 엑시트 버튼 UI | CanExit(현금≥10억) 활성화 조건 + 목표금액 진행률 표시 |
| 3 | 엔딩 결과 화면 | 4종(체포/엑시트/영웅/거지) 결과 문구, `Game_Formula.md` 5장 참고 |
| 4 | 스킬 아이콘/구매 버튼 UI | `RaiseSkillClicked`/`RaiseSkillPurchased` 발행 UI 없음 |
| 5 | 직업 선택 화면 UI | `RaiseJobSelected` 발행 UI 없음 |
| 6 | 발행량 스킬 3종 Supply 효과 부여 | 설계 결정 필요 (수치 없이 코드부터 짜기 애매함) |
| 7 | 밸런스 수치 조정 | 캔들/확률 등, 실제 플레이 후 |
| 8 | 스트리머 패널 스프라이트 연결 | 블로킹: 스프라이트 에셋 미도착 |

(2번은 "후보 : UI 연결"의 "개요 탭 콘텐츠" 항목과 같은 작업임 — 아래 상세 참고. 각 항목의 자세한 내용은
아래 섹션 및 `Completed_Tasks.md`/`Logging.md` 참고.)

---

## 후보 : UI 연결

`EventHub`의 이벤트 대부분은 Manager 쪽 구독 로직만 갖춰져 있고, 이를 발행하는 실제 UI가 아직 없다.

- 스킬 아이콘/구매 버튼 (`EventHub.RaiseSkillClicked`/`RaiseSkillPurchased`)
- 직업 선택 화면 (`EventHub.RaiseJobSelected`)
- 시사 이벤트 수동 트리거가 필요한 경우의 UI (`EventHub.RaiseNewsEvent`) — 자동 발생은 이미 `MarketManager`에 구현됨
- ~~이벤트 로그 패널(커뮤니티 탭)~~ — **완료** (`Completed_Tasks.md` 참고). Figma가 프레임 2개로 나뉘어 있다는
  걸 뒤늦게 확인했다 — node `1253:2`("뉴스,이벤트 페이지 - 스탯개요")는 "개요" 탭, node `1261:195`("뉴스,이벤트
  페이지 - 이벤트 패널")는 "커뮤니티" 탭 콘텐츠였다. 처음엔 반대로(이벤트 로그를 "개요" 탭에) 연결했다가
  수정했다. "개요" 탭은 아래 항목이 아직 없어 자리만 잡아두고 비워둠.
- 개요 탭 콘텐츠(스탯개요 + 엑시트 버튼, Figma node `1253:2`) — 기존에 "엑시트 버튼"으로 따로 있던 후보가
  이 탭 콘텐츠와 같은 화면이었음을 확인해 합쳤다. `EventLogPanel/EventPanelBox`와 동일한 자리(`ContentArea`
  안, `overviewTab` 선택 시)에 새로 만들면 됨.
  - 좌측 : 현재 스탯(코인 지지도/상승도/의심도 현재값), Job+Skill 보너스 상승률(`PlayerStat.
    JobSkillSupportBonus`/`JobSkillGrowthBonus`, Doubt는 `JobManager.CurrentJob.effects` +
    `SkillManager.GetActiveSkills()` 합산 — "개요 화면 Job+Skill 보너스 표시" 완료 항목에서 이미 데이터
    준비됨), 긍정/부정 이벤트 확률, 현금 증가량(CashBonus) 표시.
  - 우측(흰 박스, `EventPanelBox`와 동일 크기/위치) : 목표금액(`MarketManager.TargetAsset`)/현재금액
    (`PlayerManager.currentMoney`)/목표까지 남은 금액, 그리고 엑시트 버튼
    (`EventHub.RaiseExitRequested`) — `MarketManager.CanExit`(현금 >= `TargetAsset`)가 true일 때만 누를 수
    있도록 활성화 처리.
- 엔딩 결과 화면 — `EventHub.OnGameEnded(EndingType)`을 구독해 4종 엔딩(체포/엑시트/영웅/거지)에 맞는 결과 문구를
  표시. 각 엔딩의 설명 텍스트는 `Game_Formula.md` 5장에 정리되어 있음.

`TimeUI`, `PlayerUI`, `SettingsUI`만 기존처럼 `TimeManager`/`PlayerManager`를 직접 참조하는 상태이고, 나머지는
설계는 끝났으나 화면이 없다.

- 스트리머 패널의 가격 반응(스프라이트 전환/멘트) — `PlayerStat.StreamerReaction`(5단계)을 이미 읽을 수 있음.
  `Assets/Sprites/스트리머상태` 폴더에 실제 스프라이트가 들어오면 UI 팀원이 연결하면 된다. 임계값(50/10) 밸런스는
  실제 플레이 후 조정 필요할 수 있음.

## 후보 : 발행량 관련 스킬 3종에 실제 Supply 효과 부여

`추가발행권한`/`우회발행권한`/`발행량은폐` 세 스킬은 이름과 설명(예: "위기 상황에서 자금을 빠르게 마련",
"발행 사실이 드러나더라도 의심을 최소화")으로 미루어 보면 발행량과 강하게 연관되어 있지만, 지금 `.asset`
데이터에는 `SupplyIncrease`/`SupplyDecrease` 효과가 하나도 없다 (`추가발행권한`은 발행량 조작 버튼의 해금
조건 역할만 하고 있음). 아래를 정해야 한다.

- `추가발행권한`/`우회발행권한`을 구매하는 순간에도 (버튼 해금과 별개로) Supply를 직접 늘리는 1회성 효과를
  줄 것인지, 아니면 순수하게 "버튼 해금 + 기존 효과(Growth/CashBonus/DoubtDecrease)"만으로 끝낼 것인지.
- `발행량은폐`는 설명상 Supply 자체보다는 "정보를 숨긴다"는 쪽이라 `DoubtDecrease`만으로 충분해 보이는데,
  이대로 유지할지 확인 필요.
- 만약 Supply 효과를 추가한다면 구체적 수치도 함께 정해야 한다.

## 후보 : 밸런스 수치 조정 (실제 플레이 후)

- `PriceChartUI.visibleCandleCount`(기본 16)/`candleWidthRatio`(0.95) 등 캔들 차트 관련 수치
- `ProbabilityCalculator.SupportWeight`/`GrowthWeight`/`DoubtWeight`(현재 모두 0.25) 등 상승확률 가중치
- 그 외 이벤트/스킬 수치 등도 실제 플레이 데이터가 쌓이면 같이 재검토


