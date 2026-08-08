using UnityEngine;
using UnityEngine.UI;
using TMPro;

// PriceChartUI 위에 그리는 평균 매입단가(평단가) 참조선 + 우측 가격 라벨. PriceChartGrid(좌측 격자 라벨)와
// 동일하게 오브젝트를 한 번만 만들어두고 Redraw()에서 위치만 갱신하는 풀링 방식. 보유 코인이 없어
// avgPrice가 0이면 숨긴다.
public class PriceChartAvgPriceLine
{
    private readonly float lineThickness;
    private readonly RectTransform lineRect;
    private readonly TextMeshProUGUI label;

    public PriceChartAvgPriceLine(RectTransform chartArea, float lineThickness, Color lineColor,
        int labelFontSize, float labelWidth)
    {
        this.lineThickness = lineThickness;

        GameObject line = new GameObject("AvgPriceLine", typeof(RectTransform), typeof(Image));
        line.transform.SetParent(chartArea, false);

        lineRect = (RectTransform)line.transform;
        lineRect.anchorMin = new Vector2(0f, 0f);
        lineRect.anchorMax = new Vector2(0f, 0f);
        lineRect.pivot = new Vector2(0f, 0.5f);

        Image lineImage = line.GetComponent<Image>();
        lineImage.color = lineColor;
        lineImage.raycastTarget = false;

        GameObject labelObj = new GameObject("AvgPriceLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(chartArea, false);

        RectTransform labelRect = (RectTransform)labelObj.transform;
        labelRect.anchorMin = new Vector2(1f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(1f, 0.5f);
        labelRect.sizeDelta = new Vector2(labelWidth, labelFontSize + 4f);

        label = labelObj.GetComponent<TextMeshProUGUI>();
        label.fontSize = labelFontSize;
        label.color = lineColor;
        label.alignment = TextAlignmentOptions.MidlineRight;
        label.raycastTarget = false;

        Hide();
    }

    // avgPrice <= 0(보유 코인 없음)이면 숨긴다. 가격 범위(min~min+range) 밖이면 위/아래 끝에 붙여 표시한다.
    public void Redraw(float avgPrice, float min, float range, float dateLabelAreaHeight, float candleAreaHeight, float chartWidth)
    {
        if (avgPrice <= 0f)
        {
            Hide();
            return;
        }

        float t = Mathf.Clamp01((avgPrice - min) / range);
        float y = dateLabelAreaHeight + t * candleAreaHeight;

        lineRect.gameObject.SetActive(true);
        lineRect.anchoredPosition = new Vector2(0f, y);
        lineRect.sizeDelta = new Vector2(chartWidth, lineThickness);

        label.gameObject.SetActive(true);
        label.rectTransform.anchoredPosition = new Vector2(-4f, y);
        label.text = "평단 " + UIFormat.CurrencyTight(avgPrice);
    }

    public void Hide()
    {
        lineRect.gameObject.SetActive(false);
        label.gameObject.SetActive(false);
    }
}
