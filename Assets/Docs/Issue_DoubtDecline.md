# 이슈 : "의심도 하락"(DoubtDecline) 작동 방식 / 호출 스택 정리

작성일 : 2026-08-07

관련 구현 : `Next_Tesk.md` "후보 : '의심도 하락' 스탯(여론조작 스킬 한정)" 항목.

## 관련 파일

- [PlayerStat.cs](../Scripts/Stat/PlayerStat.cs) — 스킬별 진행 중인 하락을 담는 `DoubtDeclines`
  (`Dictionary<SkillID, DoubtDeclineBuff>`) 필드와 `DoubtDeclineBuff`(PerTurn/TurnsRemaining) 정의.
- [BuffCalculator.cs](../Scripts/Runtimes/Systems/BuffCalculator.cs) — N턴짜리 버프(Volume 거래량 버프,
  DoubtDecline 의심도 하락) 전담 static 클래스. `StartDoubtDecline`/`TickDoubtDeclines`.
- [StatCalculator.cs](../Scripts/Runtimes/Systems/StatCalculator.cs) — 매 턴 `Calculate()`가
  `BuffCalculator.TickDoubtDeclines`를 호출, 스킬 구매 시 `ApplySkillUse()`가 `BuffCalculator.StartDoubtDecline`을 호출.
- [EffectData.cs](../Scripts/SOs/detailData/EffectData.cs) — `EffectType.DoubtDecline`(13) 정의.
- [SkillManager.cs](../Scripts/manager/SkillManager.cs) — 스킬 구매 처리(`HandlePurchase`), `ApplySkillUse` 호출부.
- [MarketManager.cs](../Scripts/manager/MarketManager.cs) — 매 턴 `NextTurn()`이 `StatCalculator.Calculate()` 호출.
- [EventEffectFormatter.cs](../Scripts/UI/Utils/EventEffectFormatter.cs) / [UIFormat.cs](../Scripts/UI/Utils/UIFormat.cs)
  — 스킬 설명 패널에 "의심도 -N" 텍스트로 표시(라벨/부호 스위치).
- 대상 .asset 3개 : `Assets/Profile/스킬_프로파일/여론조작/실시간여론관리.asset`,
  `수상경력홍보.asset`, `후기마케팅.asset` — `effects[*].effectType: 13`.

## 작동 방식

**설계 요지** : 여론조작 카테고리 스킬 3종(실시간여론관리/수상경력홍보/후기마케팅)만 `DoubtDecrease`(즉시
1회 차감) 대신 `DoubtDecline`을 쓴다. 총량(effect.value, 기존 값 그대로 20/5/5)을 즉시 깎지 않고, **총량의
0.5%씩 200턴에 걸쳐 균등 분할 차감**한다. 방어/코인설계의 기존 `DoubtDecrease` 9개는 건드리지 않음(즉시
감소 그대로).

**스킬별 독립 버프** : `PlayerStat.DoubtDeclines`는 `SkillID`를 키로 하는 딕셔너리라, 서로 다른 스킬의
하락이 동시에 진행되면 매 턴 각자의 `PerTurn`이 전부 합산 차감된다(예: 세 스킬을 다 사면 매 턴
`20+5+5)*0.005`가 한꺼번에 깎임). **같은 스킬을 재구매**하면 그 스킬의 항목만 새 값으로 덮어써지고
(재사용 시 갱신, `Volume` 버프와 동일 패턴) 나머지 스킬의 진행 중인 하락은 그대로 유지된다.

**PlayerStat이 매 턴 새로 생성되는 구조와의 관계** : `StatCalculator.Calculate()`는 매 턴 `new PlayerStat()`을
만들고 이전 턴 값(`previous`)에서 필요한 필드를 이어받는 방식이다(`CurrentPrice`/`Support`/`Growth` 등과
동일 패턴). `DoubtDeclines`도 이 방식을 따라야 해서, `BuffCalculator.TickDoubtDeclines(stat, previous)`가
`previous.DoubtDeclines`를 순회하며 진행 중인(`TurnsRemaining > 0`) 항목만 새 `stat.DoubtDeclines`로
옮기고 `TurnsRemaining`을 하나씩 줄인다 — 만료된 항목은 옮기지 않아 자연히 사라진다(딕셔너리를 그대로
참조 복사하지 않고 매 턴 새 `DoubtDeclineBuff` 인스턴스로 옮기는 이유는 `previous`와 `stat`이 값을
공유하면 다음 턴 계산이 이전 턴 객체를 변형시키는 사이드이펙트가 생기기 때문).

**매 턴 재적용 제외** : 1회성(비재사용) 스킬은 구매 후 `SkillManager.EnableSkill`로 "활성" 상태가 되어
매 턴 `StatCalculator.ApplySkills() → ApplyToggleSkillEffects()`가 그 스킬의 effects를 다시 훑는다.
`DoubtDecline`은 이미 `ApplySkillUse` 시점에 1회만 `StartDoubtDecline`으로 처리되므로, `DoubtIncrease`/
`DoubtDecrease`/`Volume*`/`Supply*`와 같은 이유로 `ApplyToggleSkillEffects`의 제외 목록에 포함시켰다
(안 그러면 매 턴 `ApplyEffect`의 `default` 분기로 떨어져 경고 로그가 찍히거나, 최악의 경우 재사용형이
아닌 스킬에 한해 매 턴 하락이 재시작되는 버그가 된다).

## 호출 스택

### 1) 여론조작 스킬 구매 → 의심도 하락 시작

```
[구매 버튼 클릭] → EventHub.RaiseSkillPurchased()
 └─ SkillManager.HandlePurchase()                        [SkillManager.cs:89]
     ├─ (재사용형/1회성 공통) StatCalculator.ApplySkillUse(CurrentStat, skill.Profile)  [:107]
     │   └─ effect.effectType == DoubtDecline
     │       └─ BuffCalculator.StartDoubtDecline(stat, skill.id, effect.value)  [StatCalculator.cs:248-251]
     │           └─ stat.DoubtDeclines[skillId] = new DoubtDeclineBuff           [BuffCalculator.cs:54-61]
     │               { PerTurn = totalAmount * 0.005f, TurnsRemaining = 200 }
     ├─ StatCalculator.ClampStat(CurrentStat)             [SkillManager.cs:108]
     └─ (1회성이면) EnableSkill() + ApplyToggleSkillEffects() 즉시 1회 반영 [:119-123]
         └─ DoubtDecline은 제외 목록에 있어 여기서 다시 반영되지 않음   [StatCalculator.cs:179]
```

### 2) 매 턴 진행 → 등록된 하락들이 합산 차감

```
[EventHub.OnDayChanged] (TimeManager)
 └─ MarketManager.NextTurn()                              [MarketManager.cs:133]
     └─ StatCalculator.Calculate()                        [:143]
         ├─ stat.Doubt = previous.Doubt                    [StatCalculator.cs:38]
         └─ BuffCalculator.TickDoubtDeclines(stat, previous) [:39]
             └─ previous.DoubtDeclines 순회                 [BuffCalculator.cs:36-50]
                 └─ (TurnsRemaining > 0인 항목마다)
                     ├─ stat.Doubt -= entry.Value.PerTurn
                     └─ stat.DoubtDeclines[key] = { PerTurn, TurnsRemaining - 1 }
                         (0이 되면 다음 턴부터 옮겨지지 않음 → 하락 종료)
```

### 3) 스킬 정보 패널 텍스트 표시 (새 UI 코드 없이 기존 경로 재사용)

```
SkillPanelUI.RefreshDetail()                               [SkillPanelUI.cs:182]
 └─ EventEffectFormatter.BuildEffectsText(profile.effects)  [EventEffectFormatter.cs]
     └─ DescribeEffect(effect)
         ├─ label : DoubtDecrease/DoubtIncrease/DoubtDecline → "의심도"
         └─ delta : UIFormat.SignedEffectValue(effect)
             → DoubtDecline은 감소형으로 분류돼 -value 표시 (예: "의심도 -20")
```

## 알려진 이슈 / 주의점 후보

- **표시값은 "총량"이지 "진행 상황"이 아님** : 스킬 설명 패널은 `effect.value`(총량, 예 20)를 그대로
  "-20"으로 보여준다. 실제로는 200턴에 걸쳐 서서히 깎이는데, 현재 진행 중인 하락의 남은 턴 수나 이번 턴
  차감량(`PlayerStat.DoubtDeclines[skillId].PerTurn`/`TurnsRemaining`)을 UI 어디에서도 노출하지 않는다.
  플레이어가 "얼마나 남았는지" 알 방법이 없다 — 필요해지면 별도 UI 작업 필요.
- **`ApplyJob`은 `DoubtDecline`을 다루지 않음** : Job(직업) 데이터가 `DoubtDecline` 이펙트를 갖는 경우는
  현재 설계상 없다(여론조작 스킬 3종 전용). 만약 향후 Job에 `DoubtDecline`을 넣으면 `ApplyJob`/
  `ApplyJobSelection`에는 아무 처리가 없어 조용히 무시된다(경고 로그도 없음, `ApplyEffect`의
  `DoubtDecline` case가 "직접 반영 없음"으로 비어있기 때문) — 의도된 범위 밖의 사용이라 지금은 문제없음.
- **딕셔너리 순회 비용** : `TickDoubtDeclines`는 매 턴 `previous.DoubtDeclines` 전체를 순회한다.
  대상 스킬이 3개뿐이라 현재는 성능 문제 없음(최대 항목 수 3).
- **200턴/0.5% 값은 임시 밸런스 수치** : 세션 중 50→100→200턴으로 여러 번 조정됨(`BuffCalculator.cs`
  상단 상수). 실제 플레이 후 재조정 가능성 있음 — `Next_Tesk.md` "밸런스 수치 조정" 후보에 포함시킬 것.
