# Docs/cult_events.csv -> Assets/Scripts/Story/CultEvents.cs
# 사용: python Docs/gen_cult_events.py Docs/cult_events.csv Assets/Scripts/Story/CultEvents.cs
# kind: random(확률 이벤트, theme = 가중치 기준 교단 스탯) / condition(조건 이벤트, theme 자리에 조건)
# 조건 표기: "FollowerFaith>=300 Offerings<100 DoctrineLv>=3"  (모두 만족해야 발동)
# 효과 표기: "Offerings+20 Piety-1 @no.3"  (교단/개인 스탯 이름 모두 가능, @ = 숨겨진 기프티드, 성격/특기만 허용)
import csv, io, os, re, sys

CULT = {'LeaderFaith', 'FollowerFaith', 'Authority', 'Offerings', 'Followers', 'Doctrine', 'Communion', 'Reputation'}
PERSONAL = {'Health', 'Mentality', 'Piety', 'Repute', 'Wealth', 'Miracle', 'Lust', 'Greed',
            'Appetite', 'Ambition', 'Temperance', 'Charity', 'Patience', 'Integrity'}
GIFT_CATEGORIES = {'성격', '특기'}


def read_text(path):
    raw = open(path, 'rb').read()
    try:
        return raw.decode('utf-8-sig')
    except UnicodeDecodeError:
        return raw.decode('cp949')


def allowed_gifts(gifted_csv):
    rows = list(csv.reader(io.StringIO(read_text(gifted_csv))))
    return {r[0] for r in rows[2:] if len(r) > 2 and r[2].strip() in GIFT_CATEGORIES}


def esc(s):
    bs = chr(92)
    return s.replace(bs, bs * 2).replace('"', bs + '"')


def condition_code(cell, where):
    parts = []
    for token in cell.split():
        m = re.fullmatch(r'([A-Za-z]+)(>=|<=|>|<)(\d+)', token)
        if not m:
            sys.exit(f'{where}: 잘못된 조건 "{token}"')
        name, op, value = m.groups()
        if name == 'DoctrineLv':
            parts.append(f'DoctrineLevel.Of(c[StatId.Doctrine].Value) {op} {value}')
        elif name in CULT:
            parts.append(f'c[StatId.{name}].Value {op} {value}')
        else:
            sys.exit(f'{where}: 조건에 쓸 수 없는 스탯 "{name}"')
    return 'c => ' + ' && '.join(parts)


def parse_effects(cell, gifts_ok, where):
    personal, cult, gifts = [], [], []
    for token in cell.split():
        if token.startswith('@'):
            if token[1:] not in gifts_ok:
                sys.exit(f'{where}: {token[1:]} 는 성격/특기 기프티드가 아님')
            gifts.append(token[1:])
            continue
        m = re.fullmatch(r'([A-Za-z]+)([+-]\d+)', token)
        if not m:
            sys.exit(f'{where}: 잘못된 효과 "{token}"')
        name, amount = m.group(1), int(m.group(2))
        if name in CULT:
            cult.append(f'C(StatId.{name}, {amount})')
        elif name in PERSONAL:
            personal.append(f'P(PersonalStatId.{name}, {amount})')
        else:
            sys.exit(f'{where}: 알 수 없는 스탯 "{name}"')
    return personal, cult, gifts


def main(src, dst):
    gifts_ok = allowed_gifts(os.path.join(os.path.dirname(src), 'gifted.csv'))
    rows = list(csv.reader(io.StringIO(read_text(src))))
    out, ids = [], set()
    for r in rows[1:]:
        if not r or not r[0].strip():
            continue
        r = (r + [''] * 14)[:14]
        eid, kind, theme, text = r[0].strip(), r[1].strip(), r[2].strip(), r[3].strip()
        if eid in ids:
            sys.exit(f'{eid}: 중복 id')
        ids.add(eid)
        choices = []
        for i in range(5):
            label, cell = r[4 + i * 2].strip(), r[5 + i * 2]
            if not label:
                continue
            personal, cult, gifts = parse_effects(cell, gifts_ok, f'{eid} 선택지{i + 1}')
            code = f'new Choice({", ".join([chr(34) + esc(label) + chr(34)] + personal)})'
            if cult:
                code += f'.Affects({", ".join(cult)})'
            if gifts:
                code += f'.Grants({", ".join(chr(34) + g + chr(34) for g in gifts)})'
            choices.append(code)
        if not 2 <= len(choices) <= 5:
            sys.exit(f'{eid}: 선택지 {len(choices)}개 (2~5개 필요)')
        if kind == 'random':
            if theme not in CULT:
                sys.exit(f'{eid}: 잘못된 테마 "{theme}"')
            head = f'CultEvent.Random("{eid}", StatId.{theme}'
        elif kind == 'condition':
            head = f'CultEvent.Conditional("{eid}", {condition_code(theme, eid)}'
        else:
            sys.exit(f'{eid}: kind는 random 또는 condition')
        body = ',\n                '.join(choices)
        out.append(f'        {head}, new Question("{esc(text)}",\n                {body})),')
    cs = f'''// 교단 창설 후 이벤트 목록. Docs/cult_events.csv에서 생성됨 — 직접 고치지 말고 CSV 수정 후 다시 생성할 것.
public static class CultEvents
{{
    static StatEffect P(PersonalStatId stat, int amount) => new(stat, amount);
    static CultEffect C(StatId stat, int amount) => new(stat, amount);

    public static readonly CultEvent[] All =
    {{
{chr(10).join(out)}
    }};
}}
'''
    open(dst, 'w', encoding='utf-8-sig', newline='\n').write(cs)
    print(len(out), 'events')


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2])
