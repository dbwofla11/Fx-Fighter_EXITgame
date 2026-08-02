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

    [SerializeField] // 시사 이벤트로 뽑힐 수 있는 이벤트 목록 (가중치 랜덤 선택, EventCalculator.PickWeighted 참고)
    private List<EventSO> eventDatabase;

    private RuntimeEventData runtimeEventData = new();

    /// <summary>지금까지 발생한 시사 이벤트 기록 (이벤트 로그 UI가 읽어서 그린다).</summary>
    public IReadOnlyList<EventLogEntry> EventLog => runtimeEventData.Log;

    private RuntimePriceHistory runtimePriceHistory = new();

    /// <summary>지금까지 지난 턴들의 가격 캔들 기록 (캔들 차트 UI가 읽어서 그린다). 1턴 = 캔들 1개.</summary>
    public IReadOnlyList<PricePoint> PriceHistory => runtimePriceHistory.Points;

    // 엑시트/영웅 엔딩 조건인 목표 자산. 코인 보유량과 무관하게 현금만 본다.
    public const long TargetAsset = 1_000_000_000L;

    // 게임 시작 시점의 코인 가격. CurrentPrice는 매 턴 이월되는 값이라 여기서 한 번만 설정하면 된다.
    private const float InitialPrice = 1000f;

    // Doubt 자동 상승 : 게임 시간 2년(730턴)째 1회 +20, 그 이후로는 매 턴 +0.5씩 계속 증가한다.
    private const int DoubtAutoRiseStartTurn = 730;
    private const float DoubtAutoRiseInitialAmount = 20f;
    private const float DoubtAutoRisePerTurn = 0.5f;

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
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        EventHub.OnDayChanged += NextTurn;
        EventHub.OnNewsEvent += HandleNewsEvent;
        EventHub.OnBuyCoin += HandleBuyCoin;
        EventHub.OnSellCoin += HandleSellCoin;
        EventHub.OnManipulateSupply += HandleManipulateSupply;
        EventHub.OnExitRequested += HandleExitRequested;
    }

    private void OnDisable()
    {
        EventHub.OnDayChanged -= NextTurn;
        EventHub.OnNewsEvent -= HandleNewsEvent;
        EventHub.OnBuyCoin -= HandleBuyCoin;
        EventHub.OnSellCoin -= HandleSellCoin;
        EventHub.OnManipulateSupply -= HandleManipulateSupply;
        EventHub.OnExitRequested -= HandleExitRequested;
    }

    #endregion

    #region 턴 진행

    // 매 턴마다 ( 1일이 지날때 마다 패시브로 계산하는 함수 로직 )
    public void NextTurn()
    {
        if (IsGameOver)
            return;

        turnCount++;

        float priceBefore = CurrentStat.CurrentPrice;

        // 1. 내부 자동 시장 계산
        CurrentStat = StatCalculator.Calculate();

        ApplyDoubtAutoRise();

        // 1-1. 시사 이벤트 자동 발생 체크 : 이번 턴에 무조건 발생하는 이벤트가 있으면 그걸 우선 발생시키고,
        // 없으면 기존 30턴마다 확률 판정으로 넘어간다.
        EventSO guaranteed = FindGuaranteedEvent(turnCount);

        if (guaranteed != null)
        {
            TriggerGuaranteedEvent(guaranteed);
        }
        else if (turnCount % NewsEventIntervalTurns == 0 && Random.value <= NewsEventChance)
        {
            TriggerNewsEvent();
        }

        StatCalculator.ClampStat(CurrentStat);

        ProbabilityCalculator.Calculate(CurrentStat);

        PriceCalculator.Calculate(CurrentStat);

        UpdateStreamerReaction(priceBefore);

        LogPricePoint(priceBefore);

        // 2. UI 갱신 이벤트 발행
        EventHub.RaiseMarketUpdated(CurrentStat);

        CheckAutomaticEndings();
    }

    // 게임 시간 2년(730턴)째 Doubt +20, 그 이후로는 매 턴 +0.5씩 계속 증가한다. Doubt는 감쇠하지 않으므로 그대로 누적된다.
    private void ApplyDoubtAutoRise()
    {
        if (turnCount == DoubtAutoRiseStartTurn)
            CurrentStat.Doubt += DoubtAutoRiseInitialAmount;
        else if (turnCount > DoubtAutoRiseStartTurn)
            CurrentStat.Doubt += DoubtAutoRisePerTurn;
    }

    #endregion

    #region 시사 이벤트

    // 시사 이벤트 적용 요청 수신 (수동 트리거) : NextTurn()과 달리 다음 턴까지 기다리지 않고 즉시 반영한다.
    private void HandleNewsEvent()
    {
        float priceBefore = CurrentStat.CurrentPrice;

        TriggerNewsEvent();

        StatCalculator.ClampStat(CurrentStat);

        UpdateStreamerReaction(priceBefore);
        EventHub.RaiseMarketUpdated(CurrentStat);
    }

    // EventCalculator로 이벤트를 계산해 반영하고, 실제로 발생했으면 로그에 기록한다.
    private void TriggerNewsEvent()
    {
        EventSO fired = EventCalculator.Calculate(CurrentStat, eventDatabase);

        if (fired == null)
            return;

        LogEvent(fired);
    }

    // eventDatabase 중 이번 턴(turn)에 무조건 발생하도록 지정된 이벤트를 찾는다 (없으면 null).
    private EventSO FindGuaranteedEvent(int turn)
    {
        foreach (EventSO candidate in eventDatabase)
        {
            if (candidate.guaranteedTurn == turn)
                return candidate;
        }

        return null;
    }

    // 확률 판정 없이 지정된 이벤트를 그대로 발생시킨다 (EventSO.guaranteedTurn 전용).
    private void TriggerGuaranteedEvent(EventSO chosen)
    {
        EventCalculator.Apply(CurrentStat, chosen);
        LogEvent(chosen);
    }

    private void LogEvent(EventSO fired)
    {
        EventLogEntry entry = new EventLogEntry
        {
            Profile = fired,
            Date = TimeManager.Instance.CurrentGameDate
        };

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
            PlayerManager.Instance.currentCoins);

        if (ending.HasValue)
            EndGame(ending.Value);
    }

    // 엑시트 버튼 클릭 요청 수신 : 목표 자산에 못 미치면 무시한다. 조건을 만족하면 EndingCalculator가
    // Doubt/Support 기준으로 영웅 엔딩(True) 또는 엑시트 엔딩(Neutral)을 판정한다.
    private void HandleExitRequested()
    {
        if (IsGameOver || PlayerManager.Instance.currentMoney < TargetAsset)
            return;

        EndGame(EndingCalculator.CheckExit(CurrentStat));
    }

    // 엔딩을 확정하고 게임을 정지시킨다. 체포/거지 엔딩은 자산을 몰수하지 않는다 (수치는 그대로 둔다).
    private void EndGame(EndingType ending)
    {
        IsGameOver = true;
        TimeManager.Instance.PauseGame();
        EventHub.RaiseGameEnded(ending);
    }

    #endregion

    #region 캔들 기록

    // 이번 턴의 가격 캔들(Open=턴 시작 전 가격, Close=턴 계산 후 가격)을 이력에 기록한다.
    // 시사 이벤트 수동 트리거(HandleNewsEvent)는 턴을 넘기지 않으므로 여기서는 기록하지 않는다 (1턴=1캔들 유지).
    private void LogPricePoint(float open)
    {
        runtimePriceHistory.Points.Add(new PricePoint
        {
            Date = TimeManager.Instance.CurrentGameDate,
            Open = open,
            Close = CurrentStat.CurrentPrice
        });
    }

    #endregion

    #region 스트리머 반응

    // 이번 턴(또는 수동 트리거)의 가격 변화량을 계산해 스트리머 반응 상태로 변환한다.
    private void UpdateStreamerReaction(float priceBefore)
    {
        float priceChange = CurrentStat.CurrentPrice - priceBefore;

        CurrentStat.PriceChangeThisTurn = priceChange;
        CurrentStat.StreamerReaction = StreamerReactionCalculator.Calculate(priceChange);
    }

    #endregion

    #region 거래 / 발행량

    // 코인 매수 요청 수신 -> Support/Growth에 직접 반영
    private void HandleBuyCoin(long amount)
    {
        TradeCalculator.Long(CurrentStat, amount);
    }

    // 코인 매도 요청 수신 -> Support/Growth에 직접 반영
    private void HandleSellCoin(long amount)
    {
        TradeCalculator.Short(CurrentStat, amount);
    }

    // 발행량 조작 요청 수신 : 추가발행권한 스킬을 구매하기 전에는 무시한다.
    private void HandleManipulateSupply(long amount)
    {
        if (!SkillManager.Instance.IsUnlocked(SkillID.추가발행권한))
            return;

        TradeCalculator.ManipulateSupply(CurrentStat, amount);
    }

    #endregion
}
