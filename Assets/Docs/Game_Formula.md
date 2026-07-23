# 게임 공식

본 문서는 게임의 시장 계산 공식을 정의한다.

---

# 1. 가격 방향성

기본 상승 확률 : 50%

기본 하락 확률 : 50%

Support와 Growth에 따라 상승 확률이 변경된다.

상승 확률과 하락 확률의 합은 항상 100%이다.

공식

Pup = Clamp(
0.5
+ ws × Support / 100
+ wg × Growth / 100
,0,1)

Pdown = 1 - Pup

변수

- Support : 코인 지지도 (-100 ~ 100)
- Growth : 코인 상승률 (-100 ~ 100)
- ws : 지지도 가중치
- wg : 상승률 가중치

Support/Growth는 `CurrentPrice`처럼 턴을 넘어 유지되는 값이다. Job 선택, 거래(Long/Short), 스킬 사용이
발생하는 순간 그 값에 직접 반영되고, 매 턴 서서히 0으로 감쇠한다 (자세한 내용은 3장 "누적치 감쇠" 참고).

---

# 2. 가격 변화량

가격 변화량은 방향성과 별도로 계산된다.

Price(t+1)

=

Price(t)

+

0.6 × Growth

+

0.25 × Support

+

0.15 × Scarcity

Scarcity는 발행량(Supply)을 기반으로 계산되는 희소성 지표이다.

---

# 3. 거래 시스템

플레이어는 현재 가격으로 즉시 거래한다.

## Long

- 현재 가격으로 구매 : Cost = Amount × CurrentPrice (현금 감소, 코인 증가)
- Support 증가
- Growth 증가

## Short

- 현재 가격으로 판매 : Revenue = Amount × CurrentPrice (현금 증가, 코인 감소)
- Support 감소
- Growth 감소

거래량이 많을수록 영향력이 증가한다.

## 거래가 Support/Growth에 주는 영향

거래는 발생하는 그 순간 Support/Growth에 직접 반영된다. 영향의 크기는 거래량(Amount)에 비례한다.

Support += ±Amount × ws_trade

Growth += ±Amount × wg_trade

- ws_trade = 0.1 (Support 가중치)
- wg_trade = 0.1 (Growth 가중치)
- 부호는 Long(+) / Short(-)

## 누적치 감쇠

Support/Growth는 거래·직업 선택·스킬 사용으로 반영된 값을 계속 들고 있다가, 아무 행동이 없어도
시간이 지나면 (마이너스 포함) 절대값 0으로 서서히 수렴한다.

Support(t+1) = Support(t) × decayRate

Growth(t+1) = Growth(t) × decayRate

- decayRate = 0.995 (매 턴 0.5%씩 감소, 절반이 되는 데 약 138턴)
- 매 턴(`StatCalculator.Calculate()`, `MarketManager.NextTurn()`에서 호출) 적용된다.
- 직업(Job)의 Support/Growth 효과도 동일하게 취급한다 — 3-1장 참고.

---

# 3-1. 직업(Job)이 Support/Growth에 주는 영향

직업의 Support/Growth 효과는 매 턴 계속 재적용되지 않고, **직업을 선택하는 순간 직접 반영된 뒤 거래와 동일하게 감쇠**한다.
(부정 이벤트/스킬/숏 거래로 Support/Growth가 내려가야 하는데, 직업 효과가 매 턴 무한정 다시 채워지면 실질적으로 내려갈 수 없기 때문)

- 직업 선택 시 : `Support += 해당 직업의 SupportIncrease 효과 합`, Growth도 동일 (1회성)
- 이후 매 턴 : 위 "누적치 감쇠" 공식과 동일하게 감쇠
- 직업의 Support/Growth **외** 효과(DoubtDecrease, CashBonus, VolumeIncrease/Decrease, ExitUnlock 등)는 기존처럼 직업을 유지하는 동안 매 턴 계속 재적용된다 (감쇠 대상 아님).

---

# 4. 시사 이벤트

시사 이벤트는 가격에 직접 영향을 준다.

큰 이벤트일수록 긴 양봉 또는 긴 음봉이 발생한다.

또한 이벤트 종류에 따라

- Support
- Growth

값이 변경된다.

---

# 참고

본 문서는 게임 계산 로직의 기준 문서이다.

Calculator는 본 문서를 기준으로 구현한다.

게임 밸런스 변경 시 본 문서와 Calculator를 함께 수정한다.