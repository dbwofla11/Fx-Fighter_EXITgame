# 다음 작업

완료된 작업 목록은 `Completed_Tasks.md`로 분리했다. 이 문서에는 아직 안 된 것(후보/버그)만 남긴다.

## 다음 세션 작업 후보 (요약)

| # | 작업 | 비고 |
|---|---|---|
| 7 | 밸런스 수치 조정 | 캔들/확률 등, 실제 플레이 후 — 이번 세션에 손댄 발행량 조작 Support/Growth 가중치(1/5), Doubt 가중치(1.5배), 정기소각/반감기 소각량(1500/3000, 미변경), 부정 이벤트 5종 수치 절반 조정 + 신규 이벤트 3종, 코인발행 최대치(100,000)도 전부 임시 체감값이라 같이 재검토 |
| 16 | 한글 텍스트 흐림 — `NeoDunggeunmo SDF` 재생성 | 원인 파악 완료(아래 상세 섹션), 사용자가 "나중에"로 보류 — 착수 전 재확인 필요 |
| 20 | 연 단위 목표금액 오버레이 | 미착수(신규). 1년 단위(턴 환산)마다 목표금액(엑시트 조건)을 알려주는 오버레이가 뜨게 만들기. UI부터 먼저 설계하고 나서 착수하기로 함 |
| 21 | 오프닝 대화(스토리) UI 작업 | 미착수(신규), **다음 세션 우선 착수**(2026-08-10, 20번보다 먼저). 스토리 6컷 확정 — Notion ["오프닝 대화 스크립트 (6컷, 확정)"](https://app.notion.com/p/3b894f5130278102a020eae417c8271d) 참고. 구조 : 어린 시절 사건(엄마 코인 사기 피해)이 성인이 된 주인공의 행동 원인이 되는 인과 구조, 톤은 계속 가볍게·대사는 짧게, "엄마를 위해 번다"는 구원 서사 대사는 넣지 않기로 확정(뒤틀린 결심 유지). 목표금액 5억이 1컷의 "엄마가 잃은 돈"과 6컷의 "게임 목표 금액"(`MarketManager.TargetAsset`=5억, 이미 일치 확인함)으로 이중 의미를 가짐. 사용자가 이미지 목업(대사 컷)을 직접 전달하면 그때 UI 작업 시작 — Figma 없이 진행. `StoryDialogueController`+`TypewriterText.cs` 재사용(22번에서 `JobSO.openingBeats` 읽도록 이미 준비됨). 오프닝을 어디서 트리거할지(`GameSceneManager`에 아직 Opening 씬 없음, CharacterSelect 이후 vs MainGame 진입 시 오버레이)는 목업 받고 나서 결정. 6컷이라 배경 이미지도 6장(또는 그 이상, 재사용 여부에 따라) 필요. **필수** — 플레이어들이 이 게임을 코인 사기 게임이 아니라 주식 게임으로 오해하고 있어서 앞부분 스토리로 정정해야 한다는 피드백 (튜토리얼 쪽 "엑시트 목표 이유 설명"은 튜토리얼 작업 완료로 처리됨 — `Completed_Tasks.md` 참고) |
| 23 | 3차 피드백 및 버그수정 처리 | 진행 중(2026-08-12). Notion ["3차 피드백 및 버그수정"](https://app.notion.com/p/3ba94f51302780c49627e562ce883275) 기준, 완료 항목은 페이지에 취소선(~~) 표시해둠. (2차 피드백 ①③은 완료돼 `Completed_Tasks.md`로 이관, ②(엑시트 당위성/빚 스토리 — 엄마 5억 빚·비행기 도피·매년 이자 오버로드)는 여기로 이어짐: 20번 목표금액 오버레이·21번 오프닝 스토리와 내용 겹치고, 21번은 사용자가 이미지 목업 줄 때까지 대기 중이라 같이 대기. 아래 "미착수"의 초기 설명/엑시트 당위성 항목이 이 내용.) **튜토리얼 및 정보 전달 완료** — 의심도(`DoubtScorePanel`)에도 `HoverTooltipTrigger` 추가(+Image `raycastTarget` on)해 지지도/상승률/발행량과 동일한 4개 스탯 호버 툴팁 완성. **버그사항 3건 완료** — ① `DoubtMidSfxController.StopAfterDuration`이 `WaitForSeconds`(scaled)를 써서 모달 중(timeScale=0)엔 SFX 전체 재생, 배속 중(timeScale>1)엔 3초보다 일찍 끊기던 문제를 `WaitForSecondsRealtime`으로 수정. ② `ButtonPressEffect`가 눌림 시 `localScale`을 줄이는 바람에 클릭 판정용 히트박스도 같이 줄어 가장자리 클릭이 무시되던 문제 — `Graphic.raycastPadding`을 축소 비율만큼 음수로 넣어 보정, `extraHitboxPadding`(기본 10) 여유분도 추가해 시각 크기보다 히트박스가 항상 더 크게 잡히게 함(공유 컴포넌트라 다른 버튼들도 같이 개선됨). ③ `CoinPriceHeaderUI` 총자산 계산이 CashBonus%를 평가손익(profit)분에만 곱해 실제 매도(`PlayerManager.HandleSellCoin`, 코인 평가액 전체에 곱함) 결과보다 작게 표시되던 문제 — 매도 공식과 동일하게 통일. **밸런스 개선 완료** — `TradeCalculator.Short`에 매도 전용 `SellPenaltyMultiplier`(1.5x)를 추가해 매도 시 지지도/상승률이 매수보다 더 크게 떨어지게 함(매수 `Long`은 그대로 유지). Support/Growth는 `ProbabilityCalculator.UpProbability`에 직결되므로 가격 하락 확률도 같이 늘어남 — 별도 가격 로직은 손대지 않음. 1.5x는 체감 없이 잡은 임시값, 7번(밸런스 수치 조정) 재검토 대상에 포함. **UI개선3 중 2건 완료** — ① `EventEffectFormatter.DescribeEffect`가 `PriceShockPercent` 라벨을 "코인 가격(즉시 %)"에서 "코인 가격"으로 바꾸고 숫자 뒤에 suffix("%")를 붙여 "코인 가격 +25%" 형태로 표시. ③ 처음엔 "PreviewText 왼쪽에 DoubtIncreaseText를 재배치"로 잘못 이해해 정렬/위치를 바꿨었으나, 사용자 정정으로 되돌리고(`PreviewText` Center 정렬 복구, `DoubtIncreaseText` Center 정렬 + x=460 위치 복구) 대신 **새 `TradeCoinCountText`(TextMeshProUGUI)를 `PreviewText` 박스 바로 왼쪽(x=-460, size 340x50, 폰트 20)에 추가** — `TradeModalUI.tradeCoinCountText` 필드로 연결, `RefreshPreviewAndConfirmState()`에서 "코인 수: N개"로 갱신. Play 모드 밖에서는 Canvas가 렌더링 안 되는 환경 문제로 스크린샷 확인은 못 했음 — 실제 플레이 확인 필요. **② 목표금액 달성 표시 완료(2026-08-12)** — 사용자가 이미지 목업(평상시/달성 시 2장) 전달, 목업 그대로 구현. `PlayerManager.GetTotalAsset()`(현금+코인평가액, CashBonus% 포함) 신설 — 헤더 "내 자산 합계" 표시 전용, `CoinPriceHeaderUI`에 중복돼 있던 계산식을 여기로 통합. 오버레이 트리거는 처음엔 총자산 기준 `MarketManager.IsAssetGoalReached`를 별도로 뒀었으나, 사용자가 "엑시트 이벤트 트리거를 현금 5억 기준으로, 기존 코드는 삭제"로 정정해 해당 프로퍼티를 삭제하고 기존 `CanExit`(현금만 봄, EXIT 버튼과 동일 조건)를 그대로 재사용하도록 통일함. `AssetGoalNotifier`(신규, `manager/utils/`)가 `OnMarketUpdated`를 구독해 `CanExit`가 false→true로 바뀌는 달성 순간만 감지, `EventHub.OnAssetGoalAchieved` 1회 발행. `AssetGoalNotificationUI`(신규, `UI/MainModal/`)가 이를 구독해 목업의 배너(X로 닫기, 이후 재표시 안 함)를 표시 — `EventNotificationUI`/`ModalPause` 패턴 재사용. `CoinPriceHeaderUI`의 "내 자산 합계" 텍스트는 달성 후 계속 `achievedColor`(기본 노랑, Inspector에서 목업대로 조정)로 표시. `EventLogButton`은 달성 후 계속 `Outline`(SkillPanelUI와 동일 패턴) 표시 + `EventHub.OnAssetGoalAchieved` 수신 즉시, 이후 `particleIntervalSeconds`(기본 2초)마다 `FireworksBurst()` 코루틴 재생 — `UIBurstParticle.Spawn`(VFX 1차 작업과 동일한 코드 생성 버스트, 파티클 애셋 없음) 자체는 한 점에서만 방사돼서, 사용자 요청("폭죽처럼 여기저기")대로 버튼 주변 랜덤 위치(`fireworksSpreadRadius`, 기본 70) `fireworksBurstCount`(기본 4)번을 `fireworksStaggerSeconds`(기본 0.08초) 시차로 반복 호출하도록 감쌈. 배너/버튼 GameObject 배치와 각 스크립트 SerializeField 연결(panel/closeButton/achievedOutline 등)은 사용자가 목업대로 직접 진행 예정 — 아직 씬에 배치 전. 상세 작동 방식/호출 스택/씬 배치 체크리스트는 `Issue_AssetGoalOverlay.md` 참고. **미착수** — 초기 설명/엑시트 당위성, 추가사항(이벤트 선택권), 빌드 연구(Mac/Linux). |
(각 항목의 자세한 내용은 아래 섹션 및 `Completed_Tasks.md`/`Logging.md` 참고.)

---

## 후보 : 밸런스 수치 조정 (실제 플레이 후)

- `PriceChartUI.visibleCandleCount`(기본 16)/`candleWidthRatio`(0.95) 등 캔들 차트 관련 수치
- `ProbabilityCalculator.SupportWeight`/`GrowthWeight`/`DoubtWeight`(현재 모두 0.25) 등 상승확률 가중치
- 발행량/거래량 스킬 수치(`Completed_Tasks.md` "발행량 스킬 3종 Supply 효과 부여" 참고) — 억제율 20%/15%,
  `PriceCalculator.VolumeDeltaWeight`(0.01), 정기소각/반감기 소각량(1500/3000) 전부 임시값, Play 모드
  검증은 끝났으니 실제 플레이하며 체감 밸런스만 조정하면 됨
- `TradeCalculator.ManipulateSupply` 가중치 — 이번 세션에 `SupportWeightPerCoin`/`GrowthWeightPerCoin`을
  0.1→0.02(1/5), `DoubtWeightPerSupplyUnit`을 0.01→0.015(1.5배)로 조정. `CoinControlModalUI.MaxAdjustAmount`도
  2000→100,000으로 올림 — 전부 실제 플레이 체감으로 재검토 필요
- 부정 이벤트 수치 — 기존 5종(해킹_사고/소규모_해킹_사고/당국_조사/소규모_당국_조사/인플루언서_폭로) 전부
  절반으로 낮췄고, 신규 3종(전면_거래_금지→"모든 대형 거래소가 상장폐지를 선언했습니다!", 핵심_개발자_이탈,
  가짜뉴스_확산) 추가함. 전면_거래_금지는 의도적으로 다른 이벤트보다 훨씬 크게 잡은 값(weight 0.15로 희귀하게)이라
  실제 플레이하며 밸런스 확인 필요
- 스킬 가격 — 전체 스킬 `baseCost`를 1.5배로 일괄 인상, `로비`만 예외로 2배 인상(2026-08-07). 임시 조정값이라
  실제 플레이 후 재검토 필요
- 시장조작 9종 전체 재조정(2026-08-12, `Issue_PriceShockSkills.md` 참고) — baseCost 50,000~200,000,
  Doubt·Support/Growth·Volume 대부분 구 수치의 1.3~2배, `펌핑`/`허수매수벽`/`허수매도벽` 3종에 가격 즉시
  변동 추가. `펌핑`(180,000)은 `허수매수벽`(100,000)의 Support/Growth/Price를 전부 웃도는 상위호환으로
  설계함. 전부 체감 없이 잡은 임시값이라 실제 플레이 후 재검토 필요
- "의심도 하락"(`DoubtDecline`, 여론조작 스킬 3종 전용) 분할 기간 — 세션 중 50→100→200턴(0.5%씩)으로 여러 번
  조정됨(`BuffCalculator.cs`), 최종값도 임시. `Issue_DoubtDecline.md` 참고
- 그 외 이벤트/스킬 수치 등도 실제 플레이 데이터가 쌓이면 같이 재검토

## 후보 : 한글 텍스트 흐림 — `NeoDunggeunmo SDF` 폰트 재생성 (2026-08-08 원인 파악, 보류)

**증상** : "한글 텍스트 전반적으로 흐릿하다"는 피드백. 스킬 패널을 Play 모드에서 열어 확대 캡처해보니
숫자/영문("10,000", "+7.5")이나 픽셀아트 아이콘은 또렷한데 **한글만** 흐림.

**원인 파악 완료** : `Assets/Fonts/NeoDunggeunmo/NeoDunggeunmo SDF.asset`(Dynamic TMP Font Asset, 한글
폴백용으로 지난 세션에 추가)이 생성될 때 샘플링 해상도가 너무 낮게 잡혔다.

| | NeoDunggeunmo SDF (한글) | LiberationSans SDF (영문/숫자, 기본폰트) |
|---|---|---|
| Sampling Point Size | 32 | 86 |
| Atlas Padding | 2 | 9 |
| Atlas 크기 | 1024×1024 | 1024×1024 |

한글은 획수가 많아 라틴 문자보다 높은 해상도가 필요한데 오히려 기본 폰트보다도 낮은 32pt로 구워져서(같은
1024×1024 아틀라스에 수백 개 글리프를 욱여넣다 보니) 실제 화면 크기로 그려질 때 뭉개짐. 카메라/렌더링
쪽은 확인 결과 원인이 아님으로 배제함 — `Main_Canvas.Canvas.renderMode`가 Screen Space - Overlay라
카메라를 아예 거치지 않고, 카메라의 MSAA/포스트프로세싱도 꺼져있어 애초에 관여할 여지가 없음.

**해결 방향(미착수)** : `NeoDunggeunmo SDF.asset`을 Sampling Point Size ~72~90, Atlas Padding ~7~9,
필요시 Atlas 2048×2048로 재생성(LiberationSans 수준으로 맞춤) — 같은 경로/GUID를 유지해야 기존 참조
(`TMP_Settings.fallbackFontAssets`, `LiberationSans SDF.asset`의 fallbackFontAssetTable)가 안 깨짐.
체감 소요시간 20~40분 예상, 가장 큰 변수는 TMP 폰트 생성 API가 버전마다 시그니처가 달라 스크립트로 한
번에 안 될 수 있다는 점. 프로젝트 전역 한글 폴백으로 걸려있어 영향 범위가 넓으므로 착수 전 재확인 필요.

## 후보 : 직업(Job) 프로필 — 더미 수치 재조정 필요

Figma `EjUw2LdqxAYhL2180OAXHo` node `1202:186`("초반 캐릭터 선택")을 확인해 `Assets/Profile/
직업_프로파일/`에 목업과 동일한 이름의 직업 6종(일반인/기업인/유튜버/개발자/연예인/정치인, 아이콘도
`Assets/Sprites/직업아이콘/UI_일반인아이콘1~6` 순서로 매칭해 연결함)을 만들어뒀다. 단 **효과/수치는
대부분 더미(placeholder)**.

- 2026-08-08 피드백으로 효과/자금/코인이 두 차례 더 갈아엎어졌다 — Figma 원안("일반인" 특성 2개 등)은
  더 이상 유효하지 않다. 2차 조정(자금 9~12천원대)은 스킬 가격(₩15,000~1,500,000)에 비해 터무니없이
  작다는 피드백을 받아 3차로 스킬 가격 스케일에 맞춰 다시 잡았다. **현재 상태(전부 여전히 더미/가안,
  실제 기획 확정 전까지 언제든 재조정 가능)** — `Completed_Tasks.md` "직업별 밸런스 3차 조정" 참고:

  | 직업 | 스탯 효과 | 초기 자금 | 초기 코인 |
  |---|---|---|---|
  | 일반인 | 없음(기준 직업) | ₩40,000 | 10,000개 |
  | 기업인 | 거래수익+10%, 의심도+25 | ₩2,000,000(2026-08-08 의심도 밸런스 패치로 조정) | 3,000개(동일) |
  | 유튜버 | 지지도+15 | ₩45,000 | 11,000개 |
  | 개발자 | 상승도+15 | ₩45,000 | 15,000개 |
  | 연예인 | 지지도+20 | ₩35,000 | 12,000개 |
  | 정치인 | 의심도-10 | ₩50,000 | 7,000개 |

  자금 스케일 기준 : `Assets/Profile/스킬_프로파일/`의 `baseCost`가 코인설계/시장조작/여론조작은
  ₩15,000~180,000, 방어(법무팀운영 등 7종)는 ₩750,000~1,500,000(로비만 예외로 ₩120,000)로 완전히 다른
  두 티어다. 일반 직업(40,000~50,000)은 저가~중가 스킬(15,000~75,000대) 1~2개는 시작부터 살 수 있게,
  기업인(1,000,000)은 방어 최고가 스킬까지 즉시 살 수 있게 잡았다. 코인 수량 차이는
  `MarketManager.ResetState()`가 `Supply = InitialSupply(2000) + currentCoins`로 시작 발행량을 잡기
  때문에 게임 시작 시점 Supply/Scarcity에도 그대로 영향을 준다는 점 감안해서 정할 것. **실제 기획이
  나오면 전부 다시 정리해야 한다.**

캐릭터 선택 화면 자체(씬 분리 + 데이터 연결)는 완료 — `Completed_Tasks.md` "캐릭터 선택 씬 분리" 항목
참고. 여기 남은 건 순전히 수치/기획 데이터 문제다.

