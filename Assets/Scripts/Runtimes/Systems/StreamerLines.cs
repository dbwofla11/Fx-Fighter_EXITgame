using UnityEngine;

// 스트리머 반응 단계(StreamerReactionState)별 말풍선 멘트 후보 3개씩. StreamerPanelUI가 반응 단계가
// 바뀔 때 하나를 랜덤으로 골라 SpeechBubbleUI.Show()에 넘긴다. 워딩은 참고 영상 톤 기준 초안이라
// 추후 다듬을 수 있다.
public static class StreamerLines
{
    private static readonly string[] Crash =
    {
        "허걱... 이거 진짜 폭락이에요?! 저 어떡해요...",
        "잠깐만요 이거 실화예요?? 다들 계좌 괜찮으세요...",
        "으아아 떨어져도 너무 떨어지는데요?! 심장 떨려요...",
    };

    private static readonly string[] Down =
    {
        "어... 좀 빠지네요. 다들 괜찮으세요?",
        "흐음, 오늘 분위기가 심상치 않은데요...",
        "아 살짝 흔들리네... 너무 걱정은 마세요 여러분.",
    };

    private static readonly string[] Neutral =
    {
        "오늘은 조용하네요, 차트 구경 좀 하시죠.",
        "음... 딱히 특별한 건 없는 하루네요.",
        "잔잔하네요 잔잔해. 이럴 때 커피나 한 잔 하시죠.",
    };

    private static readonly string[] Up =
    {
        "오오 올라가네요! 좋은데요 이거?",
        "분위기 좋은데요? 오늘 그린 좀 보이네!",
        "이야 이거 상승 각인데요? 기대되네요!",
    };

    private static readonly string[] Surge =
    {
        "미쳤다!!! 이거 실화예요?! 완전 떡상인데요!!!",
        "대박 대박!!! 여러분 이거 보고 계세요?! 로켓 발사각이에요!!!",
        "꺄아악!!! 이런 날이 오다니!!! 오늘 치킨 쏩니다!!!",
    };

    public static string GetRandom(StreamerReactionState state)
    {
        string[] lines = state switch
        {
            StreamerReactionState.Crash => Crash,
            StreamerReactionState.Down => Down,
            StreamerReactionState.Up => Up,
            StreamerReactionState.Surge => Surge,
            _ => Neutral,
        };
        return lines[Random.Range(0, lines.Length)];
    }
}
