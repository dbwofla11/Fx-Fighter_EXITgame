using UnityEngine;

// 의심도(PlayerStat.Doubt) 구간에 따라 메인 BGM을 3단계로 전환한다.
// EventHub.OnMarketUpdated를 구독해 StatGaugeUI와 동일한 패턴으로 갱신하되,
// 티어가 실제로 바뀔 때만 PlayBGM/StopBGM을 호출해 매턴 크로스페이드가 겹치는 것을 막는다.
public class SuspicionBgmController : MonoBehaviour
{
    private enum Tier { Low, Mid, High }

    private const float MidThreshold = 55f;
    private const float HighThreshold = 60f;

    [SerializeField] private AudioClip lowDoubtClip;  // Doubt < MidThreshold
    [SerializeField] private AudioClip highDoubtClip; // Doubt >= HighThreshold (Mid 구간은 무음)

    private Tier? currentTier;

    private void OnEnable()
    {
        EventHub.OnMarketUpdated += HandleMarketUpdated;
        if (MarketManager.Instance != null)
            HandleMarketUpdated(MarketManager.Instance.CurrentStat);
    }

    private void OnDisable()
    {
        EventHub.OnMarketUpdated -= HandleMarketUpdated;
    }

    private void HandleMarketUpdated(PlayerStat stat)
    {
        Tier tier = stat.Doubt >= HighThreshold ? Tier.High
            : stat.Doubt >= MidThreshold ? Tier.Mid
            : Tier.Low;

        if (tier == currentTier)
            return;
        currentTier = tier;

        if (AudioManager.Instance == null)
            return;

        switch (tier)
        {
            case Tier.Low:
                AudioManager.Instance.PlayBGM(lowDoubtClip);
                break;
            case Tier.Mid:
                AudioManager.Instance.StopBGM();
                break;
            case Tier.High:
                AudioManager.Instance.PlayBGM(highDoubtClip);
                break;
        }
    }
}
