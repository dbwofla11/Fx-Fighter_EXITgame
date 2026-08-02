using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// MarketManager.PriceHistory(일별 PricePoint) 기반의 캔들스틱 가격 차트.
// 원본은 1턴(하루)에 1개씩 기록되지만, 이 UI는 DaysPerCandle(7)일치를 모아 캔들 1개(주봉)로 그린다.
// 최근 visibleCandleCount개(주 단위)만 오브젝트 풀링으로 그린다.
// 이 오브젝트의 RectTransform 자체를 차트 영역으로 사용한다.
public class PriceChartUI : MonoBehaviour
{
    #region 필드

    // 캔들 1개가 며칠치를 모은 것인지. 진행 중인 마지막 캔들은 이 일수가 찰 때까지 같은 슬롯에서
    // Close/Date만 매 턴 갱신되다가, 다 차면 다음 슬롯(새 캔들)으로 넘어간다.
    private const int DaysPerCandle = 7;

    [SerializeField] private int visibleCandleCount = 16; // 화면에 보이는 캔들(주봉) 개수
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

    [Header("격자판")]
    [SerializeField] private int gridLineCount = 4;
    [SerializeField] private float gridLineThickness = 2f;
    [SerializeField] private Color gridLineColor = new Color(0.5f, 0.5f, 0.5f, 0.35f);
    [SerializeField] private int gridPriceLabelFontSize = 16;
    [SerializeField] private Color gridPriceLabelColor = new Color(0.55f, 0.55f, 0.55f);
    [SerializeField] private float gridPriceLabelWidth = 90f;

    private RectTransform chartArea;
    private readonly List<RectTransform> candlePool = new();
    private readonly List<Image> candleImages = new();
    private readonly List<TextMeshProUGUI> dateLabels = new();
    private readonly List<PricePoint> boundPoints = new();
    private readonly List<RectTransform> gridLines = new();
    private readonly List<TextMeshProUGUI> gridPriceLabels = new();
    private TextMeshProUGUI tooltipText;

    #endregion

    #region Unity 생명주기

    private void Awake()
    {
        chartArea = (RectTransform)transform;
        BuildGrid(); // 캔들보다 먼저 만들어야 격자가 캔들 뒤에 깔린다 (나중에 생성된 오브젝트가 위에 그려짐).
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

    #endregion

    #region 풀 생성

    // 격자선/가격 라벨을 gridLineCount개만큼 미리 만들어두고, Redraw()에서 위치/문구만 갱신한다.
    private void BuildGrid()
    {
        for (int j = 0; j < gridLineCount; j++)
        {
            GameObject line = new GameObject($"GridLine_{j}", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(chartArea, false);

            RectTransform lineRect = (RectTransform)line.transform;
            lineRect.anchorMin = new Vector2(0f, 0f);
            lineRect.anchorMax = new Vector2(0f, 0f);
            lineRect.pivot = new Vector2(0f, 0.5f);

            Image lineImage = line.GetComponent<Image>();
            lineImage.color = gridLineColor;
            lineImage.raycastTarget = false;

            gridLines.Add(lineRect);

            GameObject label = new GameObject($"GridPriceLabel_{j}", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(chartArea, false);

            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0f, 0f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.sizeDelta = new Vector2(gridPriceLabelWidth, gridPriceLabelFontSize + 4f);

            TextMeshProUGUI labelText = label.GetComponent<TextMeshProUGUI>();
            labelText.fontSize = gridPriceLabelFontSize;
            labelText.color = gridPriceLabelColor;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.raycastTarget = false;

            gridPriceLabels.Add(labelText);
        }
    }

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

    #endregion

    #region 차트 그리기

    private void Redraw()
    {
        if (MarketManager.Instance == null)
            return;

        List<PricePoint> history = AggregateWeekly(MarketManager.Instance.PriceHistory);
        int count = Mathf.Min(history.Count, visibleCandleCount);

        if (count == 0)
        {
            HideAllCandles();
            HideGrid();
            return;
        }

        int startIndex = history.Count - count;

        ComputePriceRange(history, startIndex, count, out float min, out float range);

        float chartWidth = chartArea.rect.width;
        // 아래쪽은 날짜 라벨 자리로 비워두고, 그 위 영역에만 캔들을 그린다.
        float candleAreaHeight = chartArea.rect.height - dateLabelAreaHeight;
        float slotWidth = chartWidth / visibleCandleCount;
        float candleWidth = slotWidth * candleWidthRatio;

        UpdateGrid(min, range, candleAreaHeight, chartWidth);

        for (int i = 0; i < visibleCandleCount; i++)
        {
            if (i >= count)
            {
                HideCandle(i);
                continue;
            }

            PricePoint p = history[startIndex + i];
            UpdateCandle(i, p, slotWidth, candleWidth, candleAreaHeight, min, range);
        }
    }

    // 일별 PricePoint를 앞에서부터 DaysPerCandle(7)개씩 묶어 캔들(주봉) 1개로 집계한다.
    // 마지막 그룹은 아직 7일이 안 찼으면 지금까지 쌓인 일수만큼만 담기고(진행 중인 캔들), Open은 그 주
    // 첫날 값으로 고정된 채 Close/Date만 매 턴 갱신되다가 7일이 차는 순간 값이 확정되고 다음 턴부터는
    // 새 그룹(다음 캔들)이 시작된다. 그룹은 이력 맨 앞(index 0)부터 고정 구간으로 나누므로, 한 번 다 찬
    // 캔들은 이후에도 값이 바뀌지 않는다.
    private List<PricePoint> AggregateWeekly(IReadOnlyList<PricePoint> daily)
    {
        List<PricePoint> weekly = new();

        for (int i = 0; i < daily.Count; i += DaysPerCandle)
        {
            int end = Mathf.Min(i + DaysPerCandle, daily.Count) - 1;

            weekly.Add(new PricePoint
            {
                Date = daily[end].Date,
                Open = daily[i].Open,
                Close = daily[end].Close
            });
        }

        return weekly;
    }

    // 보이는 구간(startIndex ~ startIndex+count)의 캔들이 차지하는 가격 범위를 구하고,
    // 위/아래 끝에 딱 붙지 않도록 10% 여백을 더한다.
    private void ComputePriceRange(IReadOnlyList<PricePoint> history, int startIndex, int count, out float min, out float range)
    {
        min = float.MaxValue;
        float max = float.MinValue;

        for (int i = 0; i < count; i++)
        {
            PricePoint p = history[startIndex + i];
            min = Mathf.Min(min, Mathf.Min(p.Open, p.Close));
            max = Mathf.Max(max, Mathf.Max(p.Open, p.Close));
        }

        range = Mathf.Max(max - min, 1f);
        float padding = range * 0.1f;
        min -= padding;
        max += padding;
        range = max - min;
    }

    // 가격 범위를 gridLineCount+1개 구간으로 나누는 가로 격자선과, 각 선의 가격을 보여주는 왼쪽 라벨을 갱신한다.
    private void UpdateGrid(float min, float range, float candleAreaHeight, float chartWidth)
    {
        for (int j = 0; j < gridLineCount; j++)
        {
            float t = (j + 1) / (float)(gridLineCount + 1);
            float y = dateLabelAreaHeight + t * candleAreaHeight;
            float price = min + t * range;

            RectTransform lineRect = gridLines[j];
            lineRect.gameObject.SetActive(true);
            lineRect.anchoredPosition = new Vector2(0f, y);
            lineRect.sizeDelta = new Vector2(chartWidth, gridLineThickness);

            TextMeshProUGUI label = gridPriceLabels[j];
            label.gameObject.SetActive(true);
            label.rectTransform.anchoredPosition = new Vector2(4f, y);
            label.text = UIFormat.CurrencyTight(price);
        }
    }

    private void HideGrid()
    {
        foreach (RectTransform rect in gridLines)
            rect.gameObject.SetActive(false);
        foreach (TextMeshProUGUI label in gridPriceLabels)
            label.gameObject.SetActive(false);
    }

    private void UpdateCandle(int i, PricePoint p, float slotWidth, float candleWidth, float candleAreaHeight, float min, float range)
    {
        RectTransform rect = candlePool[i];
        TextMeshProUGUI label = dateLabels[i];

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
        label.text = UIFormat.DateSlash(p.Date);
    }

    private void HideCandle(int i)
    {
        candlePool[i].gameObject.SetActive(false);
        dateLabels[i].gameObject.SetActive(false);
        boundPoints[i] = null;
    }

    private void HideAllCandles()
    {
        foreach (RectTransform rect in candlePool)
            rect.gameObject.SetActive(false);
        foreach (TextMeshProUGUI label in dateLabels)
            label.gameObject.SetActive(false);
    }

    #endregion

    #region 호버 툴팁

    private void ShowTooltip(int index)
    {
        PricePoint p = boundPoints[index];

        if (p == null)
            return;

        tooltipText.text = $"{UIFormat.DateSlash(p.Date)}  Open {UIFormat.CurrencyTight(p.Open)} → Close {UIFormat.CurrencyTight(p.Close)}";
        tooltipText.gameObject.SetActive(true);
    }

    private void HideTooltip()
    {
        tooltipText.gameObject.SetActive(false);
    }

    // 캔들 위에 마우스를 올리면 그 캔들이 지금 어떤 PricePoint를 표시 중인지 owner에게 알려 툴팁을 띄운다.
    private class CandleHoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public PriceChartUI owner;
        public int index;

        public void OnPointerEnter(PointerEventData eventData) => owner.ShowTooltip(index);

        public void OnPointerExit(PointerEventData eventData) => owner.HideTooltip();
    }

    #endregion
}
