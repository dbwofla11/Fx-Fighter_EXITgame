using System.Collections;
using TMPro;
using UnityEngine;

// 엔딩 결과 화면(전체화면 오버레이). EventHub.OnGameEnded(EndingType)를 구독해 4종 엔딩(체포/엑시트/영웅/거지) 중
// 엑시트/영웅은 같은 씬에서 패널로 보여준다. 구독을 담당하는 이 스크립트 자신은 항상 켜진 상태를 유지하고
// (OnEnable/OnDisable로 구독 관리), 실제로 보이는 부분은 자식 패널(panel)만 SetActive로 토글한다 — 스크립트가
// 붙은 오브젝트 자체를 끄면 OnEnable/OnDisable이 다시 호출되지 않아 구독이 끊기기 때문(TradeModalUI/EventLogPanelUI처럼
// 스크립트 자신이 곧 패널인 경우와 다름).
// 체포/거지는 별도 엔딩씬(GameSceneManager.LoadEnding)으로 넘어간다 — 전용 배드엔딩 UI가 그 씬에 있다
// (EndingSceneUI). MarketManager.EndGame()이 이미 TimeManager.PauseGame()을 호출한 뒤 이 이벤트를 발행하므로,
// 여기서 별도로 게임을 멈추는 처리는 하지 않는다.
public class EndingResultUI : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void OnEnable()
    {
        EventHub.OnGameEnded += HandleGameEnded;
    }

    private void OnDisable()
    {
        EventHub.OnGameEnded -= HandleGameEnded;
    }

    private void HandleGameEnded(EndingType ending)
    {
        // 체포/거지 : StatGaugeUI 3개 패널과 차트 캔들이 무너지는 연출(ArrestCollapseDuration)이 먼저
        // 보이도록 기다린 뒤, 이 씬에 패널을 띄우는 대신 전용 엔딩씬으로 넘어간다.
        if (ending == EndingType.Arrest || ending == EndingType.Broke)
        {
            StartCoroutine(TransitionToEndingScene(ending, StatGaugeUI.ArrestCollapseDuration));
            return;
        }

        (string title, string description) = Describe(ending);
        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = description;
        if (panel != null) panel.SetActive(true);
    }

    // TimeManager.PauseGame()으로 Time.timeScale이 이미 0이라 WaitForSecondsRealtime을 쓴다.
    private IEnumerator TransitionToEndingScene(EndingType ending, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        EndingHandoff.Ending = ending;
        GameSceneManager.LoadEnding();
    }

    // 액시트 전용 엔딩 
    // 문구는 Game_Formula.md 5장(엔딩 조건) 기준. 체포/거지 문구는 EndingSceneUI로 이동했다.
    private (string title, string description) Describe(EndingType ending)
    {
        switch (ending)
        {
            case EndingType.Hero:
                return ("엑시트 엔딩", "당신은 투기 대신 신뢰를 선택했다. 많은 사람이 당신의 프로젝트로 이익을 얻었고, 당신은 업계의 모범 사례로 남았다.");
            case EndingType.Exit:
                return ("엑시트 엔딩", "당신은 돈을 얻었지만 사람들의 신뢰를 잃었다. 세상은 당신을 성공한 사업가가 아닌 사기꾼으로 기억한다.");
            default:
                return ("", "");
        }
    }
}
