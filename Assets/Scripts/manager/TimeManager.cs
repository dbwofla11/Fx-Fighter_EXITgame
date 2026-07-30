using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    // 싱글톤 패턴: 어디서든 TimeManager.Instance로 접근 가능하게 함
    public static TimeManager Instance { get; private set; }

    [Header("Time Settings")]
    public DateTime CurrentGameDate { get; private set; }
    private DateTime previousDate;
    private float currentTimeScale = 1f; // 실제 1초당 게임 내 몇 분이 흐를지 결정

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 넘어가도 파괴되지 않음
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 게임 시작 날짜 설정 (예: 2024년 1월 1일)
        CurrentGameDate = new DateTime(2021, 1, 1);
        previousDate = CurrentGameDate.Date;

        SetTimeScale(1f);
    }

    private void OnEnable()
    {
        EventHub.OnGamePaused += PauseGame;
        EventHub.OnGameResumed += ResumeGame;
    }

    private void OnDisable()
    {
        EventHub.OnGamePaused -= PauseGame;
        EventHub.OnGameResumed -= ResumeGame;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        CurrentGameDate = CurrentGameDate.AddDays(Time.deltaTime);

        if (CurrentGameDate.Date != previousDate)
        {
            previousDate = CurrentGameDate.Date;

            EventHub.RaiseDayChanged();
        }
    }

    // 시간 배속 설정 함수
    public void SetTimeScale(float scale)
    {
        currentTimeScale = scale;
        Time.timeScale = currentTimeScale;
        Debug.Log($"현재 배속: {currentTimeScale}x");
    }

    // 일시정지를 토글로 만들었다가 배속 저장이 안 되서 함수 나눔
    public void PauseGame()
    {
        Time.timeScale = 0f;
        Debug.Log("게임 일시정지");
    }

    public void ResumeGame()
    {
        // 현 TimeScale로 복귀
        Time.timeScale = currentTimeScale; 
        Debug.Log($"게임 재개: 현재 배속 x{currentTimeScale}");
    }
}