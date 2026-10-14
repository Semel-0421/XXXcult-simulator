# Docs/life_events.csv -> Assets/Scripts/Story/LifeEvents.cs
# 사용: python Docs/gen_life_events.py Docs/life_events.csv Assets/Scripts/Story/LifeEvents.cs
# 효과 표기: "Health+2 Piety-1 @no.5"  (@ = 숨겨진 기프티드 지급, 운명/외모만 허용)
import csv, io, os, re, sys

STATS = {'Health', 'Mentality', 'Piety', 'Repute', 'Wealth', 'Miracle', 'Lust', 'Greed',
         'Appetite', 'Ambition', 'Temperance', 'Charity', 'Patience', 'Integrity'}
HIDDEN_CATEGORIES = {'운명', '외모'}


def read_text(path):
    raw = open(path, 'rb').read()
    try:
        return raw.decode('utf-8-sig')
    except UnicodeDecodeError:
        return raw.decode('cp949')


def hidden_gifted_ids(gifted_csv):
    rows = list(csv.reader(io.StringIO(read_text(gifted_csv))))
    return {r[0] for r in rows[2:] if len(r) > 2 and r[2].strip() in HIDDEN_CATEGORIES}


def esc(s):
    bs = chr(92)
    return s.replace(bs, bs * 2).replace('"', bs + '"')


def parse_effects(cell, allowed_gifts, where):
    effects, gifts = [], []
    for token in cell.split():
        if token.startswith('@'):
            gid = token[1:]
            if gid not in allowed_gifts:
                sys.exit(f'{where}: {gid} 는 운명/외모 기프티드가 아님')
            gifts.append(gid)
            continue
        m = re.fullmatch(r'([A-Za-z]+)([+-]\d+)', token)
        if not m or m.group(1) not in STATS:
            sys.exit(f'{where}: 잘못된 효과 "{token}"')
        effects.append(f'E(PersonalStatId.{m.group(1)}, {int(m.group(2))})')
    return effects, gifts


def main(src, dst):
    allowed = hidden_gifted_ids(os.path.join(os.path.dirname(src), 'gifted.csv'))
    rows = list(csv.reader(io.StringIO(read_text(src))))
    out, ids = [], set()
    for r in rows[1:]:
        if not r or not r[0].strip():
            continue
        if len(r) != 12:
            sys.exit(f'{r[0]}: 열 개수 {len(r)} (12 필요)')
        eid, lo, hi, text = r[0].strip(), int(r[1]), int(r[2]), r[3].strip()
        if eid in ids:
            sys.exit(f'{eid}: 중복 id')
        ids.add(eid)
        choices = []
        for i in range(4):
            label, cell = r[4 + i * 2].strip(), r[5 + i * 2]
            effects, gifts = parse_effects(cell, allowed, f'{eid} 선택지{i + 1}')
            args = ', '.join([f'"{esc(label)}"'] + effects)
            grant = f'.Grants({", ".join(chr(34) + g + chr(34) for g in gifts)})' if gifts else ''
            choices.append(f'new Choice({args}){grant}')
        body = ',\n                '.join(choices)
        out.append(f'        new("{eid}", {lo}, {hi}, new Question("{esc(text)}",\n                {body})),')
    cs = f'''// 생애 이벤트 목록. Docs/life_events.csv에서 생성됨 — 직접 고치지 말고 CSV 수정 후 다시 생성할 것.
public static class LifeEvents
{{
    static StatEffect E(PersonalStatId stat, int amount) => new(stat, amount);

    public static readonly LifeEvent[] All =
    {{
{chr(10).join(out)}
    }};
}}
'''
    open(dst, 'w', encoding='utf-8-sig', newline='\n').write(cs)
    print(len(out), 'events')


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2])
