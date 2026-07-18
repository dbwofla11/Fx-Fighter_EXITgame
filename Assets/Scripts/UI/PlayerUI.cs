using UnityEngine;
using TMPro; // TextMeshPro 사용

public class PlayerUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI coinText;

    private void Update()
    {
        // PlayerManager가 존재할 때만 화면에 텍스트 업데이트
        if (PlayerManager.Instance != null)
        {
            // "N0"은 천 단위마다 콤마(,)를 찍어주는 포맷입니다.
            moneyText.text = "보유 현금: ₩ " + PlayerManager.Instance.currentMoney.ToString("N0");
            coinText.text = "보유 코인: " + PlayerManager.Instance.currentCoins.ToString("N0") + " 개";
        }
    }
}