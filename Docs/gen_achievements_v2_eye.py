# 황금의 눈동자 모드 전용 업적 (DEC-028 F, SYS_DEMIURGE 14절 7단계) -> Docs/achievements_v2_eye.csv
# 실행: python -I Docs/gen_achievements_v2_eye.py   (프로젝트 루트에서), 그 뒤 python -I Docs/gen_achievements_v2_cs.py
# 카운터는 Assets/Scripts/Achievement/EyeCounters.cs. 엔딩은 Endings.Reach가 남기는 profile:ending_<id>를 쓴다.
# 이 모드는 기프티드를 쓰지 않으므로 보상(기프티드 연결)은 없다.
import csv, os

HERE = os.path.dirname(os.path.abspath(__file__))

def josa(word, pair):
    if not word or not 0xAC00 <= ord(word[-1]) <= 0xD7A3:
        return word + pair
    has_batchim = (ord(word[-1]) - 0xAC00) % 28 != 0
    return word + ({"을(를)": "을", "이(가)": "이"}[pair] if has_batchim else {"을(를)": "를", "이(가)": "가"}[pair])
FIELDS = ["id", "mode", "category", "name", "name_en", "description", "description_en", "condition", "tier", "gate"]
rows = []


def add(cat, ko, en, dko, den, cond, tier=1):
    rows.append(dict(id=f"e{len(rows) + 1:03d}", mode="eye", category=cat, name=ko, name_en=en,
                     description=dko, description_en=den, condition=cond, tier=tier, gate=""))


SECTORS = [("yahweh", "야훼", "Yahweh"), ("satan", "사탄", "Satan"), ("samael", "사마엘", "Samael"), ("yaldabaoth", "얄다바오트", "Yaldabaoth")]

# 엔딩 13종
with open(os.path.join(HERE, "eye", "endings_eye.csv"), encoding="utf-8-sig") as f:
    for e in csv.DictReader(f):
        tier = 3 if e["hidden"] == "1" else 1 if e["kind"] == "bad" else 2
        add("엔딩", e["name"], e["name_en"], f"'{e['name']}' 엔딩을 봤다.", f"Saw the ending '{e['name_en']}'.", f"profile:ending_{e['id']}>=1", tier)

for n, ko, en in [(3, "세 개의 왕좌", "Three Thrones"), (8, "여덟 개의 왕좌", "Eight Thrones"), (13, "모든 거짓 신", "Every False God")]:
    add("엔딩", ko, en, f"서로 다른 황금의 눈동자 엔딩을 {n}개 봤다.", f"Saw {n} different Golden Eye endings.", f"NEW:eye_endings_seen>={n}", 1 + (n >= 8) + (n >= 13))

# 승천
for n, ko, en in [(1, "첫 찬탈", "First Usurpation"), (5, "다섯 번의 찬탈", "Five Usurpations"), (15, "찬탈의 습관", "A Habit of Usurping")]:
    add("승천", ko, en, f"주도권을 지킨 채 승천을 {n}번 했다 (모든 회차 합산).", f"Ascended {n} times while keeping dominance (all runs combined).", f"profile:eye_usurped>={n}", 1 + (n >= 5) + (n >= 15))
for n, ko, en in [(1, "삼켜진 첫 교주", "First Leader Swallowed"), (5, "다섯 번 삼켜지다", "Swallowed Five Times")]:
    add("승천", ko, en, f"주도권을 뺏긴 채 승천을 {n}번 했다 (모든 회차 합산).", f"Ascended {n} times after losing dominance (all runs combined).", f"profile:eye_lost>={n}", 1 + (n >= 5))
for n, ko, en in [(10, "열 번의 승천", "Ten Ascensions"), (30, "서른 번의 승천", "Thirty Ascensions")]:
    add("승천", ko, en, f"황금의 눈동자에서 승천을 {n}번 했다 (모든 회차 합산).", f"Ascended {n} times in The Golden Eye (all runs combined).", f"profile:eye_ascended>={n}", 2 + (n >= 30))

# 섹터: 시작 섹터 선택, 그노시스, 문턱
for key, ko, en in SECTORS:
    add("섹터", f"{ko}의 눈길", f"Gaze of {en}", f"첫 질문에서 {ko}의 눈동자를 골랐다.", f"Chose the eyes of {en} at the first question.", f"profile:eye_start_{key}>=1", 1)
    add("섹터", f"{ko}의 그노시스", f"Gnosis of {en}", f"{ko} 섹터의 그노시스(5단계)를 익혔다.", f"Learned the Gnosis (tier 5) of {en}.", f"NEW:eye_gnosis_{key}", 2)
    add("섹터", f"{ko}의 문턱", f"Threshold of {en}", f"{ko} 섹터의 플레로마의 문턱(9단계)을 익혔다.", f"Learned the Threshold of the Pleroma (tier 9) of {en}.", f"NEW:eye_threshold_{key}", 3)
add("섹터", "네 개의 눈길", "Four Gazes", "첫 질문에서 네 눈동자를 모두 한 번씩 골라 봤다.", "Chose each of the four eyes at least once at the first question.",
    " ".join(f"profile:eye_start_{k}>=1" for k, _, _ in SECTORS), 2)

# 아이온 트리
for n, ko, en in [(10, "아이온의 싹", "Sprout of Aeons"), (20, "아이온의 가지", "Branch of Aeons"), (32, "아이온의 숲", "Forest of Aeons"), (48, "아이온의 바다", "Sea of Aeons"), (64, "충만", "Pleroma")]:
    add("아이온 트리", ko, en, f"아이온 노드를 한 회차에 {n}개 익혔다 (최고 기록).", f"Learned {n} aeon nodes in a single run (best run).", f"NEW:eye_nodes>={n}", 1 + (n >= 32) + (n >= 64))
for kind, ko, en in [("good", "선한", "Good"), ("evil", "악한", "Evil")]:
    for n in (10, 20):
        add("아이온 트리", f"{ko} 아이온 {n}", f"{en} Aeons {n}", f"{ko} 아이온을 한 회차에 {n}개 익혔다 (최고 기록).", f"Learned {n} {en.lower()} aeons in a single run (best run).", f"NEW:eye_{kind}>={n}", 1 + (n >= 20))
for n, ko, en in [(2, "두 개의 깨달음", "Two Awakenings"), (3, "세 개의 깨달음", "Three Awakenings")]:
    add("아이온 트리", ko, en, f"한 회차에 {n}개 섹터의 그노시스를 익혔다.", f"Learned the Gnosis of {n} sectors in one run.", f"NEW:eye_gnosis_count>={n}", 2 + (n >= 3))
add("아이온 트리", "네 방위", "Four Quarters", "한 회차에 네 섹터의 노드를 모두 하나 이상 익혔다.", "Learned at least one node in all four sectors in one run.", "NEW:eye_all_sectors", 1)

# 재앙
for n, ko, en in [(1, "첫 재앙", "First Calamity"), (10, "열 번의 재앙", "Ten Calamities"), (30, "재앙의 계절", "Season of Calamity"), (100, "끝없는 재앙", "Endless Calamity")]:
    add("재앙", ko, en, f"재앙을 누적 {n}번 겪었다 (모든 회차 합산).", f"Suffered {n} calamities in total (all runs combined).", f"profile:eye_calamities>={n}", 1 + (n >= 10) + (n >= 100))
for n, ko, en in [(5, "다섯 번의 눈길", "Five Glances"), (15, "멈추지 않는 눈길", "Unceasing Gaze")]:
    add("재앙", ko, en, f"한 회차에 재앙을 {n}번 겪었다.", f"Suffered {n} calamities in one run.", f"NEW:eye_run_calamities>={n}", 2 + (n >= 15))
for n, ko, en in [(1, "잠든 사이", "While You Slept"), (10, "깨지 않는 잠", "Sleep Without Waking")]:
    add("재앙", ko, en, f"쉬는 동안 재앙을 누적 {n}번 겪었다.", f"Suffered {n} calamities while resting (all runs combined).", f"profile:eye_rest_calamities>={n}", 1 + (n >= 10))
for key, ko, en in [("cult", "교단을 내준 자", "Giver of the Cult"), ("body", "몸을 내준 자", "Giver of the Body"), ("half", "나눠 내준 자", "Giver of Halves")]:
    what = {"cult": "교단을 내준다", "body": "몸을 내준다", "half": "조금씩 나눠 내준다"}[key]
    what_en = {"cult": "Give up the cult", "body": "Give up the body", "half": "Give up a little of each"}[key]
    add("재앙", ko, en, f"재앙에서 '{what}'를 누적 10번 골랐다.", f"Chose '{what_en}' 10 times in calamities (all runs combined).", f"profile:eye_give_{key}>=10", 2)
add("재앙", "흠 없는 왕좌", "Unmarred Throne", "재앙을 한 번도 겪지 않고 승천했다.", "Ascended without suffering a single calamity.", "NEW:eye_ascend_calamities==0", 3)
add("재앙", "상처투성이 왕좌", "Scarred Throne", "재앙을 20번 이상 겪고 주도권을 지킨 채 승천했다.", "Ascended keeping dominance after 20+ calamities.", "NEW:eye_ascend_calamities>=20 NEW:eye_ascend_good", 3)

# 주도권
add("주도권", "기우는 저울", "Tilting Scales", "한 회차에 주도권 비율이 1.5배를 넘었다.", "Dominance passed 1.5x in one run.", "NEW:eye_dominance_peak>=150", 1)
add("주도권", "넘어간 저울", "Scales Tipped", "한 회차에 주도권 비율이 2배를 넘었다.", "Dominance passed 2x in one run.", "NEW:eye_dominance_peak>=201", 2)
add("주도권", "아슬아슬한 찬탈", "Narrow Usurpation", "주도권 1.9배 이상에서 주도권을 지킨 채 승천했다.", "Ascended keeping dominance at 1.9x or more.", "NEW:eye_ascend_dominance>=190 NEW:eye_ascend_good", 3)
add("주도권", "온전한 주인", "Whole Master", "주도권 1배 이하로 승천했다.", "Ascended with dominance at 1x or below.", "NEW:eye_ascend_dominance<=100", 2)
add("주도권", "가득 찬 거짓 신", "The Filled False God", "데미우르고스의 스탯 하나가 상한에 이르렀다.", "One of the Demiurge's stats reached its cap.", "NEW:eye_demiurge_max", 2)

# 시간
for n, ko, en in [(1, "그릇의 첫 해", "First Year of the Vessel"), (5, "다섯 해의 동거", "Five Years Together"), (10, "열 해의 동거", "Ten Years Together"),
                  (25, "사반세기의 동거", "A Quarter Century Together"), (50, "반세기의 동거", "Half a Century Together")]:
    add("시간", ko, en, f"데미우르고스와 한 회차에 {n}년을 보냈다 (최고 기록).", f"Spent {n} years with the Demiurge in one run (best run).", f"NEW:eye_years>={n}", 1 + (n >= 10) + (n >= 50))
add("도전", "빠른 찬탈", "Swift Usurpation", "교단 10년 안에 승천했다.", "Ascended within 10 cult years.", "NEW:eye_ascend_years<=10", 3)
add("도전", "느린 찬탈", "Slow Usurpation", "교단 40년 이상 지난 뒤 승천했다.", "Ascended after 40+ cult years.", "NEW:eye_ascend_years>=40", 2)
add("도전", "한 얼굴만", "Only One Face", "한 섹터의 노드만 익히고 주도권을 지킨 채 승천했다.", "Ascended keeping dominance with nodes from only one sector.", "NEW:eye_ascend_single_sector", 3)

# 행동 (누적)
ACTS = [("Wonder", "기적", "Manifest"), ("Revelation", "가르침", "Mystery"), ("Healing", "정화", "Purify"), ("Feast", "잔치", "Carnival"), ("Commune", "신내림", "Invoke"), ("Consecrate", "창조", "Create")]
for key, ko, en in ACTS:
    for n, tko, ten in [(30, "익숙한", "Practiced"), (150, "능숙한", "Masterful")]:
        add("행동", f"{tko} {ko}", f"{ten} {en}", f"{josa(ko, '을(를)')} 누적 {n}번 했다 (모든 회차 합산).", f"Did {en} {n} times in total (all runs combined).", f"profile:eye_act_{key}>={n}", 1 + (n >= 150))

# 건축·이벤트·회차
for n, ko, en in [(1, "첫 창조", "First Creation"), (10, "열 개의 피조물", "Ten Creations"), (30, "물질의 도시", "City of Matter")]:
    add("건축", ko, en, f"건물을 누적 {n}번 지었다 (모든 회차 합산).", f"Built {n} times in total (all runs combined).", f"profile:eye_build_total>={n}", 1 + (n >= 10) + (n >= 30))
for n, ko, en in [(50, "쉰 개의 계시", "Fifty Signs"), (200, "이백 개의 계시", "Two Hundred Signs"), (500, "오백 개의 계시", "Five Hundred Signs")]:
    add("이벤트", ko, en, f"황금의 눈동자 이벤트를 누적 {n}개 겪었다.", f"Experienced {n} Golden Eye events in total.", f"profile:eye_events>={n}", 1 + (n >= 200) + (n >= 500))
for n, ko, en in [(1, "첫 눈동자", "First Eyes"), (5, "다섯 번의 눈동자", "Five Gazes Met"), (20, "스무 번의 눈동자", "Twenty Gazes Met")]:
    add("회차", ko, en, f"황금의 눈동자를 {n}회차 시작했다.", f"Started {n} Golden Eye runs.", f"profile:eye_runs>={n}", 1 + (n >= 5) + (n >= 20))

# 업적 수
for n, ko, en in [(30, "눈동자 업적 서른", "Thirty Golden Marks"), (60, "눈동자 업적 예순", "Sixty Golden Marks")]:
    add("업적", ko, en, f"황금의 눈동자 업적을 {n}개 달성했다.", f"Unlocked {n} Golden Eye achievements.", f"NEW:eye_achv>={n}", 2 + (n >= 60))

with open(os.path.join(HERE, "achievements_v2_eye.csv"), "w", encoding="utf-8-sig", newline="") as f:
    w = csv.DictWriter(f, fieldnames=FIELDS)
    w.writeheader()
    w.writerows(rows)
print(len(rows), "eye achievements")
