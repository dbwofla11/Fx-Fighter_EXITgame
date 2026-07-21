# 다음 작업

## 목표

EventHub 인프라(이벤트 정의 + Manager 구독)는 구축되었다.

다음 단계에서는 실제 UI가 EventHub를 통해 이벤트를 발행하도록 연결한다.

---

## 구현 내용

- 스킬 UI에서 `EventHub.RaiseSkillClicked(SkillID)` 호출
- 직업 선택 UI에서 `EventHub.RaiseJobSelected(JobSO)` 호출
- 거래 UI(Long/Short 버튼)에서 `EventHub.RaiseBuyCoin(long)` / `EventHub.RaiseSellCoin(long)` 호출
- 시장 갱신 UI에서 `EventHub.OnMarketUpdated`를 구독하여 `PlayerStat` 표시 갱신

---

## 참고

- 위 이벤트들은 이미 `EventHub`(Assets/Scripts/manager/EventHub.cs)에 정의되어 있고, 대응하는 Manager 구독도 완료된 상태이다. (`Logging.md` 참고)
- `TimeUI`(배속/일시정지), `PlayerUI`(자산 폴링), `SettingsUI`는 이번 작업의 이벤트 목록에 포함되지 않으므로 범위 밖이다. 별도 지시가 있을 때 진행한다.

---

## 완료 조건

- UI는 EventHub만 호출한다.
- Manager는 EventHub를 통해 이벤트를 수신한다.
- UI와 Manager가 직접 참조하지 않는다.
- 기존 계산 파이프라인은 그대로 유지한다.
