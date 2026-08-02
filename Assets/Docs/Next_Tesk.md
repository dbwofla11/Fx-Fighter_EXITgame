# 다음 작업

완료된 작업 목록은 `Completed_Tasks.md`로 분리했다. 이 문서에는 아직 안 된 것(후보/버그)만 남긴다.

## 다음 세션 작업 후보 (요약)

| # | 작업 | 비고 |
|---|---|---|
| 4 | 스킬 아이콘/구매 버튼 UI | `RaiseSkillClicked`/`RaiseSkillPurchased` 발행 UI 없음 |
| 5 | 직업 선택 화면 UI | `RaiseJobSelected` 발행 UI 없음 |
| 6 | 발행량 스킬 3종 Supply 효과 부여 | 설계 결정 필요 (수치 없이 코드부터 짜기 애매함) |
| 7 | 밸런스 수치 조정 | 캔들/확률 등, 실제 플레이 후 |
| 8 | 스트리머 패널 스프라이트 연결 | 표정 스프라이트 5종 도착함(`Assets/Sprites/스트리머상태`). 이번엔 이미지 기반 표정 전환만 진행하고, 영상은 사용자가 나중에 제공 예정 — 아래 "다음 세션 시작 프롬프트" 참고 |
| 9 | 엔딩 결과 화면 UI 재작업 | 로직(`EndingResultUI`의 `EventHub.OnGameEnded` 구독/문구 표시)은 유지, Figma에 새 목업 올라오면 비주얼만 교체 |

(각 항목의 자세한 내용은 아래 섹션 및 `Completed_Tasks.md`/`Logging.md` 참고.)

---

## 후보 : UI 연결

`EventHub`의 이벤트 대부분은 Manager 쪽 구독 로직만 갖춰져 있고, 이를 발행하는 실제 UI가 아직 없다.

- 스킬 아이콘/구매 버튼 (`EventHub.RaiseSkillClicked`/`RaiseSkillPurchased`)
- 직업 선택 화면 (`EventHub.RaiseJobSelected`)
- 시사 이벤트 수동 트리거가 필요한 경우의 UI (`EventHub.RaiseNewsEvent`) — 자동 발생은 이미 `MarketManager`에 구현됨
- ~~이벤트 로그 패널(커뮤니티 탭)~~ — **완료** (`Completed_Tasks.md` 참고). Figma가 프레임 2개로 나뉘어 있다는
  걸 뒤늦게 확인했다 — node `1253:2`("뉴스,이벤트 페이지 - 스탯개요")는 "개요" 탭, node `1261:195`("뉴스,이벤트
  페이지 - 이벤트 패널")는 "커뮤니티" 탭 콘텐츠였다. 처음엔 반대로(이벤트 로그를 "개요" 탭에) 연결했다가
  수정했다. "개요" 탭은 아래 항목이 아직 없어 자리만 잡아두고 비워둠.
- ~~개요 탭 콘텐츠(스탯개요 + 엑시트 버튼)~~ — **완료** (`Completed_Tasks.md` 참고).
- ~~엔딩 결과 화면(로직)~~ — **완료** (`Completed_Tasks.md` 참고). Figma에 대응 프레임이 없어 최소 구성(제목+설명
  텍스트, 전체화면 어두운 오버레이)의 임시 UI로 만들었다. **비주얼은 재작업 예정** — 사용자가 Figma에 새
  디자인을 올린 뒤 직접 UI를 다시 짤 계획(위 표 9번). `EndingResultUI.cs`의 `EventHub.OnGameEnded` 구독/
  엔딩별 문구 로직은 그대로 유지하고 씬의 텍스트/배경 오브젝트만 교체하면 됨.

나머지는 설계는 끝났으나 화면이 없다.

- 스트리머 패널의 가격 반응(스프라이트 전환/멘트) — `PlayerStat.StreamerReaction`(5단계)을 이미 읽을 수 있고
  스프라이트 5종도 도착함. 바로 아래 "다음 세션 시작 프롬프트" 참고.

## 다음 세션 시작 프롬프트 : 스트리머 패널 (8번)

표정 스프라이트 5종은 이미 도착해 있다(`Assets/Sprites/스트리머상태/스트리머표정_매우좋음·좋음·보통·슬픔·매우슬픔.png`).
이번 패스는 **이미지 기반 표정 전환까지만** 진행하고, 말풍선 멘트/립싱크/움직임 같은 영상 기반 연출은
사용자가 참고 영상을 준 뒤 별도 세션에서 진행한다. 아래를 그대로 다음 세션 시작 프롬프트로 쓰면 된다.

```
Next_Tesk.md 8번(스트리머 패널) 작업을 시작하자.

목표: RightPanel의 빈 공간(코인 발행 버튼 옆 — Completed_Tasks.md "거래/발행량 조작 모달 리뉴얼" 항목의
"1차 시도 → 수정"에서 이 자리가 스트리머 패널용임을 이미 확인함)에 스트리머 캐릭터 이미지를 배치하고,
PlayerStat.StreamerReaction(StreamerReactionState 5단계: Crash/Down/Neutral/Up/Surge)에 따라 표정
스프라이트를 교체한다.

준비된 것:
- 표정 스프라이트 5종: Assets/Sprites/스트리머상태/
  스트리머표정_매우슬픔(Crash) / _슬픔(Down) / _보통(Neutral) / _좋음(Up) / _매우좋음(Surge)
- 상태 계산 로직은 이미 구현 완료(StreamerReactionCalculator.cs, 임계값 ±10/±50 — Next_Tesk.md 밸런스
  후보에 실플레이 후 조정 필요 항목으로 남아있음) — MarketManager.CurrentStat.StreamerReaction이 매 턴
  (및 수동 이벤트 트리거 시) 갱신되고 EventHub.OnMarketUpdated로 이미 전파된다. 새 EventHub 이벤트는
  필요 없다.

이번 세션 스코프: 표정 스프라이트 전환만. 말풍선/멘트 텍스트, 립싱크나 캐릭터 모션 등 영상 기반 연출은
사용자가 참고 영상을 준 다음 별도로 진행하기로 함 — 이번엔 손대지 않는다.

할 일:
1. Figma 목업에서 스트리머 패널 프레임 확인 (레이아웃/위치/크기)
2. StreamerPanelUI.cs 신규 — EventHub.OnMarketUpdated 구독, CurrentStat.StreamerReaction 값에 따라
   Image.sprite를 5개 중 하나로 교체 (다른 EventHub 구독형 UI와 동일 패턴 — StatGaugeUI/CoinPriceHeaderUI 참고)
3. 씬에 스트리머 캐릭터 Image 오브젝트 배치 (RightPanel 빈 공간)
4. Play 모드에서 가격 급등/급락을 강제로 발생시켜 5단계 전환이 스프라이트와 맞게 표시되는지 확인
```

## 후보 : 발행량 관련 스킬 3종에 실제 Supply 효과 부여

`추가발행권한`/`우회발행권한`/`발행량은폐` 세 스킬은 이름과 설명(예: "위기 상황에서 자금을 빠르게 마련",
"발행 사실이 드러나더라도 의심을 최소화")으로 미루어 보면 발행량과 강하게 연관되어 있지만, 지금 `.asset`
데이터에는 `SupplyIncrease`/`SupplyDecrease` 효과가 하나도 없다 (`추가발행권한`은 발행량 조작 버튼의 해금
조건 역할만 하고 있음). 아래를 정해야 한다.

- `추가발행권한`/`우회발행권한`을 구매하는 순간에도 (버튼 해금과 별개로) Supply를 직접 늘리는 1회성 효과를
  줄 것인지, 아니면 순수하게 "버튼 해금 + 기존 효과(Growth/CashBonus/DoubtDecrease)"만으로 끝낼 것인지.
- `발행량은폐`는 설명상 Supply 자체보다는 "정보를 숨긴다"는 쪽이라 `DoubtDecrease`만으로 충분해 보이는데,
  이대로 유지할지 확인 필요.
- 만약 Supply 효과를 추가한다면 구체적 수치도 함께 정해야 한다.

## 후보 : 밸런스 수치 조정 (실제 플레이 후)

- `PriceChartUI.visibleCandleCount`(기본 16)/`candleWidthRatio`(0.95) 등 캔들 차트 관련 수치
- `ProbabilityCalculator.SupportWeight`/`GrowthWeight`/`DoubtWeight`(현재 모두 0.25) 등 상승확률 가중치
- 그 외 이벤트/스킬 수치 등도 실제 플레이 데이터가 쌓이면 같이 재검토


