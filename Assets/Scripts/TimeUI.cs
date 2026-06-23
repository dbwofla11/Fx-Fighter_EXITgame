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

    // 배속 사이클 (1, 2, 4, 8)
    private float[] speedCycle = { 1f, 2f, 4f, 8f };
    private int currentSpeedIndex = 0; // 현재 배열의 몇 번째 속도인지 기억하는 변수

    private void Start()
    {
        // 1. 멈춤 버튼: 시간을 0배속으로
        pauseButton.onClick.AddListener(() =>
        {
            TimeManager.Instance.SetTimeScale(0f);
        });

        // 2. 진행 버튼: 1배속으로 정상 진행시키고, 배속 사이클도 1배속(인덱스 0)으로 초기화
        playButton.onClick.AddListener(() =>
        {
            currentSpeedIndex = 0;
            ApplySpeed();
        });

        // 3. 배속 버튼: 누를 때마다 배열의 다음 속도로 넘어가기
        speedButton.onClick.AddListener(() =>
        {
            currentSpeedIndex++; // 인덱스 1 증가

            // 만약 인덱스가 배열의 끝(8배속)을 넘어가면 다시 처음(1배속)으로 돌아감
            if (currentSpeedIndex >= speedCycle.Length)
            {
                currentSpeedIndex = 0;
            }
            ApplySpeed();
        });

        // 게임 시작 시 기본 글자를 1배속으로 세팅
        speedButtonText.text = speedCycle[0] + "배속";
    }

    // 실제 배속을 적용하고 텍스트를 바꿔주는 함수
    private void ApplySpeed()
    {
        float newSpeed = speedCycle[currentSpeedIndex];
        TimeManager.Instance.SetTimeScale(newSpeed);
        speedButtonText.text = newSpeed + "배속";
    }

    private void Update()
    {
        if (TimeManager.Instance != null && dateText != null)
        {
            // 화면에 날짜 업데이트
            dateText.text = TimeManager.Instance.CurrentGameDate.ToString("yyyy-MM-dd\nHH:mm");
        }
    }
}
