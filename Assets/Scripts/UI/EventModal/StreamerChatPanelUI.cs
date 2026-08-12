using System.Collections.Generic;
using UnityEngine;

// 커뮤니티 탭 우측 "스트리머 카페 댓글창". EventLogPanelUI.ShowCommunity()가 열릴 때마다 StreamerPanelUI가
// 쌓아둔 댓글(StreamerPanelUI.ChatLog)을 그대로 그린다 — EventLogPanelUI 카드 목록(RefreshLog)과
// 동일한 방식으로 템플릿을 Instantiate.
public class StreamerChatPanelUI : MonoBehaviour
{
    [SerializeField] private RectTransform listParent;
    [SerializeField] private ChatMessageView template;

    private void Awake()
    {
        if (template != null) template.gameObject.SetActive(false);
    }

    public void SetMessages(IReadOnlyList<StreamerComment> comments)
    {
        if (listParent == null || template == null) return;

        foreach (Transform child in listParent)
            if (child != template.transform)
                Destroy(child.gameObject);

        foreach (StreamerComment comment in comments)
        {
            ChatMessageView row = Instantiate(template, listParent);
            row.gameObject.SetActive(true);
            row.Populate(comment.Nickname, comment.NicknameColor, comment.Message, UIFormat.DateDot(comment.Date));
        }
    }
}
