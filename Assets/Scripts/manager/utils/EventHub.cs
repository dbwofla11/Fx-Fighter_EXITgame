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
    // 스킬 아이콘 클릭 요청 : 선택 상태(SelectedSkillId)만 저장한다 (구매는 OnSkillPurchased 버튼 전용)
    public static event Action<SkillID> OnSkillClicked;
    public static void RaiseSkillClicked(SkillID id) => OnSkillClicked?.Invoke(id);

    // 스킬 구매 버튼 클릭 요청 : 아이콘 클릭으로 선택해둔 스킬을 구매+적용한다. 재사용형은 잠기지 않고, 1회성은 최초 구매로 영구 해금된다.
    public static event Action OnSkillPurchased;
    public static void RaiseSkillPurchased() => OnSkillPurchased?.Invoke();

    // 스킬 구매가 실제로 성공(비용 지불+효과 적용 완료)했을 때만 발행 : VFX/SFX 트리거용.
    // OnSkillPurchased는 돈이 부족하거나 이미 잠긴 1회성 스킬이어도 그냥 호출되므로 구매 성공 신호로 못 쓴다.
    public static event Action<SkillID> OnSkillPurchaseSucceeded;
    public static void RaiseSkillPurchaseSucceeded(SkillID id) => OnSkillPurchaseSucceeded?.Invoke(id);

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
    // Supply
    // ==========================
    // 발행량 조작 요청 (양수 = 발행량 증가, 음수 = 발행량 감소). 추가발행권한 스킬을 구매하기 전에는 무시된다.
    public static event Action<long> OnManipulateSupply;
    public static void RaiseManipulateSupply(long amount) => OnManipulateSupply?.Invoke(amount);

    // ==========================
    // Market
    // ==========================
    // 시장 계산 완료 후 UI 갱신
    public static event Action<PlayerStat> OnMarketUpdated;
    public static void RaiseMarketUpdated(PlayerStat stat) => OnMarketUpdated?.Invoke(stat);

    // 시사 이벤트가 실제로 발생했을 때(자동/수동/무조건 발생 모두) 메인 화면에 알림을 띄우기 위한 통지
    public static event Action<EventLogEntry> OnEventTriggered;
    public static void RaiseEventTriggered(EventLogEntry entry) => OnEventTriggered?.Invoke(entry);

    // ==========================
    // System
    // ==========================
    // 매수/매도 모달 등 UI가 열려있는 동안 게임 진행을 멈춰야 할 때
    public static event Action OnGamePaused;
    public static void RaiseGamePaused() => OnGamePaused?.Invoke();

    public static event Action OnGameResumed;
    public static void RaiseGameResumed() => OnGameResumed?.Invoke();

    // ==========================
    // Ending
    // ==========================
    // 엑시트 시도 요청 (목표 금액 달성 후 활성화되는 버튼 클릭)
    public static event Action OnExitRequested;
    public static void RaiseExitRequested() => OnExitRequested?.Invoke();

    // 게임 종료(엔딩 확정) 통지
    public static event Action<EndingType> OnGameEnded;
    public static void RaiseGameEnded(EndingType ending) => OnGameEnded?.Invoke(ending);
}
