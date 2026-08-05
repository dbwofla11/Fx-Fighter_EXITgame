using UnityEngine;

// PriceChartUI가 "지금 어느 구간을, 얼마나 확대해서 보고 있는지" 상태와 휠/드래그 스크롤·줌 계산을 들고 있는다.
// Unity 이벤트(IScrollHandler 등) 수신 자체는 PriceChartUI가 하고, 여기엔 그 결과로 상태를 바꾸는 로직만 둔다.
public class PriceChartViewport
{
    // 캔들 1개가 며칠치인지 — 주봉/월봉 두 단계. 실제 달력 월과 무관하게 30일 고정(DaysPerCandle과 동일한
    // 근사 방식).
    public const int WeekDays = 7;
    public const int MonthDays = 30;

    private readonly int poolCapacity; // = visibleCandleCount, 캔들 풀 용량(주/월 공통으로 이 개수만큼만 오브젝트가 있다)
    private readonly int minZoomCandleCount;

    public int ZoomCandleCount { get; private set; }
    public int PeriodDays { get; private set; } = WeekDays;
    public int ViewOffset { get; private set; } // 꼬리(최신)에서 몇 캔들만큼 과거로 스크롤했는지. 0이면 최신 화면.

    private float dragAccumX; // 드래그 중 누적된 로컬 x 이동량. 캔들 1개 폭만큼 쌓이면 ViewOffset이 움직인다.
    private Vector2 dragLastLocalPoint;

    public PriceChartViewport(int poolCapacity, int minZoomCandleCount)
    {
        this.poolCapacity = poolCapacity;
        this.minZoomCandleCount = minZoomCandleCount;
        ZoomCandleCount = poolCapacity; // 시작은 줌아웃 최대(주봉)
    }

    // 전체 이력 개수 기준으로 실제 표시할 count/startIndex를 정하고, ViewOffset을 유효 범위로 되돌린다.
    // Redraw()가 매번 맨 먼저 호출해서 그릴 구간을 얻어간다.
    public void Resolve(int historyCount, out int count, out int startIndex)
    {
        count = Mathf.Min(historyCount, ZoomCandleCount);
        int maxOffset = Mathf.Max(0, historyCount - count);
        ViewOffset = Mathf.Clamp(ViewOffset, 0, maxOffset);
        startIndex = historyCount - count - ViewOffset;
    }

    public void PanBy(int candles)
    {
        ViewOffset += candles;
    }

    // 버튼 등으로 기간을 명시적으로 고를 때 쓴다. ZoomBy의 자동 전환과 달리, 항상 그 기간의
    // 기본(줌아웃 최대) 화면으로 리셋한다 — 사용자가 예측 가능한 상태로 시작하게 하려는 의도.
    public void SetPeriod(int periodDays)
    {
        if (periodDays == PeriodDays)
            return;

        PeriodDays = periodDays;
        ZoomCandleCount = poolCapacity;
    }

    // 캔들 개수 범위(minZoomCandleCount~poolCapacity)를 벗어나 더 축소/확대하면 주↔월 기간 자체를 바꿔서
    // "스케일 축소"가 끊기지 않고 계속 이어지게 한다 (주 최대축소 다음은 월 최소확대, 그 역도 동일).
    public void ZoomBy(int delta)
    {
        int next = ZoomCandleCount + delta;

        if (next > poolCapacity && PeriodDays == WeekDays)
        {
            PeriodDays = MonthDays;
            ZoomCandleCount = minZoomCandleCount;
            return;
        }

        if (next < minZoomCandleCount && PeriodDays == MonthDays)
        {
            PeriodDays = WeekDays;
            ZoomCandleCount = poolCapacity;
            return;
        }

        ZoomCandleCount = Mathf.Clamp(next, minZoomCandleCount, poolCapacity);
    }

    public void BeginDrag(RectTransform chartArea, Vector2 screenPosition, Camera eventCamera)
    {
        dragAccumX = 0f;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(chartArea, screenPosition, eventCamera, out dragLastLocalPoint);
    }

    // 드래그 누적량이 캔들 1개 폭(slotWidth)을 넘을 때마다 PanBy(±1)을 호출한다. 실제로 한 칸이라도
    // 움직였으면 true(호출부가 이걸 보고 Redraw() 여부를 판단한다).
    public bool Drag(RectTransform chartArea, Vector2 screenPosition, Camera eventCamera, float slotWidth)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(chartArea, screenPosition, eventCamera, out Vector2 localPoint))
            return false;

        dragAccumX += localPoint.x - dragLastLocalPoint.x;
        dragLastLocalPoint = localPoint;

        bool moved = false;
        while (Mathf.Abs(dragAccumX) >= slotWidth)
        {
            PanBy(dragAccumX > 0f ? 1 : -1);
            dragAccumX -= Mathf.Sign(dragAccumX) * slotWidth;
            moved = true;
        }

        return moved;
    }
}
