using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 엔딩씬(ExitEndingScene) 전용. 영웅/엑시트 엔딩(EndingHandoff.Ending)은 배경 이미지를 공유하고
// 문구만 EndingType에 따라 다르게 띄운다. 체포/거지 배드엔딩(EndingSceneUI, EndingScene)과는
// 완전히 별도 씬으로 관리한다 — StatGaugeUI/PriceChartCandles의 붕괴 연출이 없어 전환 연출도 더 단순함.
// MarketManager.EndGame()이 TimeManager.PauseGame()으로 Time.timeScale을 0으로 만든 채 이 씬에
// 들어오므로, 페이드 코루틴은 전부 unscaledDeltaTime을 쓴다(EndingSceneUI와 같은 이유).
public class ExitEndingSceneUI : MonoBehaviour
{
    [Header("배경 (Hero/Exit 공용)")]
    public Image bgImage;
    public Sprite bgSprite;

    [Header("문구")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;

    [Header("암전")]
    public CanvasGroup blackOverlay;
    public float blackDelay = 1f;
    public float blackFadeDuration = 1f;

    [Header("버튼 (\"타이틀로 돌아가기\")")]
    public Button backToTitleButton;
    public AudioClip clickSfx;

    [Header("사운드 (없으면 재생하지 않는다 — TitleScreenUI와 같은 패턴)")]
    public AudioClip bgmClip;

    private void Start()
    {
        EndingType ending = EndingHandoff.Ending ?? EndingType.Exit;

        if (bgImage != null) bgImage.sprite = bgSprite;

        if (titleText != null) titleText.text = "엑시트 엔딩";
        // 문구는 Game_Formula.md 5장(엔딩 조건) 기준.
        if (descriptionText != null)
            descriptionText.text = ending == EndingType.Hero
                ? "당신은 투기 대신 신뢰를 선택했다. 많은 사람이 당신의 프로젝트로 이익을 얻었고, 당신은 업계의 모범 사례로 남았다."
                : "당신은 돈을 얻었지만 사람들의 신뢰를 잃었다. 세상은 당신을 성공한 사업가가 아닌 사기꾼으로 기억한다.";

        if (blackOverlay != null) blackOverlay.alpha = 0f;

        // AudioManager는 DontDestroyOnLoad라 여기서 끄지 않으면 메인 게임 브금이 이 씬까지 계속 들린다.
        if (AudioManager.Instance != null)
        {
            if (bgmClip != null) AudioManager.Instance.PlayBGM(bgmClip);
            else AudioManager.Instance.StopBGM();
        }

        if (backToTitleButton != null)
        {
            backToTitleButton.onClick.AddListener(OnBackToTitle);
            backToTitleButton.gameObject.AddComponent<HoverIdleBob>().SetAlwaysActive(true);
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
