using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Support/Growth/Doubt 게이지 패널(BaseWhiteBar + PositiveBar[/NegativeBar] + 값 텍스트) 공용 스크립트.
// EventHub.OnMarketUpdated를 구독해 매 턴 갱신한다. Support/Growth는 -100~100(negativeBar 사용),
// Doubt는 0~100(negativeBar 비움)이라 Inspector에서 negativeBar를 비워두면 Doubt 모드로 동작한다.
public class StatGaugeUI : MonoBehaviour
{
    private enum StatType { Support, Growth, Doubt }

    [SerializeField] private StatType statType;
    [SerializeField] private Image positiveBar;
    [SerializeField] private Image negativeBar;
    [SerializeField] private TextMeshProUGUI valueText;

    private const float MaxAbsValue = 100f;

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
        float value = statType switch
        {
            StatType.Support => stat.Support,
            StatType.Growth => stat.Growth,
            StatType.Doubt => stat.Doubt,
            _ => 0f,
        };

        if (negativeBar != null)
        {
            positiveBar.fillAmount = Mathf.Clamp01(Mathf.Max(0f, value) / MaxAbsValue);
            negativeBar.fillAmount = Mathf.Clamp01(Mathf.Max(0f, -value) / MaxAbsValue);
        }
        else
        {
            positiveBar.fillAmount = Mathf.Clamp01(value / MaxAbsValue);
        }

        valueText.text = (value >= 0 ? "+" : "") + value.ToString("F0");
    }
}
