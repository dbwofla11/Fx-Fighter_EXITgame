using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// PriceChartUI 배경의 가격대 안내용 가로 격자선 + 왼쪽 가격 라벨.
// gridLineCount개만큼 미리 만들어두고, Redraw()에서 위치/문구만 갱신하는 오브젝트 풀링 방식.
public class PriceChartGrid
{
    private readonly int lineCount;
    private readonly float lineThickness;

    private readonly List<RectTransform> lines = new();
    private readonly List<TextMeshProUGUI> priceLabels = new();

    public PriceChartGrid(RectTransform chartArea, int lineCount, float lineThickness, Color lineColor,
        int labelFontSize, Color labelColor, float labelWidth)
    {
        this.lineCount = lineCount;
        this.lineThickness = lineThickness;

        for (int j = 0; j < lineCount; j++)
        {
            GameObject line = new GameObject($"GridLine_{j}", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(chartArea, false);

            RectTransform lineRect = (RectTransform)line.transform;
            lineRect.anchorMin = new Vector2(0f, 0f);
            lineRect.anchorMax = new Vector2(0f, 0f);
            lineRect.pivot = new Vector2(0f, 0.5f);

            Image lineImage = line.GetComponent<Image>();
            lineImage.color = lineColor;
            lineImage.raycastTarget = false;

            lines.Add(lineRect);

            GameObject label = new GameObject($"GridPriceLabel_{j}", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(chartArea, false);

            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0f, 0f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.sizeDelta = new Vector2(labelWidth, labelFontSize + 4f);

            TextMeshProUGUI labelText = label.GetComponent<TextMeshProUGUI>();
            labelText.fontSize = labelFontSize;
            labelText.color = labelColor;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.raycastTarget = false;

            priceLabels.Add(labelText);
        }
    }

    // 가격 범위를 lineCount+1개 구간으로 나누는 가로 격자선과, 각 선의 가격을 보여주는 왼쪽 라벨을 갱신한다.
    public void Redraw(float min, float range, float dateLabelAreaHeight, float candleAreaHeight, float chartWidth)
    {
        for (int j = 0; j < lineCount; j++)
        {
            float t = (j + 1) / (float)(lineCount + 1);
            float y = dateLabelAreaHeight + t * candleAreaHeight;
            float price = min + t * range;

            RectTransform lineRect = lines[j];
            lineRect.gameObject.SetActive(true);
            lineRect.anchoredPosition = new Vector2(0f, y);
            lineRect.sizeDelta = new Vector2(chartWidth, lineThickness);

            TextMeshProUGUI label = priceLabels[j];
            label.gameObject.SetActive(true);
            label.rectTransform.anchoredPosition = new Vector2(4f, y);
            label.text = UIFormat.CurrencyTight(price);
        }
    }

    public void Hide()
    {
        foreach (RectTransform rect in lines)
            rect.gameObject.SetActive(false);
        foreach (TextMeshProUGUI label in priceLabels)
            label.gameObject.SetActive(false);
    }
}
