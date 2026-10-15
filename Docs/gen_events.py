# 생애·교단 이벤트 생성기.
#   Docs/life_events.csv  + Docs/life_choices.csv  -> Assets/Scripts/Story/LifeEvents.cs
#   Docs/cult_events.csv  + Docs/cult_choices.csv  -> Assets/Scripts/Story/CultEvents.cs
# 사용: python Docs/gen_events.py   (프로젝트 루트에서 실행)
#
# [이벤트 CSV] 이벤트 1줄 = 질문 1개
#   life_events.csv: id, min_age, max_age, text
#   cult_events.csv: id, kind, theme, text
#     kind: random(확률 이벤트, theme = 가중치 기준 교단 스탯) / condition(조건 이벤트, theme 자리에 조건)
#           / negative(교단 3년차부터 확률이 오르는 부정적 이벤트, theme 자리에 '스탯이 0에 가까운' 조건)
#     조건 표기: "FollowerFaith>=300 Offerings<100 DoctrineLv>=3" (모두 만족해야 발동)
#
# [선택지 CSV] 선택지 1줄 = 선택지 1개 (같은 event_id를 2~5줄)
#   event_id, no, label, chance, success_effects, success_text, fail_effects, fail_text
#     chance: 성공 확률(%). 1~99면 실패 결과 필수, 100이면 항상 성공(실패 열 무시)
#     *_effects: 효과 표기 "Health+2 Piety-1 @no.5" (@ = 기프티드 지급)
#       생애: 개인 스탯만, @는 운명/외모만 / 교단: 교단·개인 스탯 모두, @는 성격/특기만
#     *_text: 해당 결과가 나왔을 때 보여 줄 결과문
#
# CSV는 UTF-8 BOM으로 저장(엑셀 호환). 읽기는 UTF-8, 실패 시 CP949로 한다.
import csv, io, os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DOCS = os.path.join(ROOT, 'Docs')
OUT_LIFE = os.path.join(ROOT, 'Assets', 'Scripts', 'Story', 'LifeEvents.cs')
OUT_CULT = os.path.join(ROOT, 'Assets', 'Scripts', 'Story', 'CultEvents.cs')

CULT = {'LeaderFaith', 'FollowerFaith', 'Authority', 'Offerings', 'Followers', 'Doctrine', 'Communion', 'Reputation'}
PERSONAL = {'Health', 'Mentality', 'Piety', 'Repute', 'Wealth', 'Miracle', 'Lust', 'Greed',
            'Appetite', 'Ambition', 'Temperance', 'Charity', 'Patience', 'Integrity'}


def read_text(path):
    raw = open(path, 'rb').read()
    try:
        return raw.decode('utf-8-sig')
    except UnicodeDecodeError:
        return raw.decode('cp949')


def rows_of(name):
    rows = list(csv.reader(io.StringIO(read_text(os.path.join(DOCS, name)))))
    header = [h.strip() for h in rows[0]]
    body = []
    for r in rows[1:]:
        if not r or not r[0].strip() or r[0].startswith('#'):
            continue
        body.append(dict(zip(header, (r + [''] * len(header))[:len(header)])))
    return body


def gifted_categories():
    rows = list(csv.reader(io.StringIO(read_text(os.path.join(DOCS, 'gifted.csv')))))
    return {r[0].strip(): r[2].strip() for r in rows[2:] if len(r) > 2 and r[0].strip()}


def esc(s):
    bs = chr(92)
    return s.replace(bs, bs * 2).replace('"', bs + '"').replace('\n', bs + 'n')


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


def effects_code(cell, allow_cult, allowed_gifts, where):
    code = []
    for token in cell.split():
        if token == '-':
            continue
        if token.startswith('@'):
            gid = token[1:]
            if allowed_gifts.get(gid) is None:
                sys.exit(f'{where}: 알 수 없는 기프티드 {gid}')
            code.append(('G', gid))
            continue
        m = re.fullmatch(r'([A-Za-z]+)([+-]\d+)', token)
        if not m:
            sys.exit(f'{where}: 잘못된 효과 "{token}"')
        name, amount = m.group(1), int(m.group(2))
        if name in PERSONAL:
            code.append(('P', f'PersonalStatId.{name}', amount))
        elif name in CULT and allow_cult:
            code.append(('C', f'StatId.{name}', amount))
        else:
            sys.exit(f'{where}: 이 이벤트에서 쓸 수 없는 스탯 "{name}"')
    return code


def outcome_code(text, cell, allow_cult, allowed_gifts, where):
    calls = []
    grants = []
    for item in effects_code(cell, allow_cult, allowed_gifts, where):
        if item[0] == 'P':
            calls.append(f'.P({item[1]}, {item[2]})')
        elif item[0] == 'C':
            calls.append(f'.C({item[1]}, {item[2]})')
        else:
            grants.append(item[1])
    if grants:
        calls.append(f'.G({", ".join(chr(34) + g + chr(34) for g in grants)})')
    return f'new Outcome("{esc(text.strip())}"){"".join(calls)}'


def choices_for(event_id, choice_rows, allow_cult, allowed_gifts, indent):
    rows = sorted(choice_rows, key=lambda r: int(r['no']))
    if not 2 <= len(rows) <= 5:
        sys.exit(f'{event_id}: 선택지 {len(rows)}개 (2~5개 필요)')
    codes = []
    for r in rows:
        where = f'{event_id} 선택지{r["no"]}'
        label = r['label'].strip()
        if not label:
            sys.exit(f'{where}: 선택지 문구가 비어 있음')
        chance = int(r['chance'])
        if not 1 <= chance <= 100:
            sys.exit(f'{where}: 성공 확률 {chance}% 는 1~100')
        success = outcome_code(r['success_text'], r['success_effects'], allow_cult, allowed_gifts, where + ' 성공')
        if chance == 100:
            codes.append(f'new Choice("{esc(label)}", {success})')
        else:
            if not r['fail_text'].strip():
                sys.exit(f'{where}: 실패 결과문이 필요함')
            fail = outcome_code(r['fail_text'], r['fail_effects'], allow_cult, allowed_gifts, where + ' 실패')
            codes.append(f'new Choice("{esc(label)}", {chance / 100:.2f}f, {success},\n{indent}    {fail})')
    return codes


def group_choices(rows):
    grouped = {}
    for r in rows:
        grouped.setdefault(r['event_id'].strip(), []).append(r)
    return grouped


def build_life(gifts):
    events = rows_of('life_events.csv')
    choices = group_choices(rows_of('life_choices.csv'))
    hidden = {k: v for k, v in gifts.items() if v in ('운명', '외모')}
    out, ids = [], set()
    for e in events:
        eid = e['id'].strip()
        if eid in ids:
            sys.exit(f'{eid}: 중복 id')
        ids.add(eid)
        lo, hi = int(e['min_age']), int(e['max_age'])
        codes = choices_for(eid, choices.get(eid, []), False, hidden, '                ')
        body = ',\n                '.join(codes)
        out.append(f'        new("{eid}", {lo}, {hi}, new Question("{esc(e["text"].strip())}",\n                {body})),')
    unused = set(choices) - ids
    if unused:
        sys.exit(f'선택지만 있고 이벤트가 없는 id: {sorted(unused)}')
    return out


def build_cult(gifts):
    events = rows_of('cult_events.csv')
    choices = group_choices(rows_of('cult_choices.csv'))
    allowed = {k: v for k, v in gifts.items() if v in ('성격', '특기')}
    out, ids = [], set()
    for e in events:
        eid, kind, theme = e['id'].strip(), e['kind'].strip(), e['theme'].strip()
        if eid in ids:
            sys.exit(f'{eid}: 중복 id')
        ids.add(eid)
        if kind == 'random':
            if theme not in CULT:
                sys.exit(f'{eid}: 잘못된 테마 "{theme}"')
            head = f'CultEvent.Random("{eid}", StatId.{theme}'
        elif kind == 'condition':
            head = f'CultEvent.Conditional("{eid}", {condition_code(theme, eid)}'
        elif kind == 'negative':
            head = f'CultEvent.Negative("{eid}", {condition_code(theme, eid)}'
        else:
            sys.exit(f'{eid}: kind는 random, condition, negative 중 하나')
        codes = choices_for(eid, choices.get(eid, []), True, allowed, '                ')
        body = ',\n                '.join(codes)
        out.append(f'        {head}, new Question("{esc(e["text"].strip())}",\n                {body})),')
    unused = set(choices) - ids
    if unused:
        sys.exit(f'선택지만 있고 이벤트가 없는 id: {sorted(unused)}')
    return out


def write(path, header, cls, items):
    body = '\n'.join(items)
    text = f'''{header}
public static class {cls}
{{
    public static readonly {'LifeEvent' if cls == 'LifeEvents' else 'CultEvent'}[] All =
    {{
{body}
    }};
}}
'''
    open(path, 'w', encoding='utf-8-sig', newline='\n').write(text)


def main():
    gifts = gifted_categories()
    life = build_life(gifts)
    cult = build_cult(gifts)
    write(OUT_LIFE, '// 생애 이벤트 목록. Docs/gen_events.py로 생성됨 — 직접 고치지 말고 CSV 수정 후 다시 생성할 것.', 'LifeEvents', life)
    write(OUT_CULT, '// 교단 창설 후 이벤트 목록. Docs/gen_events.py로 생성됨 — 직접 고치지 말고 CSV 수정 후 다시 생성할 것.', 'CultEvents', cult)
    print(len(life), 'life events,', len(cult), 'cult events')


if __name__ == '__main__':
    main()
