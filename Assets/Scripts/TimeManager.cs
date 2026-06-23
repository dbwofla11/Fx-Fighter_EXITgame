using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    // 싱글톤 패턴: 어디서든 TimeManager.Instance로 접근 가능하게 함
    public static TimeManager Instance { get; private set; }

    [Header("Time Settings")]
    public float timeMultiplier = 1f; // 실제 1초당 게임 내 몇 분이 흐를지 결정
    public DateTime CurrentGameDate { get; private set; }

    private float currentTimeScale = 1f;

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
        SetTimeScale(1f);
    }

    private void Update()
    {
        // Time.deltaTime은 배속(timeScale)의 영향을 받습니다.
        if (Time.timeScale > 0)
        {
            // 실제 시간 * 배속 * 설정한 배수를 게임 날짜에 더함
            CurrentGameDate = CurrentGameDate.AddMinutes(Time.deltaTime * timeMultiplier * 60);
        }
    }

    // 시간 배속 설정 함수
    public void SetTimeScale(float scale)
    {
        currentTimeScale = scale;
        Time.timeScale = currentTimeScale;
        Debug.Log($"현재 배속: {currentTimeScale}x");
    }

    // 일시 정지 토글 함수
    public void TogglePause()
    {
        if (Time.timeScale > 0)
        {
            Time.timeScale = 0f;
            Debug.Log("게임 일시정지");
        }
        else
        {
            Time.timeScale = currentTimeScale; // 원래 배속으로 복구
            Debug.Log("게임 재개");
        }
    }
}