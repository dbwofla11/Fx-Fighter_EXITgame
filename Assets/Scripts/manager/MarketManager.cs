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


    // 매 턴마다 ( 1일이 지날때 마다 패시브로 계산하는 함수 로직 )
    public void NextTurn()
    {
        // 1. 내부 자동 시장 계산 ( 아직 이것도 다 안만듬 )
        CurrentStat = StatCalculator.Calculate();

        ProbabilityCalculator.Calculate(CurrentStat);

        PriceCalculator.Calculate(CurrentStat);

        // 2. UI 갱신시키는 로직 
        // 아직 연결안함 
        // UpdateUI();
    }

}