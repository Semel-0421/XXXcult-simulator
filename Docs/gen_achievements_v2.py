# 업적 기획서 초안: 모드별 200개를 Docs/achievements_v2_<mode>.csv 로 뽑는다. 게임에는 아직 연결하지 않음.
# 실행: python Docs/gen_achievements_v2.py   (프로젝트 루트에서)
# 조건 칸: 기존 문법(c:/p:/s:/age/cultyears/doctrine/gifted/standing/profile:/achv)이면 그대로 쓸 수 있고,
#          "NEW:" 로 시작하면 카운터나 판정을 새로 만들어야 하는 항목이다.
import csv, os

HERE = os.path.dirname(os.path.abspath(__file__))
FIELDS = ["id", "mode", "category", "name", "name_en", "description", "description_en", "condition", "tier", "gate"]
TIER = ["수련생", "숙련자", "달인", "전설"]
TIER_EN = ["Apprentice", "Adept", "Master", "Legend"]

class Mode:
    def __init__(self, key, prefix, start):
        self.key, self.prefix, self.n, self.rows = key, prefix, start, []
    def add(self, cat, ko, en, dko, den, cond, tier=1):
        self.rows.append(dict(id=f"{self.prefix}{self.n:03d}", mode=self.key, category=cat, name=ko, name_en=en,
                              description=dko, description_en=den, condition=cond, tier=tier, gate="adult_option" if cat == "성인" else ""))
        self.n += 1
    def ladder(self, cat, noun_ko, noun_en, counter, steps, verb_ko="{n}번 했다.", verb_en="Done {n} times.", names=None):
        """같은 행동을 단계별로 쌓는 업적. names=(ko[], en[]) 로 단계 이름을 바꿀 수 있다."""
        for i, n in enumerate(steps):
            ko = names[0][i] if names else f"{noun_ko} {TIER[i]}"
            en = names[1][i] if names else f"{noun_en} {TIER_EN[i]}"
            self.add(cat, ko, en, f"{noun_ko}을(를) " + verb_ko.format(n=n), f"{noun_en}: " + verb_en.format(n=n), f"{counter}>={n}", i + 1)

def write(mode):
    with open(os.path.join(HERE, f"achievements_v2_{mode.key}.csv"), "w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, FIELDS); w.writeheader(); w.writerows(mode.rows)

# ───────────────────────── 일반 모드 (인생 + 교단 + 성인) ─────────────────────────
def normal():
    m = Mode("normal", "n", 101)
    life = [("Exercise", "운동", "Exercise"), ("Meditate", "명상", "Meditation"), ("Study", "공부", "Study"),
            ("Socialize", "사교", "Socializing"), ("Pray", "기도", "Prayer")]
    for key, ko, en in life:
        m.ladder("인생 행동", ko, en, f"c:act_{key}", [25, 50, 100, 200])
    cult = [("Proselytize", "포교", "Proselytizing"), ("Preach", "설교", "Preaching"), ("Serve", "봉사", "Service"),
            ("Desire", "욕망의 의식", "Rites of Desire"), ("CultPray", "교단 기도", "Congregational Prayer"), ("Consecrate", "봉헌", "Consecration")]
    for key, ko, en in cult:
        m.ladder("교단 행동", ko, en, f"c:act_{key}", [50, 100, 200, 400])

    stats = [("Health", "체력", "Health"), ("Mentality", "정신력", "Mentality"), ("Piety", "신앙심", "Piety"), ("Repute", "평판", "Repute"),
             ("Wealth", "재산", "Wealth"), ("Charity", "자애", "Charity"), ("Integrity", "청렴", "Integrity"), ("Patience", "인내", "Patience"),
             ("Temperance", "절제", "Temperance"), ("Greed", "재물욕", "Greed"), ("Ambition", "권력욕", "Ambition"), ("Lust", "음욕", "Lust"), ("Appetite", "식욕", "Appetite")]
    for key, ko, en in stats:
        m.add("개인 스탯", f"탁월한 {ko}", f"Exceptional {en}", f"{ko}이(가) 75에 이르렀다.", f"{en} reached 75.", f"p:{key}>=75", 1)
        m.add("개인 스탯", f"{ko}의 극치", f"Peak of {en}", f"{ko}이(가) 최대치 100에 이르렀다.", f"{en} reached its peak of 100.", f"p:{key}>=100", 2)

    cstats = [("Followers", "신도", "Followers", [800, 1500, 2500]), ("FollowerFaith", "신도 신앙", "Follower Faith", [900, 1000]),
              ("LeaderFaith", "교주 신앙", "Leader's Faith", [900, 1000]), ("Authority", "권위", "Authority", [900, 1000]),
              ("Offerings", "헌금", "Offerings", [900, 1000]), ("Reputation", "명망", "Reputation", [950, 1000])]
    fol = ["신도 팔백 명", "신도 천오백 명", "신도 이천오백 명"], ["Eight Hundred Souls", "Fifteen Hundred Souls", "Twenty-Five Hundred Souls"]
    for key, ko, en, steps in cstats:
        for i, n in enumerate(steps):
            name_ko = fol[0][i] if key == "Followers" else (f"{ko} {n}" if n < 1000 else f"{ko}의 만개")
            name_en = fol[1][i] if key == "Followers" else (f"{en} {n}" if n < 1000 else f"{en} in Full Bloom")
            m.add("교단 스탯", name_ko, name_en, f"{ko}이(가) {n}에 이르렀다.", f"{en} reached {n}.", f"s:{key}>={n}", i + 1)

    for n, ko, en in [(20, "스무 살", "Twenty"), (25, "청춘의 끝", "End of Youth"), (35, "서른다섯", "Thirty-Five"), (45, "불혹 너머", "Past Forty"),
                      (60, "환갑", "Sixty"), (80, "팔순", "Eighty"), (90, "구순", "Ninety"), (100, "백수", "A Hundred Years")]:
        m.add("나이", ko, en, f"{n}세가 되었다.", f"Turned {n}.", f"age>={n}", 1 + (n >= 60) + (n >= 90))
    for n, ko, en in [(2, "교단 2년", "Cult, Year 2"), (7, "교단 7년", "Cult, Year 7"), (15, "교단 15년", "Cult, Year 15"),
                      (25, "사반세기", "A Quarter Century"), (40, "교단 40년", "Cult, Year 40"), (50, "반세기의 교단", "Half a Century")]:
        m.add("교단 연수", ko, en, f"교단을 세운 지 {n}년이 지났다.", f"{n} years have passed since you founded the cult.", f"cultyears>={n}", 1 + (n >= 15) + (n >= 40))
    for n, ko, en in [(1, "교리의 첫걸음", "First Step of Doctrine"), (2, "교리 입문", "Doctrine Initiate"), (4, "교리 중급", "Doctrine Intermediate"),
                      (5, "교리 숙달", "Doctrine Proficiency"), (7, "교리 대가", "Doctrine Grandmaster"), (8, "교리 현자", "Doctrine Sage"), (9, "교리 성현", "Doctrine Saint")]:
        m.add("교리", ko, en, f"교리 단계 {n}에 올랐다.", f"Reached doctrine level {n}.", f"doctrine>={n}", 1 + (n >= 5) + (n >= 8))
    for key, ko, en in [("Lust", "음욕", "Lust"), ("Greed", "재물욕", "Greed"), ("Appetite", "식욕", "Appetite"), ("Ambition", "권력욕", "Ambition"),
                        ("Temperance", "절제", "Temperance"), ("Charity", "자애", "Charity"), ("Patience", "인내", "Patience"), ("Integrity", "청렴", "Integrity")]:
        m.add("교리 갈래", f"{ko}의 길", f"Path of {en}", f"{ko} 갈래의 최종 스킬을 익혔다.", f"Learned a final skill of the {en} branch.", f"NEW:final_branch_{key}", 3)
    for st, ko, en in [("Persecuted", "탄압받는 자", "The Persecuted"), ("Watched", "감시받는 자", "The Watched"), ("Respected", "존중받는 자", "The Respected"), ("Recognized", "공인된 자", "The Recognized")]:
        m.add("시선", f"{ko}에서 일어서다", f"Rise From {en}", f"시선이 '{ko}' 단계였다가 두 단계 이상 회복했다.",
              f"Recovered two tiers or more after sinking to the {st} tier.", f"NEW:standing_recover_{st}", 2)
    m.add("시선", "인기와 몰락", "Fame and Fall", "공인(Recognized) 단계에 올랐다가 탄압(Persecuted)까지 추락했다.", "Reached Recognized, then fell all the way to Persecuted.", "NEW:standing_swing", 3)
    m.add("시선", "줄타기", "Walking the Wire", "시선이 다섯 번 이상 오르내렸다.", "Your standing changed tier five times or more in one life.", "NEW:standing_changes>=5", 2)

    for n, ko, en in [(20, "스무 가지 재능", "Twenty Gifts"), (25, "재능의 숲", "Forest of Gifts"), (30, "서른 가지 재능", "Thirty Gifts"), (40, "재능의 바다", "Sea of Gifts"),
                      (50, "쉰 가지 재능", "Fifty Gifts"), (75, "재능 박물관", "Museum of Gifts"), (100, "백 가지 재능", "A Hundred Gifts")]:
        m.add("기프티드", ko, en, f"한 회차에 기프티드를 {n}개 보유했다.", f"Held {n} Gifted traits in one run.", f"gifted>={n}", 1 + (n >= 40) + (n >= 75))
    for n, ko, en in [(30, "도감 서른", "Thirty Catalogued"), (70, "도감 일흔", "Seventy Catalogued"), (100, "도감 백", "A Hundred Catalogued"), (141, "재능 도감 완성", "Gifted Codex Complete")]:
        m.add("기프티드", ko, en, f"기프티드 도감을 {n}종 채웠다.", f"Discovered {n} Gifted traits across all runs.", f"profile:gifted_seen>={n}", 2 + (n >= 100))
    for gid, ko, en, dko, den in [("no.20", "같은 성의 끌림", "Drawn to the Same", "동성애 기프티드를 얻고 성인 이벤트를 5번 봤다.", "Gained the same-sex Gifted trait and saw 5 adult events."),
                                  ("gifted_cleric", "타고난 성직자", "Born Cleric", "신앙 계열 기프티드만으로 교단을 세웠다.", "Founded a cult with only faith-related Gifted traits."),
                                  ("gifted_beast", "짐승의 본능", "Beast Instinct", "욕망 계열 기프티드를 5개 이상 보유했다.", "Held five or more desire-type Gifted traits."),
                                  ("gifted_saint", "성자의 자질", "Saintly Disposition", "미덕 계열 기프티드를 5개 이상 보유했다.", "Held five or more virtue-type Gifted traits."),
                                  ("gifted_none", "평범한 사람", "An Ordinary Soul", "기프티드 없이 교단을 세웠다.", "Founded a cult without any Gifted traits.")]:
        m.add("기프티드", ko, en, dko, den, f"NEW:gifted_special_{gid}", 2)

    for n, ko, en in [(1, "첫 엔딩", "First Ending"), (5, "다섯 번째 막", "Fifth Curtain"), (10, "열 번째 막", "Tenth Curtain"), (20, "스무 개의 결말", "Twenty Endings"),
                      (30, "서른 개의 결말", "Thirty Endings"), (40, "마흔 개의 결말", "Forty Endings")]:
        m.add("엔딩 수집", ko, en, f"서로 다른 엔딩을 {n}개 봤다.", f"Saw {n} different endings.", f"profile:endings_seen>={n}", 1 + (n >= 10) + (n >= 30))
    m.add("엔딩 수집", "모든 굿 엔딩", "Every Good Ending", "모든 굿 엔딩을 봤다.", "Saw every good ending.", "NEW:all_good_endings", 3)
    m.add("엔딩 수집", "모든 배드 엔딩", "Every Bad Ending", "모든 배드 엔딩을 봤다.", "Saw every bad ending.", "NEW:all_bad_endings", 3)
    for key, ko, en in [("Lust", "음욕", "Lust"), ("Greed", "재물욕", "Greed"), ("Appetite", "식욕", "Appetite"), ("Ambition", "권력욕", "Ambition"),
                        ("Temperance", "절제", "Temperance"), ("Charity", "자애", "Charity"), ("Patience", "인내", "Patience"), ("Integrity", "청렴", "Integrity")]:
        m.add("엔딩 수집", f"{ko}의 두 얼굴", f"Two Faces of {en}", f"{ko} 갈래의 굿 엔딩과 배드 엔딩을 모두 봤다.", f"Saw both the good and bad ending of the {en} branch.", f"NEW:both_endings_{key}", 3)

    for n, ko, en in [(10, "열 번째 삶", "Tenth Life"), (20, "스무 번째 삶", "Twentieth Life"), (30, "서른 번째 삶", "Thirtieth Life"), (50, "쉰 번째 삶", "Fiftieth Life")]:
        m.add("회차", ko, en, f"{n}회차를 시작했다.", f"Started run number {n}.", f"profile:runs>={n}", 1 + (n >= 30))
    m.add("회차", "다섯 번의 해산", "Five Dissolutions", "교단이 다섯 번 해산되었다.", "Your cult was dissolved five times.", "profile:dissolved>=5", 2)
    m.add("회차", "열 번의 해산", "Ten Dissolutions", "교단이 열 번 해산되었다.", "Your cult was dissolved ten times.", "profile:dissolved>=10", 3)
    m.add("회차", "다섯 교단의 주인", "Master of Five Cults", "교단을 다섯 번 창설했다.", "Founded a cult five times.", "profile:founded>=5", 2)
    m.add("회차", "열 교단의 주인", "Master of Ten Cults", "교단을 열 번 창설했다.", "Founded a cult ten times.", "profile:founded>=10", 3)
    for n, ko, en in [(150, "업적 백오십", "A Hundred and Fifty"), (200, "업적 이백", "Two Hundred"), (250, "업적 이백오십", "Two Hundred Fifty"), (299, "업적의 끝자락", "The Last Stretch")]:
        m.add("업적", ko, en, f"업적을 {n}개 달성했다.", f"Unlocked {n} achievements.", f"achv>={n}", 1 + (n >= 200) + (n >= 299))

    for n, ko, en in [(1, "첫 밤", "First Night"), (10, "밤의 단골", "Night Regular"), (30, "밤의 주인", "Master of the Night"), (60, "밤의 연대기", "Chronicle of Nights"),
                      (90, "밤의 사서", "Librarian of Nights"), (120, "모든 밤을 보다", "Every Night Seen")]:
        m.add("성인", ko, en, f"성인 이벤트를 {n}개 봤다.", f"Saw {n} adult events.", f"NEW:adult_seen>={n}", 1 + (n >= 30) + (n >= 90))
    m.add("성인", "양쪽의 밤", "Both Sides of Night", "한 회차에서 남녀 상대의 성인 이벤트를 모두 봤다.", "Saw adult events with both male and female partners in one run.", "NEW:adult_both_genders", 2)
    m.add("성인", "금욕", "Abstinence", "성인 이벤트를 켠 채 성인 이벤트 없이 교단 10년을 넘겼다.", "Passed cult year 10 with adult events enabled and none seen.", "NEW:adult_none_cultyears>=10", 3)
    m.add("성인", "열락의 교주", "Leader of Ecstasy", "음욕 100과 신도 500명을 동시에 달성했다.", "Reached Lust 100 and 500 followers at the same time.", "p:Lust>=100 s:Followers>=500", 3)

    for n, ko, en in [(100, "백 개의 사건", "A Hundred Events"), (300, "삼백 개의 사건", "Three Hundred Events"), (500, "오백 개의 사건", "Five Hundred Events"),
                      (800, "팔백 개의 사건", "Eight Hundred Events"), (1000, "천 개의 사건", "A Thousand Events")]:
        m.add("이벤트", ko, en, f"이벤트를 {n}개 겪었다 (누적).", f"Experienced {n} events (all runs).", f"profile:events>={n}", 1 + (n >= 500) + (n >= 1000))
    for n, ko, en in [(5, "연전연승", "On a Roll"), (10, "승승장구", "Unstoppable"), (20, "신의 가호", "Divine Favor")]:
        m.add("이벤트", ko, en, f"선택 판정에서 연속 {n}번 성공했다.", f"Succeeded {n} checks in a row.", f"NEW:success_streak>={n}", 1 + (n >= 10) + (n >= 20))
    for n, ko, en in [(5, "연패", "Losing Streak"), (10, "불운의 늪", "Swamp of Misfortune"), (20, "저주받은 손", "Cursed Hands")]:
        m.add("이벤트", ko, en, f"선택 판정에서 연속 {n}번 실패했다.", f"Failed {n} checks in a row.", f"NEW:fail_streak>={n}", 1 + (n >= 10) + (n >= 20))
    for n, ko, en in [(1, "첫 뇌물", "First Bribe"), (5, "뇌물의 맛", "A Taste for Bribes"), (20, "뇌물 수수자", "Habitual Taker")]:
        m.add("재산", ko, en, f"뇌물을 직접 {n}번 받았다.", f"Took a bribe in person {n} times.", f"NEW:bribes_taken>={n}", 1 + (n >= 20))
    m.add("재산", "빈털터리", "Penniless", "재산이 0이 되었다.", "Wealth fell to 0.", "p:Wealth<=0", 1)
    m.add("재산", "청빈한 교주", "Pious Pauper", "재산 0인 채 신도 100명을 모았다.", "Gathered 100 followers with 0 Wealth.", "p:Wealth<=0 s:Followers>=100", 3)
    m.add("재산", "황금 손", "Golden Touch", "재산 100과 헌금 700을 함께 달성했다.", "Reached Wealth 100 and Offerings 700 together.", "p:Wealth>=100 s:Offerings>=700", 3)

    # 숨은 업적과 도전
    hidden = [
        ("서두른 교주", "Hasty Prophet", "20세 이전에 교단을 세웠다.", "Founded a cult before turning 20.", "NEW:found_age<=19"),
        ("늦깎이 교주", "Late Bloomer", "50세 이후에 교단을 세웠다.", "Founded a cult after turning 50.", "NEW:found_age>=50"),
        ("백발의 교주", "Gray-Haired Leader", "교주로 70세를 맞았다.", "Turned 70 as a cult leader.", "age>=70 founded"),
        ("무릎 꿇은 군중", "Kneeling Crowd", "신도 1000명과 권위 900을 함께 달성했다.", "Reached 1,000 followers and 900 Authority together.", "s:Followers>=1000 s:Authority>=900"),
        ("조용한 성자", "Quiet Saint", "신도 300명 이상, 음욕 0으로 교단 10년을 넘겼다.", "Passed cult year 10 with 300+ followers and 0 Lust.", "p:Lust<=0 s:Followers>=300 cultyears>=10"),
        ("타락한 성자", "Fallen Saint", "신앙심 100과 음욕 100을 동시에 달성했다.", "Reached Piety 100 and Lust 100 at once.", "p:Piety>=100 p:Lust>=100"),
        ("청렴한 부자", "Honest Rich", "청렴 100과 재산 100을 동시에 달성했다.", "Reached Integrity 100 and Wealth 100 at once.", "p:Integrity>=100 p:Wealth>=100"),
        ("굶주린 권력자", "Hungry Tyrant", "권력욕 100과 식욕 100을 동시에 달성했다.", "Reached Ambition 100 and Appetite 100 at once.", "p:Ambition>=100 p:Appetite>=100"),
        ("한 해 한 걸음", "Step by Step", "10년 동안 단 한 번도 교단 스탯이 줄지 않았다.", "Passed 10 cult years without any cult stat dropping.", "NEW:no_cult_decline_years>=10"),
        ("전부 실패", "Total Failure", "한 해 동안 교단 행동 판정을 전부 실패했다.", "Failed every cult action check in a single year.", "NEW:year_all_fail"),
        ("전부 성공", "Flawless Year", "한 해 동안 교단 행동 판정을 전부 성공했다.", "Succeeded every cult action check in a single year.", "NEW:year_all_success"),
        ("무사안일", "Peace of Mind", "한 번도 실패하지 않고 교단 5년을 보냈다.", "Spent 5 cult years without failing a single check.", "NEW:no_fail_cultyears>=5"),
        ("일곱 번째 죄", "The Seventh Sin", "서로 다른 음욕·재물욕·식욕·권력욕 이벤트를 각각 10번씩 겪었다.", "Experienced 10 events each of lust, greed, appetite and ambition.", "NEW:desire_events_each>=10"),
        ("일곱 번째 덕", "The Seventh Virtue", "서로 다른 절제·자애·인내·청렴 이벤트를 각각 10번씩 겪었다.", "Experienced 10 events each of temperance, charity, patience and integrity.", "NEW:virtue_events_each>=10"),
        ("평형", "Equilibrium", "여덟 욕망·미덕 스탯의 최고값과 최저값 차이를 20 이내로 유지했다.", "Kept the eight desire/virtue stats within 20 of each other.", "NEW:balanced_desire_virtue<=20"),
        ("극단", "Extremes", "한 스탯은 100, 반대 스탯은 0이었다.", "Had one stat at 100 and its opposite at 0.", "NEW:opposite_extreme"),
        ("어린 시절의 끝", "End of Childhood", "13세 이전에 기프티드를 3개 얻었다.", "Gained 3 Gifted traits before age 13.", "NEW:gifted_before_13>=3"),
        ("요절", "Gone Too Soon", "20세 이전에 사망했다.", "Died before turning 20.", "NEW:death_age<=19"),
        ("노쇠", "Worn Down", "체력 0으로 사망했다.", "Died with 0 Health.", "NEW:death_cause_health"),
        ("마지막 설교", "Final Sermon", "사망한 해에 설교를 했다.", "Preached in the year you died.", "NEW:death_year_preach"),
        ("가문의 영광", "Family Glory", "교단 창설 후 20년 동안 해산 위기를 겪지 않았다.", "Went 20 cult years without nearing dissolution.", "NEW:no_dissolve_risk_cultyears>=20"),
        ("물려받은 믿음", "Inherited Faith", "회차를 넘겨 유산 효과가 5개 이상 쌓였다.", "Accumulated 5 or more legacy effects across runs.", "NEW:legacy_effects>=5"),
        ("모든 행동의 달인", "Master of All Deeds", "모든 인생 행동과 교단 행동을 각 100번 이상 했다.", "Performed every life and cult action 100+ times.", "NEW:all_actions>=100"),
        ("하루도 쉬지 않고", "Never Rest", "휴식 없이 3년을 보냈다.", "Spent 3 years without resting.", "NEW:no_rest_years>=3"),
        ("느긋한 삶", "Easy Living", "휴식을 50번 했다.", "Rested 50 times.", "c:act_Rest>=50"),
        ("모든 잔을 채워", "Every Cup Filled", "한 회차에 성인 이벤트와 일반 이벤트를 합쳐 500개를 겪었다.", "Experienced 500 events in a single run.", "c:events>=500"),
        ("두 번째 기회", "Second Chance", "해산 직후 바로 새 교단을 세웠다.", "Founded a new cult right after a dissolution.", "NEW:refound_after_dissolve"),
        ("나는 신이다", "I Am a God", "신앙심 100, 권위 1000, 신도 3000명을 동시에 달성했다.", "Reached Piety 100, 1,000 Authority and 3,000 followers together.", "p:Piety>=100 s:Authority>=1000 s:Followers>=3000"),
        ("모두에게 잊혀진", "Forgotten by All", "명망 0, 신도 0으로 마지막까지 살아남았다.", "Survived to the end with 0 Reputation and 0 followers.", "NEW:alone_survival"),
        ("만찬의 끝", "End of the Feast", "식욕 100으로 사망했다.", "Died with Appetite at 100.", "NEW:death_with_Appetite100"),
        ("순수한 마음", "Pure Heart", "모든 미덕 스탯을 60 이상으로 동시에 유지했다.", "Held all four virtue stats at 60+ at once.", "p:Temperance>=60 p:Charity>=60 p:Patience>=60 p:Integrity>=60"),
        ("불타는 마음", "Burning Heart", "모든 욕망 스탯을 60 이상으로 동시에 유지했다.", "Held all four desire stats at 60+ at once.", "p:Lust>=60 p:Greed>=60 p:Appetite>=60 p:Ambition>=60"),
        ("교리 백지", "Blank Doctrine", "교리를 하나도 배우지 않고 교단 5년을 보냈다.", "Passed cult year 5 without learning a doctrine.", "NEW:no_doctrine_cultyears>=5"),
        ("완벽한 교리", "Perfect Doctrine", "한 갈래의 교리를 전부 익혔다.", "Learned every doctrine of a single branch.", "NEW:branch_complete"),
    ]
    DROP = ['하루도 쉬지 않고', '느긋한 삶', '모든 잔을 채워', '교리 백지', '만찬의 끝', '마지막 설교', '노쇠', '어린 시절의 끝', '가문의 영광', '물려받은 믿음', '모두에게 잊혀진', '한 해 한 걸음', '서두른 교주', '늦깎이 교주', '두 번째 기회', '일곱 번째 죄', '일곱 번째 덕', '무사안일', '전부 실패', '전부 성공', '순수한 마음', '불타는 마음']
    for ko, en, dko, den, cond in hidden:
        if ko in DROP: continue
        m.add("숨은·도전", ko, en, dko, den, cond, 2)
    return m

# ───────────────────────── 준비된 신 모드 ─────────────────────────
def god():
    m = Mode("god", "g", 1)
    for n, ko, en in [(10, "신성의 싹", "Seed of Divinity"), (25, "신성의 가지", "Branch of Divinity"), (50, "신성의 나무", "Tree of Divinity"),
                      (75, "신성의 숲", "Forest of Divinity"), (100, "신성의 하늘", "Sky of Divinity"), (125, "신성의 완성", "Divinity Complete")]:
        m.add("신성 트리", ko, en, f"신성 트리 노드를 한 회차에 {n}개 익혔다 (최고 기록).", f"Learned {n} nodes of the divine tree in a single run (best run).", f"NEW:god_nodes>={n}", 1 + (n >= 50) + (n >= 100))
    branches = [("Miracle", "기적", "Miracle"), ("Prophecy", "예언", "Prophecy"), ("Mercy", "자비", "Mercy"), ("Desire", "욕망", "Desire")]
    for key, ko, en in branches:
        for n, tko, ten in [(10, "의 길잡이", " Guide"), (20, "의 사도", " Apostle"), (30, "의 주재자", " Sovereign")]:
            m.add("신성 트리", f"{ko}{tko}", f"{en}{ten}", f"{ko} 갈래 노드를 한 회차에 {n}개 익혔다 (최고 기록).", f"Learned {n} nodes of the {en} branch in a single run (best run).", f"NEW:god_branch_{key}>={n}", n // 10)
        m.add("신성 트리", f"{ko}의 핵", f"Core of {en}", f"{ko}의 핵을 익혔다.", f"Learned the Core of {en}.", f"NEW:god_core_{key}", 2)
    ends = [("승천 · 기적과 예언", "Ascension: Miracle & Prophecy"), ("승천 · 기적과 자비", "Ascension: Miracle & Mercy"), ("승천 · 기적과 욕망", "Ascension: Miracle & Desire"),
            ("승천 · 예언과 자비", "Ascension: Prophecy & Mercy"), ("승천 · 예언과 욕망", "Ascension: Prophecy & Desire"), ("승천 · 자비와 욕망", "Ascension: Mercy & Desire"),
            ("그릇을 잃은 신", "A God Without a Vessel", "그릇의 붕괴", "Collapse of the Vessel"), ("텅 빈 제단", "The Empty Altar", "잊힌 신", "The Forgotten God"), ("봉인된 신전", "The Sealed Temple", "해산된 교단", "The Disbanded Cult")]
    for i, e in enumerate(ends):
        if len(e) == 4:  # 패배 엔딩 3종: 업적 이름과 실제 엔딩 이름이 다르다
            ko, en, end_ko, end_en = e
            m.add("추락", ko, en, f"'{end_ko}' 엔딩을 봤다.", f"Saw the ending '{end_en}'.", f"NEW:god_ending_{i + 1}", 2)
        else:
            ko, en = e
            m.add("승천", ko, en, f"'{ko}' 엔딩을 봤다.", f"Saw the ending '{en}'.", f"NEW:god_ending_{i + 1}", 3)
    for n, ko, en in [(1, "첫 승천", "First Ascension"), (3, "세 번의 승천", "Three Ascensions"), (5, "다섯 번의 승천", "Five Ascensions"), (10, "열 번의 승천", "Ten Ascensions"), (20, "스무 번의 승천", "Twenty Ascensions")]:
        m.add("승천", ko, en, f"신 모드에서 승천을 {n}번 했다.", f"Ascended {n} times in God mode.", f"profile:god_ascended>={n}", 1 + (n >= 5) + (n >= 20))
    for n, ko, en in [(3, "아홉 중 셋", "Three of Nine"), (6, "아홉 중 여섯", "Six of Nine"), (9, "아홉 개의 하늘", "Nine Heavens")]:
        m.add("승천", ko, en, f"서로 다른 승천 엔딩을 {n}개 봤다.", f"Saw {n} different ascension endings.", f"NEW:god_endings_seen>={n}", 1 + (n >= 6) + (n >= 9))
    acts = [("Wonder", "이적", "Wonders"), ("Revelation", "계시", "Revelations"), ("Healing", "치유", "Healing"), ("Feast", "연회", "Feasts"), ("Commune", "교감", "Communion"), ("Consecrate", "신전 봉헌", "Temple Consecration")]
    for key, ko, en in acts:
        m.ladder("신의 행동", ko, en, f"NEW:god_act_{key}", [10, 30, 60, 100])
    bld = [("Statue", "신상", "Statues"), ("Temple", "신전", "Temples"), ("Library", "도서관", "Libraries"), ("Village", "마을", "Villages"), ("Shelter", "쉼터", "Shelters")]
    for key, ko, en in bld:
        for i, n in enumerate([1, 3, 5, 10]):
            nm_ko = [f"첫 {ko}", f"{ko} 세 곳", f"{ko} 다섯 곳", f"{ko}의 도시"][i]
            nm_en = [f"First {en[:-1] if en.endswith('s') and key != 'Library' else en}", f"Three {en}", f"Five {en}", f"City of {en}"][i]
            if key == "Library": nm_en = [f"First Library", f"Three Libraries", f"Five Libraries", f"City of Libraries"][i]
            m.add("건축", nm_ko, nm_en, f"{ko}을(를) 누적 {n}개 지었다 (모든 회차 합산).", f"Built {n} {en.lower()} in total (all runs combined).", f"profile:god_build_{key}>={n}", i + 1)
    for key, ko, en in [("Envy", "하늘의 시기", "Envy of Heaven"), ("Suspicion", "세상의 의심", "Suspicion of the World")]:
        for n, tko, ten in [(50, "절반의", "Half-Raised"), (80, "고조된", "Rising"), (95, "임계의", "Critical")]:
            m.add("압력", f"{tko} {ko}", f"{ten} {en}", f"{ko}이(가) {n}에 이르렀다.", f"{en} reached {n}.", f"NEW:god_{key}>={n}", 1 + (n >= 80) + (n >= 95))
        m.add("압력", f"{ko} 없는 신", f"God Without {en}", f"{ko}을(를) 10 이하로 10년 동안 유지했다.", f"Kept {en} at 10 or below for 10 years.", f"NEW:god_{key}_low10y>=1", 3)
        m.add("압력", f"{ko}를 견디다", f"Withstood {en}", f"{ko}이(가) 95를 넘었다가 끝내 승천했다.", f"Ascended after {en} had passed 95.", f"NEW:god_{key}_peak95_ascend", 3)
    for n, ko, en in [(1, "첫 빼앗김", "First Seizure"), (5, "다섯 번 빼앗기다", "Seized Five Times"), (15, "빼앗기는 일상", "Routine Seizure")]:
        m.add("압력", ko, en, f"그릇을 누적 {n}번 빼앗겼다 (모든 회차 합산).", f"Had your vessel seized {n} times in total (all runs combined).", f"profile:god_seized>={n}", 1 + (n >= 5) + (n >= 15))
    for n, ko, en in [(1, "신의 첫 해", "First Year as a God"), (5, "신의 오 년", "Five Years as a God"), (10, "신의 십 년", "Ten Years as a God"),
                      (25, "신의 사반세기", "A Quarter Century as a God"), (50, "신의 반세기", "Half a Century as a God"), (100, "신의 백 년", "A Century as a God")]:
        m.add("시간", ko, en, f"신으로서 한 회차에 {n}년을 보냈다 (최고 기록).", f"Spent {n} years as a god in a single run (best run).", f"NEW:god_years>={n}", 1 + (n >= 10) + (n >= 50))
    cats = [("heaven", "하늘", "Heaven"), ("earth", "땅", "Earth"), ("indulge", "탐닉", "Indulgence"), ("vessel", "그릇", "Vessel"), ("random", "우연", "Chance")]
    for key, ko, en in cats:
        for n in (10, 25, 40):
            m.add("이벤트", f"{ko}의 사건 {n}", f"{en} Events {n}", f"신 모드의 '{ko}' 이벤트를 {n}개 겪었다.", f"Experienced {n} '{en}' events in God mode.", f"profile:god_{key}>={n}", 1 + (n >= 25) + (n >= 40))
    for n, ko, en in [(50, "오십 개의 계시", "Fifty Omens"), (100, "백 개의 계시", "A Hundred Omens"), (150, "백오십 개의 계시", "A Hundred Fifty Omens"), (200, "모든 계시", "Every Omen")]:
        m.add("이벤트", ko, en, f"신 모드 이벤트를 {n}개 겪었다 (누적).", f"Experienced {n} God mode events (all runs).", f"profile:god_events>={n}", 1 + (n >= 100) + (n >= 200))
    for n, ko, en in [(100, "신앙의 불씨", "Spark of Faith"), (500, "신앙의 화로", "Hearth of Faith"), (1000, "신앙의 불길", "Blaze of Faith"),
                      (5000, "신앙의 바다", "Ocean of Faith"), (10000, "신앙의 하늘", "Heaven of Faith")]:
        m.add("신앙 포인트", ko, en, f"신앙 포인트를 누적 {n} 모았다.", f"Gathered {n} Faith Points in total.", f"NEW:god_faith_points>={n}", 1 + (n >= 1000) + (n >= 10000))
    challenge = [
        ("빠른 승천", "Swift Ascension", "신으로 5년 안에 승천했다.", "Ascended within 5 years as a god.", "NEW:god_ascend_years<=5"),
        ("느린 승천", "Slow Ascension", "신으로 50년 이상 버틴 뒤 승천했다.", "Ascended after lasting 50+ years as a god.", "NEW:god_ascend_years>=50"),
        ("건물 없는 신", "Godless Architecture", "건물을 하나도 짓지 않고 승천했다.", "Ascended without building anything.", "NEW:god_ascend_buildings==0"),
        ("신전의 신", "God of Temples", "신전만 10개 이상 짓고 승천했다.", "Ascended with 10+ temples built.", "NEW:god_ascend_Temple>=10"),
        ("기적 없는 승천", "Ascent Without Wonders", "이적을 한 번도 쓰지 않고 승천했다.", "Ascended without using a single Wonder.", "NEW:god_ascend_Wonder==0"),
        ("치유 없는 승천", "Ascent Without Healing", "치유를 한 번도 쓰지 않고 승천했다.", "Ascended without ever healing.", "NEW:god_ascend_Healing==0"),
        ("침묵의 신", "Silent God", "계시를 한 번도 내리지 않고 승천했다.", "Ascended without a single revelation.", "NEW:god_ascend_Revelation==0"),
        ("연회 없는 승천", "Ascent Without Feasts", "연회를 한 번도 열지 않고 승천했다.", "Ascended without ever holding a feast.", "NEW:god_ascend_Feast==0"),
        ("교감 없는 승천", "Ascent Without Communion", "교감을 한 번도 하지 않고 승천했다.", "Ascended without communion.", "NEW:god_ascend_Commune==0"),
        ("봉헌 없는 승천", "Ascent Without Consecration", "봉헌을 한 번도 하지 않고 승천했다.", "Ascended without consecrating a temple.", "NEW:god_ascend_Consecrate==0"),
        ("두 갈래의 신", "Two-Branch God", "두 핵의 갈래 밖 노드를 하나도 익히지 않고 승천했다.", "Ascended without learning any node outside your two core branches.", "NEW:god_ascend_single_branch"),
        ("네 갈래의 신", "Four-Branch God", "네 갈래 모두 20개 이상 익히고 승천했다.", "Ascended with 20+ nodes in every branch.", "NEW:god_ascend_all_branches>=20"),
        ("시기의 정점", "Peak of Envy", "하늘의 시기 95 이상에서 승천했다.", "Ascended while Envy was 95 or higher.", "NEW:god_ascend_Envy>=95"),
        ("의심의 정점", "Peak of Suspicion", "세상의 의심 95 이상에서 승천했다.", "Ascended while Suspicion was 95 or higher.", "NEW:god_ascend_Suspicion>=95"),
        ("두 압력의 정점", "Both Pressures Peak", "시기와 의심이 모두 90 이상일 때 승천했다.", "Ascended with both Envy and Suspicion at 90+.", "NEW:god_ascend_both>=90"),
        ("무풍지대", "Eye of the Storm", "시기와 의심이 모두 20 이하로 승천했다.", "Ascended with both Envy and Suspicion at 20 or below.", "NEW:god_ascend_both_max<=20"),
        ("신앙 부자", "Rich in Faith", "신앙 포인트 1000 이상 보유한 채 승천했다.", "Ascended holding 1,000+ Faith Points.", "NEW:god_ascend_points>=1000"),
        ("빈손의 신", "Empty-Handed God", "신앙 포인트가 0인 채 승천했다.", "Ascended with 0 Faith Points.", "NEW:god_ascend_points==0"),
        ("인간의 몸으로", "In a Mortal Frame", "그릇이 한 번도 회복되지 않은 채 승천했다.", "Ascended without ever recovering the vessel.", "NEW:god_ascend_no_vessel_recovery"),
        ("튼튼한 그릇", "Sturdy Vessel", "그릇을 한 번도 빼앗기지 않고 승천했다.", "Ascended without the vessel ever being seized.", "NEW:god_ascend_seized==0"),
        ("스무 번의 탐닉", "Twenty Indulgences", "한 회차에 탐닉 이벤트를 20번 겪었다.", "Experienced 20 indulgence events in one run.", "NEW:god_indulge_run>=20"),
        ("탐닉 없는 신", "Abstaining God", "탐닉 이벤트를 한 번도 겪지 않고 승천했다.", "Ascended without any indulgence events.", "NEW:god_ascend_indulge==0"),
        ("하늘의 총애", "Favored by Heaven", "하늘 이벤트에서 10번 연속 이로운 선택을 했다.", "Made the beneficial choice 10 times in a row in heaven events.", "NEW:god_heaven_streak>=10"),
        ("땅의 친구", "Friend of Earth", "땅 이벤트에서 10번 연속 이로운 선택을 했다.", "Made the beneficial choice 10 times in a row in earth events.", "NEW:god_earth_streak>=10"),
        ("한 번에 하나", "One at a Time", "한 해에 같은 신의 행동을 5번 이상 쓰지 않았다.", "Never used the same god action 5+ times in a year.", "NEW:god_varied_year"),
        ("같은 행동만", "One Trick God", "한 해 동안 같은 신의 행동만 10번 썼다.", "Used the same god action 10 times in a single year.", "NEW:god_single_action_year>=10"),
        ("숨 고르기", "Catching Breath", "한 해 동안 아무 행동도 하지 않았다.", "Took no god action for an entire year.", "NEW:god_idle_year"),
        ("모든 행동", "All Actions", "한 회차에 여섯 가지 신의 행동을 모두 썼다.", "Used all six god actions in one run.", "NEW:god_all_actions"),
        ("모든 건물", "All Buildings", "한 회차에 다섯 종류 건물을 모두 지었다.", "Built all five building types in one run.", "NEW:god_all_buildings"),
        ("모든 이벤트 종류", "All Kinds of Omens", "한 회차에 다섯 종류 이벤트를 모두 겪었다.", "Experienced all five event categories in one run.", "NEW:god_all_event_kinds"),
        ("화려한 승천", "Grand Ascension", "건물 20개 이상과 노드 100개 이상으로 승천했다.", "Ascended with 20+ buildings and 100+ nodes.", "NEW:god_ascend_buildings>=20 god_nodes>=100"),
        ("검소한 승천", "Frugal Ascension", "노드 30개 이하로 승천했다.", "Ascended with 30 nodes or fewer.", "NEW:god_ascend_nodes<=30"),
        ("신이 된 교주", "Leader Turned God", "일반 모드에서 교단이 해산된 적이 있는 채로 신 모드에서 승천했다.", "Ascended in God mode after having had a cult dissolved at some point.", "NEW:dissolved_then_ascended"),
        ("하늘의 아이", "Child of Heaven", "신 모드를 처음 열자마자 첫 해에 첫 건물을 지었다.", "Built your first building in the first year of a run.", "NEW:god_build_first_year"),
        ("끝없는 순례", "Endless Pilgrimage", "신 모드를 10회차 시작했다.", "Started 10 God mode runs.", "profile:god_runs>=10"),
        ("지치지 않는 신", "Tireless God", "신 모드를 30회차 시작했다.", "Started 30 God mode runs.", "profile:god_runs>=30"),
        ("실패한 신", "Failed God", "승천하지 못하고 그릇이 소멸했다.", "Your vessel perished before ascension.", "NEW:god_vessel_perished"),
        ("세 번의 추락", "Three Falls", "승천에 실패한 회차가 3번이 되었다.", "Failed to ascend in three runs.", "NEW:god_failed_runs>=3"),
        ("다시 일어서는 신", "God Who Rises Again", "승천 실패 직후 다음 회차에서 승천했다.", "Ascended in the run right after a failed one.", "NEW:god_ascend_after_fail"),
        ("백 번의 이적", "Hundred Wonders", "이적을 한 회차에 100번 썼다.", "Used Wonder 100 times in one run.", "NEW:god_run_Wonder>=100"),
        ("백 번의 치유", "Hundred Healings", "치유를 한 회차에 100번 썼다.", "Used Healing 100 times in one run.", "NEW:god_run_Healing>=100"),
        ("백 번의 계시", "Hundred Revelations", "계시를 한 회차에 100번 내렸다.", "Delivered 100 revelations in one run.", "NEW:god_run_Revelation>=100"),
        ("백 번의 연회", "Hundred Feasts", "연회를 한 회차에 100번 열었다.", "Held 100 feasts in one run.", "NEW:god_run_Feast>=100"),
        ("다섯 성소", "Five Sanctuaries", "건물을 다섯 번 봉헌했다 (신전 한정 아님, 모든 봉헌 합산).", "Consecrated buildings five times (any building, not only temples).", "NEW:god_consecrated>=5"),
        ("성소의 숲", "Forest of Sanctuaries", "건물을 열 번 봉헌했다 (신전 한정 아님, 모든 봉헌 합산).", "Consecrated buildings ten times (any building, not only temples).", "NEW:god_consecrated>=10"),
        ("하늘 아래 모든 마을", "Every Village Under Heaven", "마을 10곳과 쉼터 10곳을 지었다.", "Built 10 villages and 10 shelters.", "profile:god_build_Village>=10 profile:god_build_Shelter>=10"),
        ("지혜의 전당", "Hall of Wisdom", "도서관 10곳을 지었다.", "Built 10 libraries.", "profile:god_build_Library>=10"),
        ("경배받는 모습", "Idol Worshiped", "신상 10개를 세웠다.", "Raised 10 statues.", "profile:god_build_Statue>=10"),
    ]
    for ko, en, dko, den, cond in challenge:
        m.add("도전", ko, en, dko, den, cond, 2)
    for key, ko, en in acts:
        m.add("신의 행동", f"{ko} 오십 번", f"Fifty {en}", f"한 회차에 {ko}을(를) 50번 했다.", f"Did {en.lower()} 50 times in one run.", f"NEW:god_run_{key}>=50", 2)
    for key, ko, en in bld:
        m.add("건축", f"{ko} 스무 곳", f"Twenty {en}", f"{ko}을(를) 20개 지었다.", f"Built 20 {en.lower()}.", f"NEW:god_build_{key}>=20", 3)
    for key, ko, en in branches:
        m.add("신성 트리", f"{ko}의 극의", f"Apex of {en}", f"{ko} 갈래 노드를 모두 익혔다.", f"Learned every node of the {en} branch.", f"NEW:god_branch_{key}_full", 3)
        m.add("승천", f"{ko}으로 세 번", f"{en} Thrice", f"{ko}의 핵을 익히고 승천을 3번 했다.", f"Ascended 3 times with the Core of {en}.", f"NEW:god_core_ascend_{key}>=3", 3)
    m.add("신앙 포인트", "신앙의 우주", "Cosmos of Faith", "신앙 포인트를 누적 20000 모았다.", "Gathered 20,000 Faith Points in total.", "NEW:god_faith_points>=20000", 3)
    m.add("신앙 포인트", "신앙의 영원", "Eternity of Faith", "신앙 포인트를 누적 50000 모았다.", "Gathered 50,000 Faith Points in total.", "NEW:god_faith_points>=50000", 3)
    m.add("시간", "신의 칠십오 년", "Seventy-Five Years as a God", "신으로서 75년을 보냈다.", "Spent 75 years as a god.", "NEW:god_years>=75", 3)
    m.add("압력", "빼앗김의 달인", "Veteran of Seizure", "그릇을 30번 빼앗겼다.", "Had your vessel seized 30 times.", "profile:god_seized>=30", 3)
    for n, ko, en in [(1, "그릇의 회복", "Vessel Restored"), (10, "열 번의 회복", "Ten Recoveries"), (30, "끈질긴 그릇", "Persistent Vessel")]:
        m.add("압력", ko, en, f"연회로 그릇을 누적 {n}번 회복했다.", f"Recovered the vessel {n} times in total through feasts.", f"profile:god_vessel_recovery>={n}", 1 + (n >= 10) + (n >= 30))
    return m

# ───────────────────────── 거울 세계 모드 ─────────────────────────
def mirror():
    m = Mode("mirror", "m", 1)
    with open(os.path.join(HERE, "mirror", "endings_mirror.csv"), encoding="utf-8-sig") as f:
        endings = list(csv.DictReader(f))
    for e in endings:
        tier = 3 if e["group"] == "final_skill" else 1
        m.add("엔딩", e["title"], e["title_en"], f"'{e['title']}' 엔딩을 봤다.", f"Saw the ending '{e['title_en']}'.", f"NEW:mirror_ending_{e['id']}", tier)
    branches = [("lust", "음욕", "Lust"), ("greed", "재물욕", "Greed"), ("appetite", "식욕", "Appetite"), ("ambition", "권력욕", "Ambition"),
                ("temperance", "절제", "Temperance"), ("charity", "자애", "Charity"), ("patience", "인내", "Patience"), ("integrity", "청렴", "Integrity")]
    for key, ko, en in branches:
        for n, tko, ten in [(5, "의 입문자", " Initiate"), (10, "의 수행자", " Practitioner"), (15, "의 완성자", " Perfected")]:
            m.add("거울 교리", f"거울 {ko}{tko}", f"Mirror {en}{ten}", f"거울 세계의 {ko} 갈래 교리를 {n}개 익혔다.", f"Learned {n} Mirror doctrines of the {en} branch.", f"NEW:mirror_doctrine_{key}>={n}", n // 5)
        for suffix in "ab":
            m.add("최종 스킬", f"{ko}의 끝 {suffix.upper()}", f"End of {en} {suffix.upper()}", f"{ko} 갈래의 최종 스킬 {suffix.upper()}를 익혔다.", f"Learned final skill {suffix.upper()} of the {en} branch.", f"NEW:mirror_final_{key}10{suffix}", 3)
    for n, ko, en in [(10, "교리 열 개", "Ten Doctrines"), (30, "교리 서른 개", "Thirty Doctrines"), (60, "교리 예순 개", "Sixty Doctrines"), (90, "교리 아흔 개", "Ninety Doctrines"), (120, "거울 교리 완성", "Mirror Codex Complete")]:
        m.add("거울 교리", ko, en, f"한 회차에 거울 교리를 {n}개 익혔다.", f"Learned {n} Mirror doctrines in one run.", f"NEW:mirror_doctrines>={n}", 1 + (n >= 60) + (n >= 120))
    for n, ko, en in [(1, "거울 속 첫 해", "First Year in the Mirror"), (3, "거울 속 삼 년", "Three Years in the Mirror"), (5, "유예의 끝", "End of Grace"), (10, "거울 속 십 년", "Ten Years in the Mirror"),
                      (15, "거울 속 십오 년", "Fifteen Years in the Mirror"), (20, "거울 속 이십 년", "Twenty Years in the Mirror"), (25, "거울 속 사반세기", "A Quarter Century in the Mirror"),
                      (30, "거울 속 삼십 년", "Thirty Years in the Mirror"), (40, "거울 속 사십 년", "Forty Years in the Mirror")]:
        m.add("시간", ko, en, f"거울 세계에서 교단 {n}년을 보냈다.", f"Spent {n} cult years in the Mirror World.", f"NEW:mirror_cultyears>={n}", 1 + (n >= 10) + (n >= 25))
    for n, ko, en in [(10, "첫 열 명", "The First Ten"), (50, "오십 명", "Fifty Souls"), (100, "백 명", "A Hundred Souls"), (300, "삼백 명", "Three Hundred Souls"), (500, "오백 명", "Five Hundred Souls"), (1000, "천 명", "A Thousand Souls")]:
        m.add("연쇄 지표", f"거울 신도 {ko}", f"Mirror Followers: {en}", f"거울 세계에서 신도 {n}명을 모았다.", f"Gathered {n} followers in the Mirror World.", f"NEW:mirror_followers>={n}", 1 + (n >= 100) + (n >= 500))
    for n, ko, en in [(200, "명망의 싹", "Seed of Renown"), (400, "명망 기준선", "Renown Baseline"), (600, "높아지는 명망", "Rising Renown"), (800, "명망의 정점", "Peak Renown")]:
        m.add("연쇄 지표", ko, en, f"거울 세계에서 명망 {n}에 이르렀다.", f"Reached Reputation {n} in the Mirror World.", f"NEW:mirror_Reputation>={n}", 1 + (n >= 600))
    m.add("연쇄 지표", "명망의 선순환", "Virtuous Circle", "명망 600 이상으로 연간 신도 증가가 최대치(+10)에 도달했다.", "Reached the maximum yearly recruit gain (+10) at Reputation 600+.", "NEW:mirror_recruits_year>=10", 2)
    m.add("연쇄 지표", "명망의 악순환", "Vicious Circle", "명망이 낮아 연간 신도가 6명 줄었다.", "Lost 6 followers in a single year at low Reputation.", "NEW:mirror_recruits_year<=-6", 2)
    m.add("연쇄 지표", "고요한 균형", "Still Balance", "명망 400 근처(±50)에서 5년을 보냈다.", "Spent 5 years within ±50 of Reputation 400.", "NEW:mirror_rep_near_400>=5", 2)
    for n, ko, en in [(100, "헌금 백", "Offerings: A Hundred"), (300, "헌금 삼백", "Offerings: Three Hundred"), (500, "헌금 오백", "Offerings: Five Hundred"), (800, "헌금 팔백", "Offerings: Eight Hundred")]:
        m.add("연쇄 지표", ko, en, f"거울 세계에서 헌금 {n}에 이르렀다.", f"Reached {n} Offerings in the Mirror World.", f"NEW:mirror_Offerings>={n}", 1 + (n >= 500))
    m.add("연쇄 지표", "한 방향의 사슬", "One-Way Chain", "신도 → 명망 → 헌금의 세 지표를 모두 300 이상으로 올렸다.", "Raised Followers, Reputation and Offerings all to 300+.", "NEW:mirror_chain_all>=300", 2)
    for n, ko, en in [(2, "불운 징조", "Omen of Misfortune"), (4, "불운의 그림자", "Shadow of Misfortune"), (6, "불운의 늪", "Marsh of Misfortune"), (8, "불운의 폭풍", "Storm of Misfortune"), (10, "불운의 극치", "Peak Misfortune")]:
        m.add("불운", ko, en, f"불운 수치가 {n}에 이르렀다.", f"Misfortune reached {n}.", f"NEW:mirror_misfortune>={n}", 1 + (n >= 6) + (n >= 10))
    m.add("불운", "불운 속의 번영", "Prosperity in Misfortune", "불운 8 이상인 채 신도 500명을 모았다.", "Gathered 500 followers while Misfortune was 8 or higher.", "NEW:mirror_misfortune>=8 mirror_followers>=500", 3)
    m.add("불운", "행운아", "Lucky Soul", "불운 1 이하로 교단 10년을 보냈다.", "Spent 10 cult years with Misfortune at 1 or below.", "NEW:mirror_misfortune_low_years>=10", 3)
    m.add("불운", "불운 속 십 년", "Ten Years in Misfortune", "불운 6 이상인 채로 교단 10년을 보냈다.", "Spent 10 cult years with Misfortune at 6 or higher.", "NEW:mirror_misfortune_high_years>=10", 2)
    for n, ko, en in [(1, "첫 실패", "First Failure"), (10, "열 번의 실패", "Ten Failures"), (30, "서른 번의 실패", "Thirty Failures"), (60, "예순 번의 실패", "Sixty Failures")]:
        m.add("불운", ko, en, f"거울 세계에서 교단 행동 판정에 {n}번 실패했다.", f"Failed {n} cult action checks in the Mirror World.", f"NEW:mirror_fail_acts>={n}", 1 + (n >= 30))
    for n, ko, en in [(0, "순수한 욕망", "Pure Desire"), (5, "욕망의 기울기", "Lean of Desire")]:
        pass
    m.add("균형", "순수한 욕망", "Pure Desire", "욕망 평균이 미덕 평균보다 40 이상 높게 엔딩을 맞았다.", "Reached an ending with the desire average 40+ above the virtue average.", "NEW:mirror_gap_desire>=40", 3)
    m.add("균형", "순수한 미덕", "Pure Virtue", "미덕 평균이 욕망 평균보다 40 이상 높게 엔딩을 맞았다.", "Reached an ending with the virtue average 40+ above the desire average.", "NEW:mirror_gap_virtue>=40", 3)
    m.add("균형", "완벽한 오 대 오", "A Perfect Five-Five", "욕망과 미덕 평균 차이 5 이내로 엔딩을 맞았다.", "Reached an ending with desire and virtue averages within 5.", "NEW:mirror_gap<=5", 3)
    m.add("균형", "욕망의 우세", "Desire Prevails", "욕망 평균이 미덕 평균보다 높은 채 교단 25년을 맞았다.", "Reached cult year 25 with a higher desire average.", "NEW:mirror_desire_lead_y25", 2)
    m.add("균형", "미덕의 우세", "Virtue Prevails", "미덕 평균이 욕망 평균보다 높은 채 교단 25년을 맞았다.", "Reached cult year 25 with a higher virtue average.", "NEW:mirror_virtue_lead_y25", 2)
    m.add("균형", "저울질", "Weighing the Scales", "교단 중 욕망·미덕 우세가 세 번 이상 뒤바뀌었다.", "The desire/virtue lead swapped three or more times in a single run.", "NEW:mirror_lead_swaps>=3", 2)
    for n, ko, en in [(1, "첫 거울", "First Mirror"), (3, "세 개의 거울", "Three Mirrors"), (5, "다섯 개의 거울", "Five Mirrors"), (10, "열 개의 거울", "Ten Mirrors"), (20, "스무 개의 거울", "Twenty Mirrors")]:
        m.add("회차", ko, en, f"거울 세계를 {n}회차 시작했다.", f"Started {n} Mirror World runs.", f"profile:mirror_runs>={n}", 1 + (n >= 5) + (n >= 20))
    for n, ko, en in [(1, "첫 붕괴", "First Collapse"), (3, "세 번의 붕괴", "Three Collapses"), (5, "다섯 번의 붕괴", "Five Collapses")]:
        m.add("회차", ko, en, f"거울 세계에서 교단이 붕괴한 적이 {n}번 있다.", f"Your Mirror World cult collapsed {n} times.", f"profile:mirror_collapse>={n}", 1 + (n >= 5))
    for n, ko, en in [(1, "첫 완성", "First Completion"), (5, "다섯 번의 완성", "Five Completions"), (10, "열 번의 완성", "Ten Completions")]:
        m.add("회차", ko, en, f"거울 세계에서 최종 스킬 엔딩을 {n}번 봤다.", f"Saw a Mirror final-skill ending {n} times.", f"profile:mirror_final>={n}", 1 + (n >= 5) + (n >= 10))
    groups = [("mr001", "거울 도입부", "Mirror Prelude"), ("mr051", "거울 전개부", "Mirror Development"), ("mr101", "거울 심화부", "Mirror Depths"),
              ("mr151", "거울 종반부", "Mirror Finale"), ("recovery", "거울 회복", "Mirror Recovery"), ("special", "거울 특수", "Mirror Special"), ("final", "거울 최종", "Mirror Final")]
    for key, ko, en in groups:
        for n in (10, 25, 40):
            m.add("이벤트", f"{ko} {n}", f"{en} {n}", f"'{ko}' 이벤트를 {n}개 겪었다.", f"Experienced {n} '{en}' events.", f"profile:mirror_{key}_events>={n}" if key == "final" else f"profile:mirror_{key}>={n}", 1 + (n >= 25) + (n >= 40))
    for n, ko, en in [(100, "거울 이벤트 백", "A Hundred Mirror Events"), (200, "거울 이벤트 이백", "Two Hundred Mirror Events"), (351, "거울 이벤트 전부", "Every Mirror Event")]:
        m.add("이벤트", ko, en, f"거울 세계 이벤트를 {n}개 겪었다 (누적).", f"Experienced {n} Mirror events (all runs).", f"profile:mirror_events>={n}", 1 + (n >= 200) + (n >= 351))
    for n, ko, en in [(50, "거울 업적 오십", "Fifty Mirror Marks"), (100, "거울 업적 백", "A Hundred Mirror Marks")]:
        m.add("도전", ko, en, f"거울 세계 업적을 {n}개 달성했다.", f"Unlocked {n} Mirror World achievements.", f"NEW:mirror_achv>={n}", 2)
    m.add("엔딩", "열여섯 개의 끝", "Sixteen Ends", "거울 세계의 최종 스킬 16개를 모두 익혔다 (누적).", "Learned all 16 Mirror final skills (all runs).", "NEW:mirror_final_all16", 3)
    m.add("엔딩", "열여섯 개의 영광", "Sixteen Glories", "거울 세계의 최종 스킬 굿 엔딩 16개를 모두 봤다.", "Saw all 16 good final-skill endings.", "NEW:mirror_final_good16", 3)
    m.add("엔딩", "열여섯 개의 그림자", "Sixteen Shadows", "거울 세계의 최종 스킬 배드 엔딩 16개를 모두 봤다.", "Saw all 16 bad final-skill endings.", "NEW:mirror_final_bad16", 3)
    m.add("엔딩", "스무 개의 결말", "Twenty Mirror Endings", "거울 세계 엔딩을 20개 봤다.", "Saw 20 Mirror World endings.", "NEW:mirror_endings_seen>=20", 2)
    m.add("엔딩", "모든 거울 결말", "Every Mirror Ending", "거울 세계 엔딩 41개를 모두 봤다.", "Saw all 41 Mirror World endings.", "NEW:mirror_endings_seen>=41", 3)
    challenge = [
        ("붕괴 없이 25년", "25 Years Intact", "붕괴 없이 교단 25년을 맞았다.", "Reached cult year 25 without collapsing.", "NEW:mirror_cultyears>=25"),
        ("붕괴 직전", "On the Brink", "신도 5명 이하에서 회복해 교단 10년을 넘겼다.", "Recovered from 5 or fewer followers to pass cult year 10.", "NEW:mirror_followers_low_recover"),
        ("바닥에서 정상으로", "Bottom to Top", "신도 5명 이하에서 신도 500명까지 늘렸다.", "Grew from 5 or fewer followers to 500.", "NEW:mirror_followers_5_to_500"),
        ("한 해 만에", "In a Single Year", "한 해 동안 신도 10명을 더 모았다.", "Gained 10 followers in a single year.", "NEW:mirror_followers_year>=10"),
        ("한 해 만에 빈손", "Empty in a Year", "한 해 동안 신도 10명을 잃었다.", "Lost 10 followers in a single year.", "NEW:mirror_followers_year<=-10"),
        ("모든 유예", "Whole Grace Period", "유예 기간(16턴) 동안 한 번도 실패하지 않았다.", "Failed nothing during the 16-turn grace period.", "NEW:mirror_grace_no_fail"),
        ("유예 중 최대 신도", "Grace Period Boom", "유예 기간이 끝나기 전에 신도 100명을 모았다.", "Gathered 100 followers before the grace period ended.", "NEW:mirror_grace_followers>=100"),
        ("교리 포인트 만점", "Doctrine Points Maxed", "한 회차에 교리 포인트를 40 이상 모았다.", "Earned 40+ doctrine points in one run.", "NEW:mirror_doctrine_points>=40"),
        ("두 갈래 사이", "Between Two Branches", "서로 다른 두 갈래 교리를 각각 10개 이상 익혔다.", "Learned 10+ doctrines in two different branches.", "NEW:mirror_two_branches>=10"),
        ("네 갈래 사이", "Between Four Branches", "서로 다른 네 갈래 교리를 각각 5개 이상 익혔다.", "Learned 5+ doctrines in four different branches.", "NEW:mirror_four_branches>=5"),
        ("여덟 갈래", "All Eight Paths", "여덟 갈래 모두에서 교리를 1개 이상 익혔다.", "Learned at least one doctrine in all eight branches.", "NEW:mirror_all_branches>=1"),
        ("한 길만", "One Road", "한 갈래 교리만 익히고 엔딩을 봤다.", "Reached an ending using a single branch only.", "NEW:mirror_single_branch"),
        ("바뀐 길", "Changed Road", "엔딩 직전에 다른 갈래의 최종 스킬로 갈아탔다.", "Switched to another branch's final skill just before the ending.", "NEW:mirror_switch_final"),
        ("거울의 거울", "Mirror of the Mirror", "일반 모드와 거울 세계에서 같은 갈래 최종 스킬을 익혔다.", "Learned the same branch's final skill in Normal and Mirror modes.", "NEW:mirror_normal_same_final"),
        ("서른 번의 행동", "Thirty Deeds", "거울 세계 한 해에 교단 행동을 30번 했다.", "Took 30 cult actions in a single Mirror year.", "NEW:mirror_acts_year>=30"),
        ("게으른 교주", "Idle Leader", "거울 세계 한 해에 교단 행동을 5번 이하만 했다.", "Took 5 or fewer cult actions in a Mirror year.", "NEW:mirror_acts_year<=5"),
        ("회복의 달인", "Master of Recovery", "회복 이벤트를 10번 겪고 살아남았다.", "Survived 10 recovery events.", "NEW:mirror_recovery_events>=10"),
        ("회복 없이", "Without Recovery", "회복 이벤트 없이 교단 15년을 넘겼다.", "Passed cult year 15 without a recovery event.", "NEW:mirror_no_recovery_years>=15"),
        ("특수 이벤트 수집가", "Special Collector", "특수 이벤트를 51개 모두 겪었다.", "Experienced all 51 special events.", "profile:mirror_special>=51"),
        ("최종 이벤트 수집가", "Final Collector", "최종 이벤트 80개를 모두 겪었다.", "Experienced all 80 final events.", "profile:mirror_final>=80"),
        ("거울 속 신도들", "Followers in the Mirror", "거울 세계에서 신도 평균 200명 이상으로 25년을 보냈다.", "Averaged 200+ followers over 25 years.", "NEW:mirror_followers_avg>=200"),
        ("명망의 조각", "Fragment of Renown", "거울 세계에서 명망 400 이상을 20년 유지했다.", "Kept Reputation at 400+ for 20 years.", "NEW:mirror_rep>=400_years>=20"),
        ("명망의 추락", "Fall of Renown", "명망 700 이상에서 200 이하로 추락했다.", "Fell from Reputation 700+ to 200 or lower.", "NEW:mirror_rep_fall"),
        ("헌금의 폭락", "Offerings Crash", "헌금 700 이상에서 100 이하로 떨어졌다.", "Offerings dropped from 700+ to 100 or less.", "NEW:mirror_offerings_crash"),
        ("모든 지표 최대", "All at Maximum", "신도·명망·헌금이 모두 900 이상이었다.", "Had Followers, Reputation and Offerings all at 900+.", "NEW:mirror_chain_all>=900"),
        ("거울 마스터", "Mirror Master", "거울 세계 업적을 100개 달성했다.", "Unlocked 100 Mirror World achievements.", "NEW:mirror_achv>=100"),
        ("전부를 비추다", "Reflect Everything", "거울 세계 업적을 119개 전부 달성했다.", "Unlocked every Mirror World achievement.", "NEW:mirror_achv>=119"),
    ]
    for ko, en, dko, den, cond in challenge:
        m.add("도전", ko, en, dko, den, cond, 2)
    return m

import re
def thr(r):
    nums = re.findall(r"[<>]=?(\d+)|==(\d+)", r["condition"])
    flat = [int(a or b) for a, b in nums]
    return flat[0] if flat else None

# 압축 규칙: (모드, 분류) -> 남길 행 판정. 단계식 업적은 첫 단계·끝 단계만 남기고 가운데를 덜어낸다.
def keep_thr(*vals): return lambda r: thr(r) in vals
def prune(m):
    rules = {
        "normal": {
            "인생 행동": keep_thr(50, 200), "교단 행동": keep_thr(100, 400), "개인 스탯": keep_thr(100),
            "교단 스탯": keep_thr(2500, 1000), "나이": keep_thr(20, 45, 80, 100), "교단 연수": keep_thr(7, 25, 50), "교리": keep_thr(1, 5, 9),
            "엔딩 수집": lambda r: not r["condition"].startswith("profile:endings_seen") or thr(r) in (1, 10, 30),
            "회차": lambda r: r["condition"] in ("profile:runs>=30", "profile:dissolved>=10", "profile:founded>=10"),
            "업적": keep_thr(200, 299),
            "성인": lambda r: r["name"] in ("첫 밤", "밤의 주인", "모든 밤을 보다", "양쪽의 밤", "금욕", "열락의 교주"),
            "이벤트": lambda r: thr(r) in (300, 1000, 10),
            "재산": lambda r: r["name"] not in ("뇌물의 맛", "뇌물 수수자"),
            "기프티드": lambda r: (r["condition"].startswith("NEW:")) or thr(r) in (25, 50, 100, 30, 141),
        },
        "god": {
            "신성 트리": lambda r: ("god_branch_" in r["condition"] and "_full" not in r["condition"] and thr(r) == 30) or "god_nodes" in r["condition"] and thr(r) in (25, 75, 125) or "god_core_" in r["condition"],
            "승천": lambda r: r["condition"].startswith("NEW:god_ending_") and "endings_seen" not in r["condition"] or r["condition"].startswith("profile:god_ascended") and thr(r) in (1, 5, 20),
            "신의 행동": lambda r: r["condition"].startswith("NEW:god_act_") and thr(r) in (30, 100),
            "건축": keep_thr(1, 10),
            "압력": lambda r: not (r["condition"].startswith("NEW:god_Envy>=") or r["condition"].startswith("NEW:god_Suspicion>=")) or thr(r) == 95
                              if "seized" not in r["condition"] else thr(r) in (1, 15),
            "시간": keep_thr(1, 10, 50, 100),
            "이벤트": lambda r: thr(r) in (40, 100, 200),
            "신앙 포인트": keep_thr(1000, 10000, 50000),
            "도전": lambda r: r["name"] not in ("침묵의 신", "연회 없는 승천", "교감 없는 승천", "봉헌 없는 승천", "백 번의 이적", "백 번의 치유", "백 번의 계시", "백 번의 연회",
                                             "다섯 성소", "하늘 아래 모든 마을", "지혜의 전당", "경배받는 모습", "스무 번의 탐닉", "하늘의 총애", "땅의 친구", "한 번에 하나",
                                             "숨 고르기", "세 번의 추락", "하늘의 아이", "신전의 신", "같은 행동만", "검소한 승천", "화려한 승천", "의심의 정점", "지치지 않는 신"),
        },
        "mirror": {
            "엔딩": lambda r: r["name"] != "스무 개의 결말",
            "최종 스킬": lambda r: False,
            "거울 교리": lambda r: thr(r) in (15, 60, 120),
            "시간": keep_thr(1, 5, 10, 25, 40),
            "연쇄 지표": lambda r: not r["condition"].startswith("NEW:mirror_") or r["condition"] in
                ("NEW:mirror_followers>=50", "NEW:mirror_followers>=300", "NEW:mirror_followers>=1000", "NEW:mirror_Reputation>=400", "NEW:mirror_Reputation>=800",
                 "NEW:mirror_Offerings>=300", "NEW:mirror_Offerings>=800") or r["name"] in ("명망의 선순환", "명망의 악순환", "고요한 균형", "한 방향의 사슬"),
            "불운": lambda r: thr(r) in (4, 8, 10, 60) or r["name"] in ("행운아", "불운 회복", "불운 속의 번영", "첫 실패"),
            "회차": lambda r: thr(r) in (1, 5, 20) or r["condition"] in ("profile:mirror_final>=10", "profile:mirror_collapse>=5"),
            "이벤트": lambda r: thr(r) in (40, 200, 351),
            "도전": lambda r: r["name"] not in ("서른 번의 행동", "게으른 교주", "회복의 달인", "회복 없이", "거울 속 신도들", "명망의 조각", "명망의 추락",
                                              "헌금의 폭락", "한 해 만에 빈손", "한 해 만에", "거울 업적 오십", "특수 이벤트 수집가", "최종 이벤트 수집가"),
        },
    }[m.key]
    m.rows = [r for r in m.rows if rules.get(r["category"], lambda _: True)(r)]
    for i, r in enumerate(m.rows):  # id를 다시 매긴다
        r["id"] = f"{m.prefix}{(101 if m.key == 'normal' else 1) + i:03d}"

# 밸런스 2차 결정 (SYS_ACHIEVEMENTS_V2): 도달률 0% 업적의 기준치만 낮춘다. 압축 규칙이 기준치로 행을 고르므로
# 위 목록 값은 그대로 두고, 압축·id 부여가 끝난 뒤 id로 골라 바꾼다 (id가 밀리지 않게).
# 2026-10-25 추가: 시뮬에서도 0%였던 g090·g035·m050은 "쉽게 해금"을 위해 80% 수준으로 낮추고, 순서가 뒤집히지 않게 같은 사다리(g066·g034·g089)도 함께 80%로 맞춤
TUNE = {
    "n166": 15, "n167": 17, "n168": 18, "n169": 20, "n171": 32, "n194": 120, "n195": 130, "n205": 4, "n216": 42,
    "g046": 50, "g074": 4, "g066": 1840, "g090": 1920, "g067": 12,
    "m050": 24, "m058": 230, "m059": 260, "m067": 220, "m117": 250, "m100": 50, "m118": 60,
}

def tune(m):
    for r in m.rows:
        if r["id"] not in TUNE: continue
        old, new = thr(r), TUNE[r["id"]]
        r["condition"] = re.sub(rf"(?<=[<>=]){old}(?!\d)", str(new), r["condition"])
        for k in ("description", "description_en"):
            r[k] = r[k].replace(f"{old:,}", str(new)) if old >= 1000 else r[k]
            r[k] = re.sub(rf"(?<!\d){old}(?!\d)", str(new), r[k])

# 같은 결정: 신 행동 횟수 업적을 판 기준(NEW:)에서 누적(profile:, 모든 판 합산)으로 바꾸고 기준치를 누적 분포로 다시 잡는다
CUMULATIVE = {
    # 신 20명×20판 누적 분포(2026-10-25)의 제안값. 연회는 봇이 거의 안 써서(누적 최대 12) 분포 대신 30/100을 누적으로 씀
    "g024": 200, "g025": 210, "g026": 120, "g027": 140, "g028": 540, "g029": 570,
    "g030": 30, "g031": 100, "g032": 440, "g033": 460, "g034": 240, "g035": 256, "g089": 240,
}

def cumulative(m):
    for r in m.rows:
        if r["id"] not in CUMULATIVE: continue
        n = CUMULATIVE[r["id"]]
        r["condition"] = re.sub(r"^NEW:(\w+)>=\d+$", rf"profile:\1>={n}", r["condition"])
        if r["id"] == "g089":
            r["description"] = f"건물을 누적 {n}번 봉헌했다 (모든 건물·모든 회차 합산)."
            r["description_en"] = f"Consecrated buildings {n} times in total (any building, all runs combined)."
        else:
            r["description"] = re.sub(r"\d+번 했다\.$", f"누적 {n}번 했다 (모든 회차 합산).", r["description"])
            r["description_en"] = re.sub(r"Done \d+ times\.$", f"Done {n} times in total (all runs combined).", r["description_en"])

# 기준치를 바꾼 업적 중 이름에 숫자가 든 것은 새 기준에 맞춰 이름도 바꾼다 (ko, en)
RENAME = {
    "n167": ("열일곱 가지 재능", "Seventeen Gifts"), "n168": ("열여덟 가지 재능", "Eighteen Gifts"), "n169": ("스무 가지 재능", "Twenty Gifts"),
    "n171": ("도감 서른둘", "Thirty-Two Catalogued"), "n194": ("업적 백이십", "One Hundred Twenty"),
    "m050": ("교리 스물네 개", "Twenty-Four Doctrines"),
    "m058": ("거울 신도 이백삼십 명", "Mirror Followers: Two Hundred Thirty Souls"), "m059": ("거울 신도 이백육십 명", "Mirror Followers: Two Hundred Sixty Souls"),
    "m100": ("거울 업적 쉰", "Fifty Mirror Marks"), "m117": ("모든 지표 상승", "All Rising"),
}

def rename(m):
    for r in m.rows:
        if r["id"] in RENAME: r["name"], r["name_en"] = RENAME[r["id"]]

if __name__ == "__main__":
    out = {}
    for build in (normal, god, mirror):
        m = build()
        out[m.key] = len(m.rows)
        full = len(m.rows); prune(m); tune(m); cumulative(m); rename(m)
        print(m.key, full, "->", len(m.rows)); write(m)
