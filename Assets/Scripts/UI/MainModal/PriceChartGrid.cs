using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// PriceChartUI 배경의 격자(가로: 가격대, 세로: 시간대) + 왼쪽 가격 라벨.
// 가로선은 lineCount개, 세로선도 같은 lineCount개만큼 만들어 체크무늬로 겹치게 한다. 세로선은 순수
// 장식(날짜 라벨은 이미 캔들마다 따로 있음)이라 별도 라벨 없이 선만 그린다. 전부 오브젝트 풀링 방식.
public class PriceChartGrid
{
    private readonly int lineCount;
    private readonly float lineThickness;

    private readonly List<RectTransform> horizontalLines = new();
    private readonly List<TextMeshProUGUI> priceLabels = new();
    private readonly List<RectTransform> verticalLines = new();

    public PriceChartGrid(RectTransform chartArea, int lineCount, float lineThickness, Color lineColor,
        int labelFontSize, Color labelColor, float labelWidth)
    {
        this.lineCount = lineCount;
        this.lineThickness = lineThickness;

        for (int j = 0; j < lineCount; j++)
        {
            GameObject line = new GameObject($"GridLineH_{j}", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(chartArea, false);

            RectTransform lineRect = (RectTransform)line.transform;
            lineRect.anchorMin = new Vector2(0f, 0f);
            lineRect.anchorMax = new Vector2(0f, 0f);
            lineRect.pivot = new Vector2(0f, 0.5f);

            Image lineImage = line.GetComponent<Image>();
            lineImage.color = lineColor;
            lineImage.raycastTarget = false;

            horizontalLines.Add(lineRect);

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

            GameObject vLine = new GameObject($"GridLineV_{j}", typeof(RectTransform), typeof(Image));
            vLine.transform.SetParent(chartArea, false);

            RectTransform vLineRect = (RectTransform)vLine.transform;
            vLineRect.anchorMin = new Vector2(0f, 0f);
            vLineRect.anchorMax = new Vector2(0f, 0f);
            vLineRect.pivot = new Vector2(0.5f, 0f);

            Image vLineImage = vLine.GetComponent<Image>();
            vLineImage.color = lineColor;
            vLineImage.raycastTarget = false;

            verticalLines.Add(vLineRect);
        }
    }

    // 가격 범위를 lineCount+1개 구간으로 나누는 가로 격자선 + 가격 라벨, 캔들 영역 폭을 lineCount+1개
    // 구간으로 나누는 세로 격자선을 함께 갱신한다.
    public void Redraw(float min, float range, float dateLabelAreaHeight, float candleAreaHeight, float chartWidth)
    {
        for (int j = 0; j < lineCount; j++)
        {
            float t = (j + 1) / (float)(lineCount + 1);
            float y = dateLabelAreaHeight + t * candleAreaHeight;
            float price = min + t * range;

            RectTransform lineRect = horizontalLines[j];
            lineRect.gameObject.SetActive(true);
            lineRect.anchoredPosition = new Vector2(0f, y);
            lineRect.sizeDelta = new Vector2(chartWidth, lineThickness);

            TextMeshProUGUI label = priceLabels[j];
            label.gameObject.SetActive(true);
            label.rectTransform.anchoredPosition = new Vector2(4f, y);
            label.text = UIFormat.CurrencyTight(price);

            float x = t * chartWidth;

            RectTransform vLineRect = verticalLines[j];
            vLineRect.gameObject.SetActive(true);
            vLineRect.anchoredPosition = new Vector2(x, dateLabelAreaHeight);
            vLineRect.sizeDelta = new Vector2(lineThickness, candleAreaHeight);
        }
    }

    public void Hide()
    {
        foreach (RectTransform rect in horizontalLines)
            rect.gameObject.SetActive(false);
        foreach (TextMeshProUGUI label in priceLabels)
            label.gameObject.SetActive(false);
        foreach (RectTransform rect in verticalLines)
            rect.gameObject.SetActive(false);
    }
}
