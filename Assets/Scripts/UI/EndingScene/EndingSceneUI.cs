using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 엔딩씬(EndingScene) 전용. EndingHandoff.Ending(체포/거지)에 맞는 배경 이미지 + 문구를 띄운다.
// 배경 이미지는 순수 배경(문구 없음)이라 GameOverText/EndingText를 코드로 채운다.
// 연출: ①배경 이미지 즉시 표시 → ②잠깐 뒤 화면이 검은색으로 페이드 → ③암전이 다시 걷히며
// 이미지+문구가 드러난다.
// MarketManager.EndGame()이 TimeManager.PauseGame()으로 Time.timeScale을 0으로 만든 채 이 씬에
// 들어오므로, 페이드 코루틴은 전부 unscaledDeltaTime을 쓴다(StatGaugeUI.CollapseRoutine과 같은 이유).
public class EndingSceneUI : MonoBehaviour
{
    [Header("배경 (EndingType별 스프라이트)")]
    public Image bgImage;
    public Sprite arrestBgSprite;
    public Sprite delistingBgSprite;

    [Header("문구")]
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI endingText;

    [Header("암전")]
    public CanvasGroup blackOverlay;
    public float blackDelay = 1f;
    public float blackFadeDuration = 1f;

    [Header("버튼 (\"타이틀로 돌아가기\")")]
    public Button backToTitleButton;
    public AudioClip clickSfx;

    [Header("사운드 (EndingType별, 없으면 재생하지 않는다 — TitleScreenUI와 같은 패턴)")]
    public AudioClip arrestBgmClip;
    public AudioClip delistingBgmClip;

    private void Start()
    {
        EndingType ending = EndingHandoff.Ending ?? EndingType.Broke;

        if (bgImage != null)
            bgImage.sprite = ending == EndingType.Arrest ? arrestBgSprite : delistingBgSprite;

        if (gameOverText != null) gameOverText.text = "Game Over";
        if (endingText != null)
            endingText.text = ending == EndingType.Arrest
                ? "결국 금감원에 걸려버렸다....."
                : "코인이 상장폐지 당했다.......";

        if (blackOverlay != null) blackOverlay.alpha = 0f;

        // AudioManager는 DontDestroyOnLoad라 여기서 끄지 않으면 메인 게임 브금이 엔딩씬까지 계속 들린다
        // (TitleScreenUI가 캐릭터 선택 씬으로 넘어갈 때 StopBGM하는 것과 같은 이유).
        AudioClip bgmClip = ending == EndingType.Arrest ? arrestBgmClip : delistingBgmClip;
        if (AudioManager.Instance != null)
        {
            if (bgmClip != null) AudioManager.Instance.PlayBGM(bgmClip);
            else AudioManager.Instance.StopBGM();
        }

        if (backToTitleButton != null)
        {
            backToTitleButton.onClick.AddListener(OnBackToTitle);
            // TitleScreenUI의 "게임 시작" 버튼과 같은 idle 애니메이션(HoverIdleBob 재사용, alwaysActive로 상시 재생).
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
        // TimeManager는 DontDestroyOnLoad라 타이틀로 넘어가도 일시정지(timeScale 0)가 남아있지 않게 초기화
        // (SettingsUI.QuitGame과 같은 이유/패턴).
        if (TimeManager.Instance != null) TimeManager.Instance.SetTimeScale(1f);
        GameSceneManager.LoadTitle();
    }
}
