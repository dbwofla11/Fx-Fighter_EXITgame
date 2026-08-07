using UnityEngine;

// 엑시트 엔딩 통계 요약 화면(ExitEndingSceneUI)에 표시할 6개 지표를 집계한다.
// PlayerManager/SkillManager 등과 동일한 DontDestroyOnLoad 싱글턴 패턴.
public class GameStatsTracker : MonoBehaviour
{
    public static GameStatsTracker Instance { get; private set; }

    public long ExitCash { get; private set; }
    public long MaxCoins { get; private set; }
    public int SkillPurchaseCount { get; private set; }
    public int BuyCount { get; private set; }
    public int SellCount { get; private set; }
    public int SupplyManipulateCount { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            MaxCoins = PlayerManager.StartingCoins; // AddCoin()을 안 거치는 시작 보유량도 최대치에 반영
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        EventHub.OnBuyCoin += HandleBuyCoin;
        EventHub.OnSellCoin += HandleSellCoin;
        EventHub.OnManipulateSupply += HandleManipulateSupply;
        EventHub.OnSkillPurchaseSucceeded += HandleSkillPurchaseSucceeded;
    }

    private void OnDisable()
    {
        EventHub.OnBuyCoin -= HandleBuyCoin;
        EventHub.OnSellCoin -= HandleSellCoin;
        EventHub.OnManipulateSupply -= HandleManipulateSupply;
        EventHub.OnSkillPurchaseSucceeded -= HandleSkillPurchaseSucceeded;
    }

    private void HandleBuyCoin(long amount) => BuyCount++;
    private void HandleSellCoin(long amount) => SellCount++;
    // MintButtonUI가 추가발행권한 해금 전엔 버튼 자체를 잠가두므로, 이 이벤트는 항상 실제 조작으로 이어진다.
    private void HandleManipulateSupply(long amount) => SupplyManipulateCount++;
    private void HandleSkillPurchaseSucceeded(SkillID id) => SkillPurchaseCount++;

    // PlayerManager.AddCoin()이 코인 보유량을 바꾸는 유일한 지점이라 거기서 호출한다.
    public void NotifyCoinsChanged(long currentCoins)
    {
        if (currentCoins > MaxCoins)
            MaxCoins = currentCoins;
    }

    // MarketManager.EndGame()이 엔딩을 확정하는 순간 호출한다.
    public void CaptureExitCash(long currentMoney)
    {
        ExitCash = currentMoney;
    }

    // 새 게임 시작 시 CharacterSelectUI가 호출한다.
    public void ResetState()
    {
        ExitCash = 0;
        MaxCoins = PlayerManager.StartingCoins;
        SkillPurchaseCount = 0;
        BuyCount = 0;
        SellCount = 0;
        SupplyManipulateCount = 0;
    }
}
