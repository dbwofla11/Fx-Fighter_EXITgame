using UnityEngine;
using UnityEngine.SceneManagement;

// 씬 전환 전담 창구. 씬 이름 문자열을 여기 한 곳에만 두고, UI 스크립트는 이 클래스를 통해서만
// 씬을 넘긴다 (EventHub가 Manager 호출을 한 곳에 모으는 것과 같은 이유).
// 안드로이드는 화면비를 직접 맞춘 전용 씬(Assets/Scenes/Android/, 이름 앞에 Android 접두사)을 쓴다 —
// PC 원본과 씬 이름만 다르고 GameObject 구조/로직은 동일해서, 여기서 접두사만 붙여 해석한다.
// 단, 빌드에 Android 접두사 씬이 없으면(PC 원본 씬만으로 빌드한 경우) baseName으로 폴백한다.
public static class GameSceneManager
{
    public const string TitleScene = "TitleScene";
    public const string CharacterSelectScene = "CharacterSelectScene";
    public const string OpeningScene = "OpeningScene";
    public const string MainScene = "SampleScene";
    public const string EndingScene = "EndingScene";
    public const string ExitEndingScene = "ExitEndingScene";

    public static void LoadCharacterSelect() => SceneManager.LoadScene(ResolveSceneName(CharacterSelectScene));
    public static void LoadOpening() => SceneManager.LoadScene(ResolveSceneName(OpeningScene));
    public static void LoadMainGame() => SceneManager.LoadScene(ResolveSceneName(MainScene));
    public static void LoadTitle() => SceneManager.LoadScene(ResolveSceneName(TitleScene));
    public static void LoadEnding() => SceneManager.LoadScene(ResolveSceneName(EndingScene));
    public static void LoadExitEnding() => SceneManager.LoadScene(ResolveSceneName(ExitEndingScene));

    private static string ResolveSceneName(string baseName)
    {
        if (Application.platform != RuntimePlatform.Android) return baseName;
        string androidName = "Android" + baseName;
        return SceneExistsInBuild(androidName) ? androidName : baseName;
    }

    // 빌드에 Android 전용 씬이 없으면(PC 원본 씬만으로 빌드한 경우) baseName으로 폴백.
    private static bool SceneExistsInBuild(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName) return true;
        }
        return false;
    }
}
