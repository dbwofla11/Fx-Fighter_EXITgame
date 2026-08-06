using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimeUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI dateText;

    [Header("Buttons")]
    public Button pauseButton;  // 멈춤
    public Button playButton;   // 진행
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
        // 멈춤 버튼: 0배속 시키는 PauseGame 불러잇
        pauseButton.onClick.AddListener(() =>
        {
            PlayClickSfx();
            TimeManager.Instance.PauseGame();
        });

        // 진행 버튼: 정지든 배속(2/4/8x) 상태든 항상 1배속으로 되돌린다.
        playButton.onClick.AddListener(() =>
        {
            PlayClickSfx();
            currentSpeedIndex = 0;
            ApplySpeed();
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
        // 스페이스바/P: 정지 상태 토글
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.P))
        {
            TimeManager.Instance.TogglePause();
        }

        if (pauseHighlight != null)
        {
            bool isPaused = TimeManager.Instance.IsPaused;
            if (pauseHighlight.activeSelf != isPaused) pauseHighlight.SetActive(isPaused);
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
