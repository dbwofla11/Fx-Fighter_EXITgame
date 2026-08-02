using UnityEngine;
using UnityEngine.UI;

// RightPanel의 스트리머 캐릭터 패널. EventHub.OnMarketUpdated를 구독해 PlayerStat.StreamerReaction
// (StreamerReactionState 5단계)에 맞는 표정 스프라이트로 교체한다. StatGaugeUI/CoinPriceHeaderUI와 동일 패턴.
public class StreamerPanelUI : MonoBehaviour
{
    [SerializeField] private Image streamerImage;
    [SerializeField] private Sprite crashSprite;
    [SerializeField] private Sprite downSprite;
    [SerializeField] private Sprite neutralSprite;
    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite surgeSprite;

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
        streamerImage.sprite = stat.StreamerReaction switch
        {
            StreamerReactionState.Crash => crashSprite,
            StreamerReactionState.Down => downSprite,
            StreamerReactionState.Up => upSprite,
            StreamerReactionState.Surge => surgeSprite,
            _ => neutralSprite,
        };
    }
}
