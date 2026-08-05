using UnityEngine.SceneManagement;

// 씬 전환 전담 창구. 씬 이름 문자열을 여기 한 곳에만 두고, UI 스크립트는 이 클래스를 통해서만
// 씬을 넘긴다 (EventHub가 Manager 호출을 한 곳에 모으는 것과 같은 이유).
public static class GameSceneManager
{
    public const string TitleScene = "TitleScene";
    public const string CharacterSelectScene = "CharacterSelectScene";
    public const string MainScene = "SampleScene";

    public static void LoadCharacterSelect() => SceneManager.LoadScene(CharacterSelectScene);
    public static void LoadMainGame() => SceneManager.LoadScene(MainScene);
}
