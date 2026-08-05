using UnityEngine;
using UnityEngine.UI;

// 타이틀 씬 전용. "게임 시작" 클릭 시 캐릭터 선택 씬으로 넘어간다 (씬 전환은 항상 GameSceneManager를 통한다).
public class TitleScreenUI : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitAppButton; // "완전 게임종료" — 앱 자체를 끈다 (SettingsUI의 "게임종료"는 여기로 돌아올 뿐, 앱 종료는 여기서만)
    [SerializeField] private AudioClip titleBgmClip; // GameStarter.mainBgmClip과 동일한 패턴 (AudioManager.PlayBGM)

    private void Start()
    {
        if (AudioManager.Instance != null && titleBgmClip != null)
            AudioManager.Instance.PlayBGM(titleBgmClip);

        if (startButton != null)
        {
            startButton.onClick.AddListener(() => GameSceneManager.LoadCharacterSelect());

            // "게임 시작"만 매수·매도 버튼과 같은 둥둥 뜨는 idle 애니메이션을 쓴다 (HoverIdleBob 재사용,
            // 호버 대상이 아니라 alwaysActive로 항상 재생되게 한다). 타이틀/부제목은 고정.
            startButton.gameObject.AddComponent<HoverIdleBob>().SetAlwaysActive(true);
        }

        if (quitAppButton != null)
            quitAppButton.onClick.AddListener(QuitApplication);
    }

    private void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
