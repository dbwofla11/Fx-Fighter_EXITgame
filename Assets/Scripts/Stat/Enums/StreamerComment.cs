using System;
using UnityEngine;

// 스트리머 카페 댓글창 1건의 기록. EventLogEntry와 동일한 역할(데이터 홀더)이며, StreamerPanelUI가
// 반응 단계 변화 시점에 쌓아두고 StreamerChatPanelUI가 그대로 그린다.
public class StreamerComment
{
    public string Nickname;
    public Color NicknameColor;
    public string Message;
    public DateTime Date;
}
