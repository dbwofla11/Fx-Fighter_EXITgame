using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// PriceChartUI의 이동평균선(일봉 기준 N일 종가 평균) 오버레이.
// 캔들은 주봉/월봉(가변 candlePeriodDays)이지만 평균 자체는 원본 일별 PriceHistory로 계산해서,
// 각 캔들이 끝나는 날짜 기준 최근 N일 종가 평균을 그린다 (실제 거래 앱의 10/30/60/120일선과 동일한 정의).
// 캔들과 같은 오브젝트 풀링 방식 — 선분(캔들 간 연결선) 오브젝트를 미리 만들어두고 위치만 갱신한다.
public class PriceChartMovingAverage
{
    private readonly int[] periodDays; // 이평선 라인들의 룩백 기간(예: 10/30/60/120일). 캔들 기간(주/월)과는 별개.
    private readonly float lineThickness;
    private readonly int visibleCandleCount;
    private readonly float dateLabelAreaHeight;

    private readonly List<List<RectTransform>> segments = new(); // 라인(기간)별 세그먼트 풀

    public PriceChartMovingAverage(RectTransform chartArea, int visibleCandleCount, float dateLabelAreaHeight,
        int[] periodDays, Color[] lineColors, float lineThickness)
    {
        this.visibleCandleCount = visibleCandleCount;
        this.dateLabelAreaHeight = dateLabelAreaHeight;
        this.periodDays = periodDays;
        this.lineThickness = lineThickness;

        Build(chartArea, lineColors);
    }

    private void Build(RectTransform chartArea, Color[] lineColors)
    {
        for (int line = 0; line < periodDays.Length; line++)
        {
            List<RectTransform> lineSegments = new();
            Color color = lineColors[Mathf.Min(line, lineColors.Length - 1)];

            for (int i = 0; i < visibleCandleCount - 1; i++)
            {
                GameObject segment = new GameObject($"MaSegment_{periodDays[line]}d_{i}", typeof(RectTransform), typeof(Image));
                segment.transform.SetParent(chartArea, false);

                RectTransform rect = (RectTransform)segment.transform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0f, 0.5f);

                Image image = segment.GetComponent<Image>();
                image.raycastTarget = false;
                image.color = color;

                lineSegments.Add(rect);
            }

            segments.Add(lineSegments);
        }
    }

    // candleIndex번째 캔들이 끝나는 날짜 기준, 그 이전 일별 종가 period개의 평균. 데이터가 모자라면 null
    // (그 지점부터는 선을 그리지 않는다 — 실제 거래 차트의 이평선 시작 지점과 동일).
    private float? ComputeDayAverage(IReadOnlyList<PricePoint> daily, int candlePeriodDays, int candleIndex, int period)
    {
        int dailyEndIndex = Mathf.Min((candleIndex + 1) * candlePeriodDays, daily.Count) - 1;
        int dailyStartIndex = dailyEndIndex - period + 1;
        if (dailyStartIndex < 0)
            return null;

        float sum = 0f;
        for (int k = dailyStartIndex; k <= dailyEndIndex; k++)
            sum += daily[k].Close;
        return sum / period;
    }

    // 보이는 구간에서 이평값이 캔들 가격 범위 밖으로 나가는 경우를 위해 min/max를 넓힌다.
    // Redraw() 전에 호출해서 이 결과로 계산된 range를 그대로 Redraw()에 넘겨야 선이 차트 밖으로 안 삐져나간다.
    public void ExpandPriceRange(IReadOnlyList<PricePoint> daily, int candlePeriodDays, int startIndex, int count, ref float min, ref float max)
    {
        foreach (int period in periodDays)
        {
            for (int i = 0; i < count; i++)
            {
                float? average = ComputeDayAverage(daily, candlePeriodDays, startIndex + i, period);
                if (average == null)
                    continue;

                min = Mathf.Min(min, average.Value);
                max = Mathf.Max(max, average.Value);
            }
        }
    }

    // maPeriodDays 라인마다 보이는 구간의 이평선을 그린다. 값이 없는(아직 period일치가 안 쌓인) 구간은
    // 건너뛰고, 값이 있는 지점끼리만 이어서 그린다.
    public void Redraw(IReadOnlyList<PricePoint> daily, int candlePeriodDays, int startIndex, int count, float slotWidth, float candleAreaHeight, float min, float range)
    {
        for (int line = 0; line < periodDays.Length; line++)
        {
            int period = periodDays[line];
            List<RectTransform> lineSegments = segments[line];

            Vector2? previous = null;
            int segmentIndex = 0;

            for (int i = 0; i < count; i++)
            {
                float? average = ComputeDayAverage(daily, candlePeriodDays, startIndex + i, period);
                if (average == null)
                {
                    previous = null;
                    continue;
                }

                float x = slotWidth * i + slotWidth * 0.5f;
                float y = dateLabelAreaHeight + (average.Value - min) / range * candleAreaHeight;
                Vector2 point = new Vector2(x, y);

                if (previous.HasValue)
                {
                    UpdateSegment(lineSegments[segmentIndex], previous.Value, point);
                    segmentIndex++;
                }

                previous = point;
            }

            for (; segmentIndex < lineSegments.Count; segmentIndex++)
                lineSegments[segmentIndex].gameObject.SetActive(false);
        }
    }

    public void HideAll()
    {
        foreach (List<RectTransform> lineSegments in segments)
            foreach (RectTransform rect in lineSegments)
                rect.gameObject.SetActive(false);
    }

    private void UpdateSegment(RectTransform rect, Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        float distance = delta.magnitude;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        rect.gameObject.SetActive(true);
        rect.anchoredPosition = from;
        rect.sizeDelta = new Vector2(distance, lineThickness);
        rect.localEulerAngles = new Vector3(0f, 0f, angle);
    }
}
