# 이슈 : 스킬 패널 구매 흐름(재사용형/1회성) 작동 방식 / 호출 스택 정리

작성일 : 2026-08-03

관련 구현 : `Next_Tesk.md` "스킬 아이콘/구매 버튼" 항목. 이번 세션에서 버그 3건(1회성 스킬 구매 잠금 미구현,
구매 시 게이지 미갱신, 스킬로 인한 의심도 감소가 0 밑으로 내려감)을 고치면서 재사용형/1회성 스킬 구매 흐름
전체를 다시 정리함.

## 관련 파일

- [SkillPanelUI.cs](../../Scripts/UI/SkillModal/SkillPanelUI.cs) — 스킬 패널 UI. 아이콘 클릭/구매 버튼 클릭은
  `EventHub`만 호출하고, 상태 조회(선택된 스킬/잠금 여부/비용)는 `SkillManager`를 직접 읽는다.
- [SkillManager.cs](../../Scripts/manager/SkillManager.cs) — 스킬 런타임 상태(`IsUnlocked`/`IsEnabled`/
  `PurchaseCount`) 및 구매 처리 담당.
- [SkillSO.cs](../../Scripts/SOs/SkillSo.cs) — `isReusable` 플래그로 재사용형/1회성을 구분하는 정적 데이터.
- [StatCalculator.cs](../../Scripts/Runtimes/Systems/StatCalculator.cs) — 구매된 스킬의 효과를 `PlayerStat`에
  반영(`ApplySkillUse`).
- [EventHub.cs](../../Scripts/manager/utils/EventHub.cs) — `OnSkillClicked`/`OnSkillPurchased`/
  `OnMarketUpdated` 중계.
- [MintButtonUI.cs](../../Scripts/UI/Utils/MintButtonUI.cs) — `추가발행권한` 스킬의 `IsUnlocked` 여부로 발행량
  조작 버튼 잠금을 거는 소비자 예시.
- `Assets/Scripts/Profile/스킬_프로파일/*.asset` (6종) — 전부 `isReusable: 0`으로 설정됨 (원래 필드 자체가
  직렬화 안 돼 있어서 C# 기본값 `true`로 떨어지던 게 버그의 시작이었음).

## 작동 방식

**분류** : `SkillSO.isReusable`이 재사용형(`true`, Support/Growth 부스트형 — 클릭할 때마다 비용을 내고 즉시
반영, 잠기지 않고 비용이 구매 횟수에 따라 상승)과 1회성(`false`, 권한/행동 획득형 — 최초 구매 이후 영구
해금되고 재구매 불가)을 가른다. 현재 6개 스킬(추가발행권한/우회발행권한/독약조항/지갑분산/발행량은폐/락업)은
전부 1회성이다.

**선택 → 구매 2단계** : 아이콘 클릭은 재사용형/1회성 구분 없이 항상 `SelectedSkillId`만 선택한다(잠긴
스킬이어도 정보 패널에서 비용/효과를 미리 볼 수 있어야 하므로). 실제 구매는 별도의 구매 버튼
(`RaiseSkillPurchased`)에서만 일어난다. `SkillManager.HandlePurchase()`가 재사용형/1회성을 갈라서 처리:
1회성은 `!isReusable && IsUnlocked`면 조용히 무시(재구매 차단), 재사용형은 이 체크를 건너뛰고 계속
구매 가능.

**경계 원칙** : `SkillPanelUI`는 `EventHub.RaiseSkillClicked`/`RaiseSkillPurchased`만 호출하고, `SkillManager`가
그 이벤트를 구독해서 실제 상태를 바꾼다. UI는 상태 조회(`GetSkillProfile`/`IsUnlocked`/`GetCurrentCost`)만
직접 한다 (쓰기는 EventHub 경유).

**갱신 타이밍** : 구매가 성공하면 `HandlePurchase()`가 `StatCalculator.ClampStat` 이후
`EventHub.RaiseMarketUpdated`를 직접 발행한다 — `NextTurn()`/`HandleNewsEvent()`와 동일한 패턴. 이게 빠져있던
게 버그였다(구매는 되는데 하단 게이지가 다음 턴까지 안 바뀜).

**1회성 스킬의 의심도 감소 하한** : `StatCalculator.ApplySkillUse`(구매 시 1회 반영)와 `ApplySkills`(토글형
매 턴 재적용, 현재는 미사용)의 `DoubtDecrease` 분기 모두 `Mathf.Max(0f, ...)`로 0 밑으로 안 내려가게
막았다. 단, 이 두 곳만 고쳤고 공용 경로인 `StatCalculator.ApplyEffect`는 그대로 뒀다 — `ApplyEffect`는
`EventCalculator`(시사 이벤트)도 재사용하는 경로라, 여기를 고치면 이벤트로 인한 의심도 감소까지 0 밑으로
막히게 돼서 요구사항(스킬로 인한 감소만 0 하한, Job/이벤트는 그대로) 범위를 벗어난다.

## 호출 스택

### 1) 재사용형 스킬 구매 (반복 가능)

```
[아이콘 클릭] (SkillPanelUI.cs:67)
 └─ EventHub.RaiseSkillClicked(id)
     └─ SkillManager.HandleSkillClicked(id)          [SkillManager.cs:73]
         └─ runtimeSkillData.SelectedSkillId = id
             └─ (구독) SkillPanelUI.RefreshDetail()    [SkillPanelUI.cs:124] → 이름/비용/효과 표시, purchaseBtn 활성화

[구매 버튼 클릭] (SkillPanelUI.cs:57)
 └─ EventHub.RaiseSkillPurchased()
     └─ SkillManager.HandlePurchase()                 [SkillManager.cs:83]
         ├─ isReusable=true → 잠금 체크 건너뜀          [:93]
         ├─ PlayerManager.TrySpend(cost)                실패 시 조용히 중단
         ├─ StatCalculator.ApplySkillUse(...)           [:101] → Support/Growth/Doubt/Supply 반영
         ├─ StatCalculator.ClampStat(...)                [:102]
         ├─ GrantCashBonus(...)                          [:103] (CashBonus 효과가 있으면)
         ├─ skill.PurchaseCount++                        [:105] → 다음 구매 비용 상승(costMultiplier)
         └─ EventHub.RaiseMarketUpdated(CurrentStat)      [:107]
             └─ (구독) StatGaugeUI.HandleMarketUpdated()   → Support/Growth/Doubt 게이지 즉시 갱신
```

### 2) 1회성 스킬 최초 구매 → 영구 잠금

```
[구매 버튼 클릭] → HandlePurchase()                    [SkillManager.cs:83]
 ├─ !isReusable && IsUnlocked == false → 통과            [:93]
 ├─ (이하 1번과 동일 : 비용 차감 → 효과 반영 → skill.IsUnlocked = true)
 └─ (구독) SkillPanelUI.RefreshDetail()                  [SkillPanelUI.cs:124]
     ├─ locked = !isReusable && IsUnlocked == true → true [:144]
     ├─ descriptionText = "구매 완료"                     [:147]
     └─ purchaseBtn.interactable = false                  [:152]
 └─ (구독, 추가발행권한이면) MintButtonUI.RefreshUnlockState() [MintButtonUI.cs:29]
     └─ canvasGroup.alpha/interactable/blocksRaycasts = true → 발행량 조작 버튼 해금
```

### 3) 잠긴 1회성 스킬 재클릭 (재구매 시도)

```
[구매 버튼 클릭] → HandlePurchase()                    [SkillManager.cs:83]
 └─ !isReusable && IsUnlocked == true → return           [:93] (비용 차감/효과 반영 없음)
```

### 4) 아이콘 색 상태 (매 RefreshDetail마다)

```
SkillPanelUI.RefreshSelectionHighlight(selected)         [SkillPanelUI.cs:166]
 └─ 아이콘별로:
     ├─ 선택된 아이콘 → SelectedIconColor (연한 빨강)
     ├─ 선택 안 됐고 1회성+구매완료 → UsedOneTimeIconColor (회색)
     └─ 그 외(미구매 1회성, 재사용형 전부) → Color.white
```

## 알려진 이슈 / 주의점 후보

- `ApplySkills`(토글형 매 턴 재적용 경로)는 현재 어떤 UI도 `EnableSkill`을 호출하지 않아 사실상 죽은
  코드다 — 6개 스킬이 전부 1회성으로 바뀌면서 그렇게 됐다. 나중에 "지속효과형" 스킬(예: 매 턴 Doubt 상승률
  감소가 유지되는 버프)이 추가되면 그 스킬의 아이콘 클릭 핸들러에서 `EnableSkill`/`DisableSkill`을 연결해야
  실제로 동작한다.
- `StatCalculator.ApplyEffect`의 `DoubtDecrease`는 0 하한이 없다 — 이벤트(`EventCalculator`)가 이 경로를
  같이 쓰기 때문에 의도적으로 그대로 뒀다. 만약 나중에 "지속효과형" 스킬을 `ApplySkills`가 아니라
  `ApplyEffect`로 직접 처리하도록 바꾸면, 이 스킬 전용 분기(0 하한)를 놓치기 쉬우니 주의.
- 발행량 관련 3종(추가발행권한/우회발행권한/발행량은폐)은 아직 `SupplyIncrease`/`SupplyDecrease` 효과가
  없다 — `Next_Tesk.md` "발행량 관련 스킬 3종에 실제 Supply 효과 부여" 항목 참고, 이번 작업 범위 밖.
- 6개 스킬 전부 `isReusable=false`로 설정한 건 설명 텍스트 기반 추정이다(임시 값으로 두기로 사용자와 합의).
  실제 밸런스/기획이 나오면 재검토 필요.
