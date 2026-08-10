using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 엔딩씬(ExitEndingScene) 전용. 영웅/엑시트 엔딩(EndingHandoff.Ending) 공용 : 스토리 2비트
// (StoryDialogueController가 진행) → 통계 요약 화면(크레딧처럼 스크롤) 순서로 넘어간다.
// 체포/거지 배드엔딩(EndingSceneUI, EndingScene)과는 완전히 별도 씬으로 관리한다.
// MarketManager.EndGame()이 TimeManager.PauseGame()으로 Time.timeScale을 0으로 만든 채 이 씬에
// 들어오므로, 코루틴은 전부 unscaledDeltaTime을 쓴다(EndingSceneUI와 같은 이유).
public class ExitEndingSceneUI : MonoBehaviour
{
    [Header("스토리 (비트1~2)")]
    public StoryDialogueController storyController;

    [Header("암전 (인트로 연출)")]
    public CanvasGroup blackOverlay;
    public float blackDelay = 1f;
    public float blackFadeDuration = 1f;

    [Header("통계 요약 화면 (비트3, 크레딧 스크롤)")]
    public GameObject statsPanel;
    public ScrollRect statsScrollRect;
    public float scrollDuration = 18f;
    public TextMeshProUGUI exitCashText;
    public TextMeshProUGUI maxCoinsText;
    public TextMeshProUGUI skillPurchaseCountText;
    public TextMeshProUGUI buyCountText;
    public TextMeshProUGUI sellCountText;
    public TextMeshProUGUI manipulateCountText;

    [Header("버튼 (\"타이틀로 돌아가기\", 스크롤이 끝나야 활성화)")]
    public Button backToTitleButton;
    public AudioClip clickSfx;

    [Header("사운드 (EndingType별, 없으면 재생하지 않는다 — EndingSceneUI와 같은 패턴)")]
    public AudioClip heroBgmClip;
    public AudioClip exitBgmClip;

    private void Start()
    {
        if (blackOverlay != null) blackOverlay.alpha = 0f;
        if (statsPanel != null) statsPanel.SetActive(false);

        // AudioManager는 DontDestroyOnLoad라 여기서 끄지 않으면 메인 게임 브금이 이 씬까지 계속 들린다.
        AudioClip bgmClip = EndingHandoff.Ending == EndingType.Hero ? heroBgmClip : exitBgmClip;
        if (AudioManager.Instance != null)
        {
            if (bgmClip != null) AudioManager.Instance.PlayBGM(bgmClip);
            else AudioManager.Instance.StopBGM();
        }

        if (backToTitleButton != null)
        {
            backToTitleButton.interactable = false; // 통계 스크롤이 끝나기 전까지 조작 불가
            backToTitleButton.onClick.AddListener(OnBackToTitle);
        }

        if (storyController != null)
        {
            storyController.OnSequenceComplete += ShowStatsScreen;
            storyController.Begin();
        }

        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        yield return new WaitForSecondsRealtime(blackDelay);
        yield return FadeCanvasGroup(blackOverlay, 1f, blackFadeDuration);
        yield return FadeCanvasGroup(blackOverlay, 0f, blackFadeDuration);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float targetAlpha, float duration)
    {
        if (group == null) yield break;

        float startAlpha = group.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(t / duration));
            yield return null;
        }
        group.alpha = targetAlpha;
    }

    private void ShowStatsScreen()
    {
        if (statsPanel != null) statsPanel.SetActive(true);

        GameStatsTracker stats = GameStatsTracker.Instance;
        if (stats != null)
        {
            if (exitCashText != null) exitCashText.text = "마지막으로 엑시트 한 현금\n" + stats.ExitCash.ToString("N0") + " 원";
            if (maxCoinsText != null) maxCoinsText.text = "최대로 많이 들고 있던 코인갯수\n" + stats.MaxCoins.ToString("N0") + " 개";
            if (skillPurchaseCountText != null) skillPurchaseCountText.text = "지금까지 스킬을 구매한 횟수\n" + stats.SkillPurchaseCount + " 회";
            if (buyCountText != null) buyCountText.text = "지금까지 매수버튼을 누른 횟수\n" + stats.BuyCount + " 회";
            if (sellCountText != null) sellCountText.text = "지금까지 매도버튼을 누른 횟수\n" + stats.SellCount + " 회";
            if (manipulateCountText != null) manipulateCountText.text = "지금까지 코인발행조작의 갯수\n" + stats.SupplyManipulateCount + " 회";
        }

        StartCoroutine(AutoScrollStats());
    }

    // 크레딧처럼 위로 스크롤하다가 끝(마지막 항목 = -End-/타이틀로 돌아가기)에 도달하면 멈추고,
    // 그때부터 backToTitleButton을 눌러 조작할 수 있게 한다.
    private IEnumerator AutoScrollStats()
    {
        if (statsScrollRect != null)
        {
            statsScrollRect.verticalNormalizedPosition = 1f;

            float t = 0f;
            while (t < scrollDuration)
            {
                t += Time.unscaledDeltaTime;
                statsScrollRect.verticalNormalizedPosition = Mathf.Lerp(1f, 0f, t / scrollDuration);
                yield return null;
            }
            statsScrollRect.verticalNormalizedPosition = 0f;
        }

        if (backToTitleButton != null)
        {
            backToTitleButton.interactable = true;
            backToTitleButton.gameObject.AddComponent<HoverIdleBob>().SetAlwaysActive(true);
        }
    }

    private void OnBackToTitle()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clickSfx);
            AudioManager.Instance.StopBGM();
        }
        // TimeManager는 DontDestroyOnLoad라 타이틀로 넘어가도 일시정지(timeScale 0)가 남아있지 않게 초기화.
        if (TimeManager.Instance != null) TimeManager.Instance.SetTimeScale(1f);
        GameSceneManager.LoadTitle();
    }
}
