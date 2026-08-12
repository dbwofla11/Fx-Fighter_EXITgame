using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 스트리머 카페 댓글 1건의 뷰. EventCardView와 동일한 관례(템플릿 Instantiate로 채워지는 순수 표시 컴포넌트).
public class ChatMessageView : MonoBehaviour
{
    public Image background;
    public TextMeshProUGUI contentText;
    public TextMeshProUGUI dateText;

    public void Populate(string nickname, Color nicknameColor, string message, string date)
    {
        if (contentText != null)
            contentText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(nicknameColor)}>{nickname}</color> {message}";
        if (dateText != null) dateText.text = date;
    }
}
