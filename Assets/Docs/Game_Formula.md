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

## 시장 영향치 계산

거래 1회의 Support/Growth 영향은 거래량(Amount)에 비례한다.

ΔSupport = ±Amount × ws_trade

ΔGrowth = ±Amount × wg_trade

- ws_trade = 0.05 (Support 가중치)
- wg_trade = 0.05 (Growth 가중치)
- 부호는 Long(+) / Short(-)

거래로 발생한 ΔSupport, ΔGrowth는 즉시 소멸하지 않고 누적되며,
다음 턴 스탯 계산(StatCalculator) 시 Job/Skill 효과와 함께 합산된다.

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