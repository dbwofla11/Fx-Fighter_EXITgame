using System.Collections.Generic;
using UnityEngine;

// 계산 결과를 최종적으로 관리하고 UI에 전달하는 관리자
public class MarketManager : MonoBehaviour
{
    #region 필드 / 프로퍼티

    public static MarketManager Instance { get; private set; }

    // 계산된 현재 플레이어 능력치
    public PlayerStat CurrentStat { get; private set; }

    // 시사 이벤트 자동 발생 : NewsEventIntervalTurns턴마다 NewsEventChance 확률로 체크한다.
    private const int NewsEventIntervalTurns = 30;
    private const float NewsEventChance = 0.4f;
    private int turnCount;
    private decimal cashInterestRemainder;
    private float playerTradeAmountThisTurn;
    private int negativeCashStreak;
    private float pendingTurnPriceBefore;
    private EventSO pendingChoiceEvent;
    private bool awaitingEventChoice;
    private bool awaitingDebtPayment;

    // 상폐 기준 가격 : 이 가격 이하로 연속 방치되면 거지 엔딩(상폐)으로 처리한다. PriceCalculator.MinPrice(1,
    // 가격이 내려갈 수 있는 절대 하한)와는 별개 값이다.
    private const float DelistingPriceThreshold = 2f;

    // 가격이 DelistingPriceThreshold 이하로 붙은 채로 연속된 턴 수. NextTurn()마다 갱신되고, 그 위로
    // 벗어나면 즉시 0으로 리셋된다 (EndingCalculator.PriceFloorStreakLimit 도달 시 거지 엔딩).
    private int priceFloorStreak;

    [SerializeField] // 시사 이벤트로 뽑힐 수 있는 이벤트 목록 (가중치 랜덤 선택, EventCalculator.PickWeighted 참고)
    private List<EventSO> eventDatabase;

    private RuntimeEventData runtimeEventData = new();
    private readonly DebtManager debtManager = new();

    /// <summary>지금까지 발생한 시사 이벤트 기록 (이벤트 로그 UI가 읽어서 그린다).</summary>
    public IReadOnlyList<EventLogEntry> EventLog => runtimeEventData.Log;
    public DebtManager Debt => debtManager;
    public int CurrentTurn => turnCount;
    public bool IsAwaitingEventChoice => awaitingEventChoice;
    public bool IsAwaitingDebtPayment => awaitingDebtPayment;

    public int GetChoiceSelectionCount(EventSO profile, int choiceIndex)
    {
        return runtimeEventData.GetChoiceSelectionCount(profile, choiceIndex);
    }

    private RuntimePriceHistory runtimePriceHistory = new();

    /// <summary>지금까지 지난 턴들의 가격 캔들 기록 (캔들 차트 UI가 읽어서 그린다). 1턴 = 캔들 1개.</summary>
    public IReadOnlyList<PricePoint> PriceHistory => runtimePriceHistory.Points;

    // 엑시트 엔딩 조건인 목표 자산. 코인 보유량과 무관하게 현금만 본다.
    public const long TargetAsset = 500_000_000L;

    // 게임 시작 시점의 코인 가격. CurrentPrice는 매 턴 이월되는 값이라 여기서 한 번만 설정하면 된다.
    private const float InitialPrice = 10f;

    // 게임 시작 시점의 발행량. Supply도 CurrentPrice와 동일하게 턴을 넘어 이월되는 값이라 여기서 한 번만 설정한다.
    private const float InitialSupply = 2000f;

    // 게임 시작 시점의 스트리머 지수(중립 50). CurrentPrice와 동일하게 턴을 넘어 이월되는 값이라 여기서 한 번만 설정한다.
    private const float InitialStreamerIndex = 50f;

    // Doubt 자동 상승 : 게임 시간 2년(730턴)째 1회 +4, 그 이후로는 매 턴 +0.1을 기본으로 하되 경과 연차에
    // 비례해 턴당 증가량 자체가 선형으로 커진다(ApplyDoubtAutoRise 참고) — 후반부로 갈수록 의심도 관리가
    // 점점 빡빡해지라는 2026-08-08 재밸런스 피드백. 지수 증가는 아님(가속도 자체는 연차당 일정).
    // 2026-08-05에 자동 추적 증가율이 너무 빠르다는 피드백으로 기존 수치(20 / 0.5)의 5분의 1로 조정.
    private const int DoubtAutoRiseStartTurn = 730;
    private const float DoubtAutoRiseInitialAmount = 4f;
    private const float DoubtAutoRisePerTurn = 0.1f;
    private const float DoubtAutoRiseAccelPerYear = 0.05f;
    private const float TurnsPerYear = 365f;

    /// <summary>게임이 이미 끝났는지 여부 (엔딩 확정 후 true).</summary>
    public bool IsGameOver { get; private set; }

    /// <summary>엑시트 버튼을 누를 수 있는지 여부 (목표 자산 달성 여부만 본다).</summary>
    public bool CanExit => !IsGameOver && PlayerManager.Instance.currentMoney >= TargetAsset;

    #endregion

    #region Unity 생명주기

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            CurrentStat = new PlayerStat();
            CurrentStat.CurrentPrice = InitialPrice;
            CurrentStat.Supply = InitialSupply;
            CurrentStat.StreamerIndex = InitialStreamerIndex;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 보유 코인(PlayerManager.currentCoins)도 이미 발행된 코인이므로 발행량에 포함시킨다.
    // PlayerManager.Instance는 Awake 시점엔 초기화 순서가 보장되지 않아 Start에서 더한다(Start는 씬의
    // 모든 Awake가 끝난 뒤 호출되므로 여기선 항상 값이 준비돼 있다).
    private void Start()
    {
        // 중복 인스턴스(Awake에서 Destroy 예약된 쪽)는 Destroy가 이번 프레임 끝에 처리되기 전까지 Start가
        // 먼저 도는데, 그쪽은 CurrentStat이 아예 초기화 안 됐으므로 여기서 걸러야 널 참조가 안 난다.
        if (Instance != this)
            return;

        CurrentStat.Supply += PlayerManager.Instance.currentCoins;
    }

    // 새 게임 시작 시 CharacterSelectUI가 호출한다 (DontDestroyOnLoad라 두 번째 플레이부터는 Awake가 다시 안 불림).
    // PlayerManager.ResetState()가 먼저 호출되어 currentCoins가 초기값(10000)으로 돌아온 뒤 불려야 한다.
    public void ResetState()
    {
        CurrentStat = new PlayerStat();
        CurrentStat.CurrentPrice = InitialPrice;
        CurrentStat.Supply = InitialSupply + PlayerManager.Instance.currentCoins;
        CurrentStat.StreamerIndex = InitialStreamerIndex;

        turnCount = 0;
        cashInterestRemainder = 0m;
        playerTradeAmountThisTurn = 0f;
        priceFloorStreak = 0;
        negativeCashStreak = 0;
        pendingTurnPriceBefore = 0f;
        pendingChoiceEvent = null;
        awaitingEventChoice = false;
        awaitingDebtPayment = false;
        debtManager.Reset();
        IsGameOver = false;
        runtimeEventData = new RuntimeEventData();
        runtimePriceHistory = new RuntimePriceHistory();
    }

    private void OnEnable()
    {
        EventHub.OnDayChanged += NextTurn;
        EventHub.OnMonthChanged += PayMonthlyCashInterest;
        EventHub.OnBuyCoin += HandleBuyCoin;
        EventHub.OnSellCoin += HandleSellCoin;
        EventHub.OnManipulateSupply += HandleManipulateSupply;
        EventHub.OnExitRequested += HandleExitRequested;
        EventHub.OnEventChoiceSelected += HandleEventChoiceSelected;
        EventHub.OnDebtPaymentSelected += HandleDebtPaymentSelected;
    }

    private void OnDisable()
    {
        EventHub.OnDayChanged -= NextTurn;
        EventHub.OnMonthChanged -= PayMonthlyCashInterest;
        EventHub.OnBuyCoin -= HandleBuyCoin;
        EventHub.OnSellCoin -= HandleSellCoin;
        EventHub.OnManipulateSupply -= HandleManipulateSupply;
        EventHub.OnExitRequested -= HandleExitRequested;
        EventHub.OnEventChoiceSelected -= HandleEventChoiceSelected;
        EventHub.OnDebtPaymentSelected -= HandleDebtPaymentSelected;
    }

    #endregion

    #region 턴 진행

    // 매 턴마다 ( 1일이 지날때 마다 패시브로 계산하는 함수 로직 )
    public void NextTurn()
    {
        if (IsGameOver || awaitingEventChoice || awaitingDebtPayment)
            return;

        turnCount++;

        // 기획 순서: 30턴 주기의 대출 이자를 먼저 처리한 뒤 시장/이벤트를 계산한다.
        if (debtManager.IsPaymentDue(turnCount))
        {
            awaitingDebtPayment = true;
            EventHub.RaiseDebtPaymentRequired(debtManager.CreatePaymentRequest(turnCount));
            return;
        }

        ResolveTurn();
    }

    private void ResolveTurn()
    {
        if (IsGameOver)
            return;

        pendingTurnPriceBefore = CurrentStat.CurrentPrice;

        // 1. 내부 자동 시장 계산
        CurrentStat = StatCalculator.Calculate();

        ApplyDoubtAutoRise();

        // 1-1. 시사 이벤트 자동 발생 체크 : 이번 턴에 무조건 발생하는 이벤트가 있으면 그걸 우선 발생시키고,
        // 없으면 기존 30턴마다 확률 판정으로 넘어간다.
        EventSO guaranteed = FindGuaranteedEvent(turnCount);

        if (guaranteed != null)
        {
            TriggerEvent(guaranteed);
        }
        else if (turnCount % NewsEventIntervalTurns == 0 && Random.value <= NewsEventChance)
        {
            TriggerEvent(EventCalculator.SelectEvent(CurrentStat, eventDatabase));
        }

        if (awaitingEventChoice)
            return;

        FinishTurn();
    }

    private void FinishTurn()
    {
        StatCalculator.ClampStat(CurrentStat);

        ProbabilityCalculator.Calculate(CurrentStat);

        PriceCalculator.Calculate(CurrentStat);

        UpdatePriceFloorStreak();

        UpdateStreamerReaction(pendingTurnPriceBefore);

        LogPricePoint(pendingTurnPriceBefore);

        // 2. UI 갱신 이벤트 발행
        EventHub.RaiseMarketUpdated(CurrentStat);

        UpdateNegativeCashStreak();
        CheckAutomaticEndings();
    }

    // 게임 시간 2년(730턴)째 Doubt +4, 그 이후로는 매 턴 증가량이 경과 연차에 비례해 선형으로 커진다
    // (2~3년차 0.1/턴 → 4~5년차 0.2/턴 → 6~7년차 0.3/턴 ...). Doubt는 감쇠하지 않으므로 그대로 누적된다.
    private void ApplyDoubtAutoRise()
    {
        if (turnCount == DoubtAutoRiseStartTurn)
        {
            CurrentStat.Doubt += DoubtAutoRiseInitialAmount;
        }
        else if (turnCount > DoubtAutoRiseStartTurn)
        {
            float yearsElapsed = (turnCount - DoubtAutoRiseStartTurn) / TurnsPerYear;
            CurrentStat.Doubt += DoubtAutoRisePerTurn + DoubtAutoRiseAccelPerYear * yearsElapsed;
        }
    }

    // 가격이 상폐 기준 이하로 붙어있으면 연속 턴 수를 늘리고, 벗어나면 리셋한다.
    private void UpdatePriceFloorStreak()
    {
        priceFloorStreak = CurrentStat.CurrentPrice <= DelistingPriceThreshold ? priceFloorStreak + 1 : 0;
    }

    #endregion

    #region 시사 이벤트

    private void TriggerEvent(EventSO fired)
    {
        if (fired == null)
            return;

        if (fired.choices != null && fired.choices.Count > 0)
        {
            pendingChoiceEvent = fired;
            awaitingEventChoice = true;
            EventHub.RaiseEventChoiceRequired(new EventChoiceRequest(
                fired, TimeManager.Instance.CurrentGameDate, turnCount));
            return;
        }

        EventCalculator.Apply(CurrentStat, fired);
        LogEvent(new EventLogEntry
        {
            Profile = fired,
            Date = TimeManager.Instance.CurrentGameDate
        });
    }

    // eventDatabase 중 이번 턴(turn)에 무조건 발생하도록 지정된 이벤트를 찾는다 (없으면 null).
    private EventSO FindGuaranteedEvent(int turn)
    {
        if (eventDatabase == null)
            return null;

        foreach (EventSO candidate in eventDatabase)
        {
            if (candidate != null && candidate.guaranteedTurn == turn)
                return candidate;
        }

        return null;
    }

    // 대출 이자 납부 여부를 반영한 뒤 같은 턴의 시장 계산을 계속한다.
    private void HandleDebtPaymentSelected(bool pay)
    {
        if (!awaitingDebtPayment || IsGameOver)
            return;

        if (pay)
            debtManager.Pay(PlayerManager.Instance);
        else
            debtManager.Defer();

        awaitingDebtPayment = false;
        ResolveTurn();
    }

    private void PayMonthlyCashInterest()
    {
        if (IsGameOver || PlayerManager.Instance.currentMoney <= 0) return;

        long principal = PlayerManager.Instance.currentMoney;
        // 고정 연 0.1%를 12개월로 월할. 원 미만은 이월하고 정수 현금의 오버플로를 막는다.
        // 나누기 전 잔여분을 보관해 1/12 순환소수의 누적 오차도 피한다.
        decimal accrued = principal * 0.001m + cashInterestRemainder;
        long interest = (long)System.Math.Min(decimal.Truncate(accrued / 12m), (decimal)long.MaxValue - principal);
        cashInterestRemainder = accrued % 12m;
        PlayerManager.Instance.AddMoney(interest);

        var entry = new EventLogEntry
        {
            Date = TimeManager.Instance.CurrentGameDate,
            CashInterest = interest,
            InterestPrincipal = principal
        };
        LogEvent(entry);
    }

    private void HandleEventChoiceSelected(int choiceIndex)
    {
        if (!awaitingEventChoice || pendingChoiceEvent == null || IsGameOver)
            return;

        if (pendingChoiceEvent.choices == null || choiceIndex < 0 || choiceIndex >= pendingChoiceEvent.choices.Count)
            return;

        EventChoice choice = pendingChoiceEvent.choices[choiceIndex];
        long cashBefore = PlayerManager.Instance.currentMoney;
        long debtBefore = debtManager.TotalDebt;
        int previousSelectionCount = runtimeEventData.GetChoiceSelectionCount(pendingChoiceEvent, choiceIndex);
        EventChoiceResolution resolution = EventCalculator.ResolveChoice(CurrentStat, choice, choiceIndex,
            previousSelectionCount, cashBefore, debtManager.OverdueCount);

        if (!resolution.Valid)
            return;

        PlayerManager.Instance.AddMoney(-resolution.CashPaid);
        PlayerManager.Instance.AddMoney(resolution.CashDelta);
        PlayerManager.Instance.AddMoney(-resolution.FailureCashLoss);
        debtManager.AddPrincipal(resolution.DebtAdded);
        runtimeEventData.IncrementChoiceSelection(pendingChoiceEvent, choiceIndex);

        StatCalculator.ClampStat(CurrentStat);

        EventLogEntry entry = new EventLogEntry
        {
            Profile = pendingChoiceEvent,
            Date = TimeManager.Instance.CurrentGameDate,
            ChoiceIndex = resolution.ChoiceIndex,
            ChoiceLabel = resolution.ChoiceLabel,
            HasChoiceResult = true,
            SuccessProbability = resolution.SuccessProbability,
            Succeeded = resolution.Succeeded,
            CashBefore = cashBefore,
            CashAfter = PlayerManager.Instance.currentMoney,
            DebtBefore = debtBefore,
            DebtAfter = debtManager.TotalDebt,
            ResultEffects = resolution.Succeeded ? choice.successEffects : choice.failureEffects,
            ResultSupplyDelta = resolution.Succeeded ? choice.successSupplyDelta : choice.failureSupplyDelta,
            ResultPriceRatio = resolution.Succeeded ? choice.successPriceRatio : -choice.failurePriceRate
        };

        LogEvent(entry);
        pendingChoiceEvent = null;
        awaitingEventChoice = false;
        FinishTurn();
    }

    private void LogEvent(EventLogEntry entry)
    {
        runtimeEventData.Log.Add(entry);
        EventHub.RaiseEventTriggered(entry);
    }

    #endregion

    #region 엔딩 판정

    // 자동으로 판정되는 엔딩(체포/거지)을 확인한다. 판정 자체는 EndingCalculator가 순수하게 계산하고,
    // 여기서는 그 결과로 실제 게임 종료 처리만 수행한다.
    private void CheckAutomaticEndings()
    {
        EndingType? ending = EndingCalculator.CheckAutomatic(
            CurrentStat,
            PlayerManager.Instance.currentMoney,
            PlayerManager.Instance.currentCoins,
            priceFloorStreak,
            negativeCashStreak);

        if (ending.HasValue)
            EndGame(ending.Value);
    }

    // 엑시트 버튼 클릭 요청 수신 : 목표 자산에 못 미치면 무시한다. 조건을 만족하면 엑시트 엔딩으로 종료한다.
    private void HandleExitRequested()
    {
        if (IsGameOver || PlayerManager.Instance.currentMoney < TargetAsset)
            return;

        EndGame(EndingCalculator.CheckExit());
    }

    // 엔딩을 확정하고 게임을 정지시킨다. 체포/거지 엔딩은 자산을 몰수하지 않는다 (수치는 그대로 둔다).
    private void EndGame(EndingType ending)
    {
        IsGameOver = true;
        TimeManager.Instance.PauseGame();
        GameStatsTracker.Instance?.CaptureExitCash(PlayerManager.Instance.currentMoney);
        EventHub.RaiseGameEnded(ending);
    }

    private void UpdateNegativeCashStreak()
    {
        negativeCashStreak = PlayerManager.Instance.currentMoney < 0 ? negativeCashStreak + 1 : 0;
    }

    #endregion

    #region 캔들 기록

    // 이번 턴의 가격 캔들(Open=턴 시작 전 가격, Close=턴 계산 후 가격)과 거래량 지수를 이력에 기록한다.
    private void LogPricePoint(float open)
    {
        runtimePriceHistory.Points.Add(new PricePoint
        {
            Date = TimeManager.Instance.CurrentGameDate,
            Open = open,
            Close = CurrentStat.CurrentPrice,
            Volume = PriceCalculator.CalculateVolumeIndex(CurrentStat.Volume, open, CurrentStat.CurrentPrice,
                playerTradeAmountThisTurn, turnCount)
        });

        playerTradeAmountThisTurn = 0f;
    }

    #endregion

    #region 스트리머 반응

    // StreamerIndex가 한 턴에 움직일 수 있는 최대 폭. 가격이 아무리 크게 흔들려도 이 값 이상 못 움직이게
    // 막아서, 한 번의 큰 변동만으로 반응이 바로 튀지 않고 여러 턴 같은 방향이 이어져야 구간(20점)을 넘게 한다.
    private const float StreamerIndexMaxStepPerTurn = 6f;
    private const float StreamerIndexSensitivity = 0.15f;

    // 이번 턴의 가격 변화량을 StreamerIndex에 완만하게 누적하고, 그 구간으로 스트리머 반응 상태를 정한다.
    private void UpdateStreamerReaction(float priceBefore)
    {
        float priceChange = CurrentStat.CurrentPrice - priceBefore;
        CurrentStat.PriceChangeThisTurn = priceChange;

        float step = Mathf.Clamp(priceChange * StreamerIndexSensitivity, -StreamerIndexMaxStepPerTurn, StreamerIndexMaxStepPerTurn);
        CurrentStat.StreamerIndex = Mathf.Clamp(CurrentStat.StreamerIndex + step, 0f, 100f);

        CurrentStat.StreamerReaction = StreamerReactionCalculator.Calculate(CurrentStat.StreamerIndex);
    }

    #endregion

    #region 거래 / 발행량

    // 코인 매수 요청 수신 -> Support/Growth에 직접 반영. Doubt도 함께 오르므로(TradeCalculator.Long)
    // 거래만으로 체포 엔딩 조건(Doubt>=100)에 닿을 수 있어 NextTurn()을 기다리지 않고 바로 확인한다.
    private void HandleBuyCoin(long amount)
    {
        TradeCalculator.Long(CurrentStat, amount);
        playerTradeAmountThisTurn += Mathf.Abs((float)amount);
        StatCalculator.ClampStat(CurrentStat);
        CheckAutomaticEndings();
    }

    // 코인 매도 요청 수신 -> Support/Growth에 직접 반영. HandleBuyCoin과 동일한 이유로 즉시 엔딩을 확인한다.
    private void HandleSellCoin(long amount)
    {
        TradeCalculator.Short(CurrentStat, amount);
        playerTradeAmountThisTurn += Mathf.Abs((float)amount);
        StatCalculator.ClampStat(CurrentStat);
        CheckAutomaticEndings();
    }

    // 발행량 조작 요청 수신 : 추가발행권한 스킬을 구매하기 전에는 무시한다.
    // 발행(증가)/소각(감소)한 만큼 플레이어 보유 코인도 함께 늘거나 준다 — 발행 주체가 곧 플레이어이므로.
    // 이 조작도 Doubt를 직접 올리므로(TradeCalculator.ManipulateSupply) 거래와 동일하게 즉시 엔딩을 확인한다.
    private void HandleManipulateSupply(long amount)
    {
        if (!SkillManager.Instance.IsUnlocked(SkillID.추가발행권한))
            return;

        TradeCalculator.ManipulateSupply(CurrentStat, amount);
        PlayerManager.Instance.AddCoin(amount);
        StatCalculator.ClampStat(CurrentStat);
        CheckAutomaticEndings();
    }

    #endregion
}
