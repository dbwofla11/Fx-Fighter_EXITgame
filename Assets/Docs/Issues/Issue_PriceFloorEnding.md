# 이슈 : 가격 하한선(1) 3주 지속 시 게임오버 엔딩 추가 (예정)

작성일 : 2026-08-04
상태 : 다음 세션에서 구현 예정 (이 세션에서는 코드 변경 없음)

## 요청 배경

VFX 1차 작업 마무리하면서 나온 아이디어. 현재 자동 엔딩은 `Assets/Docs/Game_Formula.md` 5장 기준
체포(Doubt>=100)/거지(현금+코인 0) 2가지뿐이고, 가격이 하한선(`PriceCalculator.MinPrice = 1f`)에
붙은 채로 방치되는 상황에 대한 별도 게임오버 조건이 없다. 코인 가격이 1로 바닥을 찍고 그 상태가
**3주(21턴, DaysPerCandle=7 기준 1주=7턴)** 동안 이어지면 게임을 강제 종료시키고 싶다.

## 관련 기존 코드

- `Assets/Scripts/Runtimes/Systems/PriceCalculator.cs` — `MinPrice = 1f`, `ClampPrice()`
- `Assets/Scripts/Runtimes/Systems/EndingCalculator.cs` — `CheckAutomatic()` (현재 체포/거지만 판정)
- `Assets/Scripts/Stat/Enums/EndingType.cs` — 현재 Arrest/Exit/Hero/Broke 4종
- `Assets/Scripts/manager/MarketManager.cs` — `NextTurn()`, `CheckAutomaticEndings()`, `EndGame()`
- `Assets/Scripts/UI/MainModal/EndingResultUI.cs` — 엔딩별 문구 표시 (`Describe()`)
- `Assets/Docs/Game_Formula.md` 5장 — 엔딩 조건 표(신규 엔딩 추가 시 갱신 필요)

## 결정할 것 (다음 세션에서)

- 새 `EndingType` 하나 추가할지, 기존 `Broke`에 편입할지 — 의미상 다르므로(자산 소진 vs 시장 붕괴 방치)
  새 타입 쪽이 맞아 보이지만 확정은 다음 세션에서.
- "3주 연속"을 세는 카운터를 어디 둘지 — `PlayerStat`에 필드 추가 vs `MarketManager` 내부 private 필드.
  `NextTurn()`마다 `CurrentPrice <= MinPrice`면 +1, 아니면 0으로 리셋하는 단순 카운터로 충분해 보임.
- 엔딩 결과 문구(`EndingResultUI.Describe()`) 및 `Game_Formula.md` 5장 갱신 필요.
