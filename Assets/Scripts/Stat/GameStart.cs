using UnityEngine;

public class GameStarter : MonoBehaviour
{
    [Header("시작할 때 틀 배경음악")]
    public AudioClip mainBgmClip; // 유니티 인스펙터에서 오디오 파일을 넣을 빈칸

    void Start()
    {
        // 게임이 시작되자마자(Start) 오디오 매니저에게 BGM 재생을 명령!
        if (AudioManager.Instance != null && mainBgmClip != null)
        {
            AudioManager.Instance.PlayBGM(mainBgmClip);
        }
    }
}