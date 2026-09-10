# 이슈 : 정기 이자 상환 이벤트 3지선다 작동 방식 / 호출 스택 정리

작성일 : 2026-09-11

관련 구현 : `Assets/Scripts/Runtimes/Systems/DebtManager.cs`, `Assets/Scripts/manager/MarketManager.cs`

## 관련 파일

- [DebtManager.cs](../../Scripts/Runtimes/Systems/DebtManager.cs) — 대출 원금, 이자, 미납 이자와 납부 상태를 관리한다.
- [EventChoice.cs](../../Scripts/SOs/detailData/EventChoice.cs) — 이자 납부 요청과 선택 결과 데이터(`DebtPaymentRequest`, `DebtPaymentResolution`)를 정의한다.
- [EventHub.cs](../../Scripts/manager/utils/EventHub.cs) — 이자 이벤트 표시 요청과 3개 선택 결과를 전달한다.
- [MarketManager.cs](../../Scripts/manager/MarketManager.cs) — 30턴 주기 판정, 선택 결과 처리, 이벤트 로그 기록과 턴 재개를 담당한다.
- [EventNotificationUI.cs](../../Scripts/UI/EventModal/EventNotificationUI.cs) — 이자 상환 모달과 납부·연기·도주 선택 카드를 런타임에 생성한다.
- [EventEffectFormatter.cs](../../Scripts/UI/Utils/EventEffectFormatter.cs) — 선택 결과 제목과 요약을 이벤트 로그 카드에 표시한다.
- [EventLogEntry.cs](../../Scripts/Stat/Enums/EventLogEntry.cs) — 선택 결과의 제목과 요약 문구를 이벤트 로그에 보관한다.
- `Assets/Scenes/SampleScene.unity`, `Assets/Scenes/Android/AndroidSampleScene.unity` — 선택 카드 모달의 기준 패널 크기를 반영한다.

## 작동 방식

대출 원금은 `DebtManager`가 보관하고, 기획상 30턴마다 원금에 대한 이자만 청구한다. 이자 납부 이벤트가 발생한 턴에는 시장 계산을 먼저 진행하지 않고 플레이어의 선택을 기다린다. UI는 `EventHub`를 통해 요청을 받고, 선택 결과도 `EventHub`로만 전달한다. 실제 현금·부채·의심도 변경과 턴 진행은 `MarketManager`가 담당한다.

납부 선택은 현금이 이자보다 적어도 가능한 금액만 차감한다. 남은 금액은 `UnpaidInterest`로 이월하고 연체 횟수를 증가시켜 다음 이자율에 반영한다. 연기 선택은 현재 청구액 전체를 미납 이자로 이월하고 연체 횟수를 증가시킨다. 도주 선택은 성공확률 20%로 판정하며, 성공하면 대출 계약을 초기화한다. 실패하면 현금의 20%를 차감하고 Doubt를 15 증가시킨 뒤 이자를 연기한다.

선택 결과는 일반 선택형 이벤트와 동일하게 `EventLogEntry`로 기록한다. 결과 모달이 닫히거나 다음 UI 이벤트가 처리될 수 있도록 선택 처리 후 `FinishTurn()`을 호출해 시장 계산 후속 단계와 `OnMarketUpdated`를 진행한다.

## 호출 스택

### 1) 30턴 도달 → 이자 상환 선택 카드 표시

```
[TimeManager.OnDayChanged]
 └─ MarketManager.NextTurn()                         [MarketManager.cs:176]
     ├─ turnCount 증가
     ├─ DebtManager.IsPaymentDue(turnCount)          [DebtManager.cs:47]
     ├─ DebtManager.CreatePaymentRequest(turnCount)  [DebtManager.cs:52]
     │   └─ CurrentInterest / CurrentDueInterest 계산
     └─ EventHub.RaiseDebtPaymentRequired(request)    [EventHub.cs:78]
         └─ EventNotificationUI.HandleDebtPaymentRequired(request)
             └─ ShowDebtPayment(request)             [EventNotificationUI.cs:174]
                 └─ CreateDebtPaymentCards(request)  [:193]
                     ├─ 정직하게 낸다
                     ├─ 나중에 낸다
                     └─ 도망간다
```

이 시점의 `MarketManager`는 `awaitingDebtPayment`를 true로 유지하므로, 플레이어가 카드를 선택할 때까지 같은 턴의 시장 계산을 진행하지 않는다.

### 2) 납부·연기·도주 카드 클릭 → 결과 처리

```
[상환 선택 카드 클릭]
 └─ EventNotificationUI.OnDebtPaymentChoiceClicked(choiceIndex) [EventNotificationUI.cs:605]
     └─ EventHub.RaiseDebtPaymentChoiceSelected(choiceIndex)    [EventHub.cs:84]
         └─ MarketManager.HandleDebtPaymentChoiceSelected(choiceIndex) [MarketManager.cs:310]
             └─ ResolveDebtPaymentChoice(choiceIndex, cash, debt)     [:351]
                 ├─ 납부: DebtManager.Pay(PlayerManager)
                 ├─ 연기: DebtManager.Defer()
                 └─ 도주: Random.value 판정
                     ├─ 성공: DebtManager.Escape()
                     └─ 실패: 현금 20% 차감 + Doubt 15 증가 + Defer()
```

### 3) 선택 결과 기록 → 턴 재개

```
ResolveDebtPaymentChoice()
 └─ EventLogEntry 생성 (현금/부채 전후, 결과 제목/요약)
     └─ LogEvent(entry)
         ├─ MarketManager.EventLog에 추가
         └─ EventHub.RaiseEventTriggered(entry)
             └─ EventNotificationUI가 결과 카드 표시
                 └─ MarketManager.FinishTurn()       [MarketManager.cs:226]
                     ├─ 스탯 보정 / 확률 / 가격 계산
                     ├─ 가격·스트리머 반응·캔들 기록
                     └─ EventHub.RaiseMarketUpdated(CurrentStat)
                         └─ EventNotificationUI가 대기 상태 해제 및 모달 닫기
```

## 알려진 이슈 / 주의점 후보

- `DebtManager`는 현재 원금을 직접 상환하지 않고 이자만 처리한다. 연기·납부 실패 시에는 원금과 별도로 미납 이자가 `TotalDebt`에 포함된다.
- 현금이 부족한 상태에서도 납부 카드는 선택할 수 있으며, 실제 납부 가능한 금액만 차감하고 미납분을 다음 청구로 넘긴다. 따라서 현금이 음수로 내려가지 않는다.
- 도주 결과는 `UnityEngine.Random`에 의존하므로 자동화 테스트에서 성공·실패 양쪽을 검증하려면 랜덤 결과를 제어할 수 있는 별도 테스트 훅이 필요하다.
- 기존 `OnDebtPaymentSelected(bool)` 이벤트와 `OnDebtPaymentClicked(bool)` 메서드는 호환성을 위해 남아 있지만, 새 3지선다 UI와 `MarketManager`는 `OnDebtPaymentChoiceSelected(int)` 경로를 사용한다.
- 정상적인 씬 부트스트랩을 거치지 않고 `SampleScene`만 직접 실행하면 기존과 같이 매니저 싱글턴이 준비되지 않을 수 있으므로, 실제 UI 확인은 타이틀부터 정상 플로우로 진입해야 한다.
