using UnityEngine;

// 스트리머 반응 단계(StreamerReactionState)별 시청자 댓글 후보. StreamerLines(말풍선 멘트)와 동일한 트리거
// 시점(StreamerPanelUI가 반응 단계 변화 감지 + 쿨타임 통과)에 GetRandom()으로 하나씩 뽑아 채팅창에 쌓는다.
// 코인 시세 자체가 아니라 "스트리머의 표정/리액션"에 대한 댓글 — 말풍선과 같은 순간에 뜨니 시청자가 방금 본
// 반응에 대해 코멘트하는 그림이 된다. 톤은 실시간 채팅 밈체가 아니라 카페 게시판 댓글 문장형.
public static class StreamerComments
{
    private static readonly string[] Nicknames =
    {
        "가즈아형", "존버는국룰", "코인초보", "무지성매수", "떡상기원",
        "설거지왕", "지갑거지", "청산당함ㅠ", "존버의신", "가나다코인",
        "코린이탈출", "물타기장인",
    };

    private static readonly Color[] NicknameColors =
    {
        new Color(1f, 0.42f, 0.42f), new Color(0.42f, 0.62f, 1f), new Color(0.45f, 0.85f, 0.45f),
        new Color(1f, 0.75f, 0.3f), new Color(0.78f, 0.5f, 1f), new Color(0.3f, 0.85f, 0.85f),
    };

    private static readonly string[] Crash =
    {
        "표정 왜저래ㅋㅋㅋ 완전 패닉", "목소리 떨리는거 봐 ㅠㅠ", "울기 직전 아니냐 저거", "저 얼굴 실화냐...", "완전 넋나감ㅋㅋ 안쓰럽다",
    };

    private static readonly string[] Down =
    {
        "표정 살짝 굳었는데?", "한숨 쉬는거 다들 봄?", "목소리 톤 내려간거 티남ㅋㅋ", "살짝 불안해 보이심", "저 표정 뭔가 쎄한데",
    };

    private static readonly string[] Neutral =
    {
        "표정 1도 안변함ㅋㅋ", "완전 무표정 그 자체", "평온함 그 자체네", "별 반응 없는거 보니 신경도 안쓰나봄", "저 무덤덤함 뭔가 웃김",
    };

    private static readonly string[] Up =
    {
        "표정 풀리는거 보임ㅎㅎ", "목소리 톤 올라감ㅋㅋ 신나셨네", "웃는거 봐 기분좋아보임", "저 텐션 좋다ㅋㅋ", "오 살아나셨다",
    };

    private static readonly string[] Surge =
    {
        "표정 폭발함ㅋㅋㅋㅋ", "방금 소리지르신거 다들 들음?", "저 텐션 실화냐ㅋㅋㅋ", "완전 날아가심 지금", "저렇게 좋아하는거 첨봄ㅋㅋ",
    };

    public static StreamerComment GetRandom(StreamerReactionState state)
    {
        string[] lines = state switch
        {
            StreamerReactionState.Crash => Crash,
            StreamerReactionState.Down => Down,
            StreamerReactionState.Up => Up,
            StreamerReactionState.Surge => Surge,
            _ => Neutral,
        };

        return new StreamerComment
        {
            Nickname = Nicknames[Random.Range(0, Nicknames.Length)],
            NicknameColor = NicknameColors[Random.Range(0, NicknameColors.Length)],
            Message = lines[Random.Range(0, lines.Length)],
            Date = TimeManager.Instance.CurrentGameDate,
        };
    }
}
