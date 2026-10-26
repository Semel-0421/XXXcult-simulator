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
- 대기는 폴링하지 않는다: 긴 작업(컴파일·테스트·시뮬)은 한 명령 안에서 기다리거나 `run_in_background`의 완료 알림 한 번으로 받는다. 상태 확인 명령을 여러 번 나눠 부르지 않는다.
- 컴파일→테스트는 한 Bash 호출로 묶고 출력은 grep으로 `compilationFailed`·`message`만 남긴다. 실패할 때만 상세를 본다.
- Unity 확인은 CLI(`unity command`) 한 경로로. 같은 확인을 MCP 도구로 다시 하지 않는다.
- 감시(Monitor)·주기 체크는 사용자가 요청할 때만, 간격은 10분 이상, 출력은 변화·오류가 있을 때 한 줄.
- 시뮬은 처음부터 `Temp/` 완료 파일 + 백그라운드 대기 한 번 패턴으로 돌린다(eval 5초 제한 → `EditorApplication.delayCall`).
- 서브에이전트는 파일이 겹치지 않는 3개 이상 작업일 때만. 한두 파일짜리는 메인이 직접.
- 대화가 길어지면(컨텍스트 70%대) 작업 단위가 끝난 시점에 커밋 + 인계 메모리를 남기고 새 세션을 제안한다.

## Unity CLI
- `unity command recompile` → `recompile_status`가 completed 될 때까지 대기(약 60~80초).
- CLI 기본 타임아웃은 30초. 테스트는 `unity command run_tests --timeout 300 --result-only`로 한 번에 받는다(재호출 반복 금지: 테스트가 다시 시작됨).
- 테스트 전 `unity command recompile_status --result-only`의 `compilationFailed`가 false인지 확인. 컴파일이 실패하면 이전 빌드로 테스트가 돌아 결과를 믿을 수 없다.
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

## 공동 작업 (Claude Code + OpenCode + Codex)
- 상태 공유는 `.agent-sync.md`(푸시 안 함, 락 없는 공용 게시판: 각자 자기 행·자기 메시지만 수정). 작업 전 파일 락에 이름 기록, 끝나면 해제.
- **Claude Code = 취합 담당**: Unity 컴파일·테스트, git 스테이징·커밋·푸시, 충돌 정리는 Claude Code만 한다.
- **OpenCode = 분석·구현 담당**: 락 잡은 파일만 수정. Unity 명령과 `git add/commit`은 하지 않는다.
- **Codex = 콘텐츠·데이터 담당**(Docs 스크립트·CSV·생성 카탈로그, 제출물 리뷰). 항목 번호 `C1…`. 위임은 `bash .claude/codex-run.sh <ID> "<지시>"`를 `run_in_background`로, 결과는 `Temp/codex/<ID>.md`. Unity·git 쓰기는 `.codex/rules/collab.rules`로 차단됨.
- **Copilot CLI = 2차 리뷰**(읽기 전용): `bash .claude/copilot-review.sh <ID> "<지시>"`, 결과 `Temp/codex/<ID>.copilot.md`, 코드로 확인 후 반영. **DeepSeek(`dsh headless`)는 API 키 등록 전까지 보류.**
- **Orca = 터미널·에이전트 허브**: CLI `orca`(사용자 PATH: `C:\Users\Administrator\AppData\Local\Programs\orca\resources\bin`). 안 잡히는 오래된 셸에선 전체 경로 `...\bin\orca.cmd`. 항상 `--json`.
  - 워크트리는 main(`D:/My project`) 하나만. `orca worktree create` 금지(Unity Library 재빌드 + main 단일 규칙). 에이전트는 `orca agent launch --agent <id> --worktree active --prompt "..."`로 현재 체크아웃에 띄운다.
  - 이미 떠 있는 에이전트(OpenCode·Copilot 터미널)에 일을 줄 땐 `orca terminal list` → `terminal read` → `terminal send --text "..." --enter --wait-submit 10`. 결과 대기는 `terminal wait --for tui-idle --timeout-ms <ms>` 한 번(폴링 금지).
  - 상태 표시는 `orca worktree set --worktree active --comment "..."`. 게시판·락 규칙은 그대로 `.agent-sync.md`.
- **팀장 역할**: OpenCode·Codex는 성능 낮은 무료 모델. 글쓰기·설계·판단은 Claude Code가 직접 하고, 위임은 입력 파일 + 절차 5단계 이하 + `PASS` 검사 스크립트가 있는 기계적 작업만(파일 1~2개).
- 흐름: OpenCode 구현 완료 → `.agent-sync.md`에 "검증 대기" → Claude Code 컴파일·테스트 → 통과 시 커밋 → "완료".
- 같은 `Assets/Scripts` 파일을 두 에이전트가 동시에 열지 않는다. 한 번에 한 항목(P1, P2 …)씩 넘긴다.

### 공동 규칙 (OpenCode 동의, 확정)
1. 한 항목 = 한 제출. 락 표에 그 항목이 건드리는 파일을 전부 적는다.
2. 필드·메서드·시그니처를 바꾸거나 지우면 grep으로 모든 사용처(테스트 포함)를 고친 뒤 제출한다.
3. 캐시를 추가하면 비우는 지점(값이 바뀌는 곳, 새 판, 세이브 복원, `ResetForTests`)을 제출 메시지에 적고 전부 연결한다. 가능하면 상태 값(예: `Profile.UnlockedCount`)을 캐시 키에 넣어 호출 누락을 없앤다.
4. 리팩토링은 동작 불변. 테스트 기대값을 고쳐서 통과시키지 않는다.
5. 제출 형식: `항목 / 바꾼 파일 / 무효화 지점 / 관련 테스트` 네 줄.
6. 한두 줄짜리 단순 컴파일 오류(using 누락, 이름 중복, readonly 대입)는 Claude Code가 바로 고치고 게시판에 기록. 동작이 걸린 오류는 반려.
7. 게시판 메시지마다 한 줄 회신.

## 안전
- 로그인·자격 증명 입력은 하지 않는다. 시스템 설정 변경 금지.
- 플레이 테스트 전 세이브·프로필 백업, 끝나면 복구.
- 영구 삭제는 사용자가 직접(itch 구버전은 숨김 처리).
