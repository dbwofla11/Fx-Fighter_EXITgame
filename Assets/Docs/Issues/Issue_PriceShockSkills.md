# 이슈 : 시장조작 카테고리 하이리스크·하이리턴 개편 + 가격 즉시 변동(PriceShockPercent) 작동 방식

작성일 : 2026-08-12

관련 구현 : `Next_Tesk.md` #22 "2차 피드백(빨간 글씨 항목) 처리" — Notion "2차 피드백 정리" 중
"시장조작 : 가격도 임시로 조정하게 만들어야 한다(리스크 큼) / 극단적인 가격이 즉시 반영되는 스킬 추가",
그리고 뒤이은 사용자 피드백("새 스킬 만들지 말고 기존 걸 여론조작과 다르게 비싸고 하이리스크·하이리턴으로
바꿔라, 가격 즉시 변동도 기존 스킬을 변형해서").

**1차 시도(신규 스킬 2종 추가)는 되돌렸다** — 사용자가 신규 스킬 대신 기존 9종 개편을 원해서 전부 되돌리고
이 문서에 기록된 방식으로 다시 작업함.

## 관련 파일

- [EffectData.cs](../Scripts/SOs/detailData/EffectData.cs) — `EffectType.PriceShockPercent`(14) 신규 추가.
- [StatCalculator.cs](../Scripts/Runtimes/Systems/StatCalculator.cs) — `ApplySkillUse()`가 구매 시점에
  `CurrentPrice *= 1 + value/100` 직접 반영, `ApplyToggleSkillEffects()`/`ApplyEffect()` 제외·no-op 목록에 포함.
- [PriceCalculator.cs](../Scripts/Runtimes/Systems/PriceCalculator.cs) — `ClampPrice()`(기존 함수 재사용, `MinPrice` 하한 유지).
- [EventEffectFormatter.cs](../Scripts/UI/Utils/EventEffectFormatter.cs) — 스킬 설명 패널 라벨/색상.
  `IsBeneficial`이 `EffectType` 대신 `EffectData`를 받도록 시그니처 변경(값 부호로 급등/폭락 색 구분).
- 기존 .asset 9개 전부 수정 : `Assets/Profile/스킬_프로파일/시장조작/*.asset` (신규 파일 없음, `SkillID`/
  `SkillManager.skillDatabase`도 변경 없음 — 전부 기존 항목 그대로 재사용).

## 개편 내용

**가격/수치 전반 인상** — 시장조작 9종의 `baseCost`를 여론조작 카테고리(₩15,000~225,000)보다 비싼 쪽으로
재조정하고(구 ₩22,500~90,000 → 신규 ₩50,000~200,000), Doubt는 대부분 2배 이상, Support/Growth/Volume은
1.3~1.6배로 올렸다 — "여론조작과 다르게 비싸고 하이리스크·하이리턴"이라는 요청 반영. 유일하게 Doubt가 0이던
`유동성공급`에도 Doubt +7.5를 새로 붙여 카테고리 전체가 리스크를 지도록 통일했다.

| 스킬 | baseCost | 주요 효과(신규) | 비고 |
|---|---|---|---|
| 자전거래 | 22,500→50,000 | Volume+25, Growth+12.5, Doubt+10 | |
| 유동성공급 | 37,500→70,000 | Support+10, Growth+5, **Doubt+7.5(신규)** | 유일하게 Doubt 없던 스킬 |
| 거래량부풀리기 | 45,000→90,000 | Volume+40, Doubt+7.5 | |
| 허수매수벽 | 52,500→100,000 | Support+5, Growth+12.5, Doubt+7.5, **Price +10%(신규)** | 가짜 매수벽도 가격을 즉시 소폭 밀어올리게 수정 |
| 허수매도벽 | 52,500→110,000 | Support+12.5, Growth+5, Doubt+10, **Price -20%(신규)** | 즉시 폭락 후 저가 매수 컨셉으로 설명 문구도 수정 |
| 펌핑 | 60,000→**180,000** | Support+15, Growth+35, Doubt+15, **Price +25%(신규)** | 허수매수벽의 모든 효과(Support/Growth/Price)를 그대로 웃도는 **상위호환**으로 설계, 대신 값도 더 비쌈(2026-08-12 추가 조정) |
| 고래계정운용 | 75,000→150,000 | Volume+20, Growth+17.5, Doubt+12.5 | |
| 시세방어 | 52,500→110,000 | Support+20, Doubt+7.5 | |
| FOMO유도 | 90,000→200,000 | Support-30, Growth-30, Doubt+15 | 기존부터 있던 "스탯을 깎는 고벨류 스킬"(`Completed_Tasks.md` 참고), 이번엔 가격 효과 없이 기존 마이너스 폭만 키움 |

**가격 즉시 변동은 신규 EffectType 하나 + 기존 스킬 3개 확장으로 구현** — 새 SkillSO/SkillID를 만들지
않고, 이미 있던 `펌핑`(설명이 이미 "단기간에 가격을 급등시킨다"였는데 실제로는 Price를 안 건드리고
있었음 → 딱 맞는 스킬), `허수매수벽`(실제 "가짜 매수벽" 조작 기법이 가격을 즉시 밀어올리는 수법이라
매칭, 기존 "가격 하락을 억제한다"는 방어적 설명에 "즉시 소폭 끌어올린다"만 덧붙임), `허수매도벽`(반대로
"가짜 매도벽"은 가격을 즉시 끌어내리는 수법이라 매칭)에 `PriceShockPercent` 효과 한 줄씩을 추가하는
방식을 택했다. `허수매도벽`은 기존 설명("과도한 가격 상승을 제어한다")이 새 메커니즘과 안 맞아 "즉시
폭락 후 저가 매수"로 설명 문구도 같이 고쳤다.

**`펌핑` = `허수매수벽`의 상위호환(2026-08-12 추가 조정)** — 사용자 피드백으로 두 스킬을 같은 효과
구성(Support/Growth/Price)의 상·하위 관계로 재조정했다. `허수매수벽`(Support+5, Growth+12.5, Price+10%,
baseCost 100,000)이 저가형이고, `펌핑`(Support+15, Growth+35, Price+25%, baseCost 180,000)은 세 수치
전부를 그대로 웃돌아 "더 비싸지만 확실히 더 센" 상위호환으로 설계했다 — Doubt(15 vs 7.5)도 함께 올려
리스크·리턴 모두 상위 스킬답게 비례해서 커지게 했다.

**동작 방식** : 구매 즉시 `CurrentPrice`에 `(1 + value/100)`을 곱해 직접 반영하는 1회성 효과(감쇠 없음,
Support/Growth 1회성 보너스와 동일 패턴). `StatCalculator.ApplySkillUse`에서 처리하고, 매 턴 재적용 루프
(`ApplyToggleSkillEffects`)에서는 제외했다. **가격 이동은 영구다** — 이름과 달리 되돌아가는 타이머가
없다. Notion 원문 "임시로 조정"이 "일정 턴 후 원상복귀"를 뜻하는 것이라면 이 구현과 다르다 — 원상복귀형이
필요하면(예: N턴 버프처럼 `BuffCalculator`에 새 타이머 추가) 별도 작업으로 확장할 것.

## 호출 스택

```
[구매 버튼 클릭] → EventHub.RaiseSkillPurchased()
 └─ SkillManager.HandlePurchase()                          [SkillManager.cs:92]
     ├─ StatCalculator.ApplySkillUse(CurrentStat, skill.Profile)   [:113]
     │   └─ effect.effectType == PriceShockPercent (펌핑/허수매도벽만 해당)
     │       ├─ stat.CurrentPrice *= 1 + effect.value/100   [StatCalculator.cs:251-256]
     │       └─ PriceCalculator.ClampPrice(stat)
     ├─ StatCalculator.ClampStat(CurrentStat)                [:114]
     └─ 나머지 7개 스킬은 Support/Growth/Volume/Doubt만 커진 수치로 기존 경로 그대로 통과
EventHub.RaiseMarketUpdated(CurrentStat) → UI(가격/스탯 표시) 자동 갱신(기존 구독 경로 그대로, UI 코드 변경 없음)
```

## 확인/후속 필요 사항

- **UI 변경 없음** : 9개 모두 기존 SkillSO를 그대로 쓰므로 `SkillPanelUI.icons`(씬에 고정 배치된 아이콘
  버튼)도 손댈 필요가 없었다 — 수치만 바뀌고 화면엔 자동 반영된다(신규 스킬이었던 1차 시도와 달리 UI
  작업 불필요).
- **가격 이동이 "영구"인 점** — 위 참고. 실제 플레이해보고 원상복귀형이 필요한지 판단할 것.
- **수치 전부 임시값** — 이번에 잡은 baseCost/효과 배율 전부 체감 없이 정한 값이다. `Next_Tesk.md`
  "밸런스 수치 조정" 후보에 추가해뒀다.
