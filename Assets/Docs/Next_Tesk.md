# 다음 작업

## 진행 상태

- **완료** : Support/Growth 감쇠. `PlayerStat.Support`/`Growth`가 `CurrentPrice`처럼 턴을 넘어 유지되는 값이 되었고,
  매 턴 `TradeCalculator.Decay(stat)`로 감쇠한다. Job 선택/거래(Long·Short)는 발생하는 순간 `MarketManager.CurrentStat`에
  직접 반영된다. 별도 누적 데이터 클래스(`RuntimeTradeData`, `JobBoostData`)는 시도했다가 불필요해서 제거했다.
  (`Logging.md` "Support/Growth 감쇠 (Decay) — 최종 구조" 참고, 공식은 `Game_Formula.md` 3장 / 3-1장)
- **남음** : 아래 Skill 재사용 시스템 전체.

## 목표

Support/Growth가 시간이 지나면 (마이너스 포함) 0으로 서서히 수렴한다 — 완료.

- Job 효과 : 완료. 선택 시점에 `CurrentStat`에 직접 반영 후 감쇠 (Support/Growth 외 효과는 계속 재적용, 변경 없음).
- Trade(Long/Short) : 완료. 거래 시점에 `CurrentStat`에 직접 반영 후 감쇠.
- Skill 효과 : `SkillSO`에 재사용 가능 여부를 정의해서 두 그룹으로 나눈다.
  - 재사용 가능 스킬(Support/Growth 부스트형) : 클릭 시 Job/Trade와 동일하게 `MarketManager.CurrentStat`에 **직접 1회 반영**, 즉시 잠김. 재구매(현금 차감)해야 다시 사용 가능. 감쇠는 이미 있는 `TradeCalculator.Decay(CurrentStat)`가 자동으로 처리 (별도 누적 데이터 불필요).
  - 재사용 불가 스킬(영구 효과형, 예: ExitUnlock) : 기존과 동일한 토글(On/Off) 방식 유지, `ApplySkills`에서 매 턴 재적용. 감쇠 없음.

---

## 왜 이렇게 바뀌어야 하는가

`StatCalculator.Calculate()`는 매 턴 `PlayerStat`을 새로 만든다. "어떤 상태(토글 On, 직업 선택 등)가 유지되는 동안
매 턴 효과가 자동으로 계속 재적용"되는 구조를 그대로 둔 채 최종 합계만 감쇠시키면, 다음 턴에 다시 100% 재적용되어
감쇠가 상쇄된다. 그래서 Support/Growth에 영향을 주는 모든 소스(Job 선택, Trade, 재사용형 Skill 사용)는
"발생 시점에 `CurrentStat`에 직접 반영 → 매 턴 그 값 자체가 감쇠" 패턴으로 통일한다.

처음엔 소스별로 누적 데이터 클래스(`RuntimeTradeData`, `JobBoostData`)를 따로 두고 `StatCalculator`가 매 턴
합산하는 방식으로 만들었는데, 불필요하게 복잡했다. `PlayerStat.Support`/`Growth` 자체를 `CurrentPrice`처럼
턴을 넘어 유지되는 값으로 만들고 각 소스가 그 값에 직접 더하고 빼는 지금 구조가 훨씬 단순하다. Skill도 같은
패턴을 그대로 따르면 된다 — 새 누적 클래스를 또 만들 필요가 없다.

다만 `ExitUnlock`처럼 "영구 해금" 성격의 효과까지 이 방식에 끼워 넣으면 해금했다가 다시 잠기는 이상한 상황이
생기므로, `SkillSO`에 재사용 가능 여부 플래그를 두어 스킬 종류별로 다르게 처리한다.

---

## 설계 (남은 부분 : Skill 재사용 시스템)

1. **`SkillSO`에 재사용 가능 여부 필드 추가**
   ```csharp
   [Header("Reuse")]
   public bool isReusable = true;
   // true  : Support/Growth 부스트형. 사용 시 CurrentStat에 직접 1회 반영 후 잠김, 재구매로 재사용
   // false : 영구 효과형(ExitUnlock 등). 기존 토글(EnableSkill/DisableSkill) 방식 유지, 감쇠 없음
   ```

2. **`SkillRuntimeInfo.IsEnabled` 의미가 스킬 종류에 따라 갈라짐**
   - `isReusable == true` : "이미 사용해서 잠김" 여부 (`true` = 사용됨/잠김, `false` = 사용 가능)
   - `isReusable == false` : 기존과 동일한 "토글 On/Off"

3. **재사용형 스킬은 초기 무료 사용 없음**
   - `SkillManager.Initialize()`에서 `isReusable == true`인 스킬은 `defaultUnlocked` 값과 무관하게 항상 `IsUnlocked = false`로 시작한다.
   - 재사용형 스킬은 최초 1회를 포함해 항상 구매(재구매) 절차를 거쳐야 사용할 수 있다.
   - `isReusable == false`(토글형) 스킬은 기존처럼 `IsUnlocked = defaultUnlocked` 그대로 둔다 (범위 밖, 변경 없음).

4. **`EventHub`에 `OnSkillPurchased` 이벤트 추가** (재구매 요청, 기존 `OnSkillClicked`=사용/토글 요청과 분리)

5. **`SkillManager.HandleSkillClicked(id)` 분기**
   ```
   if skill == null or !IsUnlocked: return

   if Profile.isReusable:
       if IsEnabled: return                              // 이미 사용해서 잠김
       StatCalculator.ApplySkillUse(MarketManager.Instance.CurrentStat, skill.Profile) // CurrentStat에 직접 1회 반영
       IsEnabled = true                                   // 잠금
   else:
       기존 로직 그대로 (IsEnabled 토글 On/Off, EnableSkill/DisableSkill)
   ```
   - `StatCalculator.ApplySkillUse`는 `ApplyJobSelection`과 동일한 형태로 추가 (스킬의 SupportIncrease/GrowthIncrease 효과만 stat에 직접 더함).

6. **`SkillManager.HandlePurchase(id)` (신규, `isReusable == true` 스킬 전용)**
   - `PlayerManager.TrySpend(cost)` 성공 시 `IsUnlocked = true`, `PurchaseCount++`, `IsEnabled = false`(잠금 해제)
   - `cost = baseCost × costMultiplier^PurchaseCount`
   - 기존 `PurchaseSkill`/`EnableSkill`/`DisableSkill`은 삭제하지 않고 위 흐름에 맞게 재구성

7. **`PlayerManager`에 `TrySpend(long amount)` 추가**
   - 잔액 부족 시 `false` 반환(차감 안 함), 충분하면 차감 후 `true`

8. **`StatCalculator`**
   - `ApplyJob`, 이월+감쇠 : 완료, 변경 불필요
   - `ApplySkills` : `isReusable == false`인 활성 스킬만 대상으로 기존 로직 유지 (매 턴 재적용)
   - `ApplySkillUse` (신규, `ApplyJobSelection`과 동일 패턴) : 스킬 사용 시점에 stat에 직접 반영
   - 감쇠는 이미 `Calculate()` 안에서 처리되므로 추가 작업 불필요

---

## 확인 필요 (구현 전 리뷰)

없음 — 모든 항목 확정됨.

재사용형 스킬은 역할이 "스탯(Support/Growth) 조절 전용"으로 확정. 다른 효과 타입(CashBonus 등)은 현재 스킬 데이터에도 없고,
나중에 추가되더라도 재사용형 스킬은 Support/Growth만 다루므로 `ApplySkillUse`는 그 두 값만 반영하면 된다.

---

## 완료 조건

- [x] 아무 행동도 하지 않으면 매 턴 Support/Growth가 (마이너스 포함) 0 방향으로 감소한다.
- [x] 직업을 유지해도 그 Support/Growth 영향력은 매 턴 감쇠한다 (Support/Growth 외 효과는 계속 유지).
- [x] Support/Growth는 바닥값 없이 자유롭게 움직인다 — 부정 이벤트/스킬/숏이 정상적으로 하락시킬 수 있다.
- [ ] 재사용 가능 스킬을 사용하면 즉시 잠기고, 재구매하지 않으면 다시 사용할 수 없다.
- [ ] 재구매 시 비용만큼 `PlayerManager.currentMoney`가 실제로 차감되고, 잔액 부족 시 실패한다.
- [ ] 재사용 불가 스킬(ExitUnlock 등)은 기존 토글 방식 그대로 동작한다.
- [ ] 재사용형 스킬을 쓰지 않아도 그 영향력이 매 턴 감쇠한다 (Trade/Job과 동일한 감쇠가 자동 적용됨, 별도 구현 불필요).
- [ ] 기존 계산 파이프라인 순서(이월+감쇠 → Job → Skill(토글형) → Probability → Price)는 유지한다.
