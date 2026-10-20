# 거울 세계 이벤트 작성 규칙 (에이전트 공통)

## 세계관
- 미덕과 욕망이 뒤집힌 세계. **욕망(색욕 Lust·탐욕 Greed·식욕 Appetite·야망 Ambition)은 칭송받고, 미덕(절제 Temperance·자애 Charity·인내 Patience·청렴 Integrity)은 멍청한 짓으로 폄하**된다.
- 사람들은 절제를 비웃고, 탐욕을 성공으로, 정직을 순진함으로 본다. 교주(플레이어)는 이 세상에서 "욕망을 가르치는 선생"이 될 수도, 조롱받는 "수행자"가 될 수도 있다.
- 효과도 이 설정을 따른다: 욕망 쪽 선택은 대체로 평판(Reputation)·신도(Followers)·헌금(Offerings)을 올리고, 미덕 쪽 선택은 평판을 깎거나 비웃음을 사지만 개인 미덕 스탯·신앙(LeaderFaith) 등 다른 보상이 있을 수 있다. 무조건 한쪽만 유리하게 만들지 말고 대가와 위험을 섞는다.
- 성인 콘텐츠는 기본 포함 게임이지만, 이 일반 이벤트들은 노골적 성묘사 없이 암시 수준으로 쓴다 (성인 전용 이벤트는 따로 있음). 등장인물은 모두 성인.

## 파일 형식 (기존 Docs/cult_events.csv, cult_choices.csv와 같은 열)
- 이벤트 CSV: `id,kind,theme,text,text_en`
- 선택지 CSV: `event_id,no,label,label_en,chance,success_effects,success_text,success_text_en,fail_effects,fail_text,fail_text_en`
- 인코딩 UTF-8 with BOM(utf-8-sig), 줄바꿈 CRLF, Python csv 모듈로 작성(따옴표 자동 처리). 한국어를 `python -c`로 넘기면 깨지므로 스크립트 파일로 작성.
- 이벤트 하나당 선택지 2~4개, no는 1부터.
- chance: 1~99면 실패 결과(fail_effects/fail_text/fail_text_en) 필수, 100이면 실패 칸 3개에 "-" (기존 cult_choices.csv와 같음).
- 효과 표기: `Reputation+4 Lust+2 Followers-3` (공백 구분, 부호 필수). 쓸 수 있는 이름:
  - 교단: LeaderFaith FollowerFaith Authority Offerings Followers Doctrine Communion Reputation
  - 개인: Health Mentality Piety Repute Wealth Lust Greed Appetite Ambition Temperance Charity Patience Integrity
  - `@` 기프티드 지급은 쓰지 않는다.
  - 수치 크기는 기존 cult_choices.csv와 비슷하게(대개 ±1~±15).
- kind/theme:
  - 일반 이벤트: kind=`random`, theme = 교단 스탯 하나(위 교단 목록). 테마를 고르게 섞는다.
  - 특수 이벤트: kind=`condition`(theme 자리에 조건, 예 `Followers>=50 Reputation>=600`) 또는 `negative`(theme 자리에 바닥 조건, 예 `Reputation<150`) 또는 `mood`(theme=`Desire` 또는 `Virtue`, 감소 효과 금지)를 섞는다.
  - 엔딩 분기 이벤트: kind=`ending`, theme 자리에 언제 나오는지 한 단어 키(`year25`, `tie_check`, `year40`, `old_age`, `absorbed`) — 구현은 나중에 함.
  - 최종 스킬 선택 이벤트: kind=`final`, theme=`any` 또는 욕망/미덕 갈래 키(`Desire`/`Virtue`).
- 텍스트: 한국어는 기존처럼 "…합니다" 서술체, 2인칭 "당신". 영어는 자연스럽고 한국어와 의미 일치. 이벤트마다 상황·인물·소재를 다르게 하고, 결과문은 1~2문장.

## 검증 (작성 후 반드시)
- 모든 effect 토큰이 위 이름 목록 안에 있는지, chance 규칙, 선택지 수, id 중복 없음, *_en 칸이 모두 영어로 채워졌는지 스크립트로 확인.
