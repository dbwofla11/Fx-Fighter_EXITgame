using UnityEngine;
using UnityEngine.UI;

// 코인 가격이 하한선(PriceCalculator.MinPrice)에 가까워지거나 체포 위기(Doubt가 100에 근접)일 때
// 화면 테두리를 빨갛게 물들이는 경고 연출. Figma 목업이 없어 4개 변(Image)을 코드로 직접 만들고
// 색은 기존 팔레트(BtnShort 빨강)를 그대로 쓴다 — 디자인이 정해지면 교체.
// 이 컴포넌트가 붙은 오브젝트의 RectTransform을 화면 전체로 꽉 채워서(anchors 0~1) 쓴다
// (PriceChartUI가 자기 자신의 RectTransform을 차트 영역으로 쓰는 것과 같은 방식).
public class ScreenWarningBorderUI : MonoBehaviour
{
    [SerializeField] private float edgeThickness = 24f;
    [SerializeField] private Color warningColor = new Color(0.6745098f, 0.1960784f, 0.1960784f); // BtnShort와 동일 색

    // ponytail: 위험 판정 임계값. 밸런스용 임시 수치, 플레이 후 조정 필요.
    private const float PriceDangerRange = 500f; // 이 이내로 가격이 내려오면 서서히 빨개짐, MinPrice에서 최대
    private const float DoubtDangerStart = 80f;   // 체포(Doubt>=100) 전 이 수치부터 서서히 빨개짐

    private const float PulseSpeed = 4f;
    private const float PulseAmplitude = 0.25f;

    private RectTransform area;
    private Image top, bottom, left, right;
    private float dangerLevel;

    private void Awake()
    {
        area = (RectTransform)transform;
        top = BuildEdge("Top");
        bottom = BuildEdge("Bottom");
        left = BuildEdge("Left");
        right = BuildEdge("Right");
        SetAlpha(0f);
    }

    private void OnEnable()
    {
        EventHub.OnMarketUpdated += HandleMarketUpdated;
        if (MarketManager.Instance != null)
            HandleMarketUpdated(MarketManager.Instance.CurrentStat);
    }

    private void OnDisable()
    {
        EventHub.OnMarketUpdated -= HandleMarketUpdated;
    }

    private void Update()
    {
        if (dangerLevel <= 0f)
            return;

        // 위험할수록 살짝 깜빡여서 경고감을 준다.
        float pulse = 1f - PulseAmplitude + Mathf.Abs(Mathf.Sin(Time.unscaledTime * PulseSpeed)) * PulseAmplitude;
        SetAlpha(dangerLevel * pulse);
    }

    private void HandleMarketUpdated(PlayerStat stat)
    {
        float priceDanger = 1f - Mathf.Clamp01((stat.CurrentPrice - PriceCalculator.MinPrice) / PriceDangerRange);
        float doubtDanger = Mathf.Clamp01((stat.Doubt - DoubtDangerStart) / (100f - DoubtDangerStart));

        dangerLevel = Mathf.Max(priceDanger, doubtDanger);
        SetAlpha(dangerLevel);
    }

    private void SetAlpha(float alpha)
    {
        Color c = warningColor;
        c.a = alpha;
        top.color = c;
        bottom.color = c;
        left.color = c;
        right.color = c;
    }

    private Image BuildEdge(string name)
    {
        GameObject go = new GameObject($"WarningEdge_{name}", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(area, false);

        RectTransform rect = (RectTransform)go.transform;

        switch (name)
        {
            case "Top":
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(0f, edgeThickness);
                break;
            case "Bottom":
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.sizeDelta = new Vector2(0f, edgeThickness);
                break;
            case "Left":
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.sizeDelta = new Vector2(edgeThickness, 0f);
                break;
            default: // Right
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 0.5f);
                rect.sizeDelta = new Vector2(edgeThickness, 0f);
                break;
        }

        rect.anchoredPosition = Vector2.zero;

        Image img = go.GetComponent<Image>();
        img.raycastTarget = false;

        return img;
    }
}
