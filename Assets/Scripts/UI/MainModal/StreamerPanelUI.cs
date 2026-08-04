using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// RightPanel의 스트리머 캐릭터 패널. EventHub.OnMarketUpdated를 구독해 PlayerStat.StreamerReaction
// (StreamerReactionState 5단계)에 맞는 표정 스프라이트로 교체한다. StatGaugeUI/CoinPriceHeaderUI와 동일 패턴.
// 거래 확정(OnBuyCoin/OnSellCoin) 시 살짝 흔들려서 반응하는 연출을 더한다.
public class StreamerPanelUI : MonoBehaviour
{
    [SerializeField] private Image streamerImage;
    [SerializeField] private Sprite crashSprite;
    [SerializeField] private Sprite downSprite;
    [SerializeField] private Sprite neutralSprite;
    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite surgeSprite;

    private const float ShakeDuration = 0.3f;
    private const float ShakeMagnitude = 10f;

    private RectTransform imageRect;
    private Vector2 imageBasePos;
    private Coroutine shakeRoutine;

    private void OnEnable()
    {
        EventHub.OnMarketUpdated += HandleMarketUpdated;
        EventHub.OnBuyCoin += HandleTrade;
        EventHub.OnSellCoin += HandleTrade;

        if (imageRect == null)
        {
            imageRect = (RectTransform)streamerImage.transform;
            imageBasePos = imageRect.anchoredPosition;
        }

        if (MarketManager.Instance != null)
            HandleMarketUpdated(MarketManager.Instance.CurrentStat);
    }

    private void OnDisable()
    {
        EventHub.OnMarketUpdated -= HandleMarketUpdated;
        EventHub.OnBuyCoin -= HandleTrade;
        EventHub.OnSellCoin -= HandleTrade;
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

    private void HandleTrade(long amount)
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    // 거래 확정은 대개 모달이 열려 Time.timeScale이 0인 상태에서 일어나므로 unscaled time을 쓴다.
    private IEnumerator ShakeRoutine()
    {
        float t = 0f;
        while (t < ShakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float damper = 1f - t / ShakeDuration;
            imageRect.anchoredPosition = imageBasePos + Random.insideUnitCircle * ShakeMagnitude * damper;
            yield return null;
        }
        imageRect.anchoredPosition = imageBasePos;
    }
}
