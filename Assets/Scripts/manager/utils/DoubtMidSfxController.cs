using System.Collections;
using UnityEngine;

// Doubt(PlayerStat.Doubt)가 55 이상인 동안 3~6개월(턴) 간격으로 긴장감용 효과음을 한 번씩 재생한다.
// AudioManager.PlaySFX는 공용 sfxSource에 PlayOneShot으로 얹기 때문에 3초 뒤 임의로 끊을 수 없어서,
// 이 컨트롤러가 전용 AudioSource를 들고 직접 Play/Stop한다.
// 간격/구독 패턴은 SuspicionBgmController와 동일 (EventHub.OnMarketUpdated로 Doubt 캐시, OnDayChanged로 턴 카운트).
public class DoubtMidSfxController : MonoBehaviour
{
    private const float DoubtThreshold = 55f;
    private const float PlayDuration = 3f;

    // "3~6개월 간격의 턴" = 1턴(일) 기준 TurnsPerYear(365, MarketManager 상수)에서 역산한 개월당 턴 수.
    private const float TurnsPerMonth = 365f / 12f;
    private const float MinIntervalMonths = 3f;
    private const float MaxIntervalMonths = 6f;

    [SerializeField] private AudioClip[] clips; // clockdown / end-clocksound / heartsound 중 매번 랜덤 선택

    private AudioSource audioSource;
    private float currentDoubt;
    private int turnsSinceLastPlay;
    private int nextIntervalTurns;
    private Coroutine stopCoroutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        PickNextInterval();
    }

    private void OnEnable()
    {
        EventHub.OnDayChanged += HandleDayChanged;
        EventHub.OnMarketUpdated += HandleMarketUpdated;
        if (MarketManager.Instance != null)
            HandleMarketUpdated(MarketManager.Instance.CurrentStat);
    }

    private void OnDisable()
    {
        EventHub.OnDayChanged -= HandleDayChanged;
        EventHub.OnMarketUpdated -= HandleMarketUpdated;
        if (stopCoroutine != null)
            StopCoroutine(stopCoroutine);
    }

    private void HandleMarketUpdated(PlayerStat stat)
    {
        currentDoubt = stat.Doubt;
    }

    // Doubt가 55 미만인 턴은 카운트하지 않고 "정지"만 시킨다 (리셋은 아님) — 55 밑으로 잠깐 내려갔다
    // 올라와도 그동안 쌓인 간격이 날아가지 않게. 55 이상으로 계속 머무는 구간에서만 간격이 흐른다.
    private void HandleDayChanged()
    {
        if (currentDoubt < DoubtThreshold)
            return;

        turnsSinceLastPlay++;
        if (turnsSinceLastPlay < nextIntervalTurns)
            return;

        turnsSinceLastPlay = 0;
        PickNextInterval();
        PlayRandomClip();
    }

    private void PickNextInterval()
    {
        nextIntervalTurns = Mathf.RoundToInt(Random.Range(MinIntervalMonths, MaxIntervalMonths) * TurnsPerMonth);
    }

    private void PlayRandomClip()
    {
        if (clips == null || clips.Length == 0)
            return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null)
            return;

        audioSource.volume = AudioManager.Instance != null ? AudioManager.Instance.sfxVolume : 1f;
        audioSource.clip = clip;
        audioSource.Play();

        if (stopCoroutine != null)
            StopCoroutine(stopCoroutine);
        stopCoroutine = StartCoroutine(StopAfterDuration());
    }

    // 모달이 열려 timeScale=0이거나 배속(>1x) 중이어도 항상 실제 3초에 끊기도록 unscaled time을 쓴다
    // (모달 중엔 timeScale=0이라 scaled WaitForSeconds가 안 끝나 전체 파일이 재생되고,
    //  배속 중엔 timeScale>1이라 3초가 실제로는 그보다 짧게 지나가는 문제가 있었음).
    private IEnumerator StopAfterDuration()
    {
        yield return new WaitForSecondsRealtime(PlayDuration);
        audioSource.Stop();
    }
}
