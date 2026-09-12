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

    public static event Action OnMonthChanged;
    public static void RaiseMonthChanged() => OnMonthChanged?.Invoke();

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

    // 스킬 구매 가능 상태(선행조건/구매횟수/쿨타임/잔액)가 바뀌었을 때 UI가 전체 트리를 다시 그리도록 하는 통지.
    public static event Action OnSkillTreeChanged;
    public static void RaiseSkillTreeChanged() => OnSkillTreeChanged?.Invoke();

    // 구매 요청이 거절된 이유. UI가 상세 안내를 붙일 수 있도록 판정 결과를 허브로 전달한다.
    public static event Action<SkillID, SkillPurchaseFailureReason> OnSkillPurchaseRejected;
    public static void RaiseSkillPurchaseRejected(SkillID id, SkillPurchaseFailureReason reason) =>
        OnSkillPurchaseRejected?.Invoke(id, reason);

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

    // 선택형 이벤트가 발생해 플레이어의 선택을 기다리는 신호
    public static event Action<EventChoiceRequest> OnEventChoiceRequired;
    public static void RaiseEventChoiceRequired(EventChoiceRequest request) => OnEventChoiceRequired?.Invoke(request);

    // 선택형 이벤트 선택 결과 전달
    public static event Action<int> OnEventChoiceSelected;
    public static void RaiseEventChoiceSelected(int choiceIndex) => OnEventChoiceSelected?.Invoke(choiceIndex);

    // 대출 이자 납부 여부를 기다리는 신호
    public static event Action<DebtPaymentRequest> OnDebtPaymentRequired;
    public static void RaiseDebtPaymentRequired(DebtPaymentRequest request) => OnDebtPaymentRequired?.Invoke(request);

    public static event Action<bool> OnDebtPaymentSelected;
    public static void RaiseDebtPaymentSelected(bool pay) => OnDebtPaymentSelected?.Invoke(pay);

    // 정기 이자 이벤트의 3개 선택 카드(납부/연기/도주) 선택 결과 전달.
    public static event Action<int> OnDebtPaymentChoiceSelected;
    public static void RaiseDebtPaymentChoiceSelected(int choiceIndex) =>
        OnDebtPaymentChoiceSelected?.Invoke(choiceIndex);

    // 엑시트 조건(MarketManager.CanExit)이 처음 충족된 순간(false→true) 1회만 발행 (AssetGoalNotifier).
    public static event Action OnAssetGoalAchieved;
    public static void RaiseAssetGoalAchieved() => OnAssetGoalAchieved?.Invoke();

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
