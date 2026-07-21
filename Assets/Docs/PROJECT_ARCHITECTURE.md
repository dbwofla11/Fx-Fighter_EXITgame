# 프로젝트 아키텍처

## 전체 구조

Manager

↓

RuntimeData

↓

Systems (Calculator)

---

# Manager

- TimeManager
- MarketManager
- SkillManager
- JobManager
- PlayerManager

Manager는 게임 상태를 관리한다.

---

# RuntimeData

- PlayerStat
- RuntimeSkillData
- RuntimeJobData

RuntimeData는 현재 게임 상태와 계산 결과를 저장한다.

---

# Systems

- StatCalculator
- ProbabilityCalculator
- PriceCalculator

Calculator는 상태를 변경하지 않고 계산만 수행한다.

---

# 현재 계산 흐름

TimeManager

↓

OnDayChanged

↓

MarketManager.NextTurn()

↓

StatCalculator.Calculate()

↓

ProbabilityCalculator.Calculate()

↓

PriceCalculator.Calculate()

↓

PlayerStat 갱신

---

# 역할

## TimeManager

게임 시간을 관리한다.

---

## MarketManager

시장 계산을 수행한다.

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

- Manager는 상태를 관리한다.
- RuntimeData는 현재 상태를 저장한다.
- Calculator는 계산만 수행한다.
- UI는 RuntimeData를 읽는다.

---
