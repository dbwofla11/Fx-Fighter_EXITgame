using UnityEngine;
using TMPro;

// 캔들에 마우스를 올렸을 때 차트 좌상단에 뜨는 날짜/시가/종가 텍스트. 기본은 숨김.
public class PriceChartTooltip
{
    private readonly TextMeshProUGUI text;

    public PriceChartTooltip(RectTransform chartArea, int fontSize, Color color)
    {
        GameObject tooltip = new GameObject("Tooltip", typeof(RectTransform), typeof(TextMeshProUGUI));
        tooltip.transform.SetParent(chartArea, false);

        RectTransform rect = (RectTransform)tooltip.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(12f, -8f);
        rect.sizeDelta = new Vector2(600f, 32f);

        text = tooltip.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;

        tooltip.SetActive(false);
    }

    public void Show(PricePoint p)
    {
        text.text = $"{UIFormat.DateSlash(p.Date)}  Open {UIFormat.CurrencyTight(p.Open)} → Close {UIFormat.CurrencyTight(p.Close)}  거래량 {p.Volume:N0}";
        text.gameObject.SetActive(true);
    }

    public void Hide()
    {
        text.gameObject.SetActive(false);
    }
}
