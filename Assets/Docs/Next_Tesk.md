# 다음 작업

## 목표

Long/Short 거래 로직은 완료되었다.

남은 백엔드 후보 작업 중 하나를 다음 작업으로 진행한다.

---

## 후보

### 1. 발행량(Supply) 조작

`PlayerStat.Supply`는 필드만 있고 실제로 값을 설정하는 곳이 없다.
`EffectType`에도 Supply 관련 항목이 없어서, 어떤 스킬/이벤트가 발행량을 조작하는지 먼저 정의가 필요하다.

### 2. 시사 이벤트(뉴스 이벤트)

`EventCalculator`가 빈 스텁이다. `Game_Formula.md` 4장 기준으로
이벤트 발생 시 긴 양봉/음봉 및 Support/Growth 변경 로직을 구현한다.
`EventHub.OnNewsEvent` → `MarketManager.HandleNewsEvent()` → `EventCalculator.Calculate()` 연결은 이미 되어 있다.

---

## 참고

- Long/Short 관련 UI 버튼은 아직 없다. UI 작업이 가능해지면 `EventHub.RaiseBuyCoin`/`RaiseSellCoin`을 호출하도록 연결하면 된다.
