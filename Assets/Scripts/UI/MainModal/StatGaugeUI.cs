using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Support/Growth/Doubt 게이지 패널(BaseWhiteBar + PositiveBar[/NegativeBar] + 값 텍스트) 공용 스크립트.
// EventHub.OnMarketUpdated를 구독해 매 턴 갱신한다. Support/Growth는 -100~100(negativeBar 사용),
// Doubt는 0~100(negativeBar 비움)이라 Inspector에서 negativeBar를 비워두면 Doubt 모드로 동작한다.
// Doubt가 마지막으로 흔든 시점보다 50 이상 오를 때마다 패널 3개(Support/Growth/Doubt) 전부가 부들부들
// 떨리고, 체포 엔딩이 확정되면 무너지듯 아래로 떨어지며 사라진다 — 이 스크립트가 3개 패널 각각에
// 붙어있으므로 각자 자기 자신을 흔든다.
public class StatGaugeUI : MonoBehaviour
{
    // EndingResultUI가 이 시간만큼 결과 패널 표시를 늦춰서 붕괴 연출이 먼저 보이게 한다.
    public const float ArrestCollapseDuration = 0.6f;

    private enum StatType { Support, Growth, Doubt }

    [SerializeField] private StatType statType;
    [SerializeField] private Image positiveBar;
    [SerializeField] private Image negativeBar;
    [SerializeField] private TextMeshProUGUI valueText;

    private const float MaxAbsValue = 100f;
    private const float DoubtShakeDuration = 0.35f;
    private const float DoubtShakeMagnitude = 6f;
    private const float ArrestFallDistance = 250f;
    // ponytail: 마지막으로 흔든 시점 대비 Doubt가 이만큼 오르면 재발동. 원래 50으로 잡았더니 Play 모드
    // 150턴 테스트에서 Doubt가 총 15밖에 안 올라(Doubt 상승이 원래 느림) 사실상 평생 안 흔들리는 값이었다 —
    // 실제로 반응하는 걸 볼 수 있도록 10으로 낮춤. 밸런스용 임시 수치라 플레이 후 더 조정 가능.
    private const float DoubtShakeRiseThreshold = 10f;

    private RectTransform rect;
    private Vector2 basePos;
    private float doubtBaselineAtLastShake;
    private bool hasDoubtBaseline;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        rect = (RectTransform)transform;
        basePos = rect.anchoredPosition;
    }

    private void OnEnable()
    {
        EventHub.OnMarketUpdated += HandleMarketUpdated;
        EventHub.OnGameEnded += HandleGameEnded;
        if (MarketManager.Instance != null)
            HandleMarketUpdated(MarketManager.Instance.CurrentStat);
    }

    private void OnDisable()
    {
        EventHub.OnMarketUpdated -= HandleMarketUpdated;
        EventHub.OnGameEnded -= HandleGameEnded;
    }

    private void HandleMarketUpdated(PlayerStat stat)
    {
        float value = statType switch
        {
            StatType.Support => stat.Support,
            StatType.Growth => stat.Growth,
            StatType.Doubt => stat.Doubt,
            _ => 0f,
        };

        if (negativeBar != null)
        {
            positiveBar.fillAmount = Mathf.Clamp01(Mathf.Max(0f, value) / MaxAbsValue);
            negativeBar.fillAmount = Mathf.Clamp01(Mathf.Max(0f, -value) / MaxAbsValue);
        }
        else
        {
            positiveBar.fillAmount = Mathf.Clamp01(value / MaxAbsValue);
        }

        valueText.text = (value >= 0 ? "+" : "") + value.ToString("F1");

        if (!hasDoubtBaseline)
        {
            doubtBaselineAtLastShake = stat.Doubt;
            hasDoubtBaseline = true;
        }
        else if (stat.Doubt - doubtBaselineAtLastShake >= DoubtShakeRiseThreshold)
        {
            TriggerShake();
            doubtBaselineAtLastShake = stat.Doubt;
        }
    }

    private void TriggerShake()
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    // Doubt 상승은 스킬 구매(모달이 열려 일시정지된 상태)로도 일어날 수 있어 unscaled time을 쓴다.
    private IEnumerator ShakeRoutine()
    {
        float t = 0f;
        while (t < DoubtShakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float damper = 1f - t / DoubtShakeDuration;
            rect.anchoredPosition = basePos + Random.insideUnitCircle * DoubtShakeMagnitude * damper;
            yield return null;
        }
        rect.anchoredPosition = basePos;
    }

    private void HandleGameEnded(EndingType ending)
    {
        if (ending != EndingType.Arrest && ending != EndingType.Broke)
            return;

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);
        StartCoroutine(CollapseRoutine());
    }

    // 체포/거지 확정 : 흔들리던 패널이 아래로 무너지듯 떨어지며 페이드아웃된다.
    private IEnumerator CollapseRoutine()
    {
        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group == null)
            group = gameObject.AddComponent<CanvasGroup>();

        float t = 0f;
        while (t < ArrestCollapseDuration)
        {
            t += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(t / ArrestCollapseDuration);
            rect.anchoredPosition = basePos + Vector2.down * ArrestFallDistance * progress;
            group.alpha = 1f - progress;
            yield return null;
        }
    }
}
