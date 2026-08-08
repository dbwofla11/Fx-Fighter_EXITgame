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

**중복 적용(2026-08-08 변경)** : `PlayerStat.DoubtDeclines`는 `List<DoubtDeclineBuff>`라, 서로 다른
스킬의 하락은 물론 **같은 스킬을 여러 번 재구매한 경우도** 매 턴 진행 중인 항목 전부의 `PerTurn`이 합산
차감된다(예: `실시간여론관리`를 두 번 사면 `20*0.005`짜리 항목이 두 개 쌓여 매 턴 `0.2`씩 깎임). 원래는
`Dictionary<SkillID, DoubtDeclineBuff>`라 같은 스킬 재구매 시 기존 진행 중인 항목을 새 값으로 덮어써서
사실상 타이머만 리셋되고 하락량은 안 쌓이는 문제가 있었는데, 재사용형 스킬(최대 10회 구매 가능)의 재구매
효과가 없는 셈이라 리스트로 바꿔 매 구매마다 새 항목이 추가(중복 적용)되도록 고쳤다.

**PlayerStat이 매 턴 새로 생성되는 구조와의 관계** : `StatCalculator.Calculate()`는 매 턴 `new PlayerStat()`을
만들고 이전 턴 값(`previous`)에서 필요한 필드를 이어받는 방식이다(`CurrentPrice`/`Support`/`Growth` 등과
동일 패턴). `DoubtDeclines`도 이 방식을 따라야 해서, `BuffCalculator.TickDoubtDeclines(stat, previous)`가
`previous.DoubtDeclines`를 순회하며 진행 중인(`TurnsRemaining > 0`) 항목만 새 `stat.DoubtDeclines`로
옮기고 `TurnsRemaining`을 하나씩 줄인다 — 만료된 항목은 옮기지 않아 자연히 사라진다(리스트를 그대로
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
     │           └─ stat.DoubtDeclines.Add(new DoubtDeclineBuff { ... })        [BuffCalculator.cs:54-62]
     │               { SkillId, PerTurn = totalAmount * 0.005f, TurnsRemaining = 200 } (기존 항목 유지, 새로 추가)
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
             └─ previous.DoubtDeclines 순회                 [BuffCalculator.cs:36-51]
                 └─ (TurnsRemaining > 0인 항목마다)
                     ├─ stat.Doubt -= entry.PerTurn
                     └─ stat.DoubtDeclines.Add({ SkillId, PerTurn, TurnsRemaining - 1 })
                         (0이 되면 다음 턴부터 옮겨지지 않음 → 그 항목만 하락 종료, 나머지는 계속 진행)
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
  "-20"으로 보여준다. 실제로는 200턴에 걸쳐 서서히 깎이는데, 현재 진행 중인 하락들의 남은 턴 수나 이번 턴
  합산 차감량(`PlayerStat.DoubtDeclines`의 각 `PerTurn`/`TurnsRemaining`)을 UI 어디에서도 노출하지 않는다.
  플레이어가 "얼마나 남았는지"/"몇 개나 쌓여있는지" 알 방법이 없다 — 필요해지면 별도 UI 작업 필요.
- **`ApplyJob`은 `DoubtDecline`을 다루지 않음** : Job(직업) 데이터가 `DoubtDecline` 이펙트를 갖는 경우는
  현재 설계상 없다(여론조작 스킬 3종 전용). 만약 향후 Job에 `DoubtDecline`을 넣으면 `ApplyJob`/
  `ApplyJobSelection`에는 아무 처리가 없어 조용히 무시된다(경고 로그도 없음, `ApplyEffect`의
  `DoubtDecline` case가 "직접 반영 없음"으로 비어있기 때문) — 의도된 범위 밖의 사용이라 지금은 문제없음.
- **리스트 순회 비용 + 중복 적용으로 인한 무한 증식 가능성** : `TickDoubtDeclines`는 매 턴
  `previous.DoubtDeclines` 전체를 순회한다. 재구매 시 덮어쓰지 않고 계속 추가되므로, 이론상 스킬 3종을
  최대 구매 횟수(`SkillManager.MaxPurchaseCount`=10)까지 반복 구매하면 최대 30개까지 쌓일 수 있다 —
  여전히 성능 문제가 될 규모는 아니지만, 밸런스상 Doubt가 과도하게 빨리 깎일 수 있다는 뜻이므로(중복 적용이
  이번 요청의 의도이긴 하나) 실제 플레이 후 200턴/0.5% 값과 함께 상한(예: 스킬당 동시 진행 가능 개수 제한)
  필요 여부도 같이 재검토할 것.
- **200턴/0.5% 값은 임시 밸런스 수치** : 세션 중 50→100→200턴으로 여러 번 조정됨(`BuffCalculator.cs`
  상단 상수). 실제 플레이 후 재조정 가능성 있음 — `Next_Tesk.md` "밸런스 수치 조정" 후보에 포함시킬 것.
