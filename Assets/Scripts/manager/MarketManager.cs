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
    }

    private void OnDisable()
    {
        EventHub.OnDayChanged -= NextTurn;
        EventHub.OnNewsEvent -= HandleNewsEvent;
        EventHub.OnBuyCoin -= HandleBuyCoin;
        EventHub.OnSellCoin -= HandleSellCoin;
    }

    // 매 턴마다 ( 1일이 지날때 마다 패시브로 계산하는 함수 로직 )
    public void NextTurn()
    {
        turnCount++;

        // 1. 내부 자동 시장 계산
        CurrentStat = StatCalculator.Calculate();

        // 1-1. 시사 이벤트 자동 발생 체크 (30턴마다 확률 판정)
        if (turnCount % NewsEventIntervalTurns == 0 && Random.value <= NewsEventChance)
        {
            TriggerNewsEvent();
        }

        ProbabilityCalculator.Calculate(CurrentStat);

        PriceCalculator.Calculate(CurrentStat);

        // 2. UI 갱신 이벤트 발행
        EventHub.RaiseMarketUpdated(CurrentStat);
    }

    // 시사 이벤트 적용 요청 수신 (수동 트리거)
    private void HandleNewsEvent()
    {
        TriggerNewsEvent();
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

}