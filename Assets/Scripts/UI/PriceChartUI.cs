using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// MarketManager.PriceHistory 기반의 캔들스틱 가격 차트.
// 캔들 1개 = PricePoint 1개(Open→Close), 최근 visibleCandleCount개만 오브젝트 풀링으로 그린다.
// 이 오브젝트의 RectTransform 자체를 차트 영역으로 사용한다.
public class PriceChartUI : MonoBehaviour
{
    [SerializeField] private int visibleCandleCount = 16;
    [SerializeField] private float candleWidthRatio = 0.95f;
    [SerializeField] private float minCandleHeight = 4f;
    [SerializeField] private Color upColor = new Color(0.2941176f, 0.4117647f, 0.1843137f);   // BtnLong과 동일 색
    [SerializeField] private Color downColor = new Color(0.6745098f, 0.1960784f, 0.1960784f); // BtnShort와 동일 색

    [Header("X축 날짜 라벨")]
    [SerializeField] private float dateLabelAreaHeight = 32f;
    [SerializeField] private int dateLabelFontSize = 18;
    [SerializeField] private Color dateLabelColor = new Color(0.4f, 0.4f, 0.4f);

    [Header("호버 툴팁")]
    [SerializeField] private int tooltipFontSize = 22;
    [SerializeField] private Color tooltipColor = new Color(0.15f, 0.15f, 0.15f);

    private RectTransform chartArea;
    private readonly List<RectTransform> candlePool = new();
    private readonly List<Image> candleImages = new();
    private readonly List<TextMeshProUGUI> dateLabels = new();
    private readonly List<PricePoint> boundPoints = new();
    private TextMeshProUGUI tooltipText;

    private void Awake()
    {
        chartArea = (RectTransform)transform;
        BuildPool();
        BuildTooltip();
    }

    private void OnEnable()
    {
        EventHub.OnMarketUpdated += HandleMarketUpdated;
        Redraw();
    }

    private void OnDisable()
    {
        EventHub.OnMarketUpdated -= HandleMarketUpdated;
    }

    private void HandleMarketUpdated(PlayerStat stat) => Redraw();

    // 캔들/날짜 라벨로 쓸 오브젝트를 visibleCandleCount개만큼 미리 만들어두고, Redraw()에서 위치/크기/색/문구만 갱신한다.
    private void BuildPool()
    {
        for (int i = 0; i < visibleCandleCount; i++)
        {
            GameObject candle = new GameObject($"Candle_{i}", typeof(RectTransform), typeof(Image));
            candle.transform.SetParent(chartArea, false);

            RectTransform rect = (RectTransform)candle.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);

            Image image = candle.GetComponent<Image>();
            image.raycastTarget = true; // 호버로 정보를 보여주려면 레이캐스트를 받아야 한다

            CandleHoverTarget hover = candle.AddComponent<CandleHoverTarget>();
            hover.owner = this;
            hover.index = i;

            candlePool.Add(rect);
            candleImages.Add(image);
            boundPoints.Add(null);

            GameObject label = new GameObject($"DateLabel_{i}", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(chartArea, false);

            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);

            TextMeshProUGUI labelText = label.GetComponent<TextMeshProUGUI>();
            labelText.fontSize = dateLabelFontSize;
            labelText.color = dateLabelColor;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.raycastTarget = false;

            dateLabels.Add(labelText);
        }
    }

    // 호버 시 뜨는 정보(날짜/시가/종가) 텍스트. 캔들 영역 좌상단에 고정, 기본은 숨김.
    private void BuildTooltip()
    {
        GameObject tooltip = new GameObject("Tooltip", typeof(RectTransform), typeof(TextMeshProUGUI));
        tooltip.transform.SetParent(chartArea, false);

        RectTransform rect = (RectTransform)tooltip.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(12f, -8f);
        rect.sizeDelta = new Vector2(600f, 32f);

        tooltipText = tooltip.GetComponent<TextMeshProUGUI>();
        tooltipText.fontSize = tooltipFontSize;
        tooltipText.color = tooltipColor;
        tooltipText.alignment = TextAlignmentOptions.MidlineLeft;
        tooltipText.raycastTarget = false;

        tooltip.SetActive(false);
    }

    private void Redraw()
    {
        if (MarketManager.Instance == null)
            return;

        IReadOnlyList<PricePoint> history = MarketManager.Instance.PriceHistory;
        int count = Mathf.Min(history.Count, visibleCandleCount);

        if (count == 0)
        {
            foreach (RectTransform rect in candlePool)
                rect.gameObject.SetActive(false);
            foreach (TextMeshProUGUI label in dateLabels)
                label.gameObject.SetActive(false);
            return;
        }

        int startIndex = history.Count - count;

        float min = float.MaxValue;
        float max = float.MinValue;

        for (int i = 0; i < count; i++)
        {
            PricePoint p = history[startIndex + i];
            min = Mathf.Min(min, Mathf.Min(p.Open, p.Close));
            max = Mathf.Max(max, Mathf.Max(p.Open, p.Close));
        }

        // 캔들이 차트 위/아래 끝에 딱 붙지 않도록 여백을 준다.
        float range = Mathf.Max(max - min, 1f);
        float padding = range * 0.1f;
        min -= padding;
        max += padding;
        range = max - min;

        float chartWidth = chartArea.rect.width;
        // 아래쪽은 날짜 라벨 자리로 비워두고, 그 위 영역에만 캔들을 그린다.
        float candleAreaHeight = chartArea.rect.height - dateLabelAreaHeight;
        float slotWidth = chartWidth / visibleCandleCount;
        float candleWidth = slotWidth * candleWidthRatio;

        for (int i = 0; i < visibleCandleCount; i++)
        {
            RectTransform rect = candlePool[i];
            TextMeshProUGUI label = dateLabels[i];

            if (i >= count)
            {
                rect.gameObject.SetActive(false);
                label.gameObject.SetActive(false);
                boundPoints[i] = null;
                continue;
            }

            PricePoint p = history[startIndex + i];
            boundPoints[i] = p;

            float bodyTop = Mathf.Max(p.Open, p.Close);
            float bodyBottom = Mathf.Min(p.Open, p.Close);

            float yBottom = dateLabelAreaHeight + (bodyBottom - min) / range * candleAreaHeight;
            float yTop = dateLabelAreaHeight + (bodyTop - min) / range * candleAreaHeight;
            float height = Mathf.Max(yTop - yBottom, minCandleHeight);

            float x = slotWidth * i + slotWidth * 0.5f;

            rect.gameObject.SetActive(true);
            rect.anchoredPosition = new Vector2(x, yBottom);
            rect.sizeDelta = new Vector2(candleWidth, height);

            candleImages[i].color = p.Close >= p.Open ? upColor : downColor;

            label.gameObject.SetActive(true);
            label.rectTransform.anchoredPosition = new Vector2(x, 4f);
            label.rectTransform.sizeDelta = new Vector2(slotWidth, dateLabelAreaHeight - 4f);
            label.text = FormatDate(p.Date);
        }
    }

    private void ShowTooltip(int index)
    {
        PricePoint p = boundPoints[index];

        if (p == null)
            return;

        tooltipText.text = $"{FormatDate(p.Date)}  Open ₩{p.Open:N0} → Close ₩{p.Close:N0}";
        tooltipText.gameObject.SetActive(true);
    }

    private void HideTooltip()
    {
        tooltipText.gameObject.SetActive(false);
    }

    // DateTime.ToString("MM/dd")은 시스템 문화권에 따라 "/"가 다른 구분자로 바뀔 수 있어(예: "01-01") 고정 포맷으로 직접 만든다.
    private static string FormatDate(System.DateTime date) => $"{date.Month:00}/{date.Day:00}";

    // 캔들 위에 마우스를 올리면 그 캔들이 지금 어떤 PricePoint를 표시 중인지 owner에게 알려 툴팁을 띄운다.
    private class CandleHoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public PriceChartUI owner;
        public int index;

        public void OnPointerEnter(PointerEventData eventData) => owner.ShowTooltip(index);

        public void OnPointerExit(PointerEventData eventData) => owner.HideTooltip();
    }
}
