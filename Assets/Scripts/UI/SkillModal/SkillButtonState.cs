// 스킬 아이콘/구매 버튼이 "지금 살 수 있는가"를 계산한다. SkillPanelUI는 이 결과를 색상/interactable에
// 적용만 하고, 잠금·잔액·의심도 판정 로직은 여기 한 곳에서만 관리한다.
public static class SkillButtonState
{
    public readonly struct State
    {
        public readonly bool Used;        // 1회성 스킬 구매 완료
        public readonly bool MaxedOut;    // 재사용형 스킬 최대 구매 횟수 도달
        public readonly bool OnCooldown;  // 재사용형 스킬 구매 후 다음 턴까지
        public readonly bool Affordable;  // 보유 금액 >= 현재 비용
        public readonly bool DoubtSafe;   // 구매해도 의심도가 체포 문턱을 넘지 않음
        public readonly bool PrerequisitesMet; // 선행 스킬을 모두 구매함
        public readonly long Cost;

        public State(bool used, bool maxedOut, bool onCooldown, bool affordable, bool doubtSafe,
            bool prerequisitesMet, long cost)
        {
            Used = used;
            MaxedOut = maxedOut;
            OnCooldown = onCooldown;
            Affordable = affordable;
            DoubtSafe = doubtSafe;
            PrerequisitesMet = prerequisitesMet;
            Cost = cost;
        }

        public bool Locked => Used || MaxedOut || OnCooldown || !PrerequisitesMet; // 돈과 무관하게 구매 자체가 막힌 상태
        public bool Purchasable => !Locked && Affordable && DoubtSafe;
    }

    public static State Evaluate(SkillID id)
    {
        SkillSO profile = SkillManager.Instance.GetSkillProfile(id);
        if (profile == null)
            return new State(false, true, false, false, true, false, 0);

        bool used = !profile.isReusable && SkillManager.Instance.IsUnlocked(id);
        bool maxedOut = SkillManager.Instance.IsMaxedOut(id);
        bool onCooldown = SkillManager.Instance.IsOnCooldown(id);
        long cost = SkillManager.Instance.GetCurrentCost(id);
        bool affordable = PlayerManager.Instance.currentMoney >= cost;

        bool prerequisitesMet = SkillManager.Instance.ArePrerequisitesMet(id);
        bool doubtSafe = SkillManager.Instance.IsSkillDoubtSafe(id);

        return new State(used, maxedOut, onCooldown, affordable, doubtSafe, prerequisitesMet, cost);
    }
}
