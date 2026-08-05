# 다음 작업

완료된 작업 목록은 `Completed_Tasks.md`로 분리했다. 이 문서에는 아직 안 된 것(후보/버그)만 남긴다.

## 다음 세션 작업 후보 (요약)

| # | 작업 | 비고 |
|---|---|---|
| 7 | 밸런스 수치 조정 | 캔들/확률 등, 실제 플레이 후 — 이번 세션에 손댄 발행량 조작 Support/Growth 가중치(1/5), Doubt 가중치(1.5배), 정기소각/반감기 소각량(1500/3000, 미변경), 부정 이벤트 5종 수치 절반 조정 + 신규 이벤트 3종, 코인발행 최대치(100,000)도 전부 임시 체감값이라 같이 재검토 |
| 9 | 엔딩 결과 화면 UI 재작업 | 로직(`EndingResultUI`의 `EventHub.OnGameEnded` 구독/문구 표시)은 유지, Figma에 새 목업 올라오면 비주얼만 교체(이미지 받으면 진행) |
| 14 | 개요창에 발행량 증가 억제 수치 표시 추가 | `EventOverviewUI.Refresh()`에 줄 추가하는 안으로 논의만 하고 아직 미구현. 미보유 시 0%로 보일 텐데 그대로 둘지 줄 자체를 숨길지 결정 필요 (이벤트확률 0% 버그 고친 것과 같은 종류 문제). 거래량 쪽은 이번 세션에 차트 패널 월봉/주봉 버튼 왼쪽에 남은 턴 표시로 완료(`PriceChartPeriodToggle.UpdateVolumeTurns`) |

(각 항목의 자세한 내용은 아래 섹션 및 `Completed_Tasks.md`/`Logging.md` 참고.)

---

## 후보 : 밸런스 수치 조정 (실제 플레이 후)

- `PriceChartUI.visibleCandleCount`(기본 16)/`candleWidthRatio`(0.95) 등 캔들 차트 관련 수치
- `ProbabilityCalculator.SupportWeight`/`GrowthWeight`/`DoubtWeight`(현재 모두 0.25) 등 상승확률 가중치
- 발행량/거래량 스킬 수치(`Completed_Tasks.md` "발행량 스킬 3종 Supply 효과 부여" 참고) — 억제율 20%/15%,
  `PriceCalculator.VolumeDeltaWeight`(0.01), 정기소각/반감기 소각량(1500/3000) 전부 임시값, Play 모드
  검증은 끝났으니 실제 플레이하며 체감 밸런스만 조정하면 됨
- `TradeCalculator.ManipulateSupply` 가중치 — 이번 세션에 `SupportWeightPerCoin`/`GrowthWeightPerCoin`을
  0.1→0.02(1/5), `DoubtWeightPerSupplyUnit`을 0.01→0.015(1.5배)로 조정. `CoinControlModalUI.MaxAdjustAmount`도
  2000→100,000으로 올림 — 전부 실제 플레이 체감으로 재검토 필요
- 부정 이벤트 수치 — 기존 5종(해킹_사고/소규모_해킹_사고/당국_조사/소규모_당국_조사/인플루언서_폭로) 전부
  절반으로 낮췄고, 신규 3종(전면_거래_금지→"모든 대형 거래소가 상장폐지를 선언했습니다!", 핵심_개발자_이탈,
  가짜뉴스_확산) 추가함. 전면_거래_금지는 의도적으로 다른 이벤트보다 훨씬 크게 잡은 값(weight 0.15로 희귀하게)이라
  실제 플레이하며 밸런스 확인 필요
- 그 외 이벤트/스킬 수치 등도 실제 플레이 데이터가 쌓이면 같이 재검토

## 후보 : 직업(Job) 프로필 — 더미 수치 재조정 필요

Figma `EjUw2LdqxAYhL2180OAXHo` node `1202:186`("초반 캐릭터 선택")을 확인해 `Assets/Scripts/Profile/
직업_프로파일/`에 목업과 동일한 이름의 직업 6종(일반인/기업인/유튜버/개발자/연예인/정치인, 아이콘도
`Assets/Sprites/직업아이콘/UI_일반인아이콘1~6` 순서로 매칭해 연결함)을 만들어뒀다. 단 **효과/수치는
대부분 더미(placeholder)** — 기존 `New Job.asset`(필드명이 스크립트와 어긋난 빈 템플릿)은 삭제하고
이걸로 교체함.

- **일반인만 Figma에 구체 데이터가 있었다** : "평범한 투자자이다. 처음 플레이하는 유저한테 추천한다.",
  특성 "의심도 감소 -10%"(`DoubtDecrease 10`으로 매핑, 확실함) / "대중 이벤트 효과 +10%"(아이콘이
  지지도 아이콘 재사용이라 일단 `SupportIncrease 10`으로 매핑했지만 **불확실** — "대중 이벤트 효과"에
  정확히 대응하는 `EffectType`이 없어서 임시로 끼워맞춘 것, 재확인 필요). 또 "초기 자금 : 10,000$"은
  `JobSO`에 대응 필드가 아예 없다 — 지금은 `PlayerManager.currentMoney`가 모든 직업 공통 고정값
  (10,000,000)이라, 직업별로 시작 자금을 다르게 주려면 `JobSO`에 필드 추가 + `PlayerManager` 연동이
  별도로 필요함(이번 범위 밖). 캐릭터 선택 화면의 "초기 자금" 텍스트는 일단 이 고정값을 그대로 보여주는
  정적 표시일 뿐, 직업별로 값이 바뀌지는 않는다.
- **나머지 5개(기업인/유튜버/개발자/연예인/정치인)는 Figma에 이름만 있고 효과/수치가 전혀 안 정해져
  있어서**, 직업명 컨셉에 맞춰 대충 하나씩 효과를 넣은 완전 더미다(예: 기업인=CashBonus+10,
  유튜버=SupportIncrease+15, 개발자=GrowthIncrease+15, 연예인=SupportIncrease+20,
  정치인=DoubtDecrease+10). **실제 기획이 나오면 전부 다시 정리해야 한다.**

캐릭터 선택 화면 자체(씬 분리 + 데이터 연결)는 완료 — `Completed_Tasks.md` "캐릭터 선택 씬 분리" 항목
참고. 여기 남은 건 순전히 수치/기획 데이터 문제다.


