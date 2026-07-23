# 다음 작업

## 진행 상태

- **완료** : Support/Growth 감쇠. `PlayerStat.Support`/`Growth`가 `CurrentPrice`처럼 턴을 넘어 유지되는 값이 되었고,
  매 턴 `TradeCalculator.Decay(stat)`로 감쇠한다. Job 선택/거래(Long·Short)는 발생하는 순간 `MarketManager.CurrentStat`에
  직접 반영된다. 별도 누적 데이터 클래스(`RuntimeTradeData`, `JobBoostData`)는 시도했다가 불필요해서 제거했다.
  (`Logging.md` "Support/Growth 감쇠 (Decay) — 최종 구조" 참고, 공식은 `Game_Formula.md` 3장 / 3-1장)
- **완료** : Skill 재사용 시스템. `SkillSO.isReusable` 플래그로 재사용형(Support/Growth 부스트, 사용 시 1회 반영 후
  재구매 필요) / 재사용 불가(토글형, 감쇠 없음)를 구분한다. (`Logging.md` "Skill 재사용 시스템" 참고)
- **남음** : 발행량(Supply) 조작, 시사 이벤트(`EventCalculator`) — 아래 참고.

---

## 후보 : 발행량(Supply) 조작

`PlayerStat.Supply`와 `Game_Formula.md` 2장의 Scarcity(희소성) 지표가 아직 구현되어 있지 않다.

- Supply를 변화시키는 소스(직업/스킬/이벤트 등)를 정의해야 한다.
- Scarcity를 Supply로부터 계산하는 공식을 `Game_Formula.md`에 확정해야 한다 (현재 문서에는 "발행량을 기반으로
  계산되는 희소성 지표"라고만 되어 있고 구체적 공식이 없다).
- `PriceCalculator`가 이 Scarcity 값을 가격 계산에 반영해야 한다 (`0.15 × Scarcity` 항).

## 후보 : 시사 이벤트 (`EventCalculator`)

`EventCalculator.Calculate()`는 아직 빈 스텁이다. `EventHub.OnNewsEvent` → `MarketManager.HandleNewsEvent`
연결은 되어 있으나 실제 계산 로직이 없다.

- `Game_Formula.md` 4장 기준으로 이벤트 종류(긍정/부정, 크기)와 Support/Growth/가격에 주는 영향을 정의해야 한다.
- 이벤트가 발생하는 시점(확률 기반 자동 발생 vs 수동 트리거)을 결정해야 한다.
- 이벤트도 Job/Trade/Skill과 동일하게 "발생 시점에 `CurrentStat`에 직접 반영 → 매 턴 감쇠" 패턴을 따를지,
  아니면 가격에만 즉시 영향을 주고 끝나는 1회성 효과로 둘지 결정이 필요하다.
