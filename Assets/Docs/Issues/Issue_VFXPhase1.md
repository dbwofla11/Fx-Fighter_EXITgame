# 이슈 : VFX/SFX 1차 작업 (파티클/화면 흔들림/경고 테두리/붕괴 연출)

작성일 : 2026-08-04

관련 구현 : `Assets/Scripts/UI/Vfx/UIBurstParticle.cs`(신규), `Assets/Scripts/manager/utils/ScreenShaker.cs`(신규),
`Assets/Scripts/UI/MainModal/ScreenWarningBorderUI.cs`(신규), `Assets/Scripts/UI/Vfx/HoverIdleBob.cs`(신규),
`Assets/Scripts/UI/MainModal/PriceChartUI.cs`, `Assets/Scripts/UI/FeatherModal/TradeModalUI.cs`,
`Assets/Scripts/UI/MainModal/StatGaugeUI.cs`, `Assets/Scripts/UI/MainModal/StreamerPanelUI.cs`,
`Assets/Scripts/UI/MainModal/PlayerUI.cs`, `Assets/Scripts/UI/MainModal/EndingResultUI.cs`,
`Assets/Scripts/UI/SkillModal/SkillPanelUI.cs`, `Assets/Scripts/manager/SkillManager.cs`,
`Assets/Scripts/manager/utils/EventHub.cs`

## 배경

Notion "피드백 정리" 페이지 "2-1. VFX 어떻게 할 것인가?" / "2-2. 스킬 UI의 VFX & SFX" 섹션 요구사항 구현.
이동평균선은 문서에 "VFX/SOUND 다 끝나고 나서" 우선순위 낮음으로 명시돼 있어 이번 범위에서 제외.
프로젝트에 파티클 프리팹/에셋이 전혀 없어서(패키지 샘플 제외) 전부 uGUI Image를 코드로 생성하는
방식으로 구현했다 — 새 에셋 없이 기존 색상 팔레트(BtnLong/BtnShort)만 재사용.

## 구현 내역

1. **캔들 파티클** (`PriceChartUI.cs`) : 스트리머 반응이 급등(Surge)/급락(Crash)일 때만 최신 캔들 종가
   위치에 버스트. 변동폭(`|Close-Open|/Open`)에 비례한 크기.
2. **거래 버튼 반응** (`TradeModalUI.cs`, `HoverIdleBob.cs`) : 확정 시 거래액수 비율만큼 버스트.
   롱/숏 버튼(`PlayerUI.btnLong/btnShort`)에 마우스 호버 시 공중부양 idle 애니메이션.
3. **화면 흔들림** (`ScreenShaker.cs`) : Screen Space - Overlay 캔버스라 카메라 흔들기는 무의미해서
   (`Canvas.m_Camera`가 비어있음) `Main_Canvas` 자신의 RectTransform을 흔드는 방식. `UIBurstParticle.Spawn`
   내부에서 파티클이 생성될 때마다 그 크기(intensity)에 비례해 자동으로 트리거됨(중앙 집중형 — 트레이드/
   캔들/스킬구매 3곳 다 여기 하나만 거쳐감).
4. **화면 경고 테두리** (`ScreenWarningBorderUI.cs`) : 가격이 하한선(`PriceCalculator.MinPrice`)에
   가까워지거나 Doubt가 100에 근접하면 화면 4변이 빨갛게, 위험할수록 펄스.
5. **의심도 게이지 흔들림 + 체포 시 붕괴** (`StatGaugeUI.cs`) : Doubt가 마지막 흔들림 시점보다 일정 수치
   이상 오를 때마다 3개 패널(Support/Growth/Doubt) 전부 흔들림. 체포 엔딩 확정 시 패널이 무너지듯
   떨어지며 페이드아웃. `EndingResultUI`는 이 붕괴 시간(`StatGaugeUI.ArrestCollapseDuration`)만큼
   결과 패널 표시를 늦춰서 붕괴 연출이 먼저 보이게 함(별도 엔딩 씬 없이 기존 오버레이 패널 유지).
6. **캔들 붕괴 연출** (`PriceChartUI.cs`) : 체포 엔딩 시 차트 패널 전체가 아니라 보이는 캔들 하나하나가
   인덱스만큼 시차를 두고 개별적으로 위로 살짝 튀었다가(hop) 좌우로 랜덤 흩어지며(drift) 떨어짐.
7. **스킬 구매 이펙트/사운드** (`SkillPanelUI.cs`, `SkillManager.cs`) : 구매 버튼에 버스트 + `AudioManager`로
   사운드. 기존 `EventHub.OnSkillPurchased`는 돈이 부족해도 그냥 호출돼서 성공 신호로 못 씀 — 실제
   지불 성공 시에만 발행하는 `EventHub.OnSkillPurchaseSucceeded`를 새로 추가함.

## 세션 중 발견/수정한 버그

- **캔들 파티클 위치가 엉뚱한 곳에 생김** : `chartArea`를 부모로 쓰면서 `(0,0)` 앵커 기준 좌표를
  `(0.5,0.5)` 앵커로 생성되는 버스트 루트에 그대로 넘겨서 앵커 공간이 어긋났음 — 캔들 자신을 부모로
  삼아 중심 기준 오프셋으로 배치하도록 수정.
- **거래 확정 버튼에 안 지워진 파티클 조각이 남음** : `OnConfirmClicked()`가 파티클을 띄운 직후 곧바로
  패널을 비활성화해서 0.5초짜리 버스트 코루틴이 `Destroy()` 전에 얼어붙음 — `UIBurstParticle.Spawn`이
  루트를 반환하도록 바꾸고, 스폰 직후 `transform.root`(안 꺼지는 조상)로 재부모잉하도록 수정.
  `SkillPanelUI`에도 동일하게 방어적으로 적용.
- **의심도 게이지가 거의 안 흔들림** : 처음엔 "Doubt가 50 이상 오르면" 트리거였는데, Play 모드 150턴
  시뮬레이션 결과 Doubt가 총 15밖에 안 올라(원래 상승이 느림) 사실상 평생 안 흔들리는 값이었음 —
  10으로 낮춤.
- **캔들 파티클/화면 흔들림이 롱숏 버튼을 눌러야만 작동하는 것처럼 보임** : 실제로는 버그가 아니었음.
  `TimeManager`가 1배속 기준 하루(=1턴) 진행에 실제 시간 6분 이상을 쓰는 구조라, 짧은 테스트 세션
  동안은 자동 턴이 거의 안 지나가 급등/급락이 우연히 관측 안 된 것. 300턴 직접 시뮬레이션으로 버튼
  클릭과 무관하게 정상 작동함을 확인함(게임 페이스 자체는 이번 이슈 범위 밖이라 손대지 않음).
- **급락 시엔 파티클이 안 생김** : 트리거 조건을 스트리머 반응 "Up/Surge"로 잘못 짐작했었음 —
  "Surge/Crash"(급등/급락 양쪽 극단)로 정정.
- **화면 흔들림이 거래할 때만 발생하고 파티클 크기와 무관** : `ScreenShaker`가 `EventHub.OnBuyCoin/
  OnSellCoin`을 직접 구독하는 별도 트리거였음 — 이 구독을 없애고 `UIBurstParticle.Spawn` 안에서
  intensity 비례로 흔들도록 통합(위 "화면 흔들림" 항목 참고).
- **체포 시 차트 패널 전체가 한 덩어리로 떨어짐** : 캔들 하나하나가 개별적으로 떨어지도록 재설계(위
  "캔들 붕괴 연출" 항목).
- **캔들 붕괴 시 위로 튀는 게 안 보임** : 로그로는 정상 작동했지만 높이(20px)가 뒤이어 오는 낙하(250px)에
  묻혀 체감이 안 됐음 — 높이 45px로 키우고 정점에서 0.06초 홀드 추가.

## 씬에 수동으로 배치한 것 (Unity MCP로 직접 작업, 커밋 전 확인 필요)

- `Main_Canvas`에 `ScreenShaker` 컴포넌트 추가.
- `Main_Canvas` 하위에 `ScreenWarningBorder`(빈 오브젝트, RectTransform anchors 0~1 풀스트레치, layer UI)
  생성 후 `ScreenWarningBorderUI` 부착. 마지막 sibling(최상단 렌더링)으로 배치됨.
- `SampleScene.unity` 저장 완료.

## 남은 것 / 제외한 것

- 이동평균선 : 범위 밖 (Notion 문서상 우선순위 낮음).
- Figma 목업 없음 : 경고 테두리 색상/버스트 색상 등은 기존 팔레트 재사용한 임시 스타일 — 디자인
  확정되면 교체 필요.
- 여러 `ponytail:` 주석 튜닝값(붕괴 높이/속도, 위험 임계값, idle bob 진폭 등) — 밸런스 아니라 느낌
  조정용이라 플레이 후 자유롭게 조정 가능.
- MCP를 통한 정밀 프레임 단위 검증에 한계 있었음 : 에디터가 포커스 아웃 상태(`is_focused: false`)일 때
  unscaled deltaTime이 크게 튀어 코루틴이 한 프레임에 완주해버리는 테스트 아티팩트가 여러 번 관측됨 —
  로직 자체는 코드 검토로 확인했으나, 실제 프레임별 체감은 에디터 포커스 준 상태에서 직접 플레이 확인 권장.
