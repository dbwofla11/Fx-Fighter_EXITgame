# 이슈 : 스킬 패널 — 설명 색상 텍스트 + 아이콘 이름/구매횟수 라벨

작성일 : 2026-08-08

## 바뀐 부분

- [EventEffectFormatter.cs](../Scripts/UI/Utils/EventEffectFormatter.cs) `BuildEffectsText()` —
  `colorize=true`일 때(스킬 패널 전용) 스탯 텍스트 색을 파스텔(`#B1FFB1`/`#FFBAB1`) → 진한 초록/빨강
  (`#009900`/`#CC0000`)으로 변경 + `<b>` 굵게 추가. 이벤트 로그 카드·아이콘 테두리가 공유하는
  `PositiveColor`/`NegativeColor` 상수는 그대로 둠(범위 밖).
- [SkillPanelUI.cs](../Scripts/UI/SkillModal/SkillPanelUI.cs) `IconSlot.label`(TMP) 필드 추가,
  `RefreshSelectionHighlight()`에서 갱신 — 아이콘 밑에 재사용형은 "이름 (N회)", 1회성은 "이름"만 표시
  (1회성은 최대 1회라 횟수 생략).
- `Assets/Scenes/SampleScene.unity` — 아이콘 44개 밑에 `Label`(TextMeshProUGUI) 자식 44개 생성 +
  `IconSlot.label` 와이어링(Unity MCP). 처음엔 이름+횟수 2줄로 넣었다가 서브카테고리 그룹 라벨과 겹쳐서,
  1줄(높이 20, 오토사이징 6~16)로 축소.
