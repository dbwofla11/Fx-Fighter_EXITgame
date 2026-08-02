# 이슈 : 캐릭터 선택 씬 분리 작동 방식 / 호출 스택 정리

작성일 : 2026-08-02

관련 구현 : `Completed_Tasks.md` "캐릭터 선택 씬 분리" 항목 (`Next_Tesk.md` 구 5번, 이번 세션에 Figma 프레임
확인부터 구현·검증까지 진행). Figma `EjUw2LdqxAYhL2180OAXHo` node `1202:186`("초반 캐릭터 선택") 기반.

## 관련 파일

- [GameSceneManager.cs](../../Scripts/manager/utils/GameSceneManager.cs) — 신규. 씬 이름 문자열을 한 곳에 모으고
  `LoadCharacterSelect()`/`LoadMainGame()`만 노출하는 static 클래스.
- [JobSelectionHandoff.cs](../../Scripts/manager/JobSelectionHandoff.cs) — 신규. `public static JobSO
  SelectedJob` 필드 하나뿐인 static 클래스. 씬 전환 시 직업 정보를 들고 가는 다리 역할.
- [JobManager.cs](../../Scripts/manager/JobManager.cs) — `Start()` 추가(기존 파일).
- [CharacterSelectUI.cs](../../Scripts/UI/CharacterSelectUI.cs) — 신규. 캐릭터 선택 씬 UI 전담.
- [JobSO.cs](../../Scripts/SOs/JobSo.cs) / [EffectData.cs](../../Scripts/SOs/detailData/EffectData.cs) — 참고(직업
  데이터 구조, `EffectType` enum).
- [UIFormat.cs](../../Scripts/UI/Utils/UIFormat.cs) — 참고. `SignedEffectValue`/`SignedPercent`를 그대로 재사용해
  특성 텍스트("의심도 감소 -10%" 등)를 포맷함.
- `Assets/Scripts/Profile/직업_프로파일/*.asset` — 신규 6개(일반인/기업인/유튜버/개발자/연예인/정치인).
  수치 대부분 더미 — 아래 "알려진 이슈" 및 `Next_Tesk.md` 참고.
- `Assets/Scenes/CharacterSelectScene.unity` — 신규. `Build Settings` index 0.
- `Assets/Scenes/SampleScene.unity` — 내용 변경 없음, `Build Settings` index 1로 등록만 함.

## 작동 방식

**씬 순서** : `Build Settings`에 `CharacterSelectScene`(0) → `SampleScene`(1) 순으로 등록. 게임은 항상
캐릭터 선택 화면에서 시작해서 "다음으로"를 눌러야 메인 게임 씬으로 넘어간다.

**씬 전환 창구를 분리한 이유** : 사용자가 "씬 전환은 Build Settings 등록이 필요하다", "씬 관리 매니저를
따로 만들어서 관리하라"고 명시적으로 요청함. `EventHub`가 static인 것과 같은 이유로 `GameSceneManager`도
MonoBehaviour 싱글턴이 아니라 static으로 만들었다 — 씬 전환 자체는 상태가 없는 동작이라 `DontDestroyOnLoad`
오브젝트가 필요 없음.

**두 씬 사이에 선택한 직업을 넘기는 방법** : `Managers` 오브젝트(`TimeManager`/`JobManager`/`MarketManager`/
`PlayerManager`/`SkillManager`/`SettingsUI`가 전부 붙어있는 `SampleScene`의 오브젝트)를 캐릭터 선택 씬으로
통째로 옮기는 방법도 고려했지만, `SettingsUI`가 `SampleScene`의 `SettingsPanel`/`SettingsBtn` 같은 씬 전용
오브젝트를 직접 참조하고 `Start()`에서 그 참조로 바로 `SetActive(false)`를 호출한다. `Managers`를 캐릭터
선택 씬으로 옮기면 그 씬엔 `SettingsPanel`이 없어서 `Start()`가 즉시 `NullReferenceException`을 낸다.
대신 `JobSelectionHandoff`라는 정적 필드 하나로 필요한 정보(선택한 `JobSO` 참조)만 넘기는 훨씬 작은
방법을 택했다 — 정적 필드는 도메인 리로드 전까지 씬이 바뀌어도 값이 유지된다.

**`JobManager.SelectJob()`을 `Awake()`가 아니라 `Start()`에서 호출하는 이유** : `SelectJob()`은 내부에서
`MarketManager.Instance.CurrentStat`을 참조한다. 같은 `Managers` 오브젝트에 붙은 여러 컴포넌트의 `Awake()`
호출 순서는 보장되지 않으므로(`JobManager.Awake()`가 `MarketManager.Awake()`보다 먼저 실행될 수 있음),
모든 컴포넌트의 `Awake()`가 끝난 뒤 실행되는 `Start()`에 둬서 `MarketManager.Instance`가 반드시 준비된
상태를 보장한다.

## 호출 스택

### 1) 직업 슬롯 클릭 → 상세 패널 갱신

```
[JobSlot_* 클릭]
 └─ Button.onClick (Start()에서 인덱스 캡처한 람다로 연결)      [CharacterSelectUI.cs:38]
     └─ SelectJob(index)                                       [CharacterSelectUI.cs:45]
         ├─ slotImages[i].color = 선택된 슬롯만 selectedColor, 나머지 unselectedColor
         ├─ slotShadows[i].SetActive(!isSelected)               ← 선택된 슬롯만 그림자를 꺼서 "눌린" 상태로 보이게 함
         └─ RefreshDetail(jobs[index])                          [CharacterSelectUI.cs:60]
             ├─ nameText.text / descriptionText.text = job.jobName / job.description
             └─ traitRows 순회 (job.effects 개수만큼만 활성화)
                 ├─ traitIcons[i].sprite = EffectIcon(effect.effectType)   [CharacterSelectUI.cs:78]
                 └─ traitTexts[i].text = EffectLabel(...) + UIFormat.SignedPercent(UIFormat.SignedEffectValue(effect))
```

### 2) "다음으로" 클릭 → 씬 전환 → 메인 게임에 직업 반영

```
[NextButton 클릭]
 └─ ConfirmSelection()                                          [CharacterSelectUI.cs:95]
     ├─ JobSelectionHandoff.SelectedJob = jobs[selectedIndex]
     └─ GameSceneManager.LoadMainGame()                         [GameSceneManager.cs:11]
         └─ SceneManager.LoadScene("SampleScene")
             └─ (씬 로드 완료) SampleScene의 Managers 오브젝트 Awake 전부 끝난 뒤
                 └─ JobManager.Start()                           [JobManager.cs:27]
                     └─ JobSelectionHandoff.SelectedJob != null 이면
                         ├─ SelectJob(job)                       [JobManager.cs:50]
                         │   ├─ runtimeJobData.CurrentJob = job
                         │   └─ StatCalculator.ApplyJobSelection(MarketManager.Instance.CurrentStat, job)
                         └─ JobSelectionHandoff.SelectedJob = null   ← 한 번만 반영되도록 비움
```

## 색상 값 관련 메모 (Linear 컬러 스페이스)

프로젝트가 Linear 컬러 스페이스라, Figma 헥스코드를 `255분의 N`으로만 바꿔 `Image.color`에 넣으면(예:
`#333333` → 0.2,0.2,0.2) 화면에 감마 보정이 한 번 더 적용돼 실제로는 더 밝게 렌더링된다(예: 어두운 배경이
중간 회색으로 보임). sRGB→Linear 변환값으로 넣어서 화면 밝기를 Figma와 맞춰본 적이 있었으나, **사용자가
"그냥 색깔 코드 그대로 가져와서 반영"을 요청**해 최종적으로는 변환 없이 원본 sRGB 비율 그대로 쓰기로
확정했다. `CharacterSelectUI.selectedColor`/`unselectedColor` 기본값(1, 0.631, 0.404)/(1, 0.796, 0.616)도
이 원본 값이다 — 화면 밝기가 Figma 목업보다 살짝 밝게 보이는 건 알려진 상태이며 의도된 결과다.

## 그림자(버튼 입체 효과) 구현 방식

Figma 목업의 버튼/`DetailPanel`엔 `border-bottom` 10px짜리 진한 색 테두리가 있는데, 선택되지 않은
슬롯에는 있고 선택된 슬롯(기본값 `일반인`)엔 없다 — "눌리지 않은 버튼 vs 눌린 버튼"을 표현한 것이다.
box-shadow가 아니라 도형을 겹쳐서 재현했다.

- 각 버튼/패널마다 같은 너비 + 높이만 10 더 큰(top-left pivot이라 늘어난 만큼 아래로만 확장) `Shadow_*`
  오브젝트를 진한 색으로 만들고, `Transform.SetSiblingIndex()`로 앞면 오브젝트 바로 앞 순서로 옮겨서
  렌더링 순서(먼저 그려짐 = 뒤에 깔림)를 맞췄다. 앞면 도형이 위쪽만 정확히 덮어서 아래쪽 10만큼만 띠처럼
  보인다.
- 대상 : 직업 슬롯 6개(`Shadow_*`, 색 `#ff8235`) / `DetailPanel`(`Shadow_DetailPanel`, 색 `#c95f02`) /
  `NextButton`(`Shadow_NextButton`, 색 `#ff8235`). 빈 슬롯 2개(`JobSlot_Locked1/2`)는 Figma에도 이 테두리가
  없어서 그림자를 만들지 않았다.
- `CharacterSelectUI.slotShadows`(`GameObject[]`) 필드가 `SelectJob()`에서 선택된 슬롯의 그림자만
  `SetActive(false)`로 꺼서 Figma의 "선택=눌림" 표현을 그대로 재현한다.

## 알려진 이슈 / 주의점 후보

- 직업 6종 중 **일반인만 Figma에 구체 수치**가 있고(의심도 감소 -10% / 대중 이벤트 효과 +10%, 후자는
  대응하는 `EffectType`이 없어 `SupportIncrease`로 임시 매핑), **나머지 5개는 완전 더미**다. 실제 기획이
  나오면 `Assets/Scripts/Profile/직업_프로파일/*.asset` 전부 재조정 필요 — `Next_Tesk.md` 참고.
- 상세 패널의 "초기 자금 : ₩ 10,000,000" 텍스트는 `JobSO`와 연동된 값이 아니라 `PlayerManager.
  currentMoney`의 현재 고정값을 그대로 보여주는 정적 문구다. 직업별로 시작 자금을 다르게 주려면 `JobSO`에
  필드 추가 + `PlayerManager` 연동이 별도로 필요함.
- 이번 세션의 자동화(Unity MCP) 테스트 환경에서는 Editor 창이 포커스를 못 받아 Play 모드 프레임이 거의
  진행되지 않는 문제가 있었다(`Time.frameCount`가 여러 툴 호출 뒤에도 그대로 멈춰 있었음). 그래서
  `JobManager.Start()`의 자동 픽업이 실제로 실행되는 순간은 직접 관찰하지 못했고, 대신
  `JobManager.Instance.SelectJob(job)`을 수동으로 호출해 로직(효과 적용, `MarketManager.CurrentStat.Growth`
  변화)만 검증했다. `Start()` 호출 자체는 Unity의 기본 생명주기 보장이라 포커스가 있는 실제 플레이에서는
  다음 프레임에 정상 실행된다 — 이후 세션에서 실제 포커스 있는 Play로 한 번 더 확인해볼 가치는 있음.
- `JobSlot_Locked1`/`JobSlot_Locked2`(빈 회색 슬롯 2개)는 향후 직업 추가를 위한 자리 표시자일 뿐 클릭/기능
  전혀 없음.
