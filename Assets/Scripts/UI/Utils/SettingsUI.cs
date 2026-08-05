using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // Button 컴포넌트를 제어하기 위해 필요

public class SettingsUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject settingsPanel;  // 중앙에 뜰 설정 팝업창
    public Button openButton;         // 우측 상단 톱니바퀴 버튼
    public Button closeButton;        // "돌아가기" 버튼
    public Button quitButton;         // "게임종료" 버튼

    [Header("Menu")]
    public GameObject menuBox;        // 설정 메인 메뉴 (디스플레이/사운드/언어/게임종료/돌아가기)
    public Button displayButton;      // 디스플레이 (목업 없어 스텁)
    public Button soundButton;        // 사운드 → SoundPanel 열기
    public Button languageButton;     // 언어 (목업 없어 스텁)

    [Header("Sound Panel")]
    public GameObject soundPanel;
    public Button soundCloseButton;   // X 버튼 → 메뉴로 복귀
    public Button saveButton;         // 저장하기
    public Slider bgmSlider;
    public Slider sfxSlider;
    public Toggle bgmMuteToggle;
    public Toggle sfxMuteToggle;
    public TMP_Text bgmPercentText;
    public TMP_Text sfxPercentText;

    [Header("Resolution Settings")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    private const string ResolutionIndexKey = "ResolutionIndex";
    private const string FullscreenKey = "Fullscreen";
    private List<Resolution> resolutions;

    private float bgmVolumeBeforeMute;
    private float sfxVolumeBeforeMute;

    private void Start()
    {
        // 시작할 때 설정 팝업창은 보이지 않게 꺼두기
        settingsPanel.SetActive(false);

        // 버튼 클릭 시 작동할 함수를 코드로 연결
        openButton.onClick.AddListener(OpenSettings);
        closeButton.onClick.AddListener(CloseSettings);
        quitButton.onClick.AddListener(QuitGame);
        soundButton.onClick.AddListener(OpenSoundPanel);
        soundCloseButton.onClick.AddListener(CloseSoundPanel);
        saveButton.onClick.AddListener(SaveAndCloseSoundPanel);

        SetupSoundControls();
        SetupResolutionControls();
    }

    private void OpenSettings()
    {
        ModalPause.Open(settingsPanel);
        menuBox.SetActive(true);
        soundPanel.SetActive(false);
    }

    private void CloseSettings()
    {
        // TimeManager가 기억해둔 배속(1/2/4/8)으로 복귀한다.
        ModalPause.Close(settingsPanel);
    }

    private void OpenSoundPanel()
    {
        menuBox.SetActive(false);
        soundPanel.SetActive(true);

        bgmMuteToggle.SetIsOnWithoutNotify(false);
        sfxMuteToggle.SetIsOnWithoutNotify(false);
        bgmSlider.interactable = true;
        sfxSlider.interactable = true;
        bgmSlider.SetValueWithoutNotify(AudioManager.Instance.bgmVolume);
        sfxSlider.SetValueWithoutNotify(AudioManager.Instance.sfxVolume);
        UpdatePercentText(bgmPercentText, AudioManager.Instance.bgmVolume);
        UpdatePercentText(sfxPercentText, AudioManager.Instance.sfxVolume);
    }

    private void CloseSoundPanel()
    {
        soundPanel.SetActive(false);
        menuBox.SetActive(true);
    }

    private void SaveAndCloseSoundPanel()
    {
        PlayerPrefs.Save();
        CloseSoundPanel();
    }

    // "게임종료" 버튼: 앱을 끄지 않고 타이틀로 돌아간다 (완전 종료는 타이틀 화면의 "완전 게임종료"에서).
    private void QuitGame()
    {
        TimeManager.Instance.SetTimeScale(1f); // TimeManager는 DontDestroyOnLoad라 타이틀로 넘어가도 일시정지가 남아있지 않게 초기화
        AudioManager.Instance.StopBGM(); // AudioManager도 DontDestroyOnLoad라 안 끄면 메인 게임 브금이 타이틀까지 계속 들린다
        GameSceneManager.LoadTitle();
    }

    private void SetupSoundControls()
    {
        if (bgmSlider != null)
        {
            bgmSlider.value = AudioManager.Instance.bgmVolume;
            bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
        }
        if (sfxSlider != null)
        {
            sfxSlider.value = AudioManager.Instance.sfxVolume;
            sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        }
        if (bgmMuteToggle != null) bgmMuteToggle.onValueChanged.AddListener(OnBgmMuteToggled);
        if (sfxMuteToggle != null) sfxMuteToggle.onValueChanged.AddListener(OnSfxMuteToggled);
    }

    private void OnBgmSliderChanged(float volume)
    {
        AudioManager.Instance.SetBgmVolume(volume);
        UpdatePercentText(bgmPercentText, volume);
    }

    private void OnSfxSliderChanged(float volume)
    {
        AudioManager.Instance.SetSfxVolume(volume);
        UpdatePercentText(sfxPercentText, volume);
    }

    private void OnBgmMuteToggled(bool isMuted)
    {
        bgmSlider.interactable = !isMuted;
        float volume = isMuted ? 0f : bgmVolumeBeforeMute;
        if (isMuted) bgmVolumeBeforeMute = bgmSlider.value;
        bgmSlider.SetValueWithoutNotify(volume);
        OnBgmSliderChanged(volume);
    }

    private void OnSfxMuteToggled(bool isMuted)
    {
        sfxSlider.interactable = !isMuted;
        float volume = isMuted ? 0f : sfxVolumeBeforeMute;
        if (isMuted) sfxVolumeBeforeMute = sfxSlider.value;
        sfxSlider.SetValueWithoutNotify(volume);
        OnSfxSliderChanged(volume);
    }

    private static void UpdatePercentText(TMP_Text text, float volume)
    {
        if (text != null) text.text = Mathf.RoundToInt(volume * 100f) + "%";
    }

    private void SetupResolutionControls()
    {
        // 같은 해상도(다른 주사율)는 하나로 합쳐서 목록 구성
        resolutions = Screen.resolutions
            .GroupBy(r => new { r.width, r.height })
            .Select(g => g.Last())
            .ToList();

        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(resolutions.Select(r => $"{r.width} x {r.height}").ToList());

            int savedIndex = PlayerPrefs.GetInt(ResolutionIndexKey, resolutions.FindIndex(r => r.width == Screen.currentResolution.width && r.height == Screen.currentResolution.height));
            savedIndex = Mathf.Clamp(savedIndex, 0, resolutions.Count - 1);
            resolutionDropdown.SetValueWithoutNotify(savedIndex);
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }

        if (fullscreenToggle != null)
        {
            bool isFullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
            fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }
    }

    private void OnResolutionChanged(int index)
    {
        Resolution r = resolutions[index];
        Screen.SetResolution(r.width, r.height, Screen.fullScreen);
        PlayerPrefs.SetInt(ResolutionIndexKey, index);
    }

    private void OnFullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
    }
}
