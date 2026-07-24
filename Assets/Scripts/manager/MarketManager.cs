using System.Collections.Generic;
using UnityEngine;

// 계산 결과를 최종적으로 관리하고 UI에 전달하는 관리자 
public class MarketManager : MonoBehaviour
{
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

    // 엑시트/영웅 엔딩 조건인 목표 자산. 코인 보유량과 무관하게 현금만 본다.
    public const long TargetAsset = 1_000_000_000L;

    // Doubt 자동 상승 : 게임 시간 2년(730턴)째 1회 +20, 그 이후로는 매 턴 +0.5씩 계속 증가한다.
    private const int DoubtAutoRiseStartTurn = 730;
    private const float DoubtAutoRiseInitialAmount = 20f;
    private const float DoubtAutoRisePerTurn = 0.5f;

    /// <summary>게임이 이미 끝났는지 여부 (엔딩 확정 후 true).</summary>
    public bool IsGameOver { get; private set; }

    /// <summary>엑시트 버튼을 누를 수 있는지 여부 (목표 자산 달성 여부만 본다).</summary>
    public bool CanExit => !IsGameOver && PlayerManager.Instance.currentMoney >= TargetAsset;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            CurrentStat = new PlayerStat();
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

    // 매 턴마다 ( 1일이 지날때 마다 패시브로 계산하는 함수 로직 )
    public void NextTurn()
    {
        if (IsGameOver)
            return;

        turnCount++;

        // 1. 내부 자동 시장 계산
        CurrentStat = StatCalculator.Calculate();

        ApplyDoubtAutoRise();

        // 1-1. 시사 이벤트 자동 발생 체크 (30턴마다 확률 판정)
        if (turnCount % NewsEventIntervalTurns == 0 && Random.value <= NewsEventChance)
        {
            TriggerNewsEvent();
        }

        ProbabilityCalculator.Calculate(CurrentStat);

        PriceCalculator.Calculate(CurrentStat);

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

    // 자동으로 판정되는 엔딩(체포/거지)을 확인한다. 둘 다 성립하면 체포가 우선이다.
    private void CheckAutomaticEndings()
    {
        if (CurrentStat.Doubt >= 100f)
        {
            EndGame(EndingType.Arrest);
        }
        else if (PlayerManager.Instance.currentMoney == 0 && PlayerManager.Instance.currentCoins == 0)
        {
            EndGame(EndingType.Broke);
        }
    }

    // 엑시트 버튼 클릭 요청 수신 : 목표 자산에 못 미치면 무시한다. 조건을 만족하면 Doubt/Support에 따라
    // 영웅 엔딩(True) 또는 엑시트 엔딩(Neutral)으로 갈린다.
    private void HandleExitRequested()
    {
        if (IsGameOver || PlayerManager.Instance.currentMoney < TargetAsset)
            return;

        if (CurrentStat.Doubt <= 50f && CurrentStat.Support >= 80f)
            EndGame(EndingType.Hero);
        else
            EndGame(EndingType.Exit);
    }

    // 엔딩을 확정하고 게임을 정지시킨다. 체포/거지 엔딩은 자산을 몰수하지 않는다 (수치는 그대로 둔다).
    private void EndGame(EndingType ending)
    {
        IsGameOver = true;
        TimeManager.Instance.PauseGame();
        EventHub.RaiseGameEnded(ending);
    }

    // 시사 이벤트 적용 요청 수신 (수동 트리거) : NextTurn()과 달리 다음 턴까지 기다리지 않고 즉시 반영한다.
    private void HandleNewsEvent()
    {
        TriggerNewsEvent();
        EventHub.RaiseMarketUpdated(CurrentStat);
    }

    // EventCalculator로 이벤트를 계산해 반영하고, 실제로 발생했으면 로그에 기록한다.
    private void TriggerNewsEvent()
    {
        EventSO fired = EventCalculator.Calculate(CurrentStat, eventDatabase);

        if (fired == null)
            return;

        runtimeEventData.Log.Add(new EventLogEntry
        {
            Profile = fired,
            Date = TimeManager.Instance.CurrentGameDate
        });
    }

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

}