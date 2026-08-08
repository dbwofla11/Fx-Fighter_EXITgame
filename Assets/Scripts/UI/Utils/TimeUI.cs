using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimeUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI dateText;

    [Header("Buttons")]
    public Button playPauseButton; // 재생/정지 겸용 토글 버튼
    public Image playPauseIcon;
    public Sprite pauseIconSprite; // 재생 중일 때 표시(누르면 정지)
    public Sprite playIconSprite;  // 정지 중일 때 표시(누르면 재생)
    public Button speedButton;  // 배속 (누를 때마다 순환)
    public TextMeshProUGUI speedButtonText; // 배속 버튼 안의 글자를 바꿔주기 위한 참조

    [Header("Pause Indicator")]
    public GameObject pauseHighlight; // 정지 버튼 뒤 네모 배경. 일시정지 중일 때만 켜짐

    [Header("SFX")]
    [SerializeField] private AudioClip clickSfx; // 클릭소리

    // 배속 사이클 (1, 2, 4, 8)
    private float[] speedCycle = { 1f, 2f, 4f, 8f };
    private int currentSpeedIndex = 0; // 현재 배열의 몇 번째 속도인지 기억하는 변수

    private void OnEnable()
    {
        EventHub.OnDayChanged += RefreshDateText;
        RefreshDateText();
    }

    private void OnDisable()
    {
        EventHub.OnDayChanged -= RefreshDateText;
    }

    private void Start()
    {
        // 재생/정지 토글 버튼: 정지 중이면 눌렀을 때 정지 전 배속 그대로 재생, 재생 중이면 눌렀을 때 정지.
        // TimeManager.TogglePause()가 currentTimeScale을 그대로 들고 있다가 복귀시켜준다.
        playPauseButton.onClick.AddListener(() =>
        {
            PlayClickSfx();
            TimeManager.Instance.TogglePause();
        });
        // 배속 버튼: 누를 때마다 배열(1,2,4,8)의 다음 속도로 넘어가기
        speedButton.onClick.AddListener(() =>
        {
            PlayClickSfx();
            currentSpeedIndex++; // 인덱스 1 증가

            // 만약 인덱스가 배열의 끝(8배속)을 넘어가면 다시 처음(1배속)으로 돌아감
            if (currentSpeedIndex >= speedCycle.Length)
            {
                currentSpeedIndex = 0;
            }
            ApplySpeed();
        });

        // 게임 시작 시 기본 글자를 1배속으로 세팅
        speedButtonText.text = "x" + speedCycle[0];

        if (pauseHighlight != null) pauseHighlight.SetActive(false);
    }

    private void Update()
    {
        bool isPaused = TimeManager.Instance.IsPaused;

        if (pauseHighlight != null && pauseHighlight.activeSelf != isPaused)
            pauseHighlight.SetActive(isPaused);

        if (playPauseIcon != null)
        {
            Sprite targetIcon = isPaused ? playIconSprite : pauseIconSprite;
            if (playPauseIcon.sprite != targetIcon) playPauseIcon.sprite = targetIcon;
        }
    }

    private void PlayClickSfx()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clickSfx);
    }

    // 실제 배속을 적용하고 텍스트를 바꿔주는 함수
    private void ApplySpeed()
    {
        float newSpeed = speedCycle[currentSpeedIndex];
        TimeManager.Instance.SetTimeScale(newSpeed);
        speedButtonText.text = "x" + newSpeed;
    }

    private void RefreshDateText()
    {
        if (TimeManager.Instance != null && dateText != null)
        {
            dateText.text = UIFormat.DateDash(TimeManager.Instance.CurrentGameDate);
        }
    }
}
