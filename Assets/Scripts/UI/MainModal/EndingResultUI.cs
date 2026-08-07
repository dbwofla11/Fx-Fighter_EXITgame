using System.Collections;
using UnityEngine;

// 엔딩 결과 디스패처(비주얼 없음). EventHub.OnGameEnded(EndingType)를 구독해 4종 엔딩(체포/영웅/엑시트/거지)을
// 각자 전용 씬으로 넘긴다 — 체포/거지는 EndingScene(EndingSceneUI), 영웅/엑시트는 ExitEndingScene
// (ExitEndingSceneUI). MarketManager.EndGame()이 이미 TimeManager.PauseGame()을 호출한 뒤 이 이벤트를
// 발행하므로, 여기서 별도로 게임을 멈추는 처리는 하지 않는다.
public class EndingResultUI : MonoBehaviour
{
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
        // 보이도록 기다린 뒤 넘어간다. 영웅/엑시트는 그런 붕괴 연출이 없어 바로 넘어간다.
        bool isBadEnding = ending == EndingType.Arrest || ending == EndingType.Broke;
        float delay = isBadEnding ? StatGaugeUI.ArrestCollapseDuration : 0f;
        StartCoroutine(TransitionToEndingScene(ending, isBadEnding, delay));
    }

    // TimeManager.PauseGame()으로 Time.timeScale이 이미 0이라 WaitForSecondsRealtime을 쓴다.
    private IEnumerator TransitionToEndingScene(EndingType ending, bool isBadEnding, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        EndingHandoff.Ending = ending;
        if (isBadEnding) GameSceneManager.LoadEnding();
        else GameSceneManager.LoadExitEnding();
    }
}
