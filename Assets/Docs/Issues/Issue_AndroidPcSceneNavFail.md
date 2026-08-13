# 이슈 : PC 원본 씬으로 안드로이드 빌드 시 타이틀→직업선택 전환 안 됨 (미해결)

작성일 : 2026-08-14

## 증상

`Assets/Scenes/*.unity`(PC 원본, `Android` 접두사 없음) 5개로 Android Build Profile 씬 리스트를 교체하고
빌드(`Builds/Android/summer_gammaru_pcscenes.apk`, 빌드 성공/에러 0)한 뒤 실기기에 설치해 확인 — 타이틀
화면에서 직업선택 화면으로 전환이 안 됨.

## 원인 (확정, 재현 전 이미 예상했던 것과 일치)

`Assets/Scripts/manager/utils/GameSceneManager.cs:22-23`

```csharp
private static string ResolveSceneName(string baseName) =>
    Application.platform == RuntimePlatform.Android ? "Android" + baseName : baseName;
```

`Application.platform == RuntimePlatform.Android`이면 씬 이름 앞에 무조건 `"Android"`를 붙여서
`SceneManager.LoadScene()`을 호출한다. 이번 빌드는 씬 리스트에 `Android` 접두사 씬이 하나도 없으므로,
안드로이드 기기에서 `LoadCharacterSelect()` 등을 호출하면 존재하지 않는 씬 이름(`AndroidCharacterSelectScene`
등)을 찾다가 실패 → 전환이 일어나지 않음.

이전에 에디터 테스트 중 확인했던 것과 동일한 근본 원인([[project_windows_scene_transition_bug]] 계열,
`Player.log` 확인해서 재확인 가능).

## 상태

**미해결 — 이번엔 기록만.** PC 씬 기반 안드로이드 빌드는 해상도/레이아웃 확인용 1회성 실험이었고, 실제
씬 전환까지 고치는 작업(Android 접두사 로직을 어떻게 할지 포함)은 다음 세션 씬 작업 때 진행 예정.
