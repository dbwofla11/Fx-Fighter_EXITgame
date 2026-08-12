using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// RightPanel의 스트리머 캐릭터 패널. EventHub.OnMarketUpdated를 구독해 PlayerStat.StreamerReaction
// (StreamerReactionState 5단계)에 맞는 표정 스프라이트로 교체한다. StatGaugeUI/CoinPriceHeaderUI와 동일 패턴.
// 거래 확정(OnBuyCoin/OnSellCoin) 시 살짝 흔들려서 반응하는 연출을 더한다.
public class StreamerPanelUI : MonoBehaviour
{
    [SerializeField] private Image streamerImage;
    [SerializeField] private Sprite crashSprite;
    [SerializeField] private Sprite downSprite;
    [SerializeField] private Sprite neutralSprite;
    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite surgeSprite;

    [SerializeField] private SpeechBubbleUI speechBubble;
    [SerializeField] private string streamerName = "루나";

    private StreamerReactionState previousReaction;

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

        if (MarketManager.Instance != null)
            HandleMarketUpdated(MarketManager.Instance.CurrentStat);
    }

    private void OnDisable()
    {
        EventHub.OnMarketUpdated -= HandleMarketUpdated;
        EventHub.OnBuyCoin -= HandleTrade;
        EventHub.OnSellCoin -= HandleTrade;
    }

    private void HandleMarketUpdated(PlayerStat stat)
    {
        streamerImage.sprite = stat.StreamerReaction switch
        {
            StreamerReactionState.Crash => crashSprite,
            StreamerReactionState.Down => downSprite,
            StreamerReactionState.Up => upSprite,
            StreamerReactionState.Surge => surgeSprite,
            _ => neutralSprite,
        };

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
