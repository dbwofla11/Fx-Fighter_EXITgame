using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// PriceChartUI의 캔들(주/월봉) 오브젝트 풀과 그 캔들에 딸린 연출(호버 이벤트, 급등락 파티클,
// 엔딩 확정 시 붕괴 애니메이션)을 담당한다. 코루틴 실행에 호스트 MonoBehaviour(runner)가 필요하다
// (StartCoroutine은 MonoBehaviour에서만 가능해서, 이 순수 C# 클래스는 그걸 빌려 쓴다).
public class PriceChartCandles
{
    // ponytail: 변동성→크기 정규화 기준(15% 등락 = 최대 크기). 밸런스용 임시 수치, 플레이 후 조정 필요.
    private const float VolatilityForMaxBurst = 0.15f;

    private const float ShakeDuration = 0.3f;
    private const float ShakeMagnitude = 4f;

    // 심지(고가/저가)는 몸통보다 훨씬 얇게 — 토스증권류 차트 관례.
    private const float WickWidthRatio = 0.1f;

    // ponytail: 붕괴 연출 튜닝값. 밸런스 아니라 느낌 조정용이라 플레이 후 자유롭게 바꿔도 됨.
    // 처음엔 20f/0.12초로 잡았더니 수치상으로는 위로 움직이는 게 맞는데(로그로 확인함) 뒤이어 오는
    // 250f/0.35초짜리 낙하에 묻혀서 실제로는 거의 안 보였다 — 높이를 3배로 키우고, 정점에서 살짝
    // 멈췄다 떨어지게(HopHold) 해서 "튀어오름"이 눈에 띄게 만들었다.
    private const float CollapseHopHeight = 45f;
    private const float CollapseHopDuration = 0.15f;
    private const float CollapseHopHoldDuration = 0.06f; // 정점에서 살짝 멈추는 텀
    private const float CollapseFallDuration = 0.35f;
    private const float CollapseStaggerStep = 0.03f; // 캔들 인덱스당 낙하 시작을 이만큼씩 늦춘다
    private const float CollapseDriftRange = 60f; // 낙하 중 좌우로 흩어지는 범위(±)
    private const float CollapseFallDistance = 250f;

    private readonly MonoBehaviour runner;
    private readonly RectTransform chartArea;
    private readonly float candleWidthRatio;
    private readonly float minCandleHeight;
    private readonly Color upColor;
    private readonly Color downColor;
    private readonly float dateLabelAreaHeight;

    private readonly List<RectTransform> pool = new();
    private readonly List<Image> images = new();
    private readonly List<RectTransform> wickPool = new();
    private readonly List<Image> wickImages = new();
    private readonly List<TextMeshProUGUI> dateLabels = new();
    private readonly List<PricePoint> boundPoints = new();

    private int lastIndex = -1;
    private Coroutine shakeRoutine;

    public event Action<PricePoint> Hovered;
    public event Action Unhovered;

    public PriceChartCandles(RectTransform chartArea, MonoBehaviour runner, int poolCapacity,
        float candleWidthRatio, float minCandleHeight, Color upColor, Color downColor,
        float dateLabelAreaHeight, int dateLabelFontSize, Color dateLabelColor)
    {
        this.runner = runner;
        this.chartArea = chartArea;
        this.candleWidthRatio = candleWidthRatio;
        this.minCandleHeight = minCandleHeight;
        this.upColor = upColor;
        this.downColor = downColor;
        this.dateLabelAreaHeight = dateLabelAreaHeight;

        for (int i = 0; i < poolCapacity; i++)
        {
            // 심지를 몸통보다 먼저 만들어서(형제 순서상 아래) 몸통 뒤에 깔리게 한다.
            GameObject wick = new GameObject($"Wick_{i}", typeof(RectTransform), typeof(Image));
            wick.transform.SetParent(chartArea, false);

            RectTransform wickRect = (RectTransform)wick.transform;
            wickRect.anchorMin = new Vector2(0f, 0f);
            wickRect.anchorMax = new Vector2(0f, 0f);
            wickRect.pivot = new Vector2(0.5f, 0f);

            Image wickImage = wick.GetComponent<Image>();
            wickImage.raycastTarget = false; // 호버는 몸통이 담당

            wickPool.Add(wickRect);
            wickImages.Add(wickImage);

            GameObject candle = new GameObject($"Candle_{i}", typeof(RectTransform), typeof(Image));
            candle.transform.SetParent(chartArea, false);

            RectTransform rect = (RectTransform)candle.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);

            Image image = candle.GetComponent<Image>();
            image.raycastTarget = true; // 호버로 정보를 보여주려면 레이캐스트를 받아야 한다

            HoverTarget hover = candle.AddComponent<HoverTarget>();
            hover.owner = this;
            hover.index = i;

            pool.Add(rect);
            images.Add(image);
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

    public void Update(int i, PricePoint p, float slotWidth, float candleAreaHeight, float min, float range, bool isLast)
    {
        RectTransform rect = pool[i];
        TextMeshProUGUI label = dateLabels[i];

        boundPoints[i] = p;

        float bodyTop = Mathf.Max(p.Open, p.Close);
        float bodyBottom = Mathf.Min(p.Open, p.Close);

        float yBottom = dateLabelAreaHeight + (bodyBottom - min) / range * candleAreaHeight;
        float yTop = dateLabelAreaHeight + (bodyTop - min) / range * candleAreaHeight;
        float height = Mathf.Max(yTop - yBottom, minCandleHeight);

        float x = slotWidth * i + slotWidth * 0.5f;
        float candleWidth = slotWidth * candleWidthRatio;

        rect.gameObject.SetActive(true);
        rect.anchoredPosition = new Vector2(x, yBottom);
        rect.sizeDelta = new Vector2(candleWidth, height);

        Color color = p.Close >= p.Open ? upColor : downColor;
        images[i].color = color;

        // 일봉 단위(High/Low가 Open/Close 밖으로 안 나감)에서는 심지가 몸통과 겹쳐 안 보인다 — 주/월봉일 때만 눈에 띈다.
        RectTransform wickRect = wickPool[i];
        float wickBottomY = dateLabelAreaHeight + (p.Low - min) / range * candleAreaHeight;
        float wickTopY = dateLabelAreaHeight + (p.High - min) / range * candleAreaHeight;
        wickRect.gameObject.SetActive(true);
        wickRect.anchoredPosition = new Vector2(x, wickBottomY);
        wickRect.sizeDelta = new Vector2(candleWidth * WickWidthRatio, Mathf.Max(wickTopY - wickBottomY, 1f));
        wickImages[i].color = color;

        label.gameObject.SetActive(true);
        label.rectTransform.anchoredPosition = new Vector2(x, 4f);
        label.rectTransform.sizeDelta = new Vector2(slotWidth, dateLabelAreaHeight - 4f);
        label.text = UIFormat.DateSlash(p.Date);

        if (isLast)
            lastIndex = i;
    }

    public void Hide(int i)
    {
        pool[i].gameObject.SetActive(false);
        wickPool[i].gameObject.SetActive(false);
        dateLabels[i].gameObject.SetActive(false);
        boundPoints[i] = null;
    }

    public void HideAll()
    {
        foreach (RectTransform rect in pool)
            rect.gameObject.SetActive(false);
        foreach (RectTransform wick in wickPool)
            wick.gameObject.SetActive(false);
        foreach (TextMeshProUGUI label in dateLabels)
            label.gameObject.SetActive(false);
    }

    // 가장 최근(진행 중인) 캔들의 종가 위치에, 변동폭이 클수록 크게 파티클을 터뜨린다.
    public void SpawnBurstOnLast()
    {
        if (lastIndex < 0 || !pool[lastIndex].gameObject.activeSelf)
            return;

        PricePoint p = boundPoints[lastIndex];
        if (p == null)
            return;

        RectTransform rect = pool[lastIndex];

        // 캔들 자신을 부모로 삼아 그 중심(anchor 0.5,0.5) 기준 오프셋으로 배치한다 — chartArea를 부모로
        // 쓰면서 캔들의 (0,0)anchor 기준 anchoredPosition을 그대로 넘기면 anchor 공간이 달라 엉뚱한
        // 위치에 생기는 버그가 있었다(UIBurstParticle의 버스트 루트는 항상 0.5,0.5 anchor로 생성됨).
        Vector2 closeOffset = new Vector2(0f, p.Close >= p.Open ? rect.sizeDelta.y / 2f : -rect.sizeDelta.y / 2f);

        float volatility = p.Open > 0f ? Mathf.Abs(p.Close - p.Open) / p.Open : 0f;
        float intensity = Mathf.Clamp01(volatility / VolatilityForMaxBurst);
        Color color = p.Close >= p.Open ? upColor : downColor;

        // 캔들 자신을 부모로 스폰하되(오프셋 계산은 그 기준), 곧바로 chartArea로 옮긴다 — 캔들은 오브젝트
        // 풀이라 줌/스크롤 중 Redraw()가 같은 슬롯을 다른 날짜로 재활용하거나 SetActive(false)로 숨기는데,
        // 버스트가 그 밑에 계속 붙어있으면 숨겨지는 순간 코루틴(자기 GameObject 비활성화)이 얼어붙어서
        // 다 안 사라진 조각이 잔상으로 남는다. worldPositionStays:true라 위치는 그대로 유지된다.
        RectTransform burst = UIBurstParticle.Spawn(rect, closeOffset, color, intensity);
        if (burst != null)
            burst.SetParent(chartArea, true);

        Shake(rect);
    }

    private void Shake(RectTransform rect)
    {
        if (shakeRoutine != null)
            runner.StopCoroutine(shakeRoutine);
        shakeRoutine = runner.StartCoroutine(ShakeRoutine(rect));
    }

    // 다음 Update()가 이 캔들의 anchoredPosition을 새로 계산해 덮어쓰므로 별도 복원이 필요 없다
    // (StatGaugeUI 흔들림과 동일한 전제).
    private IEnumerator ShakeRoutine(RectTransform rect)
    {
        Vector2 basePos = rect.anchoredPosition;
        float t = 0f;
        while (t < ShakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float damper = 1f - t / ShakeDuration;
            rect.anchoredPosition = basePos + UnityEngine.Random.insideUnitCircle * ShakeMagnitude * damper;
            yield return null;
        }
        rect.anchoredPosition = basePos;
    }

    // 체포/거지 엔딩 확정 : 차트 전체가 아니라 캔들 하나하나가 따로 떨어진다 — 인덱스만큼 시작을 늦춰서
    // 왼쪽(오래된 캔들)부터 순서대로 무너지는 것처럼 보이게 한다.
    public void PlayCollapse()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].gameObject.activeSelf)
                continue;

            runner.StartCoroutine(CollapseRoutine(pool[i], images[i], dateLabels[i], i * CollapseStaggerStep));
            runner.StartCoroutine(CollapseRoutine(wickPool[i], wickImages[i], null, i * CollapseStaggerStep));
        }
    }

    // 캔들이 위로 살짝 튀어올랐다가(hop, ease-out) 중력 가속하듯 아래로 떨어지며(fall, ease-in) 사라진다.
    private IEnumerator CollapseRoutine(RectTransform rect, Image image, TextMeshProUGUI label, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        Vector2 basePos = rect.anchoredPosition;
        Color baseImageColor = image.color;
        Color baseLabelColor = label != null ? label.color : default;
        // 캔들마다 낙하 중 좌우로 흩어질 방향/폭을 미리 뽑아둔다(hop 중엔 그대로 위로만, fall 중에만 적용).
        float driftX = UnityEngine.Random.Range(-CollapseDriftRange, CollapseDriftRange);

        float t = 0f;
        while (t < CollapseHopDuration)
        {
            t += Time.unscaledDeltaTime;
            float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / CollapseHopDuration), 2f);
            rect.anchoredPosition = basePos + Vector2.up * CollapseHopHeight * ease;
            yield return null;
        }

        Vector2 hopPeakPos = basePos + Vector2.up * CollapseHopHeight;
        rect.anchoredPosition = hopPeakPos;
        yield return new WaitForSecondsRealtime(CollapseHopHoldDuration);

        t = 0f;
        while (t < CollapseFallDuration)
        {
            t += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(t / CollapseFallDuration);
            float ease = progress * progress;
            Vector2 fallOffset = Vector2.down * (CollapseHopHeight + CollapseFallDistance) * ease + Vector2.right * driftX * ease;
            rect.anchoredPosition = hopPeakPos + fallOffset;

            image.color = new Color(baseImageColor.r, baseImageColor.g, baseImageColor.b, baseImageColor.a * (1f - progress));
            if (label != null)
                label.color = new Color(baseLabelColor.r, baseLabelColor.g, baseLabelColor.b, baseLabelColor.a * (1f - progress));

            yield return null;
        }
    }

    private void RaiseHover(int index)
    {
        PricePoint p = boundPoints[index];
        if (p != null)
            Hovered?.Invoke(p);
    }

    private void RaiseUnhover() => Unhovered?.Invoke();

    // 캔들 위에 마우스를 올리면 그 캔들이 지금 어떤 PricePoint를 표시 중인지 owner에게 알려 Hovered/Unhovered를 올린다.
    private class HoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public PriceChartCandles owner;
        public int index;

        public void OnPointerEnter(PointerEventData eventData) => owner.RaiseHover(index);

        public void OnPointerExit(PointerEventData eventData) => owner.RaiseUnhover();
    }
}
