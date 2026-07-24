# 다음 작업

## 진행 상태

- **완료** : Support/Growth 감쇠. `PlayerStat.Support`/`Growth`가 `CurrentPrice`처럼 턴을 넘어 유지되는 값이 되었고,
  매 턴 `TradeCalculator.Decay(stat)`로 감쇠한다. Job 선택/거래(Long·Short)는 발생하는 순간 `MarketManager.CurrentStat`에
  직접 반영된다. 별도 누적 데이터 클래스(`RuntimeTradeData`, `JobBoostData`)는 시도했다가 불필요해서 제거했다.
  (`Logging.md` "Support/Growth 감쇠 (Decay) — 최종 구조" 참고, 공식은 `Game_Formula.md` 3장 / 3-1장)
- **완료** : Skill 재사용 시스템. `SkillSO.isReusable` 플래그로 재사용형(Support/Growth 부스트, 사용 시 1회 반영 후
  재구매 필요) / 재사용 불가(토글형, 감쇠 없음)를 구분한다. (`Logging.md` "Skill 재사용 시스템" 참고)
- **완료** : 시사 이벤트(`EventCalculator`) 및 발행량(Supply)/Scarcity. 이벤트 발생 시 Support/Growth와 동일한
  "직접 반영 → 매 턴 감쇠" 패턴으로 Supply/가격까지 반영하도록 구현했다 (이후 "발행량 조작" 작업으로 Supply의
  소스가 이벤트 하나만은 아니게 확장됨, 아래 참고). `Doubt`도 이벤트가 건드리게 됐지만, Doubt는 Support/Growth와
  달리 **감쇠하지 않는 값**이라(시간이
  지날수록 자동으로 100을 향해 오르다 100이 되면 게임오버가 되는 기획) Job/Skill의 `DoubtDecrease`는 기존 "활성
  상태인 동안 매 턴 재적용" 방식 그대로 유지했다. 이벤트가 반영한 Doubt가 다음 턴에 사라지지 않도록
  `StatCalculator.Calculate()`가 Doubt를 턴 사이 이월하도록만 고쳤다. (`Logging.md` "Doubt(의심도)도 이벤트에
  포함" 참고, 공식은 `Game_Formula.md` 2장 / 3장 / 3-1장 / 3-2장 / 4장)
- **완료** : EventSO 도입. `EventCalculator`에 하드코딩돼 있던 등급 상수를 `EventSO` ScriptableObject로 뺐다.
  이벤트마다 `message`(로그 문구), `category`(`Positive`/`Negative`), `weight`(같은 카테고리 안 가중치 랜덤),
  `effects`(`List<EffectData>` — Support/Growth/Doubt 등 원하는 스탯만 부호 있는 값으로 지정, Job/Skill과 동일한
  구조 재사용), `supplyDelta`/`priceRatio`(부호 포함, 이벤트 전용 필드)를 갖는다. `EventCalculator.Calculate`는
  먼저 `PositiveEventRate`/`NegativeEventRate`로 카테고리를 정하고, 그 카테고리 안에서만 `weight` 가중치 랜덤으로
  하나를 뽑아 값을 그대로(부호 변환 없이) 적용한다. `MarketManager`가 `SkillManager.skillDatabase`와 동일한
  패턴으로 `[SerializeField] List<EventSO> eventDatabase`를 들고, 발생한 이벤트는 `MarketManager.EventLog`
  (`RuntimeEventData`/`EventLogEntry`, 날짜+SO 참조)에 기록된다. (`Logging.md` "EventSO 도입" 참고, 공식은
  `Game_Formula.md` 4장)
- **완료** : 발행량 조작 액션 + EffectType 직렬화 버그 수정. Long/Short와 동일한 구조로 `EventHub.OnManipulateSupply(amount)`
  → `TradeCalculator.ManipulateSupply`를 추가했다 (현금 비용 없음, Support/Growth는 Long/Short와 반대 부호,
  Doubt는 `amount`의 절대값에 비례해 증가). `추가발행권한` 스킬을 구매(`SkillManager.IsUnlocked`)하기 전에는
  `MarketManager`가 무시한다. 이 과정에서 `EffectType`에 `DoubtIncrease`를 **중간 삽입**해 `추가발행권한.asset`의
  `effectType: 5`(원래 `CashBonus`)가 `NegativeEventRate`로 조용히 바뀌어 있던 걸 발견해 수정했다 — enum은
  항상 끝에만 추가해야 한다는 원칙을 세웠다. `SupplyIncrease`/`SupplyDecrease`를 `EffectType`에 추가하고
  `StatCalculator.ApplySkillUse`가 재사용형 스킬 구매 시 이를 반영하도록 했다 (Job/토글형 스킬 매 턴 재적용
  루프에서는 Supply 관련 타입을 방어적으로 제외 — 감쇠와 충돌해 무한 증가하는 걸 막기 위해, Doubt 때와 동일한
  이유). (`Logging.md` "발행량 조작 기능 추가" 참고, 공식은 `Game_Formula.md` 3장 "발행량 조작")
- **남음** : 아래 참고.

---

## 후보 : EventSO 데이터 작성 + Inspector 연결

`EventSO`/`EventCategory`/`EventLogEntry` 클래스와 `MarketManager.eventDatabase` 배선은 끝났지만, 실제 이벤트
데이터(`.asset`)는 아직 하나도 없다 (Job의 `New Job.asset`처럼 플레이스홀더조차 없는 빈 리스트 상태). Unity
에디터에서 사람이 직접 만들어야 한다.

- `Create > Game > EventSO`로 이벤트 에셋을 만든다. 예시(스크린샷 기준):
  - "유명 스트리머가 코인을 소개했습니다!" — `category`=Positive, `effects`=[SupportIncrease +15, GrowthIncrease +10]
  - "금융당국이 코인 조사를 시작했습니다!" — `category`=Negative, `effects`=[DoubtIncrease +20, GrowthIncrease -15]
  - Positive/Negative 각각 최소 1개 이상 있어야 그 카테고리가 뽑혔을 때 실제로 이벤트가 발생한다 (없으면
    조용히 무시됨).
- `weight`/`supplyDelta`/`priceRatio` 값과 이벤트 가짓수는 기획자가 원하는 만큼 자유롭게 정하면 된다 (예전처럼
  Small/Medium/Large 3단계로 뭉뚱그릴 필요 없음 — 이벤트 하나하나가 각자 완결된 사건이다).
- 만든 에셋들을 `MarketManager`(씬의 `DontDestroyOnLoad` 오브젝트) Inspector의 `Event Database` 리스트에 드래그해야
  실제로 이벤트가 발생한다.
- 이 데이터가 갖춰지고 나면, 이후 새 이벤트를 추가/조정하는 건 코드 수정 없이 에셋 추가/값 조정만으로 가능해진다.

---

## 후보 : 2년 경과 후 자동 Doubt 상승 + Doubt 100 게임오버

기획 의도 : 게임 시간으로 2년이 지나면 자동으로 Doubt가 +20 이상 반영되고, 그 이후로는 매 턴 자동으로 Doubt가
계속 증가한다. `Doubt`가 100에 도달하면 게임오버다. 현재는 Doubt를 자동으로 증가시키는 로직도, `Doubt >= 100`
게임오버 판정 로직도 전혀 없다 (Job/Skill의 `DoubtDecrease`, 시사 이벤트만 존재 — 둘 다 감소 위주이거나 이벤트에
따라 증감).

- 트리거 조건을 턴 수로 환산해야 한다 (게임 시간 2년 = 몇 턴인지 `TimeManager`/`MarketManager` 기준으로 정의 필요).
- 2년 시점의 최초 +20(혹은 그 이상) 반영량과, 이후 매 턴 증가량을 확정해야 한다.
- `Doubt >= 100` 게임오버를 어디서 판정하고(`MarketManager.NextTurn()` 등) 어떻게 종료 처리할지(EventHub 이벤트
  추가 여부, UI 등) 정의해야 한다.
- `Doubt`는 감쇠하지 않으므로 이 자동 증가분도 그대로 누적되면 된다 (별도 감쇠 처리 불필요).

---

## 후보 : UI 연결

`EventHub`의 이벤트 대부분은 Manager 쪽 구독 로직만 갖춰져 있고, 이를 발행하는 실제 UI가 아직 없다.

- Long/Short 거래 버튼 (`EventHub.RaiseBuyCoin`/`RaiseSellCoin`)
- 발행량 조작 버튼 (`EventHub.RaiseManipulateSupply`) — `SkillManager.IsUnlocked(SkillID.추가발행권한)`가 false면
  버튼을 비활성화/숨김 처리해야 한다 (게임 로직은 이미 막고 있지만 UI에서도 표시해줘야 함)
- 스킬 아이콘/구매 버튼 (`EventHub.RaiseSkillClicked`/`RaiseSkillPurchased`)
- 직업 선택 화면 (`EventHub.RaiseJobSelected`)
- 시사 이벤트 수동 트리거가 필요한 경우의 UI (`EventHub.RaiseNewsEvent`) — 자동 발생은 이미 `MarketManager`에 구현됨
- 이벤트 로그 패널 — `MarketManager.EventLog`(`IReadOnlyList<EventLogEntry>`)를 순회하며 `Profile.message`/`Date`/
  `Profile.effects`를 표시. 데이터는 이미 쌓이고 있으니 UI만 그리면 된다.

`TimeUI`, `PlayerUI`, `SettingsUI`만 기존처럼 `TimeManager`/`PlayerManager`를 직접 참조하는 상태이고, 나머지는
설계는 끝났으나 화면이 없다.

## 후보 : Supply 스케일 재검토 (MaxSupply)

스크린샷 UI는 "현재 발행량 : 20,000,000개"를 보여주는데, `PriceCalculator.MaxSupply`는 코드상 `20000f`다
(1000배 차이). 실제 밸런스를 어느 스케일로 갈지에 따라 `MaxSupply`뿐 아니라 이벤트/스킬/발행량 조작 버튼의
Supply 변화량도 함께 다시 잡아야 할 수 있다.

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

## 후보 : EXIT 조건

`PROJECT_OVERVIEW.md`의 게임 목표는 "EXIT 조건을 달성하는 것"이라고 되어 있으나, `PlayerStat.ExitUnlocked`
(스킬 `EffectType.ExitUnlock`으로 켜짐) 외에 실제로 게임을 종료시키거나 결과를 정산하는 로직은 아직 없다.

- `ExitUnlocked`가 true일 때 플레이어가 취할 수 있는 행동(엑싯 버튼 등)을 정의해야 한다.
- 엑싯 시 최종 자산 정산/결과 화면 등 종료 처리를 정의해야 한다.
