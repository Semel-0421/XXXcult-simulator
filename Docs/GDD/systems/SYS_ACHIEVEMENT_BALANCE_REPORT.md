# 업적 v2 도달률 · 기프티드 v2 영향 시뮬레이션 보고서

작성: 2026-10-20 · 도구: `Assets/Editor/Tests/AchievementSimulation.cs` (`[Explicit]`, 기본 테스트 실행에서는 빠짐)

## 1. 방법

- 가상 플레이어 1명이 한 모드를 **20판 연속** 플레이한다. 업적, 누적 카운터, 본 엔딩은 플레이어 단위로 쌓이고 플레이어가 바뀔 때만 비운다. 프로필은 `Profile.InMemory`로 메모리에만 두고 디스크는 쓰지 않는다.
- `Achievements.Evaluate`(턴, 선택, 교리, 엔딩마다 불림)가 끝날 때마다 `Achievements.Evaluated` 이벤트로 v2 업적 348개의 조건을 모두 확인하고, 그 판에 한 번이라도 충족했는지 기록한다. 기프티드 v2는 실제 규칙대로 적용한다(연결 업적을 달성하면 보유).
- 봇:
  - 일반·거울: `BalanceSimulation` 봇. 스타일은 Desire/Virtue/Balanced이고, 판마다 목표 가지와 굿/배드를 무작위로 정하며 6단계 갈래 A/B도 무작위로 고른다.
  - 신: `GodSimulation` 봇. Desire = 욕망+기적, Virtue = 자비+예언, Balanced = 4갈래 모두. 전략(Rush/Spread/Balanced)은 판마다 무작위로 정한다.
- 규모(시간 제한으로 계획보다 줄임):
  - 일반 140명(Desire 60 · Virtue 60 · Balanced 20) = 2,800판
  - 거울 60명(스타일당 20명) = 1,200판
  - 신 60명(스타일당 20명) = 1,200판
  - 기프티드 비교: 모드마다 Balanced 10명 × 20판을 "전부 보유"와 "하나도 없음"으로 한 번씩, 같은 시드로 돌림
- 결과 파일:
  - 원자료 `Temp/ach_raw_<mode>.tsv`
  - 조건 값 분포 `Temp/ach_keys_<mode>.tsv`
  - 모드별 보고 `Temp/ach_sim_<mode>.txt`: 전체 표, 분포, 자동 기준치 제안, 스타일별 도달률 `[D.. V.. B..]`
- 지표 정의:
  - 플레이어% = 20판 안에 한 번이라도 조건을 충족한 플레이어 비율
  - 목표 밴드 = t1 40~80%, t2 10~50%, t3·t4 1~15%, 히든 0.5~10%
- 다시 돌리는 법: Test Runner에서 `AchievementSimulationTests.Simulate`를 실행한다(전체 규모, 약 100분). 일부만 돌릴 때는 `AchievementSimulation.RunBatch(...)`를 부른 뒤 `WriteReport(mode)`로 보고를 만든다.

## 2. 요약

| 모드 | 업적 수 | 0% | 밴드 미만 | 밴드 안 | 밴드 초과 |
|---|---|---|---|---|---|
| 일반 | 121 | 45 | 7 | 10 | 59 |
| 신 | 93 | 27 | 3 | 1 | 62 |
| 거울 | 119 | 38 | 3 | 11 | 67 |
| 히든 | 15 | 8 | 0 | 2 | 5 |

- 도달률이 양극단으로 갈려 있다. 목표 밴드 안에 드는 업적은 348개 중 24개뿐이다.
- 나머지는 대부분 "첫 판~3판 안에 100%"이거나 "0%"다.
- 봇은 한 가지·한 전략에 집중하므로 실제 플레이어보다 다양성이 낮다. 따라서 0%가 "봇이 그 행동을 하지 않아서"일 수도 있다. 이를 3절에서 원인별로 나눈다.

### 모드 결과 (실제 보유 규칙)

| 모드·스타일 (Desire / Virtue / Balanced) | 붕괴 | 최종 스킬 엔딩 | 승천 | 사망·패배 | 평균 교단 연수 |
|---|---|---|---|---|---|
| 일반 | 1.0 / 0.2 / 0.0% | 83.8 / 63.2 / 76.5% | - | 사망 1.3 / 1.1 / 1.3% | 12.4 / 16.6 / 14.1년 |
| 거울 | 3.0 / 2.3 / 2.8% | 93.5 / 73.3 / 84.0% | - | 사망 0 / 1.3 / 0.8% | 11.9 / 14.1 / 12.8년 |
| 신 | - | - | 92.5 / 98.5 / 96.5% | 패배 7.5 / 1.5 / 3.5% | 18.2 / 17.5 / 17.8년 |

## 3. 0% 업적 분류

### 3-1. 이번에 고친 훅 버그 (수치 재조정 아님)

| 업적 | 문제 | 수정 |
|---|---|---|
| n101~n120 | `c:act_X`는 한 판 안의 횟수다. 생활 행동은 한 판에 최대 ~37번뿐이고, 교단 행동 400번은 100년이 걸려 대부분 불가능했다 | 이미 있던 누적 카운터 `NEW:n_act_life_X` / `NEW:n_act_cult_X`로 키를 바꿈 |
| n121~n122 | `c:act_Consecrate`는 신 모드 한 판 안의 횟수였다 | 누적 `NEW:god_act_Consecrate`로 바꿈 (여전히 신 모드에서만 오름) |
| n152~n159, n221 | `NormalCounters.OnDoctrine`을 화면(CultFlow)에서만 불러서, 상태 쪽에서 교리를 배우면(불러오기, 시뮬레이션) 세지 않았다 | `GameSession.StartNew`에서 `Doctrines.Changed`에 구독하고 CultFlow의 호출은 지움. 0% → 75~100% |
| g012~g020 | `god_ending_N` 번호를 `GodRules.AllEndings` 순서로 매겨 CSV 이름과 실제 엔딩이 어긋났다 (예: g012 '기적과 예언'이 실제로는 기적+자비 엔딩에 달려 있었음) | `GodCounters.EndingOrder`로 CSV 순서에 맞춤: 1 = god_divine … 6 = god_human, 7~9 = 패배 3종 |
| m101~m103 | `NEW:mirror_final_all16` 등에 `>=16`이 빠져 있어 최종 스킬 1개만 익혀도 달성됐다(100%) | `>=16`을 붙임 → 0% (5절 기준치 제안 참고) |
| m106 (m107) | 거울 교단은 신도 0명에서 시작해 "신도 5명 이하" 조건이 시작 직후 항상 참이었다 → m106 100% | 유예 16턴이 지난 뒤에만 바닥으로 인정 → 50%대 |
| m042~m049 | 6단계부터 A/B 갈래가 배타라 한 갈래 교리는 최대 10개인데 조건이 `>=15`였다 | `>=10`(한 갈래 전부) |
| m051 | 반대 가지 잠금 때문에 한 판에 배울 수 있는 교리는 최대 60개인데 조건이 `>=120`이었다 | `>=60`(배울 수 있는 최대) |
| m119 | `mirror_achv>=119`는 자기 자신까지 세야 해서 달성할 수 없었다 | `>=118`(자신을 뺀 나머지 전부) |
| n172, h014 | 기프티드는 140종인데 조건이 `>=141`이었다 | `>=140` |

CSV(`Docs/achievements_v2_*.csv`)를 고친 뒤 `Docs/gen_achievements_v2_cs.py`로 `AchievementCatalogV2.cs`를 다시 생성했다.

### 3-2. 지금 코드·콘텐츠로는 여전히 불가능 (손대지 않음, 기획 결정 필요)

| 업적 | 이유 | 제안 |
|---|---|---|
| m118 `mirror_achv>=150` | 거울 업적이 119개뿐 | `>=80`~`110` |
| g072 `god_ascend_single_branch` | 승천하려면 서로 다른 갈래의 핵 2개가 필요하므로 한 갈래로는 승천할 수 없다 | "핵을 만든 두 갈래 밖에서는 노드 0개"로 조건 변경 (훅 추가 필요) |
| g018~g020 이름 | '순수한 기적/자비/욕망의 승천' 엔딩은 게임에 없다. 지금 7~9번은 그릇 소멸/잊힘/해산(패배 엔딩)에 연결돼 있다 | 이름을 패배 엔딩 3종에 맞게 바꾸거나, 순수 승천 엔딩 3개를 새로 추가 |
| m050 `mirror_doctrines>=60` (t2) | m051을 고친 뒤 최대치(60)와 같은 값이 됐다 | `>=30` |

### 3-3. 다른 모드를 해야 하거나 20판으로는 닿지 않는 것 (구조상 그럼, 버그 아님)

- 다른 모드를 해야 하는 것: h001(세 모드 클리어), h004(모든 엔딩), g085(일반 모드 해산 뒤 승천), m116(일반 모드와 같은 최종 스킬), n121/n122(신 모드 봉헌).
- 판 수가 모자란 것: n191 `runs>=30`, h002 `ending_total>=30`(한 모드로는 최대 20), n180 `endings_seen>=30`, n181/n182(모든 굿/배드 엔딩), m104(거울 엔딩 41개).

### 3-4. 시뮬레이션으로 잴 수 없는 것 (훅이 화면 경로에만 있음, 실제 게임에서는 동작)

- 성인 계열 n196~n201, n173:
  - n173 `gifted_special_no.20`은 성인 이벤트를 5번 봐야 하는데 성인 플래그가 없다. 성인 업적으로 표시하거나 조건을 바꾸길 권장.
  - n200(금욕)은 시뮬레이션이 성인 이벤트를 잡지 못해 100%로 나왔다. 실제 수치와 다르다.
- 거울 이벤트 m091~m099: `MirrorCounters.Event`는 화면이 이벤트를 띄울 때(`GameSession.SetPending`)만 불린다. 봇은 이 경로를 거치지 않는다.

### 3-5. 어렵거나 봇이 하지 않는 것 (0%, 실제 플레이어는 가능)

일반:
- 봇이 하지 않음: n111/n112(포교를 고르지 않음), n129/n214(청렴 100).
- 수치가 너무 높음:
  - n136 신도 2500 (최고값 p90 ≈ 307)
  - n166~n169 보유 기프티드 (한 판 최고 p50 19, 최대 22)
  - n171 도감 100 (20판 누적 최대 31)
  - n194/n195 업적 수 (20판 ≈ 116~140)
  - n205 연속 실패 10 (p90 4, 최대 6)
  - n216 평형 (최소 격차 ≈ 43)
- 판이 일찍 끝남: n144/n145/n148/n210 나이·연수 (판이 교단 ~25년차에 최종 스킬로 끝남).
- n219 모든 행동 100회: 생활 행동은 판당 ~37턴뿐이라 사실상 불가능.
- 그 밖: n160~n164 시선 회복, n176 성자 기프티드 5개, n192 해산 10번, n211/n212/n215/n220 복합 조건.

신:
- 봇이 하지 않음: g037~g043 건물(봇은 쉼터·신상만 지음), g031 연회 100.
- 수치가 너무 높음:
  - g034/g035 봉헌 30/100 (20판 누적 p50 15, 최대 23)
  - g046/g048/g074 시기 95 (최고 p90 77, 최대 83)
  - g066/g090 신앙 포인트 1만/5만 (최대 2,436)
  - g067 5년 안 승천 (최소 ≈ 11년)
- 그 밖: g053 빼앗김 15, g056/g057 50·100년, g068, g070, g075, g077, g081, g083, g087.

거울:
- 봇이 하지 않음: m111~m113(봇이 한 갈래만 배움).
- 수치가 너무 높음:
  - m058/m059 신도 300/1000 (최고 p50 159, 최대 241)
  - m067/m117 연쇄 지표 (최대 241)
  - m100 업적 100 (20판 p50 46, 최대 61)
  - m101~m103 최종 스킬 16개
- 그 밖: m002(흡수 엔딩), m032/m034(청렴 굿 엔딩), m040/m041, m056(40년), m063, m071, m087(붕괴 5번), m107, m109, m115.

## 4. 너무 쉬운 업적 (플레이어 90% 초과)

일반 22개, 신 44개, 거울 21개. 그중 t3·t4인데 거의 모두가 달성한 것:

- 일반: n151 `doctrine>=9`(첫판 2), n193 `founded>=10`, n203 `events>=1000`. t2인 n204 연속 성공 10도 첫판에 100%.
- 신:
  - g001~g003: 한 판에 트리 125노드를 모두 익힘 (모든 플레이어의 최고값이 125)
  - g004/6/8/10 갈래 30노드, g012~g016 승천 엔딩
  - g025/g027/g029/g033 행동 100회 (t4)
  - g058~g062 사건 40, g064 `god_events>=200`, g073 네 갈래 20
- 거울: m072 불운 낮은 해 10, m085 `mirror_runs>=20`, m090 최종 스킬 엔딩 10번, m079 `gap<=5`(93%).
- 히든: h006/h007 평균 60 (일반 100%, 거울 87~90%), h011 기프티드 누적 100 (100%).
  - 특이점: h005 무실패 클리어가 일반에서 25.7%로 나와 히든치고 너무 높다. 판정 확률 상한을 확인할 필요가 있다.

## 5. 기준치 제안 (핵심만, 실제 수치는 바꾸지 않음)

- 플레이어별 20판 최고값 분포의 분위수에서 뽑은 값이다. 목표 도달률은 t1 60%, t2 30%, t3 8%, 히든 5%로 잡았다.
- 전체 자동 제안은 8절 표와 `Temp/ach_sim_<mode>.txt`의 "기준치 제안" 절에 있다.

| 업적 | 현재 | 현재 도달 | 제안 | 근거 |
|---|---|---|---|---|
| n101/n103/n107 생활 행동 숙련 (t2) | 50 | 74~99% | 200~250 | 누적 p70 ≈ 230~400 |
| n102/n104/n108 생활 행동 전설 (t4) | 200 | 41~57% | 350~500 | 누적 p92 ≈ 300~510 |
| n113/n115/n117/n119 교단 행동 숙련 | 100 | 56~99% | 250~350 | p70 ≈ 270~440 |
| n114/n118/n120 교단 행동 전설 | 400 | 24~49% | 550 | p92 ≈ 460~570 |
| n123~n135 개인 스탯 100 (t2) | 100 | 55~100% | 이미 최대치 → t1로 내리거나 "두 스탯 동시" 같은 조건으로 | 스탯 최대 100 |
| n142 / n146 / n149 (t1) | 20세 / 7년 / 교리 1 | 100% | 입문 업적으로 두되 t1 밴드에서 빼서 봄 | - |
| n150 / n151 교리 레벨 | 5 / 9 | 100% | 9 / 10(최대)에 연수 조건을 더함 | 교리 최대 10 |
| n166~n169 보유 기프티드 | 25/30/50/100 | 0% | 18 / 20 / 21 / 22 | 한 판 최고 p40 18, 최대 22 |
| n171 도감 | 100 | 0% | 30 (n172의 140은 유지) | 20판 누적 최대 31 |
| n194 / n195 업적 수 | 200 / 299 | 0% | 125 / 140 | 20판 p50 121, 최대 140 |
| n202 / n203 이벤트 | 300 / 1000 | 100% | 1500 / 1800 | 누적 p10 1496, p90 1783 |
| n205 연속 실패 | 10 | 0% | 5 | p90 4, 최대 6 |
| n216 평형 | <=20 | 0% | <=40 | 최소 격차 p30 ≈ 43 |
| n219 모든 행동 | 100 | 0% | 20 | 생활 행동 판당 ~37턴 |
| g001 / g002 / g003 노드 | 25/75/125 | 100% | 노드 수로는 구분이 안 됨 → 승천 횟수·기간 조건으로 바꾸길 권장 | 한 판에 125(전부) |
| g024~g033 권능 행동 | 30 / 100 | 100% | 300 / 450 | 누적 p70 ≈ 280~410 |
| g034/g035 봉헌 | 30 / 100 | 0% | 15 / 21 | 누적 p50 15, 최대 23 |
| g058~g062 사건 40 (t3) | 40 | 100% | 160~260 | 누적 p92 |
| g063 / g064 사건 | 100 / 200 | 100% | 1100 / 1200 | 누적 분포 |
| g065 / g066 / g090 신앙 포인트 | 1천 / 1만 / 5만 | 100 / 0 / 0% | 2200 / 2300 / 2400, 또는 누적 키로 바꾸고 1만/5만 유지 | 최고 p50 2,139, 최대 2,436 |
| g067 빠른 승천 | <=5년 | 0% | <=11년 | 최소 p30 ≈ 11 |
| g046 / g074 시기 95 | 95 | 0% | 80 | 최고 p90 77, 최대 83 |
| m052~m054 거울 연수 | 1 / 5 / 10 | 100% | 1(t1 유지) / 15 / 20 | - |
| m057 / m058 / m059 신도 | 50 / 300 / 1000 | 100 / 0 / 0% | 50 / 180 / 220 | 최고 p50 159, p90 203, 최대 241 |
| m067 / m117 연쇄 지표 | 300 / 900 | 0% | 180 / 220 | 최고 p50 154, 최대 241 |
| m072 불운 낮은 해 | 10 | 100% | 150 | 누적 p92 ≈ 180 |
| m074 / m075 / m076 실패 횟수 | 1 / 10 / 60 | 100 / 100 / 72% | 1(t1) / 40 / 90 | - |
| m085 / m090 | 판 20 / 최종 10 | 100% | 30 / 19 | - |
| m100 / m118 거울 업적 수 | 100 / 150 | 0% | 50 / 60 | 20판 p50 46, 최대 61 |
| m101 / m102 / m103 최종 16 | 16 | 0% | 12 / 8 / 8, 또는 16 유지(장기 플레이어용 t3) | 20판 p92 ≈ 12 / 7 / 8 |
| m110 교리 포인트 | 40 | 100% | 160 | 최고 p50 156, p90 167 |
| h006 / h007 평균 60 | 60 | 87~100% | 85 | 일반 최고 p90 85 |
| h011 기프티드 누적 100 | 100 | 100% | 300 (h012는 → 500) | 누적 p90 304 |
| h015 동시 보유 40 | 40 | 0% | 21 | 한 판 최대 22 |

## 6. 기프티드 v2 영향 (120개 전부 보유 vs 하나도 없음)

- `Achievements.ForceGiftedV2`(시뮬레이션 전용)로 보유 상태를 강제했다.
- 모드마다 Balanced 10명 × 20판 = 200판씩, 두 조건에 같은 시드를 썼다.

| 모드 | 지표 | 없음 | 전부 | 차이 |
|---|---|---|---|---|
| 일반 | 붕괴(해산) | 0.5% | 0.0% | -0.5%p |
| 일반 | 최종 스킬 엔딩 | 72.5% | 54.0% | **-18.5%p** |
| 일반 | 사망 | 3.0% | 0.0% | -3.0%p |
| 일반 | 기타(25년차 극단 엔딩 등) | 24.0% | 46.0% | +22.0%p |
| 일반 | 평균 교단 연수 | 14.6년 | 18.0년 | +3.4년 |
| 거울 | 붕괴(흡수·이탈) | 7.5% | 0.0% | **-7.5%p** |
| 거울 | 최종 스킬 엔딩 | 76.0% | 96.5% | **+20.5%p** |
| 거울 | 평균 교단 연수 | 14.6년 | 11.4년 | -3.2년 |
| 신 | 승천 | 88.0% | 99.0% | **+11.0%p** |
| 신 | 패배 | 12.0% | 1.0% | -11.0%p |
| 신 | 평균 교단 연수 | 20.9년 | 11.8년 | -9.1년 |

해석:
- 거울·신: 전부 보유하면 실패가 사실상 없어지고 판 길이도 크게 줄어든다. 후반 보상으로는 괜찮지만 **누적 효과가 강하다**. 가장 큰 변화는 신 모드 승천 소요 기간으로, 20.9년에서 11.8년으로 거의 절반이 된다.
- 일반: 최종 스킬 엔딩이 오히려 54%로 줄었다.
  - 기프티드가 개인 스탯을 크게 올려서, 봇이 굿/배드 판정 조건(반대쪽 스탯 평균)을 맞추지 못한다.
  - 그래서 최종 스킬을 계속 미루다가 25년차 극단 엔딩으로 끝나는 봇 특성 영향이 크다.
  - 실제 플레이어에게는 "스탯이 너무 쉽게 올라 원하는 굿/배드 엔딩을 맞추기 어렵다"로 나타날 수 있다. 일반 모드의 스탯 증가형 기프티드 배율을 점검하길 권장.
- 실제 규칙(연결 업적을 달성해야 보유)에서는 20판 동안 보유량이 적어, 기본 결과가 "없음" 쪽에 가깝다.

## 7. 한계

- 봇 정책이 치우쳐 있다: 한 갈래에 집중하고, 포교·연회·일부 건물은 쓰지 않으며, 6단계 뒤로는 한 갈래만 배운다. 그래서 0%가 곧 "불가능"을 뜻하지 않는다. 확실히 불가능한 것은 3-2절과 3-3절뿐이다.
- 시간 제한으로 거울·신은 스타일당 20명, 일반 Balanced도 20명만 돌렸다. 일반의 "전체" 수치는 Desire/Virtue 쪽 비중이 크다.
- 성인 이벤트, 거울 이벤트 노출(SetPending), 이어하기는 측정하지 않았다.
- `Counters.Read`는 이번 판 값이 있으면 그 값을 읽는다. 그래서 `Both` 키(신 모드 행동·건물 등)의 누적값은 다음 판이 시작될 때에야 보이고, 업적이 한 판 늦게 달성된다. 동작은 하지만 의도와 다를 수 있다.

## 8. 전체 표 (플레이어%, 각 모드 자체 시뮬레이션 기준)

### normal

| id | t | 조건 | 플레이어% | 첫판 | 자동 제안 |
|---|---|---|---|---|---|
| n101 | 2 | `NEW:n_act_life_Exercise>=50` | 74.3 | 7 | >=230 |
| n102 | 4 | `NEW:n_act_life_Exercise>=200` | 41.4 | 16 | >=300 |
| n103 | 2 | `NEW:n_act_life_Meditate>=50` | 92.9 | 4 | >=400 |
| n104 | 4 | `NEW:n_act_life_Meditate>=200` | 57.1 | 10 | >=510 |
| n105 | 2 | `NEW:n_act_life_Study>=50` | 49.3 | 9 |  |
| n106 | 4 | `NEW:n_act_life_Study>=200` | 6.4 | 18 |  |
| n107 | 2 | `NEW:n_act_life_Socialize>=50` | 98.6 | 7 | >=240 |
| n108 | 4 | `NEW:n_act_life_Socialize>=200` | 50.0 | 16 | >=330 |
| n109 | 2 | `NEW:n_act_life_Pray>=50` | 12.1 | 10 |  |
| n110 | 4 | `NEW:n_act_life_Pray>=200` | 0.0 | - | >=58 |
| n111 | 2 | `NEW:n_act_cult_Proselytize>=100` | 0.0 | - |  |
| n112 | 4 | `NEW:n_act_cult_Proselytize>=400` | 0.0 | - |  |
| n113 | 2 | `NEW:n_act_cult_Preach>=100` | 99.3 | 3 | >=440 |
| n114 | 4 | `NEW:n_act_cult_Preach>=400` | 48.6 | 17 | >=570 |
| n115 | 2 | `NEW:n_act_cult_Serve>=100` | 95.7 | 6 | >=270 |
| n116 | 4 | `NEW:n_act_cult_Serve>=400` | 9.3 | 16 |  |
| n117 | 2 | `NEW:n_act_cult_Desire>=100` | 82.1 | 8 | >=380 |
| n118 | 4 | `NEW:n_act_cult_Desire>=400` | 25.0 | 19 | >=460 |
| n119 | 2 | `NEW:n_act_cult_CultPray>=100` | 55.7 | 7 | >=330 |
| n120 | 4 | `NEW:n_act_cult_CultPray>=400` | 23.6 | 17 | >=540 |
| n121 | 2 | `NEW:god_act_Consecrate>=100` | 0.0 | - |  |
| n122 | 4 | `NEW:god_act_Consecrate>=400` | 0.0 | - |  |
| n123 | 2 | `p:Health>=100` | 74.3 | 5 | >=100 |
| n124 | 2 | `p:Mentality>=100` | 100.0 | 2 | >=100 |
| n125 | 2 | `p:Piety>=100` | 64.3 | 5 | >=100 |
| n126 | 2 | `p:Repute>=100` | 97.9 | 3 | >=100 |
| n127 | 2 | `p:Wealth>=100` | 78.6 | 4 | >=100 |
| n128 | 2 | `p:Charity>=100` | 99.3 | 2 | >=100 |
| n129 | 2 | `p:Integrity>=100` | 0.0 | - | >=76 |
| n130 | 2 | `p:Patience>=100` | 57.9 | 6 | >=100 |
| n131 | 2 | `p:Temperance>=100` | 54.3 | 5 | >=100 |
| n132 | 2 | `p:Greed>=100` | 32.9 | 14 |  |
| n133 | 2 | `p:Ambition>=100` | 98.6 | 2 | >=100 |
| n134 | 2 | `p:Lust>=100` | 66.4 | 7 | >=100 |
| n135 | 2 | `p:Appetite>=100` | 55.7 | 9 | >=100 |
| n136 | 3 | `s:Followers>=2500` | 0.0 | - | >=310 |
| n137 | 2 | `s:FollowerFaith>=1000` | 87.1 | 3 | >=1000 |
| n138 | 2 | `s:LeaderFaith>=1000` | 52.9 | 8 | >=1000 |
| n139 | 2 | `s:Authority>=1000` | 12.1 | 1 |  |
| n140 | 2 | `s:Offerings>=1000` | 61.4 | 5 | >=1000 |
| n141 | 2 | `s:Reputation>=1000` | 53.6 | 6 | >=1000 |
| n142 | 1 | `age>=20` | 100.0 | 1 | >=43 |
| n143 | 1 | `age>=45` | 32.1 | 7 | >=43 |
| n144 | 2 | `age>=80` | 0.0 | - | >=45 |
| n145 | 3 | `age>=100` | 0.0 | - | >=53 |
| n146 | 1 | `cultyears>=7` | 100.0 | 1 | >=25 |
| n147 | 2 | `cultyears>=25` | 100.0 | 1 | >=26 |
| n148 | 3 | `cultyears>=50` | 0.0 | - | >=34 |
| n149 | 1 | `doctrine>=1` | 100.0 | 1 | >=10 |
| n150 | 2 | `doctrine>=5` | 100.0 | 1 | >=10 |
| n151 | 3 | `doctrine>=9` | 100.0 | 2 | >=10 |
| n152 | 3 | `NEW:final_branch_Lust` | 55.0 | 6 |  |
| n153 | 3 | `NEW:final_branch_Greed` | 56.4 | 5 |  |
| n154 | 3 | `NEW:final_branch_Appetite` | 55.7 | 6 |  |
| n155 | 3 | `NEW:final_branch_Ambition` | 55.7 | 5 |  |
| n156 | 3 | `NEW:final_branch_Temperance` | 55.7 | 5 |  |
| n157 | 3 | `NEW:final_branch_Charity` | 50.0 | 7 |  |
| n158 | 3 | `NEW:final_branch_Patience` | 56.4 | 4 |  |
| n159 | 3 | `NEW:final_branch_Integrity` | 55.7 | 6 |  |
| n160 | 2 | `NEW:standing_recover_Persecuted` | 0.0 | - |  |
| n161 | 2 | `NEW:standing_recover_Watched` | 0.0 | - |  |
| n162 | 2 | `NEW:standing_recover_Respected` | 0.0 | - |  |
| n163 | 2 | `NEW:standing_recover_Recognized` | 0.0 | - |  |
| n164 | 3 | `NEW:standing_swing` | 0.0 | - |  |
| n165 | 2 | `NEW:standing_changes>=5` | 80.0 | 1 | >=10 |
| n166 | 1 | `gifted>=25` | 0.0 | - | >=18 |
| n167 | 1 | `gifted>=30` | 0.0 | - | >=18 |
| n168 | 2 | `gifted>=50` | 0.0 | - | >=19 |
| n169 | 3 | `gifted>=100` | 0.0 | - | >=21 |
| n170 | 2 | `profile:gifted_seen>=30` | 20.0 | 17 |  |
| n171 | 3 | `profile:gifted_seen>=100` | 0.0 | - | >=31 |
| n172 | 3 | `profile:gifted_seen>=140` | 0.0 | - | >=31 |
| n173 | 2 | `NEW:gifted_special_no.20` | 0.0 | - |  |
| n174 | 2 | `NEW:gifted_special_gifted_cleric` | 6.4 | 1 |  |
| n175 | 2 | `NEW:gifted_special_gifted_beast` | 2.1 | 11 |  |
| n176 | 2 | `NEW:gifted_special_gifted_saint` | 0.0 | - |  |
| n177 | 2 | `NEW:gifted_special_gifted_none` | 7.9 | 1 |  |
| n178 | 1 | `profile:endings_seen>=1` | 100.0 | 1 | >=13 |
| n179 | 2 | `profile:endings_seen>=10` | 100.0 | 13 | >=14 |
| n180 | 3 | `profile:endings_seen>=30` | 0.0 | - | >=16 |
| n181 | 3 | `NEW:all_good_endings` | 0.0 | - |  |
| n182 | 3 | `NEW:all_bad_endings` | 0.0 | - |  |
| n183 | 3 | `NEW:both_endings_Lust` | 37.1 | 11 |  |
| n184 | 3 | `NEW:both_endings_Greed` | 40.0 | 13 |  |
| n185 | 3 | `NEW:both_endings_Appetite` | 40.7 | 13 |  |
| n186 | 3 | `NEW:both_endings_Ambition` | 39.3 | 11 |  |
| n187 | 3 | `NEW:both_endings_Temperance` | 43.6 | 10 |  |
| n188 | 3 | `NEW:both_endings_Charity` | 10.0 | 9 |  |
| n189 | 3 | `NEW:both_endings_Patience` | 47.9 | 13 |  |
| n190 | 3 | `NEW:both_endings_Integrity` | 10.0 | 15 |  |
| n191 | 2 | `profile:runs>=30` | 0.0 | - | >=20 |
| n192 | 3 | `profile:dissolved>=10` | 0.0 | - | >=1 |
| n193 | 3 | `profile:founded>=10` | 100.0 | 10 | >=20 |
| n194 | 2 | `achv>=200` | 0.0 | - | >=120 |
| n195 | 3 | `achv>=299` | 0.0 | - | >=130 |
| n196 | 1 | `NEW:adult_seen>=1 (성인)` | 0.0 | - |  |
| n197 | 2 | `NEW:adult_seen>=30 (성인)` | 0.0 | - |  |
| n198 | 3 | `NEW:adult_seen>=120 (성인)` | 0.0 | - |  |
| n199 | 2 | `NEW:adult_both_genders (성인)` | 0.0 | - |  |
| n200 | 3 | `NEW:adult_none_cultyears>=10 (성인)` | 100.0 | 1 | >=34 |
| n201 | 3 | `p:Lust>=100 s:Followers>=500 (성인)` | 0.0 | - |  |
| n202 | 1 | `profile:events>=300` | 100.0 | 3 | >=1600 |
| n203 | 3 | `profile:events>=1000` | 100.0 | 13 | >=1800 |
| n204 | 2 | `NEW:success_streak>=10` | 100.0 | 1 | >=42 |
| n205 | 2 | `NEW:fail_streak>=10` | 0.0 | - | >=4 |
| n206 | 1 | `NEW:bribes_taken>=1` | 5.7 | 17 | 목표 도달률을 만들 수 없음(대부분 0) |
| n207 | 1 | `p:Wealth<=0` | 98.6 | 1 | <=0 |
| n208 | 3 | `p:Wealth<=0 s:Followers>=100` | 45.7 | 8 |  |
| n209 | 3 | `p:Wealth>=100 s:Offerings>=700` | 63.6 | 9 |  |
| n210 | 2 | `age>=70 founded` | 0.0 | - |  |
| n211 | 2 | `s:Followers>=1000 s:Authority>=900` | 0.0 | - |  |
| n212 | 2 | `p:Lust<=0 s:Followers>=300 cultyears>=10` | 0.0 | - |  |
| n213 | 2 | `p:Piety>=100 p:Lust>=100` | 0.7 | 4 |  |
| n214 | 2 | `p:Integrity>=100 p:Wealth>=100` | 0.0 | - |  |
| n215 | 2 | `p:Ambition>=100 p:Appetite>=100` | 0.0 | - |  |
| n216 | 2 | `NEW:balanced_desire_virtue<=20` | 0.0 | - | <=43 |
| n217 | 2 | `NEW:opposite_extreme` | 4.3 | 1 |  |
| n218 | 2 | `NEW:death_age<=19` | 12.9 | 11 |  |
| n219 | 2 | `NEW:all_actions>=100` | 0.0 | - | 목표 도달률을 만들 수 없음(대부분 0) |
| n220 | 2 | `p:Piety>=100 s:Authority>=1000 s:Followers>=3000` | 0.0 | - |  |
| n221 | 2 | `NEW:branch_complete` | 0.0 | - |  |

### god

| id | t | 조건 | 플레이어% | 첫판 | 자동 제안 |
|---|---|---|---|---|---|
| g001 | 1 | `NEW:god_nodes>=25` | 100.0 | 1 | >=120 |
| g002 | 2 | `NEW:god_nodes>=75` | 100.0 | 1 | >=120 |
| g003 | 3 | `NEW:god_nodes>=125` | 100.0 | 2 | >=120 |
| g004 | 3 | `NEW:god_branch_Miracle>=30` | 100.0 | 1 | >=31 |
| g005 | 2 | `NEW:god_core_Miracle` | 100.0 | 1 |  |
| g006 | 3 | `NEW:god_branch_Prophecy>=30` | 100.0 | 1 | >=31 |
| g007 | 2 | `NEW:god_core_Prophecy` | 100.0 | 1 |  |
| g008 | 3 | `NEW:god_branch_Mercy>=30` | 100.0 | 1 | >=31 |
| g009 | 2 | `NEW:god_core_Mercy` | 100.0 | 1 |  |
| g010 | 3 | `NEW:god_branch_Desire>=30` | 100.0 | 1 | >=31 |
| g011 | 2 | `NEW:god_core_Desire` | 100.0 | 1 |  |
| g012 | 3 | `NEW:god_ending_1` | 31.7 | 4 |  |
| g013 | 3 | `NEW:god_ending_2` | 33.3 | 4 |  |
| g014 | 3 | `NEW:god_ending_3` | 66.7 | 2 |  |
| g015 | 3 | `NEW:god_ending_4` | 66.7 | 2 |  |
| g016 | 3 | `NEW:god_ending_5` | 33.3 | 4 |  |
| g017 | 3 | `NEW:god_ending_6` | 31.7 | 4 |  |
| g018 | 3 | `NEW:god_ending_7` | 0.0 | - |  |
| g019 | 3 | `NEW:god_ending_8` | 30.0 | 10 |  |
| g020 | 3 | `NEW:god_ending_9` | 30.0 | 7 |  |
| g021 | 1 | `profile:god_ascended>=1` | 100.0 | 1 | >=19 |
| g022 | 2 | `profile:god_ascended>=5` | 100.0 | 5 | >=20 |
| g023 | 3 | `profile:god_ascended>=20` | 46.7 | 20 | >=20 |
| g024 | 2 | `NEW:god_act_Wonder>=30` | 100.0 | 3 | >=410 |
| g025 | 4 | `NEW:god_act_Wonder>=100` | 100.0 | 7 | >=430 |
| g026 | 2 | `NEW:god_act_Revelation>=30` | 100.0 | 2 | >=390 |
| g027 | 4 | `NEW:god_act_Revelation>=100` | 100.0 | 6 | >=440 |
| g028 | 2 | `NEW:god_act_Healing>=30` | 100.0 | 3 | >=280 |
| g029 | 4 | `NEW:god_act_Healing>=100` | 100.0 | 7 | >=300 |
| g030 | 2 | `NEW:god_act_Feast>=30` | 46.7 | 13 |  |
| g031 | 4 | `NEW:god_act_Feast>=100` | 0.0 | - | >=62 |
| g032 | 2 | `NEW:god_act_Commune>=30` | 100.0 | 3 | >=340 |
| g033 | 4 | `NEW:god_act_Commune>=100` | 100.0 | 7 | >=360 |
| g034 | 2 | `NEW:god_act_Consecrate>=30` | 0.0 | - | >=17 |
| g035 | 4 | `NEW:god_act_Consecrate>=100` | 0.0 | - | >=21 |
| g036 | 1 | `NEW:god_build_Statue>=1` | 98.3 | 6 | >=5 |
| g037 | 4 | `NEW:god_build_Statue>=10` | 0.0 | - | >=8 |
| g038 | 1 | `NEW:god_build_Temple>=1` | 5.0 | 8 | 목표 도달률을 만들 수 없음(대부분 0) |
| g039 | 4 | `NEW:god_build_Temple>=10` | 0.0 | - | 목표 도달률을 만들 수 없음(대부분 0) |
| g040 | 1 | `NEW:god_build_Library>=1` | 0.0 | - |  |
| g041 | 4 | `NEW:god_build_Library>=10` | 0.0 | - |  |
| g042 | 1 | `NEW:god_build_Village>=1` | 8.3 | 15 | 목표 도달률을 만들 수 없음(대부분 0) |
| g043 | 4 | `NEW:god_build_Village>=10` | 0.0 | - | 목표 도달률을 만들 수 없음(대부분 0) |
| g044 | 1 | `NEW:god_build_Shelter>=1` | 100.0 | 3 | >=9 |
| g045 | 4 | `NEW:god_build_Shelter>=10` | 46.7 | 18 | >=13 |
| g046 | 3 | `NEW:god_Envy>=95` | 0.0 | - | >=78 |
| g047 | 3 | `NEW:god_Envy_low10y>=1` | 40.0 | 3 | >=1 |
| g048 | 3 | `NEW:god_Envy_peak95_ascend` | 0.0 | - |  |
| g049 | 3 | `NEW:god_Suspicion>=95` | 38.3 | 5 | >=100 |
| g050 | 3 | `NEW:god_Suspicion_low10y>=1` | 81.7 | 2 | >=1 |
| g051 | 3 | `NEW:god_Suspicion_peak95_ascend` | 36.7 | 7 |  |
| g052 | 1 | `NEW:god_seized>=1` | 28.3 | 13 | 목표 도달률을 만들 수 없음(대부분 0) |
| g053 | 3 | `NEW:god_seized>=15` | 0.0 | - | >=1 |
| g054 | 1 | `NEW:god_years>=1` | 100.0 | 1 | >=24 |
| g055 | 2 | `NEW:god_years>=10` | 100.0 | 1 | >=24 |
| g056 | 3 | `NEW:god_years>=50` | 0.0 | - | >=26 |
| g057 | 3 | `NEW:god_years>=100` | 0.0 | - | >=26 |
| g058 | 3 | `profile:god_heaven>=40` | 100.0 | 4 | >=260 |
| g059 | 3 | `profile:god_earth>=40` | 100.0 | 5 | >=210 |
| g060 | 3 | `profile:god_indulge>=40` | 100.0 | 2 | >=390 |
| g061 | 3 | `profile:god_vessel>=40` | 100.0 | 5 | >=180 |
| g062 | 3 | `profile:god_random>=40` | 100.0 | 6 | >=160 |
| g063 | 2 | `profile:god_events>=100` | 100.0 | 2 | >=1100 |
| g064 | 3 | `profile:god_events>=200` | 100.0 | 4 | >=1200 |
| g065 | 2 | `NEW:god_faith_points>=1000` | 100.0 | 10 | >=2200 |
| g066 | 3 | `NEW:god_faith_points>=10000` | 0.0 | - | >=2300 |
| g067 | 2 | `NEW:god_ascend_years<=5` | 0.0 | - | <=11 |
| g068 | 2 | `NEW:god_ascend_years>=50` | 0.0 | - | >=24 |
| g069 | 2 | `NEW:god_ascend_buildings==0` | 100.0 | 1 |  |
| g070 | 2 | `NEW:god_ascend_Wonder==0` | 0.0 | - |  |
| g071 | 2 | `NEW:god_ascend_Healing==0` | 95.0 | 5 |  |
| g072 | 2 | `NEW:god_ascend_single_branch` | 0.0 | - |  |
| g073 | 2 | `NEW:god_ascend_all_branches>=20` | 100.0 | 1 | >=31 |
| g074 | 2 | `NEW:god_ascend_Envy>=95` | 0.0 | - | >=23 |
| g075 | 2 | `NEW:god_ascend_both>=90` | 0.0 | - | >=9 |
| g076 | 2 | `NEW:god_ascend_both_max<=20` | 100.0 | 1 | <=0 |
| g077 | 2 | `NEW:god_ascend_points>=1000` | 0.0 | - | >=14 |
| g078 | 2 | `NEW:god_ascend_points==0` | 95.0 | 4 |  |
| g079 | 2 | `NEW:god_ascend_no_vessel_recovery` | 100.0 | 1 |  |
| g080 | 2 | `NEW:god_ascend_seized==0` | 100.0 | 1 |  |
| g081 | 2 | `NEW:god_ascend_indulge==0` | 0.0 | - |  |
| g082 | 2 | `NEW:god_all_actions` | 100.0 | 5 |  |
| g083 | 2 | `NEW:god_all_buildings` | 0.0 | - |  |
| g084 | 2 | `NEW:god_all_event_kinds` | 100.0 | 1 |  |
| g085 | 2 | `NEW:dissolved_then_ascended` | 0.0 | - |  |
| g086 | 2 | `profile:god_runs>=10` | 100.0 | 10 | >=20 |
| g087 | 2 | `NEW:god_vessel_perished` | 0.0 | - |  |
| g088 | 2 | `NEW:god_ascend_after_fail` | 53.3 | 8 |  |
| g089 | 2 | `NEW:god_consecrated>=10` | 86.7 | 15 | >=17 |
| g090 | 3 | `NEW:god_faith_points>=50000` | 0.0 | - | >=2300 |
| g091 | 1 | `NEW:god_vessel_recovery>=1` | 100.0 | 2 | >=19 |
| g092 | 2 | `NEW:god_vessel_recovery>=10` | 85.0 | 9 | >=34 |
| g093 | 3 | `NEW:god_vessel_recovery>=30` | 36.7 | 16 | >=45 |

### mirror

| id | t | 조건 | 플레이어% | 첫판 | 자동 제안 |
|---|---|---|---|---|---|
| m001 | 1 | `NEW:mirror_ending_inv_end_followers` | 48.3 | 1 |  |
| m002 | 1 | `NEW:mirror_ending_inv_end_absorbed` | 0.0 | - |  |
| m003 | 1 | `NEW:mirror_ending_inv_end_old_age` | 16.7 | 15 |  |
| m004 | 3 | `NEW:mirror_ending_inv_end_final_lust10a_good` | 30.0 | 8 |  |
| m005 | 3 | `NEW:mirror_ending_inv_end_final_lust10a_bad` | 46.7 | 8 |  |
| m006 | 3 | `NEW:mirror_ending_inv_end_final_lust10b_good` | 35.0 | 10 |  |
| m007 | 3 | `NEW:mirror_ending_inv_end_final_lust10b_bad` | 41.7 | 5 |  |
| m008 | 3 | `NEW:mirror_ending_inv_end_final_greed10a_good` | 35.0 | 13 |  |
| m009 | 3 | `NEW:mirror_ending_inv_end_final_greed10a_bad` | 33.3 | 11 |  |
| m010 | 3 | `NEW:mirror_ending_inv_end_final_greed10b_good` | 26.7 | 9 |  |
| m011 | 3 | `NEW:mirror_ending_inv_end_final_greed10b_bad` | 36.7 | 10 |  |
| m012 | 3 | `NEW:mirror_ending_inv_end_final_appetite10a_good` | 50.0 | 10 |  |
| m013 | 3 | `NEW:mirror_ending_inv_end_final_appetite10a_bad` | 36.7 | 10 |  |
| m014 | 3 | `NEW:mirror_ending_inv_end_final_appetite10b_good` | 43.3 | 9 |  |
| m015 | 3 | `NEW:mirror_ending_inv_end_final_appetite10b_bad` | 40.0 | 11 |  |
| m016 | 3 | `NEW:mirror_ending_inv_end_final_ambition10a_good` | 33.3 | 8 |  |
| m017 | 3 | `NEW:mirror_ending_inv_end_final_ambition10a_bad` | 40.0 | 9 |  |
| m018 | 3 | `NEW:mirror_ending_inv_end_final_ambition10b_good` | 43.3 | 9 |  |
| m019 | 3 | `NEW:mirror_ending_inv_end_final_ambition10b_bad` | 40.0 | 9 |  |
| m020 | 3 | `NEW:mirror_ending_inv_end_final_temperance10a_good` | 40.0 | 10 |  |
| m021 | 3 | `NEW:mirror_ending_inv_end_final_temperance10a_bad` | 33.3 | 11 |  |
| m022 | 3 | `NEW:mirror_ending_inv_end_final_temperance10b_good` | 40.0 | 10 |  |
| m023 | 3 | `NEW:mirror_ending_inv_end_final_temperance10b_bad` | 46.7 | 10 |  |
| m024 | 3 | `NEW:mirror_ending_inv_end_final_charity10a_good` | 41.7 | 6 |  |
| m025 | 3 | `NEW:mirror_ending_inv_end_final_charity10a_bad` | 35.0 | 8 |  |
| m026 | 3 | `NEW:mirror_ending_inv_end_final_charity10b_good` | 45.0 | 6 |  |
| m027 | 3 | `NEW:mirror_ending_inv_end_final_charity10b_bad` | 36.7 | 13 |  |
| m028 | 3 | `NEW:mirror_ending_inv_end_final_patience10a_good` | 40.0 | 12 |  |
| m029 | 3 | `NEW:mirror_ending_inv_end_final_patience10a_bad` | 38.3 | 7 |  |
| m030 | 3 | `NEW:mirror_ending_inv_end_final_patience10b_good` | 40.0 | 9 |  |
| m031 | 3 | `NEW:mirror_ending_inv_end_final_patience10b_bad` | 33.3 | 12 |  |
| m032 | 3 | `NEW:mirror_ending_inv_end_final_integrity10a_good` | 0.0 | - |  |
| m033 | 3 | `NEW:mirror_ending_inv_end_final_integrity10a_bad` | 6.7 | 13 |  |
| m034 | 3 | `NEW:mirror_ending_inv_end_final_integrity10b_good` | 0.0 | - |  |
| m035 | 3 | `NEW:mirror_ending_inv_end_final_integrity10b_bad` | 1.7 | 18 |  |
| m036 | 1 | `NEW:mirror_ending_inv_end_desire_good` | 51.7 | 7 |  |
| m037 | 1 | `NEW:mirror_ending_inv_end_desire_bad` | 8.3 | 5 |  |
| m038 | 1 | `NEW:mirror_ending_inv_end_virtue_good` | 55.0 | 5 |  |
| m039 | 1 | `NEW:mirror_ending_inv_end_virtue_bad` | 23.3 | 3 |  |
| m040 | 1 | `NEW:mirror_ending_inv_end_handover` | 0.0 | - |  |
| m041 | 1 | `NEW:mirror_ending_inv_end_master_death` | 0.0 | - |  |
| m042 | 3 | `NEW:mirror_doctrine_lust>=10` | 61.7 | 4 | >=10 |
| m043 | 3 | `NEW:mirror_doctrine_greed>=10` | 63.3 | 3 | >=10 |
| m044 | 3 | `NEW:mirror_doctrine_appetite>=10` | 66.7 | 5 | >=10 |
| m045 | 3 | `NEW:mirror_doctrine_ambition>=10` | 66.7 | 5 | >=10 |
| m046 | 3 | `NEW:mirror_doctrine_temperance>=10` | 65.0 | 4 | >=10 |
| m047 | 3 | `NEW:mirror_doctrine_charity>=10` | 63.3 | 3 | >=10 |
| m048 | 3 | `NEW:mirror_doctrine_patience>=10` | 63.3 | 3 | >=10 |
| m049 | 3 | `NEW:mirror_doctrine_integrity>=10` | 60.0 | 4 | >=10 |
| m050 | 2 | `NEW:mirror_doctrines>=60` | 0.0 | - | >=10 |
| m051 | 3 | `NEW:mirror_doctrines>=60` | 0.0 | - | >=10 |
| m052 | 1 | `NEW:mirror_cultyears>=1` | 100.0 | 1 | >=25 |
| m053 | 1 | `NEW:mirror_cultyears>=5` | 100.0 | 1 | >=25 |
| m054 | 2 | `NEW:mirror_cultyears>=10` | 100.0 | 1 | >=25 |
| m055 | 3 | `NEW:mirror_cultyears>=25` | 78.3 | 3 | >=25 |
| m056 | 3 | `NEW:mirror_cultyears>=40` | 0.0 | - | >=25 |
| m057 | 1 | `NEW:mirror_followers>=50` | 100.0 | 2 | >=150 |
| m058 | 2 | `NEW:mirror_followers>=300` | 0.0 | - | >=180 |
| m059 | 3 | `NEW:mirror_followers>=1000` | 0.0 | - | >=200 |
| m060 | 1 | `NEW:mirror_Reputation>=400` | 100.0 | 1 | >=660 |
| m061 | 2 | `NEW:mirror_Reputation>=800` | 26.7 | 15 |  |
| m062 | 2 | `NEW:mirror_recruits_year>=10` | 58.3 | 7 | >=12 |
| m063 | 2 | `NEW:mirror_recruits_year<=-6` | 0.0 | - | <=-1 |
| m064 | 2 | `NEW:mirror_rep_near_400>=5` | 100.0 | 2 | >=64 |
| m065 | 1 | `NEW:mirror_Offerings>=300` | 98.3 | 5 | >=1000 |
| m066 | 2 | `NEW:mirror_Offerings>=800` | 70.0 | 4 | >=1000 |
| m067 | 2 | `NEW:mirror_chain_all>=300` | 0.0 | - | >=180 |
| m068 | 1 | `NEW:mirror_misfortune>=4` | 100.0 | 1 | >=7 |
| m069 | 2 | `NEW:mirror_misfortune>=8` | 60.0 | 1 | >=8 |
| m070 | 3 | `NEW:mirror_misfortune>=10` | 3.3 | 3 |  |
| m071 | 3 | `NEW:mirror_misfortune>=8 mirror_followers>=500` | 0.0 | - |  |
| m072 | 3 | `NEW:mirror_misfortune_low_years>=10` | 100.0 | 2 | >=180 |
| m073 | 2 | `NEW:mirror_misfortune_high_years>=10` | 33.3 | 2 |  |
| m074 | 1 | `NEW:mirror_fail_acts>=1` | 100.0 | 1 | >=66 |
| m075 | 1 | `NEW:mirror_fail_acts>=10` | 100.0 | 2 | >=66 |
| m076 | 2 | `NEW:mirror_fail_acts>=60` | 71.7 | 12 | >=83 |
| m077 | 3 | `NEW:mirror_gap_desire>=40` | 66.7 | 8 | >=56 |
| m078 | 3 | `NEW:mirror_gap_virtue>=40` | 61.7 | 7 | >=70 |
| m079 | 3 | `NEW:mirror_gap<=5` | 93.3 | 5 | <=0 |
| m080 | 2 | `NEW:mirror_desire_lead_y25` | 53.3 | 7 |  |
| m081 | 2 | `NEW:mirror_virtue_lead_y25` | 65.0 | 3 |  |
| m082 | 2 | `NEW:mirror_lead_swaps>=3` | 20.0 | 12 |  |
| m083 | 1 | `profile:mirror_runs>=1` | 100.0 | 1 | >=20 |
| m084 | 2 | `profile:mirror_runs>=5` | 100.0 | 5 | >=20 |
| m085 | 3 | `profile:mirror_runs>=20` | 100.0 | 20 | >=20 |
| m086 | 1 | `profile:mirror_collapse>=1` | 48.3 | 1 |  |
| m087 | 2 | `profile:mirror_collapse>=5` | 0.0 | - | >=1 |
| m088 | 1 | `profile:mirror_final>=1` | 100.0 | 1 | >=16 |
| m089 | 2 | `profile:mirror_final>=5` | 100.0 | 6 | >=18 |
| m090 | 3 | `profile:mirror_final>=10` | 100.0 | 12 | >=19 |
| m091 | 3 | `profile:mirror_mr001>=40` | 0.0 | - |  |
| m092 | 3 | `profile:mirror_mr051>=40` | 0.0 | - |  |
| m093 | 3 | `profile:mirror_mr101>=40` | 0.0 | - |  |
| m094 | 3 | `profile:mirror_mr151>=40` | 0.0 | - |  |
| m095 | 3 | `profile:mirror_recovery>=40` | 0.0 | - |  |
| m096 | 3 | `profile:mirror_special>=40` | 0.0 | - |  |
| m097 | 3 | `profile:mirror_final_events>=40` | 0.0 | - |  |
| m098 | 2 | `profile:mirror_events>=200` | 0.0 | - |  |
| m099 | 3 | `profile:mirror_events>=351` | 0.0 | - |  |
| m100 | 2 | `NEW:mirror_achv>=100` | 0.0 | - | >=50 |
| m101 | 3 | `NEW:mirror_final_all16>=16` | 0.0 | - | >=12 |
| m102 | 3 | `NEW:mirror_final_good16>=16` | 0.0 | - | >=7 |
| m103 | 3 | `NEW:mirror_final_bad16>=16` | 0.0 | - | >=8 |
| m104 | 3 | `NEW:mirror_endings_seen>=41` | 0.0 | - | >=16 |
| m105 | 2 | `NEW:mirror_cultyears>=25` | 78.3 | 3 | >=25 |
| m106 | 2 | `NEW:mirror_followers_low_recover` | 50.0 | 3 |  |
| m107 | 2 | `NEW:mirror_followers_5_to_500` | 0.0 | - |  |
| m108 | 2 | `NEW:mirror_grace_no_fail` | 100.0 | 4 |  |
| m109 | 2 | `NEW:mirror_grace_followers>=100` | 0.0 | - | >=45 |
| m110 | 2 | `NEW:mirror_doctrine_points>=40` | 100.0 | 1 | >=160 |
| m111 | 2 | `NEW:mirror_two_branches>=10` | 0.0 | - | 목표 도달률을 만들 수 없음(대부분 0) |
| m112 | 2 | `NEW:mirror_four_branches>=5` | 0.0 | - | 목표 도달률을 만들 수 없음(대부분 0) |
| m113 | 2 | `NEW:mirror_all_branches>=1` | 0.0 | - | 목표 도달률을 만들 수 없음(대부분 0) |
| m114 | 2 | `NEW:mirror_single_branch` | 100.0 | 1 |  |
| m115 | 2 | `NEW:mirror_switch_final` | 0.0 | - |  |
| m116 | 2 | `NEW:mirror_normal_same_final` | 0.0 | - |  |
| m117 | 2 | `NEW:mirror_chain_all>=900` | 0.0 | - | >=180 |
| m118 | 2 | `NEW:mirror_achv>=150` | 0.0 | - | >=50 |
| m119 | 2 | `NEW:mirror_achv>=118` | 0.0 | - | >=50 |

### hidden

| id | t | 조건 | 일반% | 신% | 거울% | 제안(일반 기준) |
|---|---|---|---|---|---|---|
| h001 | 3 | `hidden_clear_modes>=3` | 0 | 0 | 0 | >=1 |
| h002 | 3 | `ending_total>=30` | 0 | 0 | 0 | >=20 |
| h003 | 3 | `both_endings_all8` | 0 | 0 | 0 |  |
| h004 | 3 | `all_endings_seen` | 0 | 0 | 0 |  |
| h005 | 3 | `clear_no_fail` | 26 | 0 | 8 |  |
| h006 | 3 | `desire_avg_peak>=60` | 100 | 0 | 90 | >=87 |
| h007 | 3 | `virtue_avg_peak>=60` | 100 | 0 | 87 | >=88 |
| h008 | 3 | `desire_avg_peak>=90` | 5 | 0 | 0 |  |
| h009 | 3 | `virtue_avg_peak>=90` | 1 | 0 | 0 |  |
| h010 | 3 | `dual_avg_peak>=80` | 0 | 0 | 0 | >=72 |
| h011 | 3 | `gifted_total>=100` | 100 | 0 | 100 | >=310 |
| h012 | 3 | `gifted_total>=300` | 14 | 0 | 0 | >=310 |
| h013 | 3 | `gifted_total>=1000` | 0 | 0 | 0 | >=310 |
| h014 | 3 | `gifted_kinds_seen>=140` | 0 | 0 | 0 | >=31 |
| h015 | 3 | `gifted_run_peak>=40` | 0 | 0 | 0 | >=21 |

## 재시뮬레이션 (2026-10-25, 공유형 기프티드 반감 후, 스타일당 20명×20판)
원자료 `Temp/ach_sim_{normal,god,mirror}.txt`의 "기준치 제안" 절. 수치는 아직 적용 안 함.
- 기프티드 영향(전부 vs 없음): 일반 최종스킬 79/72%, 신 패배 1.5/12%, 거울 붕괴 0/9.5%.
- 너무 쉬움(도달 90%↑): 신 노드·행동·갈래·압력 누적(g001~g010, g024~g033, g058~g065), 거울 교단 연수·신도·헌금(m052~m066), 일반 교리·연수·이벤트 누적(n146~n151, n202~n204).
- 너무 어려움(0%): 일반 행동 숙련(n101~n118), 기프티드 개수(n166~n172), 나이 80·100, 거울 교리(m042~m051), 신 연수 50·100.
- 수치가 아니라 기록 누락: n196~n198(성인, 봇 미지원), g040·g041(도서관), m091~m099(거울 이벤트 누적).
- 20판 한계로 판단 불가: 회차·엔딩·업적 누적(runs>=30, endings_seen>=30, achv>=200, god_ascended>=20 등).
