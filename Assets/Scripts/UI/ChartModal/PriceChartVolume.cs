using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 캔들 차트 하단에 같은 시간축으로 표시하는 거래량 막대 그래프.
// 일봉의 거래량 지수는 PricePoint에 기록되고, PriceChartUI가 주봉/월봉 단위로 합산해 넘긴다.
public class PriceChartVolume
{
    private const float MinBarHeight = 2f;
    private const float HeaderHeight = 22f;
    private const float HorizontalPadding = 8f;

    private readonly float barWidthRatio;
    private readonly Color upColor;
    private readonly Color downColor;
    private readonly float separatorThickness;

    private readonly List<RectTransform> bars = new();
    private readonly List<Image> barImages = new();
    private readonly RectTransform separator;
    private readonly TextMeshProUGUI titleLabel;
    private readonly TextMeshProUGUI maxLabel;

    public PriceChartVolume(RectTransform chartArea, int poolCapacity, float barWidthRatio,
        Color upColor, Color downColor, float separatorThickness, Color separatorColor,
        int labelFontSize, Color labelColor)
    {
        this.barWidthRatio = barWidthRatio;
        this.upColor = upColor;
        this.downColor = downColor;
        this.separatorThickness = separatorThickness;

        GameObject separatorObject = new GameObject("VolumeSeparator", typeof(RectTransform), typeof(Image));
        separatorObject.transform.SetParent(chartArea, false);
        separator = (RectTransform)separatorObject.transform;
        separator.anchorMin = Vector2.zero;
        separator.anchorMax = Vector2.zero;
        separator.pivot = new Vector2(0f, 0.5f);
        Image separatorImage = separatorObject.GetComponent<Image>();
        separatorImage.color = separatorColor;
        separatorImage.raycastTarget = false;

        for (int i = 0; i < poolCapacity; i++)
        {
            GameObject barObject = new GameObject($"VolumeBar_{i}", typeof(RectTransform), typeof(Image));
            barObject.transform.SetParent(chartArea, false);

            RectTransform bar = (RectTransform)barObject.transform;
            bar.anchorMin = Vector2.zero;
            bar.anchorMax = Vector2.zero;
            bar.pivot = new Vector2(0.5f, 0f);

            Image image = barObject.GetComponent<Image>();
            image.raycastTarget = false;

            bars.Add(bar);
            barImages.Add(image);
        }

        titleLabel = CreateLabel(chartArea, "VolumeTitle", labelFontSize, labelColor,
            TextAlignmentOptions.MidlineLeft);
        titleLabel.text = "거래량";

        maxLabel = CreateLabel(chartArea, "VolumeMaxLabel", labelFontSize, labelColor,
            TextAlignmentOptions.MidlineRight);
    }

    public void Redraw(IReadOnlyList<PricePoint> history, int startIndex, int count, float slotWidth,
        float areaBottom, float areaHeight, float chartWidth)
    {
        float maxVolume = 1f;
        for (int i = 0; i < count; i++)
            maxVolume = Mathf.Max(maxVolume, history[startIndex + i].Volume);

        float graphHeight = Mathf.Max(1f, areaHeight - HeaderHeight);
        float barWidth = slotWidth * barWidthRatio;

        separator.gameObject.SetActive(true);
        separator.anchoredPosition = new Vector2(0f, areaBottom + areaHeight);
        separator.sizeDelta = new Vector2(chartWidth, separatorThickness);

        titleLabel.gameObject.SetActive(true);
        titleLabel.rectTransform.anchoredPosition = new Vector2(HorizontalPadding,
            areaBottom + areaHeight - HeaderHeight);
        titleLabel.rectTransform.sizeDelta = new Vector2(120f, HeaderHeight);

        maxLabel.gameObject.SetActive(true);
        maxLabel.rectTransform.anchoredPosition = new Vector2(-HorizontalPadding,
            areaBottom + areaHeight - HeaderHeight);
        maxLabel.rectTransform.sizeDelta = new Vector2(160f, HeaderHeight);
        maxLabel.text = maxVolume.ToString("N0");

        for (int i = 0; i < bars.Count; i++)
        {
            if (i >= count)
            {
                bars[i].gameObject.SetActive(false);
                continue;
            }

            PricePoint point = history[startIndex + i];
            float normalized = Mathf.Clamp01(point.Volume / maxVolume);
            float height = Mathf.Max(MinBarHeight, normalized * graphHeight);
            float x = slotWidth * i + slotWidth * 0.5f;

            RectTransform bar = bars[i];
            bar.gameObject.SetActive(true);
            bar.anchoredPosition = new Vector2(x, areaBottom);
            bar.sizeDelta = new Vector2(barWidth, height);
            barImages[i].color = point.Close >= point.Open ? upColor : downColor;
        }
    }

    public void HideAll()
    {
        foreach (RectTransform bar in bars)
            bar.gameObject.SetActive(false);

        separator.gameObject.SetActive(false);
        titleLabel.gameObject.SetActive(false);
        maxLabel.gameObject.SetActive(false);
    }

    private static TextMeshProUGUI CreateLabel(RectTransform parent, string name, int fontSize,
        Color color, TextAlignmentOptions alignment)
    {
        GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)labelObject.transform;
        bool alignRight = alignment == TextAlignmentOptions.MidlineRight;
        rect.anchorMin = alignRight ? Vector2.right : Vector2.zero;
        rect.anchorMax = alignRight ? Vector2.right : Vector2.zero;
        rect.pivot = alignRight ? new Vector2(1f, 0f) : Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        return label;
    }
}
