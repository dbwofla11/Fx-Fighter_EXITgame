using System.Collections.Generic;
using UnityEngine;

// 계산 결과를 최종적으로 관리하고 UI에 전달하는 관리자 
public class MarketManager : MonoBehaviour
{
    public static MarketManager Instance { get; private set; }

    // 계산된 현재 플레이어 능력치
    public PlayerStat CurrentStat { get; private set; }

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
        // 1. 내부 자동 시장 계산 ( 아직 이것도 다 안만듬 )
        CurrentStat = StatCalculator.Calculate();

        ProbabilityCalculator.Calculate(CurrentStat);

        PriceCalculator.Calculate(CurrentStat);

        // 2. UI 갱신 이벤트 발행
        EventHub.RaiseMarketUpdated(CurrentStat);
    }

    // 시사 이벤트 적용 요청 수신
    private void HandleNewsEvent()
    {
        new EventCalculator().Calculate();
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