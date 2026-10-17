# Docs/gifted.csv -> Assets/Scripts/Talent/GiftedCatalog.cs
# 사용: python Docs/gen_gifted.py Docs/gifted.csv Assets/Scripts/Talent/GiftedCatalog.cs
# 스탯 열 값은 퍼센트 보정. '특수효과' 열 표기 (공백으로 여러 개):
#   chance:Preach+10   해당 행동 성공률 +10%p (All = 모든 행동, 음수 가능)
#   failhalf:Exercise  해당 행동 실패 시 불이익 절반 (All 가능)
#   year:Health+2      해마다(4분기마다) 스탯 변화 (교단 스탯은 창설 후에만)
# 행동 키: Exercise Meditate Study Socialize Pray Proselytize Preach Serve Desire CultPray All
import csv, io, re, sys

CULT = {'믿음': 'LeaderFaith', '신도의 믿음': 'FollowerFaith', '권위': 'Authority', '봉헌금': 'Offerings', '신도': 'Followers',
        '교리': 'Doctrine', '접신(히든)': 'Communion', '명망': 'Reputation'}
PERS = {'체력': 'Health', '정신력': 'Mentality', '신앙심': 'Piety', '평판': 'Repute', '재산': 'Wealth', '기적(히든)': 'Miracle',
        '음욕': 'Lust', '재물욕': 'Greed', '식욕': 'Appetite', '권력욕': 'Ambition', '절제': 'Temperance', '자애': 'Charity',
        '인내': 'Patience', '청렴': 'Integrity'}
ACTIONS = {'Exercise', 'Meditate', 'Study', 'Socialize', 'Pray', 'Proselytize', 'Preach', 'Serve', 'Desire', 'CultPray', 'All'}
CATEGORIES = {'운명', '성격', '특기', '외모', '업적'}


def read_text(path):
    raw = open(path, 'rb').read()
    try:
        return raw.decode('utf-8-sig')
    except UnicodeDecodeError:
        return raw.decode('cp949')


def esc(s):
    bs = chr(92)
    return s.replace(bs, bs * 2).replace('"', bs + '"').replace(chr(10), bs + 'n').replace(SEP, bs + 'u001F')

SEP = chr(0x1F)  # 한국어와 영어 사이 구분자 (게임에서 Lang.Pick이 현재 언어 쪽을 고른다)


def bi(row, col):
    """한국어 열과 같은 이름 + _en 열이 있으면 한국어+SEP+영어로 묶는다."""
    ko, en = (row.get(col) or '').strip(), (row.get(col + '_en') or '').strip()
    return f'{ko}{SEP}{en}' if ko and en else ko



def special_code(cell, where):
    out = []
    for token in cell.split():
        m = re.fullmatch(r'(chance|failhalf|year):([A-Za-z]+)([+-]\d+)?', token)
        if not m:
            sys.exit(f'{where}: 잘못된 특수효과 "{token}"')
        kind, target, num = m.groups()
        if kind == 'chance':
            if target not in ACTIONS or num is None: sys.exit(f'{where}: "{token}"')
            out.append(f'GiftedSpecial.Chance("{target}", {int(num)})')
        elif kind == 'failhalf':
            if target not in ACTIONS: sys.exit(f'{where}: "{token}"')
            out.append(f'GiftedSpecial.FailHalf("{target}")')
        else:
            stat = {**{v: f'PersonalStatId.{v}' for v in PERS.values()}, **{v: f'StatId.{v}' for v in CULT.values()}}.get(target)
            if stat is None or num is None: sys.exit(f'{where}: "{token}"')
            out.append(f'GiftedSpecial.Yearly({stat}, {int(num)})')
    return out


def main(src, dst):
    rows = list(csv.reader(io.StringIO(read_text(src))))
    header = rows[1]
    col = {h: i for i, h in enumerate(header)}
    stat_cols = [(i, h) for i, h in enumerate(header) if h in CULT or h in PERS]
    out, names, warn = [], set(), []
    for r in rows[2:]:
        r = (r + [''] * len(header))[:len(header)]
        if not r[1].strip():
            continue
        gid, name, cat = r[0].strip(), r[1].strip(), r[2].strip()
        if name in names: sys.exit(f'{gid}: 중복 이름 {name}')
        if cat not in CATEGORIES: sys.exit(f'{gid}: 잘못된 분류 {cat}')
        names.add(name)
        mods = []
        for i, h in stat_cols:
            v = r[i].strip()
            if not v:
                continue
            if not v.endswith('%'): warn.append(f'{gid} {h} "{v}" -> {v}%로 처리')
            n = f'{float(v.rstrip("%")):g}f'
            mods.append(f'Cult(StatId.{CULT[h]}, {n})' if h in CULT else f'Personal(PersonalStatId.{PERS[h]}, {n})')
        specials = special_code(r[col['특수효과']], gid) if '특수효과' in col else []
        extra = r[col['기타 효과']].strip()
        mod_code = f'new StatModifier[] {{ {", ".join(mods)} }}' if mods else 'System.Array.Empty<StatModifier>()'
        def two(i, en_col):
            en = r[col[en_col]].strip() if en_col in col else ''
            return f'{r[i].strip()}{SEP}{en}' if r[i].strip() and en else r[i].strip()
        line = f'        new("{esc(gid)}", "{esc(two(1, "재능명_en"))}", "{cat}",\n            "{esc(two(3, "플레이버 텍스트_en"))}",\n            "{esc(two(col["기타 효과"], "기타 효과_en"))}",\n            {mod_code}'
        if specials:
            line += f',\n            {", ".join(specials)}'
        out.append(line + '),')
    cs = f'''// 기프티드 목록. Docs/gifted.csv에서 생성됨 — 직접 고치지 말고 CSV 수정 후 다시 생성할 것.
// 스탯 보정은 퍼센트(%) 단위. 특수효과는 GiftedSpecial, 기타 효과(ExtraEffect)는 설명 텍스트.
using System.Collections.Generic;
using System.Linq;

public static class GiftedCatalog
{{
    static StatModifier Cult(StatId stat, float percent) => new(stat, percent);
    static StatModifier Personal(PersonalStatId stat, float percent) => new(stat, percent);

    public static readonly IReadOnlyList<Talent> All = new Talent[]
    {{
{chr(10).join(out)}
    }};

    public static Talent Find(string id) => All.FirstOrDefault(gifted => gifted.Id == id);
}}
'''
    open(dst, 'w', encoding='utf-8-sig', newline='\n').write(cs)
    print(len(out), 'entries')
    print('\n'.join(warn))


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2])
