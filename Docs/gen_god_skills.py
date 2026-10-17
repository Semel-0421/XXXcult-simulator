# Docs/god_skills.csv -> Assets/Scripts/God/GodSkillCatalog.Data.cs  (준비된 신 모드의 권능 트리)
# 사용: python Docs/gen_god_skills.py   (프로젝트 루트에서 실행)
#
# 열: id, ring, parent, kind, name, description, effects
#   ring  : 1(가장 바깥) ~ 5(핵), 0 = 중심(승천). 바깥에서 안으로 키운다
#   parent: 바로 안쪽 노드 id (중심은 비움). 중심은 핵 4개, 나머지 노드는 바깥 노드 2개를 자식으로 가져야 한다
#   kind  : center / core / group / stat / attack(공격형) / defense(방어형)
#   effects (공백으로 여러 개):
#     gain:Followers+10      스탯 증가량 +10% (공격형: 늘어나는 쪽만 키운다)
#     guard:FollowerFaith+8  스탯 감소량 -8% (방어형: 줄어드는 쪽만 막는다)
#     chance:Wonder+5        행동 성공률 +5%p (Wonder Revelation Healing Feast Commune Consecrate All)
#     resist:Heaven+5        압박 가산치 상승 -5% (Heaven 신계 / Earth 지상)
#     seize+5                건물 압류를 막을 확률 +5%
#     commune+5              기적의 접신 소모 -5%
#     vessel+5               향연의 그릇 회복 +5%
#     faith+5                신앙 비례 포인트 +5%
#     year:Health+1          해가 바뀔 때마다 스탯 변화
import csv, io, os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, 'Docs', 'god_skills.csv')
DST = os.path.join(ROOT, 'Assets', 'Scripts', 'God', 'GodSkillCatalog.Data.cs')

CULT = {'LeaderFaith', 'FollowerFaith', 'Authority', 'Offerings', 'Followers', 'Doctrine', 'Communion', 'Reputation'}
PERSONAL = {'Health', 'Mentality', 'Piety', 'Repute', 'Wealth', 'Lust', 'Greed', 'Appetite', 'Ambition',
            'Temperance', 'Charity', 'Patience', 'Integrity'}
ACTIONS = {'Wonder', 'Revelation', 'Healing', 'Feast', 'Commune', 'Consecrate', 'All'}
KINDS = {'center': 'Center', 'core': 'Core', 'group': 'Group', 'stat': 'Stat', 'attack': 'Attack', 'defense': 'Defense'}
RING_OF_KIND = {'center': {0}, 'core': {5}, 'group': {4}, 'stat': {3}, 'attack': {1, 2}, 'defense': {1, 2}}
SIMPLE = {'seize': 'SeizeGuard', 'commune': 'CommunionSave', 'vessel': 'VesselRecovery', 'faith': 'FaithPoints'}


def read_text(path):
    raw = open(path, 'rb').read()
    try:
        return raw.decode('utf-8-sig')
    except UnicodeDecodeError:
        return raw.decode('cp949')


def esc(s):
    bs = chr(92)
    return s.replace(bs, bs * 2).replace('"', bs + '"')


def stat_code(name, where):
    if name in CULT: return f'StatId.{name}'
    if name in PERSONAL: return f'PersonalStatId.{name}'
    sys.exit(f'{where}: 알 수 없는 스탯 "{name}"')


def parse(cell, where):
    out = []
    for token in cell.split():
        m = re.fullmatch(r'(seize|commune|vessel|faith)\+(\d+)', token)
        if m:
            out.append(f'GodEffect.Of(GodEffect.Type.{SIMPLE[m.group(1)]}, {int(m.group(2))})')
            continue
        m = re.fullmatch(r'(gain|guard|chance|resist|year):([A-Za-z]+)([+-]\d+)', token)
        if not m: sys.exit(f'{where}: 잘못된 효과 "{token}"')
        kind, target, num = m.group(1), m.group(2), int(m.group(3))
        if kind in ('gain', 'guard'):
            if num <= 0: sys.exit(f'{where}: {kind}는 양수만')
            out.append(f'GodEffect.{kind.capitalize()}({stat_code(target, where)}, {num})')
        elif kind == 'chance':
            if target not in ACTIONS: sys.exit(f'{where}: 알 수 없는 행동 "{target}"')
            out.append(f'GodEffect.Chance("{target}", {num})')
        elif kind == 'resist':
            if target not in ('Heaven', 'Earth'): sys.exit(f'{where}: 압박은 Heaven 또는 Earth')
            out.append(f'GodEffect.Resist(Pressure.{target}, {num})')
        else:
            out.append(f'GodEffect.Yearly({stat_code(target, where)}, {num})')
    return out


def main():
    rows = [r for r in csv.DictReader(io.StringIO(read_text(SRC))) if r['id'].strip()]
    by_id, children = {}, {}
    for r in rows:
        sid = r['id'].strip()
        if sid in by_id: sys.exit(f'{sid}: 중복 id')
        by_id[sid] = r
    for r in rows:
        sid, parent, kind, ring = r['id'].strip(), r['parent'].strip(), r['kind'].strip(), int(r['ring'])
        if kind not in KINDS: sys.exit(f'{sid}: 잘못된 kind {kind}')
        if ring not in RING_OF_KIND[kind]: sys.exit(f'{sid}: {kind}는 고리 {sorted(RING_OF_KIND[kind])}')
        if kind == 'center':
            if parent: sys.exit(f'{sid}: 중심은 부모가 없음')
            continue
        if parent not in by_id: sys.exit(f'{sid}: 부모 {parent} 없음')
        if int(by_id[parent]['ring']) != (0 if ring == 5 else ring + 1): sys.exit(f'{sid}: 부모는 바로 안쪽 고리여야 함')
        if kind in ('attack', 'defense') and ring == 1 and by_id[parent]['kind'].strip() != kind:
            sys.exit(f'{sid}: 바깥 노드는 부모와 같은 공격형/방어형')
        children.setdefault(parent, []).append(sid)
    centers = [r['id'].strip() for r in rows if r['kind'].strip() == 'center']
    if len(centers) != 1: sys.exit('중심(승천)은 하나여야 함')
    for r in rows:
        sid, ring = r['id'].strip(), int(r['ring'])
        want = 4 if ring == 0 else 0 if ring == 1 else 2
        if len(children.get(sid, [])) != want: sys.exit(f'{sid}: 자식 {len(children.get(sid, []))}개 (기대 {want}개)')
    out = []
    for r in rows:
        sid = r['id'].strip()
        effects = parse(r['effects'], sid)
        eff_code = f'new[] {{ {", ".join(effects)} }}' if effects else 'System.Array.Empty<GodEffect>()'
        out.append(f'        new("{sid}", {int(r["ring"])}, "{r["parent"].strip()}", GodSkillKind.{KINDS[r["kind"].strip()]}, '
                   f'"{esc(r["name"].strip())}", "{esc(r["description"].strip())}", {eff_code}),')
    cs = f'''// 권능 트리 노드 목록. Docs/god_skills.csv에서 생성됨 — 직접 고치지 말고 CSV 수정 후 다시 생성할 것.
public static partial class GodSkillCatalog
{{
    static GodSkill[] Data() => new GodSkill[]
    {{
{chr(10).join(out)}
    }};
}}
'''
    os.makedirs(os.path.dirname(DST), exist_ok=True)
    open(DST, 'w', encoding='utf-8-sig', newline='\n').write(cs)
    print(len(out), 'god skills')


if __name__ == '__main__':
    main()
