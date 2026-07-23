using System;

// UI와 Manager 사이를 중계하는 이벤트 허브.
// UI는 EventHub만 호출하고, Manager는 EventHub를 구독한다.
public static class EventHub
{
    // ==========================
    // Time
    // ==========================
    public static event Action OnDayChanged;
    public static void RaiseDayChanged() => OnDayChanged?.Invoke();

    // ==========================
    // Skill
    // ==========================
    // 스킬 아이콘 클릭 요청 : 토글형은 활성화/비활성화. 재사용형은 선택 상태(SelectedSkillId)만 저장한다 (구매는 OnSkillPurchased 버튼 전용)
    public static event Action<SkillID> OnSkillClicked;
    public static void RaiseSkillClicked(SkillID id) => OnSkillClicked?.Invoke(id);

    // 스킬 구매 버튼 클릭 요청 (재사용형 스킬 전용) : 아이콘 클릭으로 선택해둔 스킬을 구매+적용한다. 잠기지 않는다.
    public static event Action OnSkillPurchased;
    public static void RaiseSkillPurchased() => OnSkillPurchased?.Invoke();

    // ==========================
    // Job
    // ==========================
    // 직업 선택 요청
    public static event Action<JobSO> OnJobSelected;
    public static void RaiseJobSelected(JobSO job) => OnJobSelected?.Invoke(job);

    // ==========================
    // Trade
    // ==========================
    // 코인 매수 요청
    public static event Action<long> OnBuyCoin;
    public static void RaiseBuyCoin(long amount) => OnBuyCoin?.Invoke(amount);

    // 코인 매도 요청
    public static event Action<long> OnSellCoin;
    public static void RaiseSellCoin(long amount) => OnSellCoin?.Invoke(amount);

    // ==========================
    // Market
    // ==========================
    // 시사 이벤트 적용 요청
    public static event Action OnNewsEvent;
    public static void RaiseNewsEvent() => OnNewsEvent?.Invoke();

    // 시장 계산 완료 후 UI 갱신
    public static event Action<PlayerStat> OnMarketUpdated;
    public static void RaiseMarketUpdated(PlayerStat stat) => OnMarketUpdated?.Invoke(stat);
}
