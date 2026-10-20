# v2 기프티드 효과 프레임워크

코드: `Assets/Scripts/Talent/GiftedV2Effects.cs`(훅·보유 규칙), `GiftedV2Table.cs`(등록표, 예시 3개), 테스트 `Assets/Editor/Tests/GiftedV2EffectsTests.cs`.
데이터: `Docs/gifted_v2.csv` → `GiftedV2Catalog.cs`.

## 보유·적용 규칙
- **보유**: `LinkedAchievement`를 프로필에서 달성했으면 보유 (`Achievements.OwnsGiftedV2`). 회차를 넘어 유지, 달성한 판에도 바로 효과. TalentList에는 넣지 않는다.
- 보상 끔(`Profile.RewardsEnabled == false`) 또는 `Profile.Enabled == false`(에디터 테스트·밸런스 시뮬)면 아무것도 보유하지 않음 → 시뮬 결과 불변.
- **적용**: `Shared` → 모든 모드, 아니면 자기 `Mode`(`GameSession` 플래그로 정함)에서만. `AdultGated` → `AdultGate.AchievementsEnabled`일 때만 (테스트는 `GiftedV2Effects.AdultEnabled`를 바꿈).
- 보유 없음 = 모든 훅 무변화(배율 1, 보너스 0, false), 난수도 쓰지 않음.

## 등록 패턴
```csharp
GiftedV2Effects.Register("v2m107", new EffectDef { LossPercent = 20, StatWhen = c => c.Cult == StatId.Reputation });
```
- 모드별 구현은 partial 파일 `GiftedV2Table.Normal.cs / .God.cs / .Mirror.cs`에서 `static partial void RegisterNormal() { ... }` 등을 구현. 예시 3개(v2n001, v2g066, v2m107)는 `GiftedV2Table.cs`에 있음 — 중복 등록은 예외이므로 옮기면 거기서 지울 것.
- 상태(연속 실패·누적)는 `Counters` run 범위(`perRun: true`), 다음 판에 넘길 값은 profile 범위에 저장.
- 수치는 상수로 두고 테스트에서 상수를 참조.

## 훅 목록

| 약칭 | 시그니처 | EffectDef 칸 | 부르는 곳 |
|---|---|---|---|
| CHK | `float SuccessChanceBonus(CheckContext c)` | `CheckBonus`+`CheckWhen`, `CheckBonusFn` | `Turns.Life`(→`LifeActions.Roll` extraBonus), `Turns.Cult`(→`CultActions.Roll` extraBonus), `Turns.Answer`(→`Choice.Roll` penalty에서 뺌) |
| RES | `bool ResolveCheck(CheckContext c, bool success)` | `RescueFailure` | 위 3곳, 굴린 직후 |
| ONCHK | (ResolveCheck 안) | `OnCheck(c, finalSuccess)` | 위 3곳 |
| STAT | `float StatMultiplier(StatChange c)` (+ `StatId`/`PersonalStatId` 오버로드) | `GainPercent`, `LossPercent`, `StatWhen`, `StatMultiplierFn` | `GiftedEffects.Apply` 두 오버로드 (행동·이벤트 효과, 신의 기적 그릇 비용) |
| SRC | `Scope During(ChangeSource s, string key)` | — | `Turns.Life/Cult`(Action, 행동 키), `Turns.Answer`(Event, 이벤트 id) |
| DEATH | `bool TryPreventDeath()` | `PreventDeath` | `GameSession.AdvanceTurn` 연령 사망 판정 |
| END | `Endings.Ending FilterEnding(Endings.Ending e)` | `PreventEnding` | `Progress.ForcedEnding`, `Progress.DueEnding` (3모드 공통) |
| ENDED | `void OnEnding(Endings.Ending e)` | `OnEnding` | `Progress.Reach` (Endings.Reach 전) |
| RUN | `void OnRunStart()` | `OnRunStart` | `Achievements.ApplyStartRewards` 끝(일반·거울), `GodRules.Begin` 끝(신) |
| YEAR | `void OnYearEnd()` | `OnYearEnd` | `GameSession.AdvanceTurn`, `Age.Quarter == 0` |
| TOK | `bool TryUseToken(string id, int perRun = 1)`, `int TokensUsed(id)` | — | 효과 안에서 (키 `gv2used:<id>`, Counters run 범위) |
| UI | `bool IsActive(string id, string mode = null)`, `IEnumerable<Entry> Owned(string mode = null)` | — | UI·디버그 |

- `CheckContext`: `Kind`(Life/Cult/Event), `Key`(LifeAction 이름 / `CultActions.Key` — 신 모드 권능 행동은 `GodRules.ActionOf`: Proselytize=Wonder, Preach=Revelation, Serve=Healing, Desire=Feast, CultPray=Commune / 이벤트 id, 없으면 null).
- `StatChange`: `Source`(Other/Action/Event), `Key`, `Cult` 또는 `Personal`, `Amount`, `IsGain`. 증가 ×(1+ΣGain%), 감소 ×(1−ΣLoss%), 그 뒤 Fn 곱.
- shortcut: run 토큰·카운터는 세이브에 안 들어감(이어하기 시 초기화), 등록 항목은 호출마다 훑음(120개 이하).

## 120개 대응표
표기: **WIRE** = 훅은 있으나 그 수치가 지나는 경로가 아직 `GiftedEffects.Apply`/훅을 안 거침(해당 코드에 `StatMultiplier` 등 한 줄 연결 필요). **NEW** = 새 훅 필요.

| id | 효과 | 훅 |
|---|---|---|
| v2n001 | 실패 25% → 성공 | RES (**예시 구현**) |
| v2n002 | 치명적 사망 1회 방지 | DEATH + TOK |
| v2n003 | 이벤트 보상 +5% | STAT (Source=Event) |
| v2n004 | 위기 이벤트에 구원 선택지 | **NEW:EventChoice** (선택지 주입) |
| v2n005 | 노화 체력 하락 절반 | **NEW** — 노화 스탯 하락 로직이 게임에 없음 |
| v2n006 | 기프티드 획득 확률 +10% | **NEW:GiftedGrant** (`Outcome.Grants`/GiftedGranter) |
| v2n007 | 불운·압박 증가 −15% | **NEW:Heat** (`GameSession.AddMisfortune`, `GodPressure.AddHeat`) |
| v2n008 | 성공 시 5% 보상 2배 | ONCHK + STAT(Fn) |
| v2n009 | 실패 재굴림 3회/판 | RES + TOK, **NEW:CheckContext.Chance** (재굴림 확률 필요) |
| v2n010 | 엔딩 직전 선택 되돌리기 | **NEW:Undo** (UI·스냅샷) |
| v2n011 | 운동 성공 시 체력 +1 | ONCHK (Life/Exercise 성공 시 직접 +1) |
| v2n012 | 명상 성공 +8% | CHK (Life/Meditate) |
| v2n013 | 공부 성공 +8% | CHK (Life/Study) |
| v2n014 | 사교 성공 +8% | CHK (Life/Socialize) |
| v2n015 | 기도 효과 +20% | STAT (Action Pray/CultPray, 증가) |
| v2n016 | 포교 신도 +15% | STAT (Action Proselytize, Followers) |
| v2n017 | 설교 신도 신앙 +15% | STAT (Action Preach, FollowerFaith) |
| v2n018 | 봉사 명망 +15% | STAT (Action Serve, Reputation) |
| v2n019 | 욕구 의식 헌금 +15% | STAT (Action Desire, Offerings) |
| v2n020 | 합동 기도 교주 신앙 +15% | STAT (Action CultPray, LeaderFaith) |
| v2n021 | 봉헌 효과 +20% | STAT — 대상 행동 불명확(확인 필요) |
| v2n022 | 권위 하락 −20% | STAT (Authority 감소) |
| v2n023 | 헌금 하락 −20% | STAT (Offerings 감소) |
| v2n024 | 신도 이탈 확률 −20% | STAT + **WIRE** (`Misfortune.ApplyTurn`, `SocialStanding`) |
| v2n025 | 교리 비용 −10% | **NEW:Cost** (`DoctrineTree` 비용) |
| v2n026 | 시선 하락 속도 절반 | **WIRE** (`SocialStanding.ApplyTurn`) |
| v2n027 | 탄압 단계 교단 스탯 하락 −25% | STAT(Fn: 시선 단계) + **WIRE** (`SocialStanding`) |
| v2n028 | 재산 증가 +15% | STAT (Wealth 증가) |
| v2n029 | 재산 0이면 경건 +3% | STAT (StatWhen: Wealth==0, Piety) |
| v2n030 | 뇌물 거절 시 청렴 +2 | ONCHK + **NEW:CheckContext.Choice** (고른 선택지 필요) |
| v2n031 | 욕망·미덕 평균 차가 작을수록 전 스탯 최대 +3% | STAT(Fn) |
| v2n032 | 욕망/미덕 평균 80+ 그 그룹 +5% | STAT(Fn) |
| v2n033 | 욕망·미덕 스탯 하락 −15% | STAT |
| v2n034 | 성인 이벤트 보상 +10% (성인 게이트) | STAT (Event, AdultEvents id) |
| v2n035 | 노화로 평판 하락 없음 | **NEW** — 노화 하락 로직 없음 |
| v2n036 | 교단 5년마다 교리 포인트 +1 | YEAR + **NEW:Points** (교리 포인트 지급 API 확인) |
| v2n037 | 해체 직전 명망 1회 회복 | END (Dissolved) + TOK |
| v2n038 | 시작 시 개인 스탯 +2 | RUN |
| v2n039 | 사망 직전 마지막 설교 신도 +10% | ENDED (사망 엔딩) |
| v2n040 | 새 엔딩 시 다음 판 개인 스탯 +1 | ENDED + RUN (profile 카운터) |
| v2g041 | 1년 휴식 시 불운·압박 −2 | **NEW:Heat** + **NEW:Rest** |
| v2g042 | 매년 전 스탯 +1% | YEAR |
| v2g043 | 3연속 실패 뒤 다음 판정 +20% | ONCHK + CHK(Fn) |
| v2g044 | 선택지 성공 확률 표시 | UI |
| v2g045 | 같은 행동 2연속 성공 보상 +10% | ONCHK + STAT(Fn) |
| v2g046 | 엔딩 진행도 표시 | UI |
| v2g047 | 처음 겪는 이벤트마다 +1 포인트 | ONCHK (Event, profile 카운터) |
| v2g048 | 같은 이벤트 재등장 절반 | **NEW:EventPick** |
| v2g049 | 판 첫 실패 무효 | RES + TOK |
| v2g050 | 모든 증가 +3% | STAT |
| v2g051 | 기적 성공 +10% | CHK (Cult/Proselytize, 신) |
| v2g052 | 계시 효과 +15% | STAT (Action Preach) |
| v2g053 | 치유 효과 +15% | STAT (Action Serve) |
| v2g054 | 연회 신앙 포인트 +15% | **NEW:Points** (권능 포인트 획득) |
| v2g055 | 교감 성공 +10% | CHK (Cult/CultPray) |
| v2g056 | 축성 비용 −15% | **NEW:Cost** (`GodBuildings.Build`) |
| v2g057 | 신상 효과 +20% | **WIRE** (`GodBuildings` 효과 경로) |
| v2g058 | 신전 효과 +20% | **WIRE** (`GodBuildings`) |
| v2g059 | 도서관 효과 +20% | **WIRE** (`GodBuildings`) |
| v2g060 | 마을 효과 +20% | **WIRE** (`GodBuildings`) |
| v2g061 | 쉼터 효과 +20% | **WIRE** (`GodBuildings`) |
| v2g062 | 천계 질투 증가 −20% | **NEW:Heat** |
| v2g063 | 인간 의심 증가 −20% | **NEW:Heat** |
| v2g064 | 그릇 빼앗김 확률 −20% | **NEW** (`GodRules` 그릇 판정) |
| v2g065 | 그릇 회복 속도 +25% | **WIRE** (`GodRules` 회복 AddWithRemainder) |
| v2g066 | 매년 신앙(권능) 포인트 +2 | YEAR (**예시 구현**, `God.Points.Add`) |
| v2g067 | 기적 갈래 노드 비용 −10% | **NEW:Cost** (`GodTree`) |
| v2g068 | 예언 갈래 노드 비용 −10% | **NEW:Cost** |
| v2g069 | 자비 갈래 노드 비용 −10% | **NEW:Cost** |
| v2g070 | 욕망 갈래 노드 비용 −10% | **NEW:Cost** |
| v2g071 | 천계 이벤트 유리 선택 +10% | CHK (Event, 천계 이벤트 id) |
| v2g072 | 지상 이벤트 유리 선택 +10% | CHK (Event, 지상 이벤트 id) |
| v2g073 | 방종 이벤트 압박 −25% | **NEW:Heat** |
| v2g074 | 우연 이벤트 보상 +15% | STAT (Event, 무작위 이벤트 id) |
| v2g075 | 그릇 이벤트 실패 피해 −20% | ONCHK + STAT(Fn) |
| v2g076 | 두 핵심 개방 시 승천 조건 완화 | **NEW** (`GodRules.DueEnding` 문턱) |
| v2g077 | 신으로 25년마다 노드 포인트 +1 | YEAR |
| v2g078 | 건물 비용 −10% | **NEW:Cost** |
| v2g079 | 축성 신전 보유 시 압박 −10% | **NEW:Heat** |
| v2g080 | 승천 직전 압박 절반 | **NEW:Heat** |
| v2m081 | 성공 시 10% 턴 소모 없이 추가 행동 | **NEW:ExtraAction** (CultFlow·AdvanceTurn) |
| v2m082 | 스탯 0 하락 1회 방지 | STAT(Fn, 현재값 읽기) + TOK (Apply 경로만, 나머지 **WIRE**) |
| v2m083 | 패배 선택 피해 −20% | ONCHK + STAT(Fn) |
| v2m084 | 휴식 효과 +25% | **WIRE** (`Rest.ApplyTurn`) |
| v2m085 | 모든 쇠퇴 효과 −10% | **WIRE** (`Misfortune.ApplyTurn`, `SocialStanding`) |
| v2m086 | 선택지 스탯 변화 미리 보기 | UI |
| v2m087 | 실패 5번마다 다음 판정 확정 성공 | ONCHK + CHK(Fn) |
| v2m088 | 연말 정산 보너스 +5% | **WIRE** (`SocialStanding.YearEnd`) |
| v2m089 | 1년 1회 이벤트 다시 굴리기 | **NEW:EventPick** + UI |
| v2m090 | 같은 행동 연속 효과 +5% (3중첩) | ONCHK + STAT(Fn) |
| v2m091 | 모집 음수인 해 손실 −2 | **WIRE** (`SocialStanding.YearEnd`) |
| v2m092 | 신도→명망 환산 +10% | **NEW** (`CultScore`) |
| v2m093 | 명망→헌금 환산 +10% | **NEW** (`CultScore`) |
| v2m094 | 불운 증가 −20% | **NEW:Heat** |
| v2m095 | 불운 6+면 매년 −1 | YEAR + **NEW** (불운 감소 API, `GameSession.Misfortune`은 private set) |
| v2m096 | 유예 기간 +4턴 | **NEW** (`MirrorRules` 유예 상수) |
| v2m097 | 연말 욕망·미덕 차 10 이하면 불운 −1 | YEAR + 불운 감소 API(위와 같음) |
| v2m098 | 욕망 평균 60+ 연간 모집 +1 | **WIRE** (`SocialStanding.YearEnd`) |
| v2m099 | 미덕 평균 60+ 불운 증가 −25% | **NEW:Heat** |
| v2m100 | 신도 0이 되면 1회 10명 복구 | TOK + **NEW:TurnEnd** (또는 YEAR로 근사) |
| v2m101 | 명망 400+ 연간 모집 +1 | **WIRE** (`SocialStanding.YearEnd`) |
| v2m102 | 교단 연수마다 교리 포인트 +1 | YEAR + **NEW:Points** |
| v2m103 | 최종 스킬 선행 조건 1개 면제 | **NEW** (`DoctrineTree.PrerequisitesMet`) |
| v2m104 | 교리 포인트 획득 +10% | **NEW:Points** |
| v2m105 | 한 갈래 교리 효과 +15% | **NEW:DoctrineScale** (`TalentList.ExtraEffects` 퍼센트) |
| v2m106 | 두 갈래 각 효과 +5% | **NEW:DoctrineScale** |
| v2m107 | 명망 하락 −20% | STAT (**예시 구현**) |
| v2m108 | 헌금 300 이하일 때 헌금 하락 −50% | STAT (StatWhen: Offerings≤300) |
| v2m109 | 신도 이탈 −20% | STAT + **WIRE** (쇠퇴 경로) |
| v2m110 | 회복 이벤트 효과 +25% | STAT (Event, 회복 이벤트 id) |
| v2m111 | 특수 이벤트 보상 +15% | STAT (Event, 특수 이벤트 id) |
| v2m112 | 교단 20년 이후 모든 보상 +5% | STAT(StatWhen: CultTime) |
| v2m113 | 교단 5년 이전 모든 보상 +10% | STAT(StatWhen: CultTime) |
| v2m114 | 욕망·미덕 평균 50+ 보상 +5% | STAT(Fn) |
| v2m115 | 불운 6+ 헌금 +15% | STAT (StatWhen: Misfortune≥6, Offerings 증가) |
| v2m116 | 일반 모드 최종 스킬 경험 시 시작 보너스 | RUN |
| v2m117 | 실패 10번마다 스탯 +1 | ONCHK |
| v2m118 | 교단 30년 이후 불운 증가 없음 | **NEW:Heat** |
| v2m119 | 새 거울 엔딩 시 다음 판 교리 포인트 +2 | ENDED + RUN + **NEW:Points** |
| v2m120 | 붕괴 엔딩 직전 1회 존속 | END + TOK |

## 새 훅 필요 요약
- **NEW:Heat** (불운·압박 증가 배율/차단): v2n007, v2g041, v2g062, v2g063, v2g073, v2g079, v2g080, v2m094, v2m099, v2m118 (+ 불운 감소 API: v2m095, v2m097)
- **NEW:Cost**: v2n025, v2g056, v2g067~070, v2g078
- **NEW:Points** (교리·권능 포인트 획득): v2n036, v2g054, v2m102, v2m104, v2m119
- **NEW:EventPick / EventChoice**: v2n004, v2g048, v2m089
- **NEW:CheckContext.Chance / .Choice**: v2n009, v2n030
- **NEW:DoctrineScale**: v2m105, v2m106 · **CultScore 환산**: v2m092, v2m093
- 개별 규칙 변경: v2n006, v2n010, v2g064, v2g076, v2m081, v2m096, v2m100, v2m103 · 대상 로직 없음: v2n005, v2n035 · 대상 불명확: v2n021
- **WIRE** (쇠퇴·연말·휴식·건물 경로에 `StatMultiplier` 연결): v2n024, v2n026, v2n027, v2g057~061, v2g065, v2m082, v2m084, v2m085, v2m088, v2m091, v2m098, v2m101, v2m109
- UI 전용(`IsActive`): v2g044, v2g046, v2m086
