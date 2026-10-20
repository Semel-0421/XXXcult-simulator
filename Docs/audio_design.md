# 사운드 기획서 — 배경음악·효과음

음악·효과음을 생성하기 전에 확인하는 문서. 각 항목은 **한국어 분위기 설명**과 생성 도구에 그대로 넣을 **영문 프롬프트**를 함께 적었다.

**공통 방향**
- 장르: 다크 종교음악. 그레고리안 성가, 오르간, 현악 드론, 종, 향로 같은 소리를 바탕으로 한다.
- 사이비 교단의 "성스러움 속의 불길함"을 기본 정서로 한다. 밝은 곡도 완전히 순수하지는 않게, 어딘가 어긋난 화음을 하나 남긴다.
- 금지: 가사 있는 보컬(라틴어 허밍·모음 합창은 허용), 실존 종교의 기도문 낭송, 전자 드럼, 팝 비트.

**파일 규칙**
- 배경음악: `Assets/Resources/Audio/Music/{키}.ogg`. 반복 재생되므로 끝과 시작이 이어지게 만든다(루프). 길이 1분 30초~3분.
- 효과음: `Assets/Resources/Audio/Sfx/{키}.ogg`. 최대 1초, 앞뒤 무음 없이. 음량은 RMS -24 dBFS·피크 -6 dBFS 이하로 통일(음악보다 작게, 게임 안 조정 불필요) — gen_ai_audio.py의 SFX_RMS_DB·SFX_PEAK_DB.
- 상태 표시: **[연결됨]** = 코드가 이 파일을 재생함. 2026-10-17 신규 항목의 재생 코드까지 모두 연결했다.
- 생성: 로컬 ComfyUI (`Docs/gen_ai_audio.py`, 프롬프트는 `Docs/audio_prompts.csv`). 배경음악은 ACE-Step 1.5 turbo로 150초를 뽑고(2026-10-18, 언어 모델 악보 생성은 끔 — 켜려면 `--codes`, 곡당 약 30분), 효과음은 Stable Audio Open 1.0. 음악은 끝 4초를 처음에 겹쳐 루프로 만들고, 곡마다 음량을 맞춘다. 같은 조(D단조/D장조)로 맞춰 크로스페이드(3초)가 자연스럽게 한다.

---

## 1. 배경음악

### 1-1. 공통 화면

| 키 | 상태 | 상황 | 분위기 | BPM |
|---|---|---|---|---|
| `menu` | 연결됨 | 타이틀 화면 | 어둡고 스산한 성가. 텅 빈 대성당에 멀리서 들려오는 남성 합창, 낮은 오르간 페달음, 가끔 울리는 먼 종소리. 들어오면 안 될 곳에 들어온 느낌. | 60 |
| `life` | 연결됨 | 교단 창설 전, 개인의 성장기 | 소박하고 쓸쓸한 일상. 낡은 시골 교회의 풍금, 첼로 한 대, 오르골 같은 고음 멜로디. 평범하지만 무언가가 싹트고 있는 예감. | 72 |

- `menu` 프롬프트: `dark gregorian chant, distant male choir humming in an empty cathedral, low pipe organ pedal drone, faint distant church bell, eerie and sacred, slow, seamless loop, no lyrics`
- `life` 프롬프트: `melancholic small-town church harmonium, solo cello, music-box melody, quiet and nostalgic with a hint of foreboding, slow, seamless loop, no vocals`

### 1-2. 교단 명망 단계 (일반 모드 본편)

명망이 낮을수록 어둡고 높을수록 성스럽게. 같은 주제 선율을 단계마다 편곡만 바꿔 쓰면 전환이 자연스럽다.

| 키 | 상태 | 명망 | 분위기 | BPM |
|---|---|---|---|---|
| `tier_Persecuted` | 연결됨 | 탄압 | 가장 어둡다. 악마에게 지배당한 듯한 불협화음 합창, 쇠사슬 같은 금속성 타악, 비틀린 오르간. 숨어서 예배하는 지하 동굴. | 54 |
| `tier_Watched` | 연결됨 | 경계 | 긴장과 감시. 피치카토 현악이 발소리처럼 이어지고, 낮은 합창이 속삭인다. 누군가 문밖에서 듣고 있는 느낌. | 66 |
| `tier_Neutral` | 연결됨 | 중립 | 담담한 미사. 오르간과 현악 패드, 단조와 장조를 오가는 성가 선율. 아직 아무도 우리를 모른다. | 70 |
| `tier_Respected` | 연결됨 | 존중 | 따뜻하고 자신감 있는 찬가. 혼성 합창, 하프, 금관 한두 대. 신도들이 늘어 가는 예배당. | 76 |
| `tier_Recognized` | 연결됨 | 공인 | 가장 성스럽고 장엄하다. 대성당 오르간 풀 스톱, 대합창, 종소리. 다만 마지막 화음 하나는 살짝 어긋나 "이 영광이 진짜일까"를 남긴다. | 80 |
| `tier_Dissolved` | 연결됨 | 해체 결말 | 무너진 성전. 끊어지듯 사라지는 합창, 금이 간 종, 바람 소리. 모든 것이 끝난 뒤의 고요. (없으면 탄압 곡 재생) | 50 |

- `tier_Persecuted`: `oppressive dark ritual music, dissonant demonic choir, metallic chain-like percussion, warped pipe organ, underground cave worship, terrifying and suffocating, slow, seamless loop, no lyrics`
- `tier_Watched`: `tense sacred suspense, pizzicato strings like footsteps, whispered low choir, sparse organ, feeling of being watched, slow, seamless loop, no lyrics`
- `tier_Neutral`: `calm solemn mass, organ and string pad, chant melody shifting between minor and major, restrained, seamless loop, no lyrics`
- `tier_Respected`: `warm confident hymn, mixed choir, harp, soft brass, growing congregation, uplifting yet solemn, seamless loop, no lyrics`
- `tier_Recognized`: `majestic cathedral music, full pipe organ, grand choir, ringing bells, triumphant and holy, one subtly unresolved dissonant chord, seamless loop, no lyrics`
- `tier_Dissolved`: `ruined temple aftermath, fading broken choir, cracked bell, wind through empty halls, desolate silence, very slow, seamless loop`

### 1-3. 준비된 신 모드

신의 시점이라 인간 교단보다 크고 초월적인 소리. 신계(하늘)와 지상의 압박이 곡의 색을 바꾼다.

| 키 | 상태 | 상황 | 분위기 | BPM |
|---|---|---|---|---|
| `god_main` | 연결됨 | 준비된 신 모드 기본 | 인간의 몸에 갇힌 신. 아주 높은 소프라노 모음 합창과 아주 낮은 드론이 동시에, 사이가 텅 비어 있다. 별빛 같은 글라스 하모니카. | 64 |
| `god_heaven` | 연결됨 | 신계 압박이 지상보다 크고 3단계 이상 | 하늘이 노려본다. 천사 합창이 점점 날카로워지고, 천둥 같은 팀파니가 멀리서 굴러온다. 아름답지만 위협적. | 72 |
| `god_earth` | 연결됨 | 지상 압박이 신계보다 크고 3단계 이상 | 땅의 의심. 낮은 남성 합창의 웅성거림, 횃불 타는 소리, 성난 군중을 연상시키는 북. | 68 |
| `god_clash` | 연결됨 | 신계·지상 압박이 같은 단계 (3단계 이상) | 하늘과 땅의 전쟁. 위에서는 날카로운 천사 합창, 아래에서는 성난 군중의 웅성거림. 천둥 팀파니와 전쟁 북이 한데 섞인다. | 70 |
| `god_ascend` | 연결됨 | 승천 직전·승천 결말 | 그릇을 벗고 하늘로. 모든 성부가 하나로 모이며 끝없이 상승하는 화음, 종과 오르간이 폭발하듯 퍼진다. | 자유 |

- `god_main`: `transcendent sacred ambient, very high soprano vowel choir over very low drone with empty space between, glass harmonica shimmer, a god trapped in a human body, slow, seamless loop, no lyrics`
- `god_heaven`: `beautiful but threatening angelic choir growing sharper, distant rolling timpani like thunder, heavenly judgment, seamless loop, no lyrics`
- `god_earth`: `murmuring low male choir, crackling torches, war-like frame drums, suspicious angry crowd, dark folk ritual, seamless loop, no lyrics`
- `god_ascend`: `ascension finale, all voices converging into an endlessly rising chord, bells and full organ bursting, overwhelming divine light, 60 to 90 seconds, ends on sustained chord`

### 1-4. 결말

| 키 | 상태 | 상황 | 분위기 |
|---|---|---|---|
| `ending_glory` | 연결됨 | 좋은 결말(공인·승천 계열) | 장엄하지만 쓸쓸한 마무리. 대합창이 천천히 잦아들고 오르간 한 음만 남는다. |
| `ending_ruin` | 연결됨 | 나쁜 결말(해체·몰락·잊힘) | 장례 행렬. 느린 장송 행진, 낮은 종 세 번, 끊어지는 현악. |
| `ending_death` | 연결됨 | 교주의 죽음 | 고요한 레퀴엠. 소년 합창 같은 맑은 모음 한 줄, 그 위로 내려앉는 첼로. |

- `ending_glory`: `grand but lonely finale, choir slowly fading until a single organ note remains, bittersweet holy ending, 60 seconds`
- `ending_ruin`: `slow funeral procession, low bell tolling three times, breaking strings, mournful and final, 60 seconds`
- `ending_death`: `quiet requiem, pure treble choir vowel line, descending solo cello, peaceful grief, 60 seconds`

### 1-5. 성인 이벤트

배경음악은 바꾸지 않고 효과음 `adult_open`만 쓴다 (2026-10-17 결정).

---|---|---|---|---|
| `adult` | 연결됨 | 성인 이벤트 창이 떠 있는 동안 (끝나면 원래 곡으로 복귀) | 관능적이고 은밀한 의식. 촛불이 흔들리는 밀실, 낮은 첼로와 느린 핸드드럼, 숨결 같은 여성 허밍, 향로의 사슬 소리. 성스러움과 퇴폐가 섞인다. | 66 |

- `adult`: `sensual secret candlelit ritual, low cello, slow hand drum, breathy wordless female humming, censer chains, sacred yet decadent, intimate, seamless loop, no lyrics`

---

## 2. 효과음

| 키 | 상태 | 상황 | 소리 |
|---|---|---|---|
| `click` | 연결됨 | 선택지 확정, 행동 버튼 | 작은 성당 종(핸드벨)을 가볍게 한 번. 짧고 맑게. |
| `success` | 연결됨 | 행동 성공 | 상승하는 짧은 합창 화음 "아—" + 종 한 번. 축복받은 느낌. |
| `fail` | 연결됨 | 행동 실패 | 낮게 꺼지는 오르간 불협화음 + 촛불 꺼지는 "훅". |
| `achievement` | 연결됨 | 업적 달성 | 반짝이는 하프 글리산도 위에 종 세 번. |
| `event_open` | 연결됨 | 이벤트 창이 뜰 때 | 두꺼운 경전 페이지 넘기는 소리 + 낮은 허밍 한 숨. |
| `event_bad` | 연결됨 | 부정적 이벤트·신계/지상 압박 이벤트 | 멀리서 울리는 낮은 종 + 짧은 불협 합창 찌르기. |
| `doctrine_learn` | 연결됨 | 교리 습득·신 스킬 습득 | 돌판에 글자를 새기는 끌 소리 + 짧은 합창 화음. |
| `cult_found` | 연결됨 | 교단 창설 | 큰 종 한 번 길게, 뒤따르는 짧은 오르간 화음. 출발의 무게. |
| `consecrate` | 연결됨 | 축성(건물 건설) | 돌 쌓는 소리 + 성수 뿌리는 물방울 + 종. |
| `pressure_heaven` | 연결됨 | 신계 압박 단계 상승 | 멀리서 굴러오는 천둥 + 높은 합창 비명 같은 화음. |
| `pressure_earth` | 연결됨 | 지상 압박 단계 상승 | 군중 웅성거림이 커졌다 끊김 + 땅 울림. |
| `ascend` | 연결됨 | 승천 | 상승하는 종소리 연쇄 + 빛이 퍼지는 듯한 신시사이저 없는 합창 스웰. |
| `death` | 연결됨 | 교주 사망 | 심장 박동 두 번 뒤 정적, 아주 낮은 종 하나. |
| `adult_open` | 연결됨 | 성인 이벤트 창이 뜰 때 | 비단 천 스치는 소리 + 촛불 일렁임 + 낮은 숨결 같은 허밍. |
| `menu_open` | 연결됨 | 일시정지·옵션 창 열기 | 무거운 나무 문 여는 삐걱 소리(짧게). |
| `save` | 연결됨 | 저장 완료 | 밀랍 봉인 찍는 "턱" 소리. |

효과음 프롬프트는 위 "소리" 칸을 영어로 옮겨 쓰면 된다. 예: `small church handbell single light ring, short and clear, no reverb tail` (click).

---

## 3. 결정 사항 (2026-10-17)

- 생성은 로컬 컴퓨터로. 결과가 마음에 들지 않으면 외부 서비스를 검토한다.
- 범위는 최대: 배경음악 16곡, 효과음 16개 전부.
- 결말 곡은 영광·몰락·죽음 3가지. 해체 결말은 `tier_Dissolved`(없으면 몰락 곡).
- 준비된 신 모드는 신계·지상 중 압박이 센 쪽 곡, 같으면 두 분위기를 함께 담은 `god_clash`.
- 루프와 곡 전환이 어색하지 않게: 루프 크로스페이드 4초, 곡 전환 크로스페이드 3초, 같은 조, 같은 음량.
- 성인 이벤트는 효과음만.
