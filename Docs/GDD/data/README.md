# 데이터 사전

파일별 용도와 필드를 정의한다. 원본 데이터를 열기 전에 여기서 구조를 확인한다.

- CSV: 평면 표. 첫 열 id, 헤더 snake_case, 단위는 접미사(_s, _pct 등), 빈칸 = 미정, 리스트는 `;` 구분.
- JSON: 중첩 구조. 최상위 객체 `{ "schema", "items": [...] }`, 키 camelCase, 참조는 id 문자열.

## inverted_endings.csv
- 용도: 뒤집힌 세계 모드 전용 엔딩 목록 · 시스템: SYS-INVERTED

| 필드 | 타입 | 단위/범위 | 설명 |
|---|---|---|---|
| id | string | inv_end_* | 엔딩 묶음 ID (Claude 명명) |
| group | string | collapse / final_skill / no_final / year40 | 엔딩 묶음 (Claude 명명) |
| trigger | string | - | 발동 조건 |
| variants | string | - | 굿/배드 등 갈래 |
| count | int | 1~32 | 이 행이 차지하는 엔딩 수 |
| hidden | bool | true/false | 히든 엔딩 여부 |

## adult_gender.csv
- 용도: 성인 이벤트 NPC 성별 판정 · 시스템: 성인 이벤트 성별 분리

| 필드 | 타입 | 단위/범위 | 설명 |
|---|---|---|---|
| id | string | xn01~xg30 | 성인 이벤트 id |
| npc_gender | string | F/M/N/C/S | 여성·남성·성별 표기 없음·커플/무리·플레이어와 같은 모습 (Claude 명명) |
| basis | string | - | 판정 근거 |
| split | string | yes/no/art-only | 성별 분리 여부 |
