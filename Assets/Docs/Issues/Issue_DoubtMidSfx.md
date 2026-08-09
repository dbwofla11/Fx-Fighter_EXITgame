# 이슈 : 의심도 55~100 구간 중간 효과음

작성일 : 2026-08-09

## 추가된 것

- [DoubtMidSfxController.cs](../../Scripts/manager/utils/DoubtMidSfxController.cs) — 신규 컴포넌트.
  Doubt 55 이상인 동안 3~6개월(턴) 랜덤 간격마다 `Assets/Audio/DouptSFX/`의 `clockdown`/`end-clocksound`/
  `heartsound` 중 하나를 랜덤으로 골라 앞 3초만 재생.
- `Assets/Scenes/SampleScene.unity` — `/GameStarter` 오브젝트(`SuspicionBgmController`와 같은 자리)에
  컴포넌트 추가, 클립 3개 연결.
