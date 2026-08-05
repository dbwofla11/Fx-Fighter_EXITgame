using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 차트 패널 우상단의 "월봉"/"주봉" 전환 버튼. Ctrl+휠로도 기간 전환이 되지만, 그 조작이 눈에 안 띄어서
// 명시적으로 고를 수 있는 버튼을 추가로 둔다. 현재 선택된 쪽만 activeColor로 강조해서 보여준다.
public class PriceChartPeriodToggle
{
    private const float ButtonWidth = 56f;
    private const float ButtonGap = 4f;
    private const float RightPadding = 12f;
    private const float TopPadding = 8f;

    private readonly PriceChartViewport viewport;
    private readonly Color activeColor;
    private readonly Color inactiveColor;

    private readonly TextMeshProUGUI monthLabel;
    private readonly TextMeshProUGUI weekLabel;

    public event Action Changed;

    public PriceChartPeriodToggle(RectTransform chartArea, PriceChartViewport viewport,
        int fontSize, Color activeColor, Color inactiveColor)
    {
        this.viewport = viewport;
        this.activeColor = activeColor;
        this.inactiveColor = inactiveColor;

        // 오른쪽부터: 주봉, 그 왼쪽에 월봉 — "월봉","주봉" 순서로 왼→오 읽히게 배치.
        float weekX = -RightPadding;
        float monthX = weekX - ButtonWidth - ButtonGap;

        monthLabel = CreateButton(chartArea, "월봉", fontSize, monthX, () => SetPeriod(PriceChartViewport.MonthDays));
        weekLabel = CreateButton(chartArea, "주봉", fontSize, weekX, () => SetPeriod(PriceChartViewport.WeekDays));

        Refresh();
    }

    private TextMeshProUGUI CreateButton(RectTransform chartArea, string label, int fontSize, float x, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject($"PeriodButton_{label}", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(Button));
        go.transform.SetParent(chartArea, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(x, -TopPadding);
        rect.sizeDelta = new Vector2(ButtonWidth, fontSize + 8f);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = true;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = text;
        button.transition = Selectable.Transition.None; // 색은 Refresh()가 직접 칠하므로 기본 트랜지션은 끔
        button.onClick.AddListener(onClick);

        return text;
    }

    private void SetPeriod(int periodDays)
    {
        viewport.SetPeriod(periodDays);
        Refresh();
        Changed?.Invoke(); // 호출부(PriceChartUI)가 이 이벤트를 받아 전체 Redraw()를 돌린다.
    }

    // 지금 선택된 기간에 맞춰 두 라벨 중 하나만 강조 색으로 표시한다. Ctrl+휠로 기간이 바뀌었을 때도
    // 버튼 표시가 어긋나지 않도록 PriceChartUI.Redraw()가 매번 호출해준다.
    public void Refresh()
    {
        monthLabel.color = viewport.PeriodDays == PriceChartViewport.MonthDays ? activeColor : inactiveColor;
        weekLabel.color = viewport.PeriodDays == PriceChartViewport.WeekDays ? activeColor : inactiveColor;
    }
}
