// 스킬 아이콘/구매 버튼이 "지금 살 수 있는가"를 계산한다. SkillPanelUI는 이 결과를 색상/interactable에
// 적용만 하고, 잠금·잔액·의심도 판정 로직은 여기 한 곳에서만 관리한다.
public static class SkillButtonState
{
    // 이 값 이상이면 체포 엔딩(EndingCalculator) — 구매로 이 문턱을 넘게 되는 스킬은 잔액 부족과 동일하게 막는다.
    private const float DoubtArrestThreshold = 100f;

    public readonly struct State
    {
        public readonly bool Used;        // 1회성 스킬 구매 완료
        public readonly bool MaxedOut;    // 재사용형 스킬 최대 구매 횟수 도달
        public readonly bool Affordable;  // 보유 금액 >= 현재 비용
        public readonly bool DoubtSafe;   // 구매해도 의심도가 체포 문턱을 넘지 않음
        public readonly long Cost;

        public State(bool used, bool maxedOut, bool affordable, bool doubtSafe, long cost)
        {
            Used = used;
            MaxedOut = maxedOut;
            Affordable = affordable;
            DoubtSafe = doubtSafe;
            Cost = cost;
        }

        public bool Locked => Used || MaxedOut; // 돈과 무관하게 구매 자체가 막힌 상태
        public bool Purchasable => !Locked && Affordable && DoubtSafe;
    }

    public static State Evaluate(SkillID id)
    {
        SkillSO profile = SkillManager.Instance.GetSkillProfile(id);
        if (profile == null)
            return new State(false, true, false, true, 0);

        bool used = !profile.isReusable && SkillManager.Instance.IsUnlocked(id);
        bool maxedOut = SkillManager.Instance.IsMaxedOut(id);
        long cost = SkillManager.Instance.GetCurrentCost(id);
        bool affordable = PlayerManager.Instance.currentMoney >= cost;

        float predictedDoubt = MarketManager.Instance.CurrentStat.Doubt + PredictDoubtDelta(profile);
        bool doubtSafe = predictedDoubt < DoubtArrestThreshold;

        return new State(used, maxedOut, affordable, doubtSafe, cost);
    }

    // 구매 즉시 반영되는 DoubtIncrease/DoubtDecrease만 계산한다 (StatCalculator.ApplySkillUse와 동일 대상).
    // DoubtDecline은 즉시 반영이 아니라 200턴에 걸쳐 서서히 깎이는 버프라 여기서 제외한다.
    private static float PredictDoubtDelta(SkillSO profile)
    {
        float delta = 0f;

        foreach (EffectData effect in profile.effects)
        {
            if (effect.effectType == EffectType.DoubtIncrease)
                delta += effect.value;
            else if (effect.effectType == EffectType.DoubtDecrease)
                delta -= effect.value;
        }

        return delta;
    }
}
