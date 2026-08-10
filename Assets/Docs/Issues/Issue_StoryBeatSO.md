# 이슈 : 대사/스토리 데이터 SO 리팩토링 작동 방식 정리

작성일 : 2026-08-11

관련 구현 : `Next_Tesk.md` 22번 항목("대사/스토리 데이터 SO 리팩토링"), `Completed_Tasks.md` 해당 항목 참고.
21번(오프닝 대화 UI, 직업 6종×6컷) 착수 전에 먼저 한 구조 정리 — 콘텐츠(대사 문구/배경)는 하나도 바꾸지
않고, 어디에 어떻게 저장되는지만 씬 인스턴스/하드코딩 문자열 → ScriptableObject 에셋으로 옮겼다.

## 관련 파일

- [JobSo.cs](../../Scripts/SOs/JobSo.cs) — 직업 프로필 SO. `openingBeats`(21번용, 현재 빈 리스트) 필드 추가.
- [StoryBeatSetSO.cs](../../Scripts/SOs/StoryBeatSetSO.cs) — 신규. `List<StoryBeat> beats`만 갖는 재사용
  가능한 SO 타입.
- [StoryDialogueController.cs](../../Scripts/UI/StoryScene/StoryDialogueController.cs) — `StoryBeat` 클래스
  정의도 여기 있음(`[Serializable]`, `background`+`line`). `beats` 필드를 `storySet`(SO 참조)로 교체.
  `Main_Canvas`(`ExitEndingScene`)에 부착.
- [EndingSceneUI.cs](../../Scripts/UI/StoryScene/EndingSceneUI.cs) — 체포/거지 엔딩 문구를 `arrestStory`/
  `delistingStory`(SO 참조)로 교체. `Main_Canvas`(`EndingScene`)에 부착.
- `Assets/Profile/엔딩_스토리/체포.asset`, `거지.asset`, `엑시트_영웅.asset` — 신규 SO 에셋 3개. 기존
  씬/코드에 있던 문구·배경 스프라이트를 그대로 이관.

**폴더 위치 주의** : 위 3개 스크립트가 있던 `Assets/Scripts/UI/EndingScene/` 폴더는 이 작업 도중 사용자가
Unity Editor에서 `StoryScene`으로 직접 리네임했다(엔딩 전용이 아니라 21번 오프닝 대사도 같이 쓸 폴더라는
의미로 보임). Editor 네이티브 리네임이라 `.meta`의 GUID는 전부 그대로 보존됐고(`git show`로 대조 확인),
스크립트 참조가 깨지지 않았다 — 앞으로 이 폴더를 코드에서 언급할 땐 `StoryScene` 경로를 쓴다.

## 작동 방식

**왜 SO로 뺐는가** : 기존엔 두 갈래 다 재사용 불가능한 구조였다.
- `EndingSceneUI`(체포/거지) : `Start()`에 문자열 리터럴 2개가 그대로 박혀 있었다.
- `ExitEndingSceneUI`+`StoryDialogueController`(엑시트/영웅) : `StoryBeat`가 SO가 아니라 그냥
  `[Serializable]` 클래스라, `StoryDialogueController.beats`(`List<StoryBeat>`)에 씬 컴포넌트 Inspector로
  직접 값을 채우는 방식 — 데이터가 씬 `.unity` 파일에 그대로 직렬화됐다.

21번(오프닝, 직업 6종×6컷 = 36비트)이 같은 패턴을 그대로 쓰면 씬 오브젝트를 직업 수만큼 늘려야 해서
관리가 안 되는 게 확실했다 → SO로 빼서 씬과 분리했다.

**타입을 나눈 기준** : 오프닝 비트(`JobSO.openingBeats`)는 새 SO 타입을 만들지 않았다 — 직업 선택이 이미
`JobSO` 인스턴스 하나를 고르는 구조라(`JobManager.CurrentJob`), 오프닝 데이터도 "선택된 직업의 프로필
데이터"로 자연스럽게 들어간다(`SkillSO`/`EventSO`처럼 "직업별 프로필에 여러 필드" 붙이는 기존 관례와 동일).
반면 엔딩 대사(체포/거지/엑시트/영웅)는 직업과 무관하게 독립적인 데이터라 `JobSO`에 넣을 자리가 없어서,
`List<StoryBeat>` 하나만 감싸는 최소 SO 타입 `StoryBeatSetSO`를 새로 만들어 엔딩 4종이 각자 에셋 하나씩
갖게 했다.

**`EndingSceneUI`가 `StoryBeat.background`를 안 쓰는 이유** : `EndingSceneUI`는 배경 스프라이트를 이미
`arrestBgSprite`/`delistingBgSprite`라는 별도 필드로 갖고 있었다(이건 처음부터 하드코딩이 아니라 Inspector
데이터였음 — 이번 리팩토링 대상은 "문구"뿐이었다). 그래서 `arrestStory`/`delistingStory` 에셋은 1비트짜리
`beats` 리스트를 갖지만 그 비트의 `background` 필드는 비워뒀다 — `line`만 읽는다(`EndingSceneUI.cs:49`).
새 SO 타입을 하나 더 만드는 대신 `StoryBeatSetSO`를 재사용하는 쪽을 택한 트레이드오프라, 이 두 에셋만
`background`가 항상 비어있다는 점을 기억해둘 것.

**엑시트/영웅 엔딩은 여전히 공용 대사** : `ExitEndingSceneUI`는 `EndingHandoff.Ending`이 Hero인지 Exit인지
와 무관하게 `storyController`(같은 `StoryBeatSetSO`)를 그대로 재생한다 — 이건 리팩토링 이전부터 있던
동작이고 이번에 바꾸지 않았다(`Issue_ExitEndingStoryStats.md` "알려진 이슈" 참고, 나중에 갈라치기하고
싶으면 `EndingHandoff.Ending` 분기로 다른 `StoryBeatSetSO`를 골라 넣으면 됨).

## 호출 스택

### 체포/거지 엔딩 (EndingScene)

```
EndingSceneUI.Start()                                    [EndingSceneUI.cs:38]
 └─ ending == Arrest ? arrestStory : delistingStory       (SerializeField, Inspector에서 연결)
     └─ endingText.text = story.beats[0].line             [:49]
```

### 엑시트/영웅 엔딩 (ExitEndingScene)

```
ExitEndingSceneUI.Start()
 └─ storyController.Begin()                               [StoryDialogueController.cs:38]
     └─ ShowNext()                                         [:55]
         ├─ List<StoryBeat> beats = storySet.beats         (SerializeField, Inspector에서 연결)
         ├─ bgImage.sprite = beats[index].background
         └─ typewriter.Play(beats[index].line)
```

`storySet`을 안 채우면(Inspector 연결 누락) `ShowNext()`의 `storySet.beats`에서 즉시
`NullReferenceException` — 이전엔 `beats` 리스트 자체가 비어있으면 그냥 즉시 `OnSequenceComplete`가
발동했던 것과 실패 모드가 다르다(에셋 참조 자체가 비어야만 터짐, 리스트가 빈 채로 있는 정상적인 경우는
없음).

## Unity 측 데이터 이관 (Unity Editor MCP로 처리)

기존 씬에 박혀있던 값을 코드 리팩토링과 별개로 그대로 옮겨야 게임이 안 깨져서, `mcp__unity-editor-mcp__*`
툴로 직접 진행했다.

1. `create_asset`으로 SO 3개 생성(`Profile/엔딩_스토리/체포.asset`, `거지.asset`, `엑시트_영웅.asset`).
2. `set_serialized_field`로 `beats.Array.size`/`.line`/`.background`(guid+fileId ObjectRef) 채움 —
   엑시트_영웅 에셋의 배경 스프라이트 2장은 씬에 있던 값을 `grep`으로 먼저 읽어 GUID/fileID를 그대로
   복사했다(`Assets/Sprites/엑시트이미지1.png`, `엑시트이미지2.png`).
3. `find_gameobjects`로 두 씬의 `Main_Canvas`(각각 `StoryDialogueController`/`EndingSceneUI` 부착 대상)를
   찾고, `set_serialized_field`로 `storySet`/`arrestStory`/`delistingStory`를 새 에셋에 연결.
4. `save_scene` ×2, `save_all`.

Play 모드 실동작(체포/거지/엑시트/영웅 4종 엔딩 전부 화면에 문구가 정상 표시되는지)은 아직 확인 안 함 —
컴파일 에러 없음 + `get_serialized_fields`로 참조가 정확히 연결됐다는 것까지만 검증했다.

## 알려진 이슈 / 주의점 후보

- 21번 착수 시 `StoryDialogueController`가 `storySet`(SO 참조) 대신 `JobManager.CurrentJob.openingBeats`
  (List 직접)를 읽는 경로가 추가로 필요하다 — 지금 구조는 SO 에셋 하나를 통째로 참조하는 형태라, 오프닝은
  "선택된 직업에 따라 다른 리스트"라는 점에서 약간 다른 배선이 필요함(직접 `List<StoryBeat>`를 넘기거나,
  `StoryDialogueController`에 `SetBeats(List<StoryBeat>)` 같은 오버로드를 추가하는 식 — 21번에서 실제
  UI 붙일 때 결정).
- `Assets/Profile/엔딩_스토리/체포.asset`/`거지.asset`은 `background` 필드가 항상 비어있는 게 정상이다 —
  나중에 실수로 채워도 `EndingSceneUI`는 그 값을 안 읽는다(위 "작동 방식" 참고).
- 엑시트/영웅 엔딩이 여전히 같은 대사를 공유하는 것도(리팩토링 이전부터 있던 설계) 이번에 안 건드렸다.
