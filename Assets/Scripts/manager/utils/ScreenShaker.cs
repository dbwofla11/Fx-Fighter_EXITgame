using System.Collections;
using UnityEngine;

// 파티클(UIBurstParticle)이 생길 때마다 그 크기(intensity01)에 비례해 화면을 흔든다.
// Main_Canvas의 Render Mode가 Screen Space - Overlay라 카메라를 참조하지 않으므로
// (Canvas.m_Camera가 비어있음) Camera.main을 흔드는 건 화면에 아무 영향이 없다 —
// 대신 이 스크립트를 Canvas(Main_Canvas) 자신에 붙여 그 RectTransform을 흔든다.
// 트리거는 UIBurstParticle.Spawn 한 곳뿐이다(거래 확정 시 EventHub.OnBuyCoin/OnSellCoin으로 직접
// 흔들던 예전 방식은 파티클 크기와 무관한 고정 흔들림이라 제거 — 이제 파티클이 생기는 곳은 전부
// 여기를 거쳐가므로 이중 트리거 없이 여기 하나로 충분하다).
// EventHub.RaiseGamePaused()로 Time.timeScale이 0인 동안(거래/스킬 모달이 열려있는 동안)에도
// 흔들려야 하므로 unscaled time을 쓴다.
public class ScreenShaker : MonoBehaviour
{
    public static ScreenShaker Instance { get; private set; }

    [SerializeField] private float duration = 0.25f;
    [SerializeField] private float maxMagnitude = 10f;
    [SerializeField] private float minMagnitude = 2f; // intensity01=0이어도 최소한의 반응은 느껴지게

    private RectTransform rect;
    private Vector2 basePos;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);

        rect = (RectTransform)transform;
        basePos = rect.anchoredPosition;
    }

    public void Shake(float intensity01 = 1f)
    {
        float magnitude = Mathf.Lerp(minMagnitude, maxMagnitude, Mathf.Clamp01(intensity01));

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine(magnitude));
    }

    private IEnumerator ShakeRoutine(float magnitude)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float damper = 1f - t / duration;
            rect.anchoredPosition = basePos + Random.insideUnitCircle * magnitude * damper;
            yield return null;
        }
        rect.anchoredPosition = basePos;
    }
}
