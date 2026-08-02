# 이슈 : 스킬 아이콘/구매 버튼 UI 작동 방식

작성일 : 2026-08-03

관련 구현 : `Completed_Tasks.md` "스킬 아이콘/구매 버튼 UI" 항목. 처음엔 Figma MCP가 Starter 플랜 월 호출
한도(6회, 매월 결제일 기준 초기화)에 걸려 "스킬구상도 - 디테일1" 프레임을 못 읽어서 `MintModal`을 복제한
예시 버전으로 시작했다. 이후 사용자가 실제 Figma 스크린샷을 줘서, 탭 4개 + 확대된 선택 아이콘 미리보기
박스 구조로 다시 잡았다 — 지금 구현은 이 두 번째(정식 목업 기준) 버전이다.

## 관련 파일

- [SkillPanelUI.cs](../../Scripts/UI/SkillModal/SkillPanelUI.cs) — 신규.
- `Assets/Scenes/SampleScene.unity` — `Main_Canvas` 밑에 `SkillPanel` 신규 배치. 기존 `SkillBtn`의 죽은
  `EventLogButton` 컴포넌트(참조 미할당 상태로 방치돼 있던 것) 제거 후 `SkillBtn.onClick`에
  `SkillPanelUI.Toggle`을 Persistent Listener로 연결. 씬 구조는 아래 "씬 구조" 참고.

## 작동 방식

- `SkillPanel`은 `EventLogPanel`/`MintModal`과 동일하게 기본 비활성 상태로 시작하고, `Open()`/`Close()`가
  자기 자신을 `SetActive`한다. `Open()`은 항상 첫 번째 탭(`SkillCategory.CoinDesign`)으로 초기화한다.
- 탭 4개(`SkillCategory` enum과 1:1 대응 : `코인 설계`=CoinDesign/`시장 조작`=Marketing/`여론 조작`=Propaganda/
  `방어 및 엑시트`=Depence_Exit) 클릭 → `SelectTab(category)`가 탭 색상(`EventLogPanelUI`와 동일한
  #FF8686/#FFDBDB 선택/비선택 색)과, 해당 카테고리에 속한 아이콘만 보이도록 `Icon1~6`의 `SetActive`를 갱신.
  **현재 `SkillSO` 6개 전부 `category = CoinDesign`이라, 나머지 3개 탭은 지금 당장 빈 화면이다** — 데이터
  이슈라 UI 문제는 아님(아래 "알려진 차이" 참고).
- 아이콘 6개(`SkillID` 전부, `SkillManager.skillDatabase` 순서가 아니라 씬에 고정 배치된 `Icon1~6` 각각에
  `SkillID`를 수동 매핑)는 `Start()`에서 `SkillManager.GetSkillProfile(id).icon`을 읽어 버튼 이미지를 채운다.
- 아이콘 클릭 → `EventHub.RaiseSkillClicked(id)` → `SkillManager`가 `SelectedSkillId` 갱신 → `EventHub.OnSkillClicked`
  구독으로 `RefreshDetail()`이 이름/설명/효과/비용 텍스트(우측 `DetailBox`)를 채우고 구매 버튼을 보여준다.
  동시에 `RefreshSelectionHighlight()`가 선택된 아이콘만 연한 빨강으로 tint하고, `PreviewFrame`(좌상단 확대
  박스)의 스프라이트를 선택된 스킬 아이콘으로 바꾼다.
- 구매 버튼 → `EventHub.RaiseSkillPurchased()` → `SkillManager.HandlePurchase()`가 비용 차감 + 효과 적용.

## 씬 구조

`Main_Canvas/SkillPanel`(기본 비활성) 밑에:

- `Tab1~4` : `EventLogPanel/OverviewTab`을 복제한 탭 버튼(같은 색상 컨벤션 재사용).
- `SkillPanelBox`("코인 설계" 등 아이콘이 나열되는 큰 회색 박스) : `Icon1~6`(각각 `SkillID` 하나에 매핑) +
  `PreviewFrame`(선택된 스킬을 크게 보여주는 미리보기 박스, `Icon1`을 복제해 크기만 키움 — 클릭 불가
  (`Button.interactable = false`)).
- `DetailBox`(우측 정보 박스) : `NameText`/`DescriptionText`/`PurchaseBtn`.
- `CloseBtn`(우상단 X) : `EventNotification/Panel/CloseBtn`을 복제해 재사용(같은 취소버튼 스프라이트/틴트).

## 버그 수정 이력 (2026-08-03, 사용자가 Play 테스트로 발견)

- **닫은 뒤 재오픈이 안 되던 버그** : `SkillPanel`이 기본 비활성 상태라 `Start()`가 첫 `Open()` 전까지 실행되지
  않는다 — 그래서 `SkillBtn`(항상 활성) 클릭에 `Toggle()`을 연결하려고 씬에 Persistent Listener로 미리
  걸어뒀는데, `Start()` 안에 있던 `toggleBtn.onClick.AddListener(Toggle)` 코드를 지우는 걸 깜빡했다. 그 결과
  처음 열릴 때 `Start()`가 실행되면서 같은 버튼에 리스너가 **두 개** 걸렸고, 두 번째 클릭부터는 한 번의
  클릭 안에서 열림→닫힘이 상쇄돼 아무 반응이 없었다. `Start()`의 중복 `AddListener` 줄을 제거해 해결 —
  `EventNotificationUI`/`EventLogButton`이 쓰는 "토글 대상은 항상 활성인 오브젝트에, 패널 자신의 Start()에
  의존하지 않게" 관례를 따르지 않아 생긴 버그였다.
- **"이미지가 안 바뀐다"** : 아이콘 스프라이트 자체는 6개 전부 정상 매핑돼 있었다(코드로 확인). 실제로는
  스킬을 바꿔 클릭해도 화면상 아무 것도 달라지지 않아 보인 것 — 선택된 스킬을 시각적으로 표시하는 요소가
  전혀 없었기 때문. 사용자가 실제 Figma 스크린샷을 공유해줘서, 탭+확대 미리보기 박스 구조로 다시 잡으며
  근본적으로 해결(`PreviewFrame`/`RefreshSelectionHighlight`, 위 "작동 방식" 참고).
- **(재작업 중 자체 발견) `DetailBox` 중복 자식 오브젝트** : `DetailBox`를 `SkillPanelBox`를 복제해서 만들었는데,
  그 시점의 `SkillPanelBox`에 이미 `Icon1~6`/`NameText`/`DescriptionText`/`PurchaseBtn`/`CloseBtn` 등이 자식으로
  붙어 있어서 전부 같이 복제돼버렸다(당시엔 알아채지 못함). 그 뒤 원본 `NameText`/`DescriptionText`/
  `PurchaseBtn`을 `DetailBox`로 재부모지정하면서, 복제로 생긴 동명의 가짜 사본들과 뒤섞여 화면 좌하단에
  깨진 패널로 나타났다 — Play 모드 스크린샷으로 발견해 가짜 사본 12개를 전부 삭제해 해결.
- **스킬 아이콘(`SkillBtn`) on/off 스프라이트 안 바뀜** : `SkillBtn`을 눌러 패널을 열어도 아이콘이
  `UI스킬아이콘_on`으로 안 바뀌던 문제. 처음 만들 때 `SkillBtn`에 죽어있던 `EventLogButton`(참조 미할당)을
  지우고 `SkillPanelUI.Toggle`을 Persistent Listener로 직접 건 게 원인 — on/off 스프라이트를 갱신하는 로직
  자체가 아예 없었다. `EventLogButton`과 동일한 패턴으로 신규 [SkillPanelButton.cs](../../Scripts/UI/Utils/SkillPanelButton.cs)를
  만들어 `SkillBtn`에 붙이고(`onSprite=UI스킬아이콘_on`, `offSprite=UI스킬아이콘13`, `skillPanel=SkillPanelUI`),
  기존 Persistent Listener는 제거했다 — 이제 클릭 연결과 아이콘 갱신을 이 스크립트 하나가 전담한다
  (`SkillPanelUI.toggleBtn` 필드도 더 이상 안 써서 제거).

## 알려진 차이 (정식 Figma 목업 대비, 남은 것)

- 우측 정보 박스 스타일(픽셀 폰트, 어두운 배경)이 목업과 다르다 — 이 프로젝트 전반의 폰트/색상 컨벤션과
  얼마나 맞출지는 그대로 유지.
- 정식 목업은 선택된 아이콘을 작은 그리드에서 제거하고 확대 박스에만 보여주는 것처럼 보이는데, 지금은
  작은 그리드에도 계속 남겨두고(tint만 표시) 확대 박스에 추가로 보여주는 방식으로 단순화했다 — 그리드에서
  제거/재배치하는 로직은 상태 관리가 더 복잡해져서 일부러 뺐다.
- `SkillCategory` 데이터가 6개 전부 `CoinDesign`이라 나머지 3개 탭은 빈 화면이다(UI 문제 아님, 스킬 데이터
  기획 이슈 — `Next_Tesk.md` "발행량 관련 스킬 3종" 항목과 연결해서 볼 것).
