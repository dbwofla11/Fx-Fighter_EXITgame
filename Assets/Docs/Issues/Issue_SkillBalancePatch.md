# 이슈 : 스킬 1차 스탯 밸런스 패치 반영 (Notion → Unity)

작성일 : 2026-08-04

관련 구현 : `Assets/Profile/스킬_프로파일/**/*.asset`(42개), `Assets/Scripts/SOs/SkillSo.cs`,
`Assets/Scripts/UI/SkillModal/SkillPanelUI.cs`, Notion "게임 룰 설명(스킬정리)" 페이지

## 배경

사용자가 코인설계/시장조작/여론조작/방어 4개 카테고리, 42개 스킬의 1차 수정된 효과 수치(지지도/상승률/
의심도/발행량/거래량/수익증가)를 텍스트로 전달함. 기존에 `논의사항 - 스킬 밸런스 패치` Notion 페이지에
"효과 비어있는 스킬 25개"로 남겨뒀던 항목들(투자자 수/유동성/시가총액 등 `PlayerStat`에 없는 개념으로
막혀있던 것)과, 이미 대략적인 효과가 있던 나머지 스킬들을 전부 이번 수치로 교체하는 작업.

## 1. Notion 문서 반영

Notion "게임 룰 설명(스킬정리)" 페이지의 각 스킬 표 `효과` 컬럼을 새 수치로 교체(`notion-update-page`
`update_content`, old_str/new_str 42쌍 일괄 치환). "어떤 스탯이 증가/변화하는지만" 적어달라는 요청에 맞춰
`지지도 +15, 상승률 +5` 형식으로 통일하고, 기존에 붙어있던 부가 설명(예: "위기 상황에서 자금을 빠르게
마련할 수 있다" 류)은 효과 칸에서 제외.

- 반영 안 한 것 : **인플루언서 계약**(이번 목록에 없어서 기존 "투자자 +1000" 유지), **FOMO 유도**(스탯
  델타 대신 "일부러 가격 하락 후 저점 재매수"라는 메커니즘 설명이라 "스탯 미정 — 별도 메커니즘, 수치
  논의 필요"로만 표시).
- "정시소각"(사용자 표기) → 기존 Notion 항목명 "정기 소각"으로 매칭.

## 2. Unity 반영 (1차 시도에서 누락됐던 부분)

Notion만 갱신하고 실제 `.asset`(`SkillSO`)의 `effects` 리스트는 하나도 안 건드렸던 게 뒤늦게 발견됨
(사용자가 "유니티에는 반영된게 하나도 없는디?"로 지적). Unity MCP `execute_code`로 42개 `SkillSO` 에셋을
전부 로드해서 `effects`를 새 수치로 덮어쓰고 `AssetDatabase.SaveAssets()`로 저장.

### 스탯 → EffectType 변환 규칙

`EffectData.effectType`/`value` 부호 규약(`StatCalculator.ApplyEffect` 기준):

| 사용자가 준 스탯 | 양수(+N) | 음수(-N) |
|---|---|---|
| 지지도 | `SupportIncrease`, value=N | `SupportIncrease`, value=-N (Support 전용 Decrease 타입이 없어서 음수 그대로) |
| 상승률 | `GrowthIncrease`, value=N | `GrowthIncrease`, value=-N (동일 이유) |
| 의심도 | `DoubtIncrease`, value=N | `DoubtDecrease`, value=N (부호 있는 값이 아니라 타입 자체가 방향) |
| 발행량 | `SupplyIncrease`, value=N | `SupplyDecrease`, value=N |
| 거래량 | `VolumeIncrease`, value=N | (해당 스킬 없음) |
| 수익증가 | `CashBonus`, value=N | (해당 스킬 없음) |

예 : "책임 전가 : 지지도 -15, 의심도 -10" → `[{SupportIncrease, -15}, {DoubtDecrease, 10}]`

### 검증 (Unity MCP Play 모드, `SkillManager.GetSkillProfile()`로 실제 로드)

```
추가발행권한 : GrowthIncrease 30, CashBonus 15, DoubtDecrease 5
펌핑         : SupportIncrease 15, GrowthIncrease 45, DoubtIncrease 10
바지사장     : SupportIncrease -5, DoubtDecrease 20
```

42개 전부 정상 반영 확인 (`updated=42 missing=0`).

## 3. 추가발행권한 설명 보강

"코인 발행을 할 수 있게 된다"는 내용이 설명에 빠져있다는 지적 → `SkillSO.description`에 한 문장 추가.

```
Before: 언제든 새로운 코인을 찍어낼 수 있는 권한을 확보한다. 위기 상황에서 자금을 빠르게 마련할 수 있다.
After:  (위 문장) + 이 스킬을 구매하면 코인 발행(민팅) 버튼이 해금된다.
```

## 4. 스킬 설명 텍스트가 버튼에 가려짐 → 폰트 크기 축소

`SkillPanelUI.descriptionText`(씬의 `DescriptionText` 오브젝트, 420x380 박스)가 `[재사용형/1회성]` +
비용/구매횟수 + 효과 텍스트(`EventEffectFormatter.BuildEffectsText`) + 설명까지 한 번에 출력하는데, 스탯이
2~3개로 늘어난 스킬들은 줄 수가 늘어 텍스트가 넘쳐 구매 버튼과 겹침. `fontSize` 30 → 20으로 축소
(`SerializedObject`로 씬에 직접 반영 후 `EditorSceneManager.SaveScene`).

## 여담 : 테스트 중 발견한 Unity MCP 환경 이슈

이번 검증 과정에서 Play 모드 진입 직후 수 초~수백 프레임 사이에 `MarketManager.Instance`/
`PlayerManager.Instance`가 간헐적으로 `null`이 되거나 `EventHub`의 이벤트 구독이 통째로 비는 현상을
반복적으로 겪었다(코드 버그 아님 — 정지→재생 후 곧바로 재확인하면 정상). 원인은 명확히 특정 못 했고,
대량의 에셋 저장(`SaveAssets`)/씬 저장 직후 Play 모드에 들어갈 때 도메인 리로드가 비동기로 늦게 끝나면서
생기는 것으로 추정. 검증할 땐 stop→play 직후 **한 번의 `execute_code` 호출 안에서** 준비 상태 체크와
실제 테스트를 같이 넣는 방식으로 우회함.
