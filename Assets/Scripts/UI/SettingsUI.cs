using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI; // Button 컴포넌트를 제어하기 위해 필요

public class SettingsUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject settingsPanel;  // 중앙에 뜰 설정 팝업창
    public Button openButton;         // 우측 상단 톱니바퀴 버튼
    public Button closeButton;        // 팝업창 안의 닫기 버튼

    [Header("Quit Settings")]
    public Button quitButton;

    private void Start()
    {
        // 시작할 때 설정 팝업창은 보이지 않게 꺼두기
        settingsPanel.SetActive(false);

        // 버튼 클릭 시 작동할 함수를 코드로 연결
        openButton.onClick.AddListener(OpenSettings);
        closeButton.onClick.AddListener(CloseSettings);
        quitButton.onClick.AddListener(QuitGame);
    }

    private void OpenSettings()
    {
        // 설정창 켜기
        settingsPanel.SetActive(true);
        
        // 설정창이 열릴 때 게임을 일시정지
        Time.timeScale = 0f; 
    }

    private void CloseSettings()
    {
        // 설정창 끄기
        settingsPanel.SetActive(false);
        
        // 일시정지를 풀려면 주석을 해제하세요. (TimeManager가 있다면 연동 필요)
         Time.timeScale = 1f; 
    }

    private void QuitGame()
    {
        Debug.Log("Quit Game");
// if문은 유니티 에디터 환경에서 플레이 중일 때 끄기 / else는 빌드에서
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
// #이 붙은 명령어는 '전처리기 지시어'라는 빌드 전에 작동해서 환경을 나누는 코드
    }
}