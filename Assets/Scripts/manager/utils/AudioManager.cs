using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // 싱글톤 패턴: 어디서든 AudioManager.Instance 로 접근 가능
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSourceA; // BGM 재생기 1
    public AudioSource bgmSourceB; // BGM 재생기 2
    public AudioSource sfxSource;  // 효과음 재생기

    [Header("Settings")]
    public float crossFadeDuration = 1.0f; // 크로스페이드에 걸리는 시간 (1초)

// 유니티 인스펙터에서 슬라이더로 볼륨 조절 가능 (0.0 ~ 1.0)
    [Range(0f, 1f)] public float bgmVolume = 0.3f;
    [Range(0f, 1f)] public float sfxVolume = 0.5f;
    private bool isPlayingSourceA = true; // 현재 A 재생기를 쓰고 있는지 여부

    private const string BgmVolumeKey = "BgmVolume";
    private const string SfxVolumeKey = "SfxVolume";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 넘어가도 배경음악이 끊기지 않게 유지

            // 저장된 볼륨 설정 복원 (없으면 인스펙터 기본값 유지)
            bgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, bgmVolume);
            sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, sfxVolume);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 설정 UI 슬라이더에서 호출: 현재 재생 중인 BGM 볼륨도 즉시 반영
    public void SetBgmVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        AudioSource activeSource = isPlayingSourceA ? bgmSourceA : bgmSourceB;
        if (activeSource != null) activeSource.volume = bgmVolume;
        PlayerPrefs.SetFloat(BgmVolumeKey, bgmVolume);
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
    }

// BGM 재생 (크로스페이드 적용)
    public void PlayBGM(AudioClip newClip)
    {
        if (newClip == null) return;

        // 현재 재생 중인 소스와, 다음으로 재생할 소스를 정함
        AudioSource activeSource = isPlayingSourceA ? bgmSourceA : bgmSourceB;
        AudioSource nextSource = isPlayingSourceA ? bgmSourceB : bgmSourceA;

        // 다음 재생기에 새 음악 세팅 및 재생 (볼륨은 0으로 시작)
        nextSource.clip = newClip;
        nextSource.volume = 0f;
        nextSource.Play();

        // 크로스페이드 코루틴 실행
        StartCoroutine(CrossFadeCoroutine(activeSource, nextSource, crossFadeDuration));
        
        // 사용 중인 재생기 스위치
        isPlayingSourceA = !isPlayingSourceA;
    }

    // 크로스페이드 타이머 로직
    private IEnumerator CrossFadeCoroutine(AudioSource activeSource, AudioSource nextSource, float duration)
    {
        float time = 0;
        float targetVolume = bgmVolume; // ▼ 1.0이 아니라 우리가 설정한 볼륨을 목표로 함 ▼

        while (time < duration)
        {
            time += Time.deltaTime;
            // 서서히 볼륨 줄이고 / 키우기
            activeSource.volume = Mathf.Lerp(targetVolume, 0f, time / duration);
            nextSource.volume = Mathf.Lerp(0f, targetVolume, time / duration);
            yield return null;
        }

        // 완벽하게 마무리
        activeSource.volume = 0f;
        activeSource.Stop();
        nextSource.volume = targetVolume;
    }
    // 브금을 완전히 멈춘다 (타이틀로 돌아갈 때 메인 게임 브금이 계속 들리는 문제 방지용)
    public void StopBGM()
    {
        StopAllCoroutines(); // 진행 중이던 크로스페이드가 있으면 취소
        bgmSourceA.Stop();
        bgmSourceB.Stop();
        bgmSourceA.volume = 0f;
        bgmSourceB.volume = 0f;
    }

    // SFX (효과음) 재생
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
        {
            // PlayOneShot을 쓰면 소리가 겹쳐도 끊기지 않고 덧입혀져서 재생됨
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }
}