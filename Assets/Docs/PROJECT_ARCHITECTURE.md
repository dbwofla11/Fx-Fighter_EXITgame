# 프로젝트 아키텍처

## 전체 구조

UI

↓

EventHub

↓

Manager

↓

RuntimeData

↓

Systems (Calculator)

↓

EventHub

↓

UI

---

# EventHub

UI와 Manager 사이를 중계하는 정적(static) 이벤트 허브. (Assets/Scripts/manager/EventHub.cs)

- UI는 EventHub만 호출한다 (`EventHub.Raise*()`).
- Manager는 EventHub 이벤트를 구독한다 (`OnEnable`/`OnDisable`).
- UI와 Manager는 서로 직접 참조하지 않는다.
- Manager끼리는 직접 호출할 수 있다.

## 이벤트 목록

- OnDayChanged : 하루 경과 (TimeManager 발행 → MarketManager 구독)
- OnSkillClicked : 스킬 활성화/비활성화 요청 (SkillManager 구독)
- OnJobSelected : 직업 선택 요청 (JobManager 구독)
- OnBuyCoin / OnSellCoin : 코인 매수/매도 요청 (PlayerManager, MarketManager 구독)
- OnNewsEvent : 시사 이벤트 적용 요청 (MarketManager 구독)
- OnMarketUpdated : 시장 계산 완료 후 UI 갱신 (MarketManager 발행)

---

# Manager

- TimeManager
- MarketManager
- SkillManager
- JobManager
- PlayerManager

Manager는 게임 상태를 관리하며, EventHub를 구독하여 요청을 처리한다.

---

# RuntimeData

- PlayerStat
- RuntimeSkillData
- RuntimeJobData
- RuntimeTradeData

RuntimeData는 현재 게임 상태와 계산 결과를 저장한다.

---

# Systems

- StatCalculator
- ProbabilityCalculator
- PriceCalculator
- TradeCalculator
- EventCalculator

Calculator는 상태를 변경하지 않고 계산만 수행한다.

---

# 현재 계산 흐름

## 턴 진행

TimeManager

↓

EventHub.OnDayChanged

↓

MarketManager.NextTurn()

↓

StatCalculator.Calculate() (Job/Skill/Trade 누적치 합산)

↓

ProbabilityCalculator.Calculate()

↓

PriceCalculator.Calculate()

↓

PlayerStat 갱신

↓

EventHub.OnMarketUpdated (UI 갱신)

## 거래 (Long/Short)

EventHub.OnBuyCoin / OnSellCoin

↓

PlayerManager : 현재가 기준 현금 정산 + 코인 증감

MarketManager : TradeCalculator.Long/Short → RuntimeTradeData 누적 (다음 턴 StatCalculator에 반영)

---

# 역할

## TimeManager

게임 시간을 관리한다.

---

## MarketManager

시장 계산을 수행한다. 거래/시사 이벤트로 인한 시장 영향치(RuntimeTradeData)를 보유한다.

---

## SkillManager

스킬 구매 및 활성화를 관리한다.

---

## JobManager

현재 직업을 관리한다.

---

## PlayerManager

플레이어 자산과 보유 코인을 관리한다.

---

# 설계 원칙

- UI는 EventHub만 호출한다.
- Manager는 EventHub를 구독하여 상태를 관리한다.
- RuntimeData는 현재 상태를 저장한다.
- Calculator는 계산만 수행한다.
- UI는 RuntimeData를 읽는다.

---
