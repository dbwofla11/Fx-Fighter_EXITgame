using UnityEngine;

// 엑시트 조건(MarketManager.CanExit, 현금 5억 보유)이 이번 게임에서 처음 충족되는 순간만 감지해
// EventHub.OnAssetGoalAchieved를 한 번 발행한다. 매수/매도 직후에도 OnMarketUpdated가 발행되므로
// (TradeModalUI) 턴 종료를 기다리지 않고 바로 감지된다.
public class AssetGoalNotifier : MonoBehaviour
{
    private bool hasNotifiedAchievement;

    private void OnEnable()
    {
        EventHub.OnMarketUpdated += HandleMarketUpdated;
    }

    private void OnDisable()
    {
        EventHub.OnMarketUpdated -= HandleMarketUpdated;
    }

    private void HandleMarketUpdated(PlayerStat stat)
    {
        if (hasNotifiedAchievement || MarketManager.Instance == null || !MarketManager.Instance.CanExit)
            return;

        hasNotifiedAchievement = true;
        EventHub.RaiseAssetGoalAchieved();
    }
}
