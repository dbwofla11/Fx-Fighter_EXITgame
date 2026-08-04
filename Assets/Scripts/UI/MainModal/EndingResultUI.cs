using System.Collections;
using TMPro;
using UnityEngine;

// 엔딩 결과 화면(전체화면 오버레이). EventHub.OnGameEnded(EndingType)를 구독해 4종 엔딩(체포/엑시트/영웅/거지)에
// 맞는 제목/설명 문구를 표시한다. 구독을 담당하는 이 스크립트 자신은 항상 켜진 상태를 유지하고(OnEnable/OnDisable로
// 구독 관리), 실제로 보이는 부분은 자식 패널(panel)만 SetActive로 토글한다 — 스크립트가 붙은 오브젝트
// 자체를 끄면 OnEnable/OnDisable이 다시 호출되지 않아 구독이 끊기기 때문(TradeModalUI/EventLogPanelUI처럼
// 스크립트 자신이 곧 패널인 경우와 다름).
// MarketManager.EndGame()이 이미 TimeManager.PauseGame()을 호출한 뒤 이 이벤트를 발행하므로, 여기서 별도로
// 게임을 멈추는 처리는 하지 않는다. 재시작 등 후속 액션은 아직 기획되지 않아 결과 문구 표시만 담당한다.
// Figma 목업이 아직 없어(엔딩 결과 화면에 대응하는 프레임을 찾지 못함) 최소 구성(제목 + 설명 텍스트)의
// 임시 UI로 만들었다 — 디자인이 정해지면 교체 필요.
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
        (string title, string description) = Describe(ending);

        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = description;

        // 체포 엔딩은 StatGaugeUI 3개 패널이 무너지는 연출(ArrestCollapseDuration)이 먼저 보이도록
        // 결과 패널 표시를 그만큼 늦춘다. TimeManager.PauseGame()으로 Time.timeScale이 이미 0이라
        // WaitForSecondsRealtime을 쓴다.
        if (ending == EndingType.Arrest)
            StartCoroutine(ShowPanelAfterDelay(StatGaugeUI.ArrestCollapseDuration));
        else if (panel != null)
            panel.SetActive(true);
    }

    private IEnumerator ShowPanelAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (panel != null) panel.SetActive(true);
    }

    // 문구는 Game_Formula.md 5장(엔딩 조건) 기준. 거지 엔딩은 문서에 "결과: 미정(추가 기획 필요)"라고만
    // 돼 있어 임시 문구로 채웠다 — 기획이 확정되면 교체 필요.
    private (string title, string description) Describe(EndingType ending)
    {
        switch (ending)
        {
            case EndingType.Arrest:
                return ("체포 엔딩", "끝없는 탐욕은 결국 법의 심판으로 돌아왔다.");
            case EndingType.Hero:
                return ("영웅 엔딩", "당신은 투기 대신 신뢰를 선택했다. 많은 사람이 당신의 프로젝트로 이익을 얻었고, 당신은 업계의 모범 사례로 남았다.");
            case EndingType.Exit:
                return ("엑시트 엔딩", "당신은 돈을 얻었지만 사람들의 신뢰를 잃었다. 세상은 당신을 성공한 사업가가 아닌 사기꾼으로 기억한다.");
            case EndingType.Broke:
                return ("거지 엔딩", "가진 것을 모두 잃었다. 현금도 코인도 남지 않은 채, 화려했던 시작은 빈털터리로 끝났다.");
            default:
                return ("", "");
        }
    }
}
