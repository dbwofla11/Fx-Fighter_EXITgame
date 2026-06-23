using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Player Stats")]
    public long currentMoney = 10000000; // 시작 자금 (예: 1천만 원)
    public long currentCoins = 0;        // 현재 보유 코인 수량

    // 지지도, 의심도 등은 나중에 여기에 추가

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 돈을 벌거나 쓸 때 호출할 함수
    public void AddMoney(long amount)
    {
        currentMoney += amount;
    }

    // 코인을 사거나 팔 때 호출할 함수
    public void AddCoin(long amount)
    {
        currentCoins += amount;
    }
}