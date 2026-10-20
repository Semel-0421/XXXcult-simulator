# XXXcult Simulator — 작업 규칙

## 응답
- 한국어, 음슴체, 짧게. 결과부터 말하고 진행 설명은 생략.
- 성인 콘텐츠는 기본 포함으로 다룬다.

## 모호한 지시의 기본 해석 (묻지 말고 이대로 진행, 다르면 사용자가 고침)
- "커밋해줘" = 이번 작업 파일만 골라 main에 커밋 + 푸시. 아래 "커밋 제외"는 절대 넣지 않는다.
- "패치/진행해줘" = 기획서(Docs/GDD)에 있는 다음 미완료 항목을 구현 → 컴파일 → 테스트까지.
- "점검/확인해줘" = 데이터·코드 검증만 하고 고칠 것은 목록으로 보고. 수치는 건드리지 않는다.
- "병렬로" = 파일이 겹치지 않게 나눈 에이전트로 진행. 그 외에는 에이전트를 띄우지 않는다.
- 수치·밸런스 조정은 사용자가 명시하기 전까지 보고서 제안으로만 남긴다.
- 브랜치는 따로 나누지 않고 main에서 작업.

## 에이전트
- 설계·판단(구조 설계, 밸런스 분석)은 opus, 반복 구현(데이터 변환, 훅 연결, 모드별 구현)은 sonnet.
- 최종 취합(컴파일, 테스트, 충돌 정리, 커밋)은 메인 세션이 직접.
- 에이전트는 Unity 컴파일·테스트를 돌리지 않게 하고, 끝난 뒤 메인이 한 번에 돌린다. Unity를 쓰는 에이전트는 한 번에 하나만.
- 프롬프트에 소유 파일을 명시해 겹침을 막는다.

## 토큰 절약
- 큰 파일·CSV는 통째로 읽지 말고 grep/cut/head로 필요한 열만.
- 에이전트 transcript(output 파일)는 읽지 않는다. 진행 확인은 결과 파일 mtime·내용으로.
- 시뮬레이션은 작은 규모(스타일당 20명×20판)로 먼저 돌리고, 필요할 때만 늘린다.

## Unity CLI
- `unity command recompile` → `recompile_status`가 completed 될 때까지 대기(약 60~80초).
- CLI는 30초 타임아웃. `run_tests --result-only`가 타임아웃이면 20~45초 기다린 뒤 다시 호출하면 결과가 나온다.
- 긴 작업은 `run_script`로 Temp/에 결과 파일을 쓰게 하고 파일로 확인.
- 플랫폼 전환은 도메인 리로드로 run_script를 끊지만 빌드는 끝난다(산출물 mtime으로 확인).
- 테스트 전 에디터의 `Profile.Enabled`가 false인지 확인(플레이 모드 잔여 상태로 실제 프로필이 섞이면 테스트가 깨짐).

## 데이터 원본
- 업적: `Docs/gen_achievements_v2.py` → CSV → `Docs/gen_achievements_v2_cs.py` → `AchievementCatalogV2.cs`. 히든은 `Docs/achievements_v2_hidden.csv`.
- 기프티드 v2: `Docs/gifted_v2.csv`가 원본(생성 스크립트 없음) → `Docs/gen_gifted_v2_cs.py`.
- 거울 이벤트: `cd Docs && python gen_mirror.py` (`-I` 쓰지 말 것). 그 외 Docs 스크립트는 `python -I`.

## 커밋 제외 (사용자가 따로 관리)
- `Assets/TextMesh Pro/Fonts/*`, `Assets/TilemapLevel/*`, `Assets/Welcome/*`, `Windows/*`, `Packages/packages-lock.json`, `ProjectSettings/ProjectSettings.asset`(버전 줄만 예외), `Assets/Scripts/Shop/*`(다른 세션 작업), `.claude/ .codex/ .serena/ Assets/_Recovery/ Docs/__pycache__/`, Docs의 임시 CSV(`action_*`, `itch_stats.csv`).

## 작업 흐름·보고
- 순서: 기획서(`Docs/GDD/systems/*.md`)에 결정 기록 → 구현 → 컴파일 → 테스트 → 사용자 요청 시 커밋.
- 결정이 나면 바로 해당 SYS 문서에 "(결정)" 절로 남긴다. 미정 항목은 "정할 것" 목록으로.
- 단계가 끝날 때마다 보고: 한 일 / 남은 문제 / 사용자가 정할 것. 미검증 항목(에디터 화면 미확인 등)은 반드시 명시.
- 에이전트 보고는 그대로 옮기지 말고 결정 필요 사항 위주로 요약.
- 큰 작업은 중간 체크포인트 커밋(테스트 통과 상태에서만).

## 빌드·배포
- 버전: `ProjectSettings.asset`의 `bundleVersion`·`AndroidBundleVersionCode`만 커밋(나머지 diff는 제외).
- 빌드: Windows / Android / Web. 끝났는지는 산출물 mtime과 `globalgamemanagers`의 버전 문자열로 확인.
- itch.io: butler `C:\Users\Administrator\Tools\butler\butler.exe`, 대상 `semel-0421/xxxcult-simulator`, 채널 `windows`·`android`·`html`. Windows는 폴더째 `--ignore "*BackUpThisFolder*"`.
- 대시보드 작업(작품 소개, 개발 일지)은 Claude in Chrome으로. 개발 일지는 저장 후 공개(Published) 여부를 꼭 확인.
- GitHub: `gh`로 릴리스 생성. 개발 일지는 GitHub 릴리스 노트와 itch devlog 양쪽에.
- 구버전 업로드는 지우지 않고 숨김 처리.

## 안전
- 로그인·자격 증명 입력은 하지 않는다. 시스템 설정 변경 금지.
- 플레이 테스트 전 세이브·프로필 백업, 끝나면 복구.
- 영구 삭제는 사용자가 직접(itch 구버전은 숨김 처리).
