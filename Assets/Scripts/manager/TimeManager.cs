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
    private bool isManuallyPaused = false; // 정지 버튼으로 직접 멈춘 상태인지 (모달 열고 닫는 것과 구분하기 위함)
    public bool IsPaused => isManuallyPaused;

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
        ResetState();
    }

    // 새 게임 시작(직업 선택 확정) 시 CharacterSelectUI가 호출한다. Managers는 DontDestroyOnLoad라
    // 두 번째 플레이부터는 Awake/Start가 다시 안 불리므로, 여기서 명시적으로 초기값으로 되돌려야 한다.
    public void ResetState()
    {
        // 게임 시작 날짜 설정 (예: 2024년 1월 1일)
        CurrentGameDate = new DateTime(2021, 1, 1);
        previousDate = CurrentGameDate.Date;

        SetTimeScale(1f);
    }

    private void OnEnable()
    {
        EventHub.OnGamePaused += HandleModalPaused;
        EventHub.OnGameResumed += ResumeGame;
    }

    private void OnDisable()
    {
        EventHub.OnGamePaused -= HandleModalPaused;
        EventHub.OnGameResumed -= ResumeGame;
    }

    // 거래/스킬/이벤트 등 모달이 열릴 때 EventHub를 통해 호출됨. 정지 버튼(PauseGame)과 달리
    // isManuallyPaused를 건드리지 않는다 — 안 그러면 모달을 닫을 때 ResumeGame이 자기 자신 때문에
    // 막혀서 시간이 안 흐르는 버그가 생긴다.
    private void HandleModalPaused()
    {
        Time.timeScale = 0f;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        CurrentGameDate = CurrentGameDate.AddDays(Time.deltaTime);

        if (CurrentGameDate.Date != previousDate)
        {
            bool monthChanged = CurrentGameDate.Year != previousDate.Year || CurrentGameDate.Month != previousDate.Month;
            previousDate = CurrentGameDate.Date;

            if (monthChanged) EventHub.RaiseMonthChanged();
            EventHub.RaiseDayChanged();
        }
    }

    // 시간 배속 설정 함수
    public void SetTimeScale(float scale)
    {
        currentTimeScale = scale;
        isManuallyPaused = false; // 배속을 직접 고르는 행동 = 일시정지 해제
        Time.timeScale = currentTimeScale;
        Debug.Log($"현재 배속: {currentTimeScale}x");
    }

    // 일시정지를 토글로 만들었다가 배속 저장이 안 되서 함수 나눔
    public void PauseGame()
    {
        Time.timeScale = 0f;
        isManuallyPaused = true;
        Debug.Log("게임 일시정지");
    }

    // 모달(스킬/설정 등)이 닫힐 때 호출됨. 정지 버튼으로 직접 멈춘 상태라면 모달을 닫아도 계속 멈춰있어야 한다.
    public void ResumeGame()
    {
        if (isManuallyPaused) return;

        // 현 TimeScale로 복귀
        Time.timeScale = currentTimeScale;
        Debug.Log($"게임 재개: 현재 배속 x{currentTimeScale}");
    }

    // 스페이스바/P 단축키용: 정지 중이면 이전 배속으로, 아니면 정지로 전환
    public void TogglePause()
    {
        if (isManuallyPaused)
            SetTimeScale(currentTimeScale);
        else
            PauseGame();
    }
}
