# Docs/doctrines.csv -> Assets/Scripts/Status/DoctrineCatalog.cs
# 사용: python Docs/gen_doctrines.py   (프로젝트 루트에서 실행)
#
# 열: id, branch, tier, path, name, description, max_level, effects
#   branch : Lust Greed Appetite Ambition (욕망) / Temperance Charity Patience Integrity (덕목)
#   tier   : 1~10. 같은 가지에서 앞 단계를 배워야 다음 단계를 배울 수 있다
#   path   : 1~5단계는 비움, 6~10단계는 A 또는 B (6단계에서 한 갈래를 고르면 다른 갈래는 막힘)
#            10단계는 갈래마다 최종 스킬 (가지 8 x 갈래 2 = 16개), max_level 2 = 중앙 문양에서 하는 최종 업그레이드
#   max_level: 1이면 한 번, 2 이상이면 여러 번 올리는 스킬 (효과가 레벨만큼 곱해진다) — 5단계가 이 스킬
#   effects (공백으로 여러 개, 모두 레벨당 값):
#     Offerings+5%       해당 스탯 증감 보정 (교단·개인 스탯)
#     chance:Preach+5    행동 성공률 +5%p
#     failhalf:All       행동 실패 불이익 절반
#     year:Offerings+10  해마다 스탯 변화
#     resist:5           불운 저항 +5%
import csv, io, os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, 'Docs', 'doctrines.csv')
DST = os.path.join(ROOT, 'Assets', 'Scripts', 'Status', 'DoctrineCatalog.cs')

CULT = {'LeaderFaith', 'FollowerFaith', 'Authority', 'Offerings', 'Followers', 'Doctrine', 'Reputation'}
PERSONAL = {'Health', 'Mentality', 'Piety', 'Repute', 'Wealth', 'Lust', 'Greed', 'Appetite', 'Ambition',
            'Temperance', 'Charity', 'Patience', 'Integrity'}
ACTIONS = {'Proselytize', 'Preach', 'Serve', 'Desire', 'CultPray', 'All'}
BRANCHES = ['Lust', 'Greed', 'Appetite', 'Ambition', 'Temperance', 'Charity', 'Patience', 'Integrity']


def read_text(path):
    raw = open(path, 'rb').read()
    try:
        return raw.decode('utf-8-sig')
    except UnicodeDecodeError:
        return raw.decode('cp949')


def esc(s):
    bs = chr(92)
    return s.replace(bs, bs * 2).replace('"', bs + '"').replace(SEP, bs + 'u001F')

SEP = chr(0x1F)  # 한국어와 영어 사이 구분자 (게임에서 Lang.Pick이 현재 언어 쪽을 고른다)


def bi(row, col):
    """한국어 열과 같은 이름 + _en 열이 있으면 한국어+SEP+영어로 묶는다."""
    ko, en = (row.get(col) or '').strip(), (row.get(col + '_en') or '').strip()
    return f'{ko}{SEP}{en}' if ko and en else ko



def stat_code(name, where):
    if name in CULT: return f'StatId.{name}'
    if name in PERSONAL: return f'PersonalStatId.{name}'
    sys.exit(f'{where}: 알 수 없는 스탯 "{name}"')


def parse(cell, where):
    mods, specials, resist = [], [], 0
    for token in cell.split():
        m = re.fullmatch(r'([A-Za-z]+)([+-]\d+(?:\.\d+)?)%', token)
        if m:
            name, value = m.groups()
            kind = 'Cult' if name in CULT else 'Personal'
            mods.append(f'{kind}({stat_code(name, where)}, {float(value):g}f)')
            continue
        m = re.fullmatch(r'(chance|failhalf|year|resist):([A-Za-z]*)([+-]?\d+)?', token)
        if not m: sys.exit(f'{where}: 잘못된 효과 "{token}"')
        kind, target, num = m.groups()
        if kind == 'resist':
            resist += int(num)
        elif kind == 'chance':
            if target not in ACTIONS or num is None: sys.exit(f'{where}: "{token}"')
            specials.append(f'GiftedSpecial.Chance("{target}", {int(num)})')
        elif kind == 'failhalf':
            if target not in ACTIONS: sys.exit(f'{where}: "{token}"')
            specials.append(f'GiftedSpecial.FailHalf("{target}")')
        else:
            if num is None: sys.exit(f'{where}: "{token}"')
            specials.append(f'GiftedSpecial.Yearly({stat_code(target, where)}, {int(num)})')
    return mods, specials, resist


def build(src, dst, cls):
    rows = list(csv.DictReader(io.StringIO(read_text(src))))
    out, ids, seen = [], set(), set()
    for r in rows:
        sid = r['id'].strip()
        if not sid: continue
        if sid in ids: sys.exit(f'{sid}: 중복 id')
        ids.add(sid)
        branch, tier, path = r['branch'].strip(), int(r['tier']), r['path'].strip().upper()
        if branch not in BRANCHES: sys.exit(f'{sid}: 잘못된 가지 {branch}')
        if not 1 <= tier <= 10: sys.exit(f'{sid}: 단계는 1~10')
        if (tier <= 5) != (path == '') or path not in ('', 'A', 'B'): sys.exit(f'{sid}: 1~5단계는 갈래 없음, 6~10단계는 A/B')
        if (branch, tier, path) in seen: sys.exit(f'{sid}: {branch} {tier}{path}단계 중복')
        seen.add((branch, tier, path))
        max_level = int(r['max_level'])
        # resist 토큰은 resist:5 형태 → 정규식에서 target 자리에 숫자가 들어오지 않도록 별도 처리
        cell = re.sub(r'resist:(\d+)', r'resist:+\1', r['effects'])
        mods, specials, resist = parse(cell, sid)
        mod_code = f'new StatModifier[] {{ {", ".join(mods)} }}' if mods else 'System.Array.Empty<StatModifier>()'
        line = (f'        new("{sid}", DoctrineBranch.{branch}, {tier}, "{path}", "{esc(bi(r, "name"))}", '
                f'"{esc(bi(r, "description"))}", {max_level}, {resist}, {mod_code}')
        if specials: line += ', ' + ', '.join(specials)
        out.append(line + '),')
    for b in BRANCHES:
        expected = {(t, '') for t in range(1, 6)} | {(t, p) for t in range(6, 11) for p in 'AB'}
        got = {(t, p) for (bb, t, p) in seen if bb == b}
        if got != expected: sys.exit(f'{b}: 1~5단계와 6~10단계 A/B가 모두 있어야 함')
    # 본편 목록은 Normal, All은 거울 세계 판이면 거울 목록을 돌려준다 (id·구조가 같아 DoctrineTree가 그대로 쓴다)
    head = ('    public static IReadOnlyList<DoctrineSkill> All => GameSession.MirrorMode ? MirrorDoctrineCatalog.All : Normal;\n\n'
            '    public static readonly IReadOnlyList<DoctrineSkill> Normal') if cls == 'DoctrineCatalog' else '    public static readonly IReadOnlyList<DoctrineSkill> All'
    cs = f'''// 교리 스킬 목록. Docs/{os.path.relpath(src, os.path.join(ROOT, 'Docs')).replace(os.sep, '/')}에서 생성됨 — 직접 고치지 말고 CSV 수정 후 다시 생성할 것.
using System.Collections.Generic;
using System.Linq;

public static class {cls}
{{
    static StatModifier Cult(StatId stat, float percent) => new(stat, percent);
    static StatModifier Personal(PersonalStatId stat, float percent) => new(stat, percent);

{head} = new DoctrineSkill[]
    {{
{chr(10).join(out)}
    }};

    public static DoctrineSkill Find(string id) => id == DoctrineSkill.DeclarationId ? DoctrineSkill.Declaration : All.FirstOrDefault(s => s.Id == id);

    // 6~10단계는 갈래(A/B)를 지정한다
    public static DoctrineSkill At(DoctrineBranch branch, int tier, string path = "") => All.FirstOrDefault(s => s.Branch == branch && s.Tier == tier && s.Path == (tier <= 5 ? "" : path));
}}
'''
    open(dst, 'w', encoding='utf-8-sig', newline='\n').write(cs)
    print(len(out), cls)


def main():
    build(SRC, DST, 'DoctrineCatalog')
    build(os.path.join(ROOT, 'Docs', 'mirror', 'doctrines_mirror.csv'),
          os.path.join(ROOT, 'Assets', 'Scripts', 'Status', 'MirrorDoctrineCatalog.cs'), 'MirrorDoctrineCatalog')


if __name__ == '__main__':
    main()
