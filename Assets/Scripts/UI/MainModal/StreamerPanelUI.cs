using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// RightPanel의 스트리머 캐릭터 패널. EventHub.OnMarketUpdated를 구독해 PlayerStat.StreamerReaction
// (StreamerReactionState 5단계)에 맞는 표정 프레임 애니메이션을 재생한다. StatGaugeUI/CoinPriceHeaderUI와 동일 패턴.
// 거래 확정(OnBuyCoin/OnSellCoin) 시 살짝 흔들려서 반응하는 연출을 더한다.
public class StreamerPanelUI : MonoBehaviour
{
    [SerializeField] private Image streamerImage;
    [SerializeField] private Sprite[] crashFrames;
    [SerializeField] private Sprite[] downFrames;
    [SerializeField] private Sprite[] neutralFrames;
    [SerializeField] private Sprite[] upFrames;
    [SerializeField] private Sprite[] surgeFrames;

    [SerializeField] private SpeechBubbleUI speechBubble;
    [SerializeField] private string streamerName = "루나";

    // 도트 애니메이션 재생 속도.
    [SerializeField] private float frameInterval = 1f / 6f;

    // Neutral은 계속 반복하지 않고 이 주기(초)마다 한 번씩만 재생하고 나머지 시간은 첫 프레임에서 쉰다.
    [SerializeField] private float neutralCycleInterval = 5f;

    private StreamerReactionState previousReaction;
    private Coroutine frameRoutine;

    // Neutral만 1→2→3→4→3→2(→반복) 핑퐁으로 재생한다. neutralFrames를 늘어난 순서로 한 번만 펼쳐서 캐싱.
    private Sprite[] neutralPingPongFrames;

    // 말풍선은 반응 단계가 바뀌어도 최소 이 턴 수(30~50 랜덤)가 지나야 다시 뜬다 (너무 자주 뜨는 것 방지).
    private const int MinBubbleCooldownTurns = 30;
    private const int MaxBubbleCooldownTurns = 50;

    // MaxBubbleCooldownTurns로 시작해야 첫 반응 변화 때 바로 뜬다 (int.MaxValue로 두면 다음 줄의 ++에서
    // 바로 오버플로해 음수가 되고, 그 뒤로 쿨타임 조건을 몇십억 턴 동안 못 넘기는 버그가 있었다).
    private int turnsSinceLastBubble = MaxBubbleCooldownTurns;
    private int bubbleCooldownTarget = MinBubbleCooldownTurns;

    // 스트리머 카페 댓글창(StreamerChatPanelUI)이 그대로 그릴 누적 댓글 목록. 말풍선과 동일한 타이밍(반응
    // 단계 변화 + 쿨타임 통과)에 1~3개씩 쌓인다. 무한정 쌓이지 않게 오래된 것부터 잘라낸다.
    private const int MaxChatLogEntries = 40;
    private readonly List<StreamerComment> chatLog = new();
    public IReadOnlyList<StreamerComment> ChatLog => chatLog;

    private const float ShakeDuration = 0.3f;
    private const float ShakeMagnitude = 10f;

    private RectTransform imageRect;
    private Vector2 imageBasePos;
    private Coroutine shakeRoutine;

    private void OnEnable()
    {
        EventHub.OnMarketUpdated += HandleMarketUpdated;
        EventHub.OnBuyCoin += HandleTrade;
        EventHub.OnSellCoin += HandleTrade;

        if (imageRect == null)
        {
            imageRect = (RectTransform)streamerImage.transform;
            imageBasePos = imageRect.anchoredPosition;
        }

        if (neutralPingPongFrames == null)
            neutralPingPongFrames = BuildPingPong(neutralFrames);

        if (MarketManager.Instance != null)
            HandleMarketUpdated(MarketManager.Instance.CurrentStat);
    }

    private void OnDisable()
    {
        EventHub.OnMarketUpdated -= HandleMarketUpdated;
        EventHub.OnBuyCoin -= HandleTrade;
        EventHub.OnSellCoin -= HandleTrade;

        // OnDisable 시 코루틴은 자동으로 멈추지만, 재활성화 때 새로 시작되도록 참조는 비워둔다.
        frameRoutine = null;
    }

    // 반응 단계별 프레임을 frameInterval 간격으로 순환 재생한다. 흔들림 연출(ShakeRoutine)은
    // anchoredPosition만 건드리므로 sprite 교체와 동시에 돌아도 서로 부딪히지 않는다.
    private IEnumerator FrameRoutine(Sprite[] frames)
    {
        int index = 0;
        while (true)
        {
            streamerImage.sprite = frames[index % frames.Length];
            index++;
            yield return new WaitForSecondsRealtime(frameInterval);
        }
    }

    // Neutral 전용: 핑퐁 시퀀스를 한 번 재생한 뒤 첫 프레임(쉬는 자세)에서 대기하다가
    // neutralCycleInterval마다 다시 재생한다.
    private IEnumerator NeutralFrameRoutine()
    {
        float playDuration = neutralPingPongFrames.Length * frameInterval;
        while (true)
        {
            foreach (var frame in neutralPingPongFrames)
            {
                streamerImage.sprite = frame;
                yield return new WaitForSecondsRealtime(frameInterval);
            }

            streamerImage.sprite = neutralFrames[0];
            float idle = neutralCycleInterval - playDuration;
            if (idle > 0f)
                yield return new WaitForSecondsRealtime(idle);
        }
    }

    // [1,2,3,4] -> [1,2,3,4,3,2] (앞뒤 끝 프레임은 겹치지 않게 중간만 뒤집어 붙인다). 순방향으로 그대로
    // 순환 재생하면 1,2,3,4,3,2,1,2,3,4,3,2,... 왕복 애니메이션이 된다.
    private static Sprite[] BuildPingPong(Sprite[] frames)
    {
        if (frames.Length <= 2)
            return frames;

        var result = new Sprite[frames.Length * 2 - 2];
        frames.CopyTo(result, 0);
        for (int i = 1; i < frames.Length - 1; i++)
            result[frames.Length + i - 1] = frames[frames.Length - 1 - i];
        return result;
    }

    private void HandleMarketUpdated(PlayerStat stat)
    {
        if (stat.StreamerReaction != previousReaction || frameRoutine == null)
        {
            if (frameRoutine != null)
                StopCoroutine(frameRoutine);

            if (stat.StreamerReaction == StreamerReactionState.Neutral)
            {
                frameRoutine = StartCoroutine(NeutralFrameRoutine());
            }
            else
            {
                Sprite[] frames = stat.StreamerReaction switch
                {
                    StreamerReactionState.Crash => crashFrames,
                    StreamerReactionState.Down => downFrames,
                    StreamerReactionState.Up => upFrames,
                    _ => surgeFrames,
                };
                frameRoutine = StartCoroutine(FrameRoutine(frames));
            }
        }

        turnsSinceLastBubble++;

        // 반응 단계가 바뀌었어도 쿨타임이 아직 안 지났으면 참는다.
        if (stat.StreamerReaction != previousReaction && turnsSinceLastBubble >= bubbleCooldownTarget)
        {
            if (speechBubble != null)
                speechBubble.Show(streamerName, StreamerLines.GetRandom(stat.StreamerReaction));

            int commentCount = Random.Range(1, 4);
            for (int i = 0; i < commentCount; i++)
                chatLog.Add(StreamerComments.GetRandom(stat.StreamerReaction));
            while (chatLog.Count > MaxChatLogEntries)
                chatLog.RemoveAt(0);

            turnsSinceLastBubble = 0;
            bubbleCooldownTarget = Random.Range(MinBubbleCooldownTurns, MaxBubbleCooldownTurns + 1);
        }

        previousReaction = stat.StreamerReaction;
    }

    private void HandleTrade(long amount)
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    // 거래 확정은 대개 모달이 열려 Time.timeScale이 0인 상태에서 일어나므로 unscaled time을 쓴다.
    private IEnumerator ShakeRoutine()
    {
        float t = 0f;
        while (t < ShakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float damper = 1f - t / ShakeDuration;
            imageRect.anchoredPosition = imageBasePos + Random.insideUnitCircle * ShakeMagnitude * damper;
            yield return null;
        }
        imageRect.anchoredPosition = imageBasePos;
    }
}
