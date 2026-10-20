# Docs/achievements_v2_{normal,god,mirror,hidden}.csv (+ gifted_v2.csv 의 linked_achievement)
#   -> Assets/Scripts/Achievement/AchievementCatalogV2.cs
# 사용: python -I Docs/gen_achievements_v2_cs.py   (프로젝트 루트에서 실행)
import csv, sys

BS = chr(92)
SEP = BS + 'u001F'
MODES = ['normal', 'god', 'mirror', 'hidden']


def e(s):
    return s.replace(BS, BS * 2).replace('"', BS + '"').replace('\r', '').replace('\n', BS + 'n')


def q(ko, en):
    return '"' + e(ko) + SEP + e(en) + '"'


def read(path):
    return list(csv.DictReader(open(path, encoding='utf-8-sig', newline='')))


links = {}
for g in read('Docs/gifted_v2.csv'):
    if g['linked_achievement']:
        if g['linked_achievement'] in links: sys.exit('중복 연결 ' + g['linked_achievement'])
        links[g['linked_achievement']] = g['id']

lines, ids, used = [], set(), set()
for mode in MODES:
    for r in read('Docs/achievements_v2_%s.csv' % mode):
        i = r['id'].strip()
        if i in ids: sys.exit('중복 id ' + i)
        ids.add(i)
        hidden, adult = mode == 'hidden', r['gate'].strip() == 'adult_option'
        gid = None if hidden else links.get(i)
        if gid: used.add(i)
        reward = 'AchievementReward.Gifted("%s")' % gid if gid else 'AchievementReward.None'
        lines.append('        new("%s", %s, %s, "%s", %s, "%s", %d, %s, %s),' % (
            i, q(r['name'], r['name_en']), q(r['description'], r['description_en']),
            e(r['condition'].strip()), reward, mode, int(r['tier'] or 0),
            'true' if hidden else 'false', 'true' if adult else 'false'))
missing = set(links) - used
if missing: sys.exit('기프티드가 가리키는 업적 없음: ' + ', '.join(sorted(missing)))

HEADER = '''// v2 업적 목록. Docs/achievements_v2_*.csv + gifted_v2.csv에서 Docs/gen_achievements_v2_cs.py로 생성됨 — 직접 고치지 말 것.
// 보상은 기프티드 v2 id만 연결(효과는 아직 게임에 미연결, Achievements가 지급을 건너뜀). 히든(mode=hidden)은 보상 없음.
//
// 히든 업적용 카운터 키 (Counters, Profile 범위 — 게임 쪽에서 올려 줄 것):
//   hidden_clear_modes   일반·신·거울 중 엔딩까지 클리어한 모드 수 (0~3, Max로 올림)
//   ending_total         모든 모드 합쳐 엔딩을 본 누적 횟수 (Add)
//   both_endings_all8    여덟 갈래 모두 굿·배드 엔딩을 봤으면 1 (Set)
//   all_endings_seen     일반·신·거울 엔딩을 전부 봤으면 1 (Set)
//   clear_no_fail        판정 실패 0으로 엔딩까지 클리어했으면 1 (Set)
//   desire_avg_peak      욕망 스탯 4개 평균의 최고치 (Max)
//   virtue_avg_peak      미덕 스탯 4개 평균의 최고치 (Max)
//   dual_avg_peak        min(욕망 평균, 미덕 평균)의 최고치 (Max)
//   gifted_total         기프티드 누적 획득 횟수 (Add)
//   gifted_kinds_seen    획득해 본 기프티드 종류 수 (Set/Max, 140이면 도감 완성 = GiftedCatalog 전체)
//   gifted_run_peak      한 회차 최대 동시 보유 기프티드 수 (Max)
// 조건 문법 참고: "X for 10y" = 카운터 "hold:X" >= 10 (연속 유지 햇수), "<모드>_achv" = 그 모드 달성 업적 수.
using System.Collections.Generic;

public static class AchievementCatalogV2
{
    public static readonly IReadOnlyList<Achievement> All = new Achievement[]
    {
'''
cs = HEADER + '\n'.join(lines) + '\n    };\n}\n'
open('Assets/Scripts/Achievement/AchievementCatalogV2.cs', 'w', encoding='utf-8-sig', newline='\n').write(cs)
print(len(lines), 'achievements')
