# Docs/achievements.csv -> Assets/Scripts/Achievement/AchievementCatalog.cs
# 사용: python Docs/gen_achievements.py   (프로젝트 루트에서 실행)
#
# 열: id, name, description, condition, reward
#   condition: 공백으로 나눈 조건 모두 만족 (문법은 AchievementCondition.cs 머리말 참고)
#   reward   : (비움) / gifted:no.122 (기프티드 지급) / stat:Health+5 (다음 판부터 개인 스탯 초기치 보너스)
import csv, io, os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, 'Docs', 'achievements.csv')
GIFTED = os.path.join(ROOT, 'Docs', 'gifted.csv')
DST = os.path.join(ROOT, 'Assets', 'Scripts', 'Achievement', 'AchievementCatalog.cs')

PERSONAL = {'Health', 'Mentality', 'Piety', 'Repute', 'Wealth', 'Miracle', 'Lust', 'Greed',
            'Appetite', 'Ambition', 'Temperance', 'Charity', 'Patience', 'Integrity'}


def read_text(path):
    raw = open(path, 'rb').read()
    try:
        return raw.decode('utf-8-sig')
    except UnicodeDecodeError:
        return raw.decode('cp949')


def esc(s):
    bs = chr(92)
    return s.replace(bs, bs * 2).replace('"', bs + '"')


def main():
    gifted = {r[0].strip() for r in list(csv.reader(io.StringIO(read_text(GIFTED))))[2:] if len(r) > 2 and r[0].strip()}
    rows = list(csv.DictReader(io.StringIO(read_text(SRC))))
    out, ids, names, used_gifts = [], set(), set(), set()
    for r in rows:
        aid, name = r['id'].strip(), r['name'].strip()
        if not aid:
            continue
        if aid in ids: sys.exit(f'{aid}: 중복 id')
        if name in names: sys.exit(f'{aid}: 중복 이름 {name}')
        ids.add(aid); names.add(name)
        reward = r['reward'].strip()
        if not reward:
            code = 'AchievementReward.None'
        elif reward.startswith('gifted:'):
            gid = reward[7:]
            if gid not in gifted: sys.exit(f'{aid}: 알 수 없는 기프티드 {gid}')
            if gid in used_gifts: sys.exit(f'{aid}: 기프티드 {gid} 중복 지급')
            used_gifts.add(gid)
            code = f'AchievementReward.Gifted("{gid}")'
        elif reward.startswith('stat:'):
            m = re.fullmatch(r'([A-Za-z]+)([+-]\d+)', reward[5:])
            if not m or m.group(1) not in PERSONAL: sys.exit(f'{aid}: 잘못된 보상 "{reward}"')
            code = f'AchievementReward.StatBonus(PersonalStatId.{m.group(1)}, {int(m.group(2))})'
        else:
            sys.exit(f'{aid}: 잘못된 보상 "{reward}"')
        out.append(f'        new("{aid}", "{esc(name)}", "{esc(r["description"].strip())}", "{esc(r["condition"].strip())}", {code}),')
    cs = f'''// 업적 목록. Docs/achievements.csv에서 생성됨 — 직접 고치지 말고 CSV 수정 후 다시 생성할 것.
using System.Collections.Generic;

public static class AchievementCatalog
{{
    public static readonly IReadOnlyList<Achievement> All = new Achievement[]
    {{
{chr(10).join(out)}
    }};
}}
'''
    os.makedirs(os.path.dirname(DST), exist_ok=True)
    open(DST, 'w', encoding='utf-8-sig', newline='\n').write(cs)
    print(len(out), 'achievements')


if __name__ == '__main__':
    main()
