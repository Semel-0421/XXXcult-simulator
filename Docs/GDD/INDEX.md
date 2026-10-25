---
doc: INDEX
title: XXXcult simulator 기획서 인덱스
status: draft
updated: 2026-10-19
---

# XXXcult simulator 기획서

> 읽기 순서: 이 파일 → 작업과 관련된 문서만 → 필요하면 data/README.md → 해당 데이터 파일.
> 상태 마커: [확정] 결정됨 · [가안] 잠정 · [미정] 비어 있음. ID: SYS-* 시스템 · DEC-* 결정 · Q-* 미결.

## 한눈에 보기
| 항목 | 값 |
|---|---|
| 한 줄 소개 | 인생을 살고 교단을 세워 신앙을 원하는 대로 비트는 텍스트 인생·교단 경영 시뮬레이션 |
| 장르 | 텍스트 시뮬레이션 |
| 플랫폼 | Windows · Android · Web |
| Unity 버전 | 6000.6.5f1 |
| 개발 단계 | 기획 |

## 문서 맵
<!-- AUTO:FILEMAP -->
| 파일 | 내용 | 상태 | 갱신 |
|---|---|---|---|
| [01_Overview.md](01_Overview.md) | 게임 개요 · 코어 루프 | draft | 2026-10-19 |
| [02_Tech.md](02_Tech.md) | Unity 기술 구조 | draft | 2026-10-19 |
| [03_Decisions.md](03_Decisions.md) | 결정 로그 · 미결 사항 | draft | 2026-10-19 |
| [systems/SYS_INVERTED.md](systems/SYS_INVERTED.md) | 신규 모드: 거울 세계 | draft | 2026-10-19 |
| [systems/SYS_DEMIURGE.md](systems/SYS_DEMIURGE.md) | 신규 모드: 황금의 눈동자 (신+거울 통합) | draft | 2026-10-26 |
| [systems/SYS_UI_DESIGN.md](systems/SYS_UI_DESIGN.md) | 인게임 UI·텍스트 개선 방향 | draft | 2026-10-25 |
<!-- /AUTO:FILEMAP -->

## 데이터 맵
필드 정의: [data/README.md](data/README.md)
<!-- AUTO:DATAMAP -->
| 파일 | 행 | 필드 |
|---|---|---|
| [data/adult_gender.csv](data/adult_gender.csv) | 60 | id, npc_gender, basis, split |
| [data/inverted_endings.csv](data/inverted_endings.csv) | 8 | id, group, trigger, variants, count, hidden |
<!-- /AUTO:DATAMAP -->

## 열린 질문
상세: [03_Decisions.md](03_Decisions.md)
<!-- AUTO:OPEN -->
_(없음)_
<!-- /AUTO:OPEN -->

## 중간 점검
<!-- AUTO:REVIEW -->
- [reviews/REVIEW_2026-10-19.md](reviews/REVIEW_2026-10-19.md) · 판정: 차단
- 점검 이후 변경 0건
<!-- /AUTO:REVIEW -->

## 최근 변경
<!-- AUTO:RECENT -->
- 2026-10-26 [systems/SYS_DEMIURGE.md] 정할 것 14건 결정: 모드 이름 '황금의 눈동자', 해금은 신 승천+거울 전용 엔딩, 그노시스 뒤 평판 반전, 전용 업적, 행동 이름 전부 새로, 히든 5종 (DEC-028)
- 2026-10-25 [systems/SYS_DEMIURGE.md] 가안 수정: 눈동자 첫 질문, 데미우르고스 스탯·재앙, 주도권 엔딩 8종 (DEC-027)
- 2026-10-25 [systems/SYS_DEMIURGE.md] 신규 모드 기획 초안: 영지주의 데미우르고스, 4섹터 아이온 트리, 교리 0~10단계, 정할 것 9건 (DEC-026)
- 2026-10-25 [systems/SYS_UI_DESIGN.md] UI·텍스트 개선 기획 초안, Design 스킬 점검(비평·접근성·문구·디자인 시스템), 업적 창 버그 2건 수정
- 2026-10-19 [systems/SYS_INVERTED.md] 동점 반올림 (DEC-023), 보유 후 진행 (DEC-024), 회차·업적 (DEC-025)
- 2026-10-19 [systems/SYS_INVERTED.md] 최종 스킬 굿/배드 (DEC-020), 신도 소멸 조건 (DEC-021), 엔딩 제목 다양화 (DEC-022)
- 2026-10-19 [systems/SYS_INVERTED.md] 몰빵 플레이 밸런스 보조 결정 (DEC-019)
- 2026-10-19 [systems/SYS_INVERTED.md] 교리 효과를 밸런스 우선 신규 작성으로 변경 (DEC-018)
- 2026-10-19 [systems/SYS_INVERTED.md] 교리 트리 반전 방식 확정 (DEC-017)
- 2026-10-19 [systems/SYS_INVERTED.md] 엔딩 분기 1개·최종 스킬 이벤트 16×5로 정정 (DEC-016)
- 2026-10-19 [systems/SYS_INVERTED.md] 능력치 반전 규칙 (DEC-014), 신규 이벤트 구성 (DEC-015)
- 2026-10-19 [systems/SYS_INVERTED.md] 점검 반영: 제목 '거울 세계', 해금 문구 DEC-012로 교체, Q-004 잔여 줄 삭제, 미결 5건 등록 (Q-010, Q-011, Q-012, Q-013, Q-014)
- 2026-10-19 [data/inverted_endings.csv] '명망치' → '평판' 용어 통일
- 2026-10-19 [data/README.md] adult_gender.csv 사전 추가
<!-- /AUTO:RECENT -->
