using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// MarketManager.PriceHistory(일별 PricePoint) 기반의 캔들스틱 가격 차트.
// 원본은 1턴(하루)에 1개씩 기록되지만, 이 UI는 여러 날치를 모아 캔들 1개(주봉/월봉)로 그린다.
// 실제 렌더링(격자/캔들/이동평균선/툴팁)과 스크롤·줌 상태는 각각 별도 클래스로 분리돼있고,
// 이 클래스는 MarketManager/EventHub와 그 모듈들을 잇는 얇은 오케스트레이터 역할만 한다.
// 이 오브젝트의 RectTransform 자체를 차트 영역으로 사용한다.
public class PriceChartUI : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler
{
    #region 필드

    // 풀 용량이자 최대로 축소했을 때(줌아웃) 보이는 캔들 개수. 줌인하면 viewport.ZoomCandleCount가 이보다 작아진다.
    [SerializeField] private int visibleCandleCount = 16;
    [SerializeField] private int minZoomCandleCount = 6; // 최대로 확대(줌인)했을 때 보이는 캔들 개수
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

    // 실제 거래 앱에서 흔히 쓰는 일봉 기준 이평선(10/30/60/120일). 캔들은 주/월봉이지만 평균 자체는
    // 원본 일별 PriceHistory로 계산해서 각 캔들이 끝나는 날짜 기준 최근 N일 종가 평균을 그린다.
    [Header("이동평균선")]
    [SerializeField] private int[] maPeriodDays = { 10, 30, 60, 120 };
    [SerializeField] private float maLineThickness = 3f;
    [SerializeField]
    private Color[] maLineColors =
    {
        new Color(0.85f, 0.65f, 0.13f), // 10일 - 골드
        new Color(0.62f, 0.36f, 0.85f), // 30일 - 보라
        new Color(0.20f, 0.55f, 0.85f), // 60일 - 파랑
        new Color(0.85f, 0.30f, 0.55f), // 120일 - 마젠타
    };

    // Ctrl+휠로도 기간이 바뀌지만 눈에 안 띄어서, 패널 우상단에 명시적으로 고를 수 있는 버튼도 둔다.
    [Header("주/월봉 전환 버튼")]
    [SerializeField] private int periodToggleFontSize = 20;
    [SerializeField] private Color periodToggleActiveColor = new Color(0.15f, 0.15f, 0.15f);
    [SerializeField] private Color periodToggleInactiveColor = new Color(0.7f, 0.7f, 0.7f);

    private RectTransform chartArea;
    private PriceChartViewport viewport;
    private PriceChartGrid grid;
    private PriceChartCandles candles;
    private PriceChartMovingAverage movingAverage;
    private PriceChartTooltip tooltip;
    private PriceChartPeriodToggle periodToggle;

    #endregion

    #region Unity 생명주기

    private void Awake()
    {
        chartArea = (RectTransform)transform;
        viewport = new PriceChartViewport(visibleCandleCount, minZoomCandleCount);

        // 배경 Image의 raycastTarget이 꺼져있으면 캔들 사이 빈 공간에서 휠/드래그를 못 받는다 — 과거 스크롤엔 필요.
        Image background = GetComponent<Image>();
        if (background != null)
            background.raycastTarget = true;

        // 생성 순서 = 렌더링 순서(나중에 생성된 오브젝트가 위에 그려짐): 격자 → 캔들 → 이동평균선 → 툴팁.
        grid = new PriceChartGrid(chartArea, gridLineCount, gridLineThickness, gridLineColor,
            gridPriceLabelFontSize, gridPriceLabelColor, gridPriceLabelWidth);

        candles = new PriceChartCandles(chartArea, this, visibleCandleCount, candleWidthRatio, minCandleHeight,
            upColor, downColor, dateLabelAreaHeight, dateLabelFontSize, dateLabelColor);

        movingAverage = new PriceChartMovingAverage(chartArea, visibleCandleCount, dateLabelAreaHeight,
            maPeriodDays, maLineColors, maLineThickness);

        tooltip = new PriceChartTooltip(chartArea, tooltipFontSize, tooltipColor);
        candles.Hovered += tooltip.Show;
        candles.Unhovered += tooltip.Hide;

        periodToggle = new PriceChartPeriodToggle(chartArea, viewport,
            periodToggleFontSize, periodToggleActiveColor, periodToggleInactiveColor);
        periodToggle.Changed += Redraw;
    }

    private void OnEnable()
    {
        EventHub.OnMarketUpdated += HandleMarketUpdated;
        EventHub.OnGameEnded += HandleGameEnded;
        Redraw();
    }

    private void OnDisable()
    {
        EventHub.OnMarketUpdated -= HandleMarketUpdated;
        EventHub.OnGameEnded -= HandleGameEnded;
    }

    private void HandleGameEnded(EndingType ending)
    {
        if (ending != EndingType.Arrest && ending != EndingType.Broke)
            return;

        candles.PlayCollapse();
    }

    private void HandleMarketUpdated(PlayerStat stat)
    {
        Redraw();

        // 급등(Surge)/급락(Crash) — 스트리머 반응 중 가장 극단적인 두 단계일 때만 종가에 파티클을 띄운다.
        // Redraw()가 끝나 마지막 캔들 위치가 확정된 뒤에 터뜨려야 종가 위치에 정확히 찍힌다.
        // 과거로 스크롤 중(viewport.ViewOffset != 0)이면 화면의 마지막 캔들이 실제 최신 캔들이 아니므로 터뜨리지 않는다.
        if (viewport.ViewOffset == 0 && (stat.StreamerReaction == StreamerReactionState.Surge || stat.StreamerReaction == StreamerReactionState.Crash))
            candles.SpawnBurstOnLast();
    }

    #endregion

    #region 차트 그리기

    private void Redraw()
    {
        if (MarketManager.Instance == null)
            return;

        periodToggle.Refresh(); // Ctrl+휠로 기간이 바뀌었을 수도 있으니 버튼 표시도 매번 동기화한다.
        periodToggle.UpdateVolumeTurns(MarketManager.Instance.CurrentStat.VolumeBuffTurnsRemaining);

        IReadOnlyList<PricePoint> daily = MarketManager.Instance.PriceHistory;
        List<PricePoint> history = AggregateHistory(daily, viewport.PeriodDays);
        viewport.Resolve(history.Count, out int count, out int startIndex);

        if (count == 0)
        {
            candles.HideAll();
            grid.Hide();
            movingAverage.HideAll();
            return;
        }

        ComputePriceRange(history, daily, startIndex, count, out float min, out float range);

        float chartWidth = chartArea.rect.width;
        // 아래쪽은 날짜 라벨 자리로 비워두고, 그 위 영역에만 캔들을 그린다.
        float candleAreaHeight = chartArea.rect.height - dateLabelAreaHeight;
        float slotWidth = chartWidth / viewport.ZoomCandleCount;

        grid.Redraw(min, range, dateLabelAreaHeight, candleAreaHeight, chartWidth);

        for (int i = 0; i < visibleCandleCount; i++)
        {
            if (i >= count)
            {
                candles.Hide(i);
                continue;
            }

            PricePoint p = history[startIndex + i];
            candles.Update(i, p, slotWidth, candleAreaHeight, min, range, i == count - 1);
        }

        movingAverage.Redraw(daily, viewport.PeriodDays, startIndex, count, slotWidth, candleAreaHeight, min, range);
    }

    // 일별 PricePoint를 앞에서부터 periodDays개씩 묶어 캔들(주봉/월봉) 1개로 집계한다.
    // 마지막 그룹은 아직 periodDays일이 안 찼으면 지금까지 쌓인 일수만큼만 담기고(진행 중인 캔들), Open은 그
    // 구간 첫날 값으로 고정된 채 Close/Date만 매 턴 갱신되다가 다 차는 순간 값이 확정되고 다음 턴부터는
    // 새 그룹(다음 캔들)이 시작된다. 그룹은 이력 맨 앞(index 0)부터 고정 구간으로 나누므로, 한 번 다 찬
    // 캔들은 이후에도 값이 바뀌지 않는다.
    private List<PricePoint> AggregateHistory(IReadOnlyList<PricePoint> daily, int periodDays)
    {
        List<PricePoint> result = new();

        for (int i = 0; i < daily.Count; i += periodDays)
        {
            int end = Mathf.Min(i + periodDays, daily.Count) - 1;

            float high = float.MinValue;
            float low = float.MaxValue;
            for (int k = i; k <= end; k++)
            {
                high = Mathf.Max(high, daily[k].Open, daily[k].Close);
                low = Mathf.Min(low, daily[k].Open, daily[k].Close);
            }

            result.Add(new PricePoint
            {
                Date = daily[end].Date,
                Open = daily[i].Open,
                Close = daily[end].Close,
                High = high,
                Low = low
            });
        }

        return result;
    }

    // 보이는 구간(startIndex ~ startIndex+count)의 캔들이 차지하는 가격 범위를 구하고,
    // 위/아래 끝에 딱 붙지 않도록 10% 여백을 더한다. 이동평균값도 캔들 범위 밖으로 나갈 수 있어
    // 같이 포함시켜야 이평선이 차트 밖으로 삐져나가지 않는다.
    private void ComputePriceRange(IReadOnlyList<PricePoint> history, IReadOnlyList<PricePoint> daily, int startIndex, int count, out float min, out float range)
    {
        min = float.MaxValue;
        float max = float.MinValue;

        for (int i = 0; i < count; i++)
        {
            PricePoint p = history[startIndex + i];
            min = Mathf.Min(min, p.Low);
            max = Mathf.Max(max, p.High);
        }

        movingAverage.ExpandPriceRange(daily, viewport.PeriodDays, startIndex, count, ref min, ref max);

        range = Mathf.Max(max - min, 1f);
        float padding = range * 0.1f;
        min -= padding;
        max += padding;
        range = max - min;
    }

    #endregion

    #region 과거 스크롤 / 줌

    // 휠만: 캔들 1개씩 과거/최신으로 이동. Ctrl+휠: 확대/축소(다른 차트 앱들의 표준 관례).
    // 확대/축소가 캔들 개수 한계를 넘어가면 PriceChartViewport가 주↔월 기간 자체를 바꿔서 스케일을 이어간다.
    public void OnScroll(PointerEventData eventData)
    {
        bool zoomModifier = Keyboard.current != null && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed);

        if (zoomModifier)
            viewport.ZoomBy(eventData.scrollDelta.y > 0f ? -1 : 1); // 휠 위로 = 확대(캔들 수 감소, 더 크게)
        else
            viewport.PanBy(eventData.scrollDelta.y > 0f ? 1 : -1);

        Redraw(); // Redraw() 안에서 viewport.Resolve()가 유효 범위로 다시 클램프한다.
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        viewport.BeginDrag(chartArea, eventData.position, eventData.pressEventCamera);
    }

    // 오른쪽으로 끌수록 과거 캔들이 드러나도록(= ViewOffset 증가) 커서를 그대로 따라가는 느낌을 준다.
    public void OnDrag(PointerEventData eventData)
    {
        float slotWidth = chartArea.rect.width / viewport.ZoomCandleCount;
        if (viewport.Drag(chartArea, eventData.position, eventData.pressEventCamera, slotWidth))
            Redraw();
    }

    #endregion
}
