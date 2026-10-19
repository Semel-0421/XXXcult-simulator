# 거울 세계 이벤트 생성기. Docs/mirror/events_*.csv + choices_*.csv -> Assets/Scripts/Story/MirrorEvents.cs
# (교리는 gen_doctrines.py가 Docs/mirror/doctrines_mirror.csv -> MirrorDoctrineCatalog.cs 로 함께 만든다)
# 사용: python Docs/gen_mirror.py   (프로젝트 루트에서 실행). 열·효과·조건 규칙은 gen_events.py와 같고, 조건에 개인 스탯도 쓸 수 있다.
#   kind=final  : theme = 최종 스킬 id. 그 스킬을 배우면 매 턴 Turns.MirrorFinalChance 확률로 일반 이벤트 사이에 무작위로 한 번씩 섞여 나온다 (MirrorEvents.Final)
#   kind=ending : theme = 나오는 시점 키 (year40 등). 지금은 보관만 한다 (MirrorEvents.Ending) — 엔딩 작업에서 발동
import glob, os, re, sys
import gen_events as g

MIRROR = os.path.join(g.DOCS, 'mirror')
OUT = os.path.join(g.ROOT, 'Assets', 'Scripts', 'Story', 'MirrorEvents.cs')
ENDINGS_OUT = os.path.join(g.ROOT, 'Assets', 'Scripts', 'Story', 'MirrorEndings.cs')


def rows(pattern):
    out = []
    for path in sorted(glob.glob(os.path.join(MIRROR, pattern))):
        out += g.rows_of(os.path.relpath(path, g.DOCS))
    return out


def main():
    doctrine_ids = {r['id'].strip() for r in g.rows_of(os.path.join('mirror', 'doctrines_mirror.csv'))}
    choices = g.group_choices(rows('choices_*.csv'))
    pools, ids = {'main': [], 'final': [], 'ending': []}, set()
    for e in rows('events_*.csv'):
        eid, kind, theme = e['id'].strip(), e['kind'].strip(), e['theme'].strip()
        if eid in ids:
            sys.exit(f'{eid}: 중복 id')
        ids.add(eid)
        pool, bonus = 'main', g.bonus_stat_for(kind, theme)
        if kind == 'random':
            if theme not in g.CULT:
                sys.exit(f'{eid}: 잘못된 테마 "{theme}"')
            head = f'CultEvent.Random("{eid}", StatId.{theme}'
        elif kind in ('condition', 'negative'):
            head = f'CultEvent.{"Conditional" if kind == "condition" else "Negative"}("{eid}", {g.condition_code(theme, eid)}'
        elif kind == 'mood':
            if theme not in ('Desire', 'Virtue'):
                sys.exit(f'{eid}: mood 테마는 Desire 또는 Virtue')
            for r in choices.get(eid, []):
                if re.search(r'[A-Za-z]+-\d', r['success_effects'] + ' ' + r['fail_effects']):
                    sys.exit(f'{eid} 선택지{r["no"]}: 긍정 이벤트에는 감소 효과를 쓸 수 없음')
            head = f'CultEvent.Mood("{eid}", CultEvent.MoodTrait.{theme}'
        elif kind == 'final':
            if theme not in doctrine_ids or not theme.endswith(('10a', '10b')):
                sys.exit(f'{eid}: final 테마는 최종 스킬 id ("{theme}")')
            pool, bonus = 'final', g.BONUS_STAT['Desire' if theme.startswith(('lust', 'greed', 'appetite', 'ambition')) else 'Virtue']
            head = f'CultEvent.Conditional("{eid}", c => c.Doctrines.LevelOf(MirrorDoctrineCatalog.Find("{theme}")) > 0'
        elif kind == 'ending':
            pool, bonus = 'ending', None
            if theme != 'year40':
                sys.exit(f'{eid}: ending 테마는 year40만 쓴다')
            head = f'CultEvent.Conditional("{eid}", _ => MirrorRules.BranchDue' # 40년 갈림길 (MirrorRules)
        else:
            sys.exit(f'{eid}: 알 수 없는 kind "{kind}"')
        codes = g.choices_for(eid, choices.get(eid, []), True, {}, '                ', bonus)
        body = ',\n                '.join(codes)
        pools[pool].append(f'        {head}, new Question("{g.esc(g.bi(e, "text"))}",\n                {body})),')
    unused = set(choices) - ids
    if unused:
        sys.exit(f'선택지만 있고 이벤트가 없는 id: {sorted(unused)}')

    def arr(name, items, note):
        return f'    // {note}\n    public static readonly CultEvent[] {name} =\n    {{\n{chr(10).join(items)}\n    }};\n'
    text = f'''// 거울 세계 모드 전용 이벤트 목록. Docs/gen_mirror.py로 생성됨 — 직접 고치지 말고 CSV 수정 후 다시 생성할 것.
using System.Linq;

public static class MirrorEvents
{{
{arr('All', pools['main'], '교단 턴에 뽑는 이벤트 (random / condition / negative / mood)')}
{arr('Final', pools['final'], '최종 스킬을 배운 뒤 차례로 나오는 이벤트 (kind=final, 조건 = 그 스킬을 배움)')}
{arr('Ending', pools['ending'], '엔딩 분기 이벤트 (kind=ending). 교단 40년까지 평균 차이가 없으면 나온다 (MirrorRules)')}
    // id로 찾을 때(저장 복원 등) 쓰는 전체 목록
    public static readonly CultEvent[] Pool = All.Concat(Final).Concat(Ending).ToArray();
}}
'''
    open(OUT, 'w', encoding='utf-8-sig', newline='\n').write(text)
    endings(doctrine_ids)
    print(len(pools['main']), 'mirror events +', len(pools['final']), 'final +', len(pools['ending']), 'ending')


# 엔딩 41종: Docs/mirror/endings_mirror.csv -> MirrorEndings.cs. 설명(hint)은 엔딩 목록·수수께끼에 쓰는 판정 조건
def endings(doctrine_ids):
    names = {r['id'].strip(): r for r in g.rows_of(os.path.join('mirror', 'doctrines_mirror.csv'))}
    fixed = {
        'inv_end_followers': ('교단 3년 뒤 신도가 0이 되고 8턴 안에 돌아오지 않음', 'After year 3, followers fell to zero and did not return within 8 turns.'),
        'inv_end_absorbed': ('명망이 0이 됨', 'Reputation fell to zero.'),
        'inv_end_old_age': ('교주가 세월을 이기지 못함', 'The leader died of old age.'),
    }
    side = {'D': ('욕망', 'Desire'), 'V': ('미덕', 'Virtue')}
    items, seen = [], set()
    for r in g.rows_of(os.path.join('mirror', 'endings_mirror.csv')):
        eid, gb, hidden = r['id'].strip(), r['good_bad'].strip(), r['hidden'].strip() == 'true'
        if eid in seen:
            sys.exit(f'{eid}: 중복 엔딩 id')
        seen.add(eid)
        kind = {'good': 'Good', 'bad': 'Bad', 'none': 'Neutral'}[gb]
        good = gb == 'good'
        m = re.fullmatch(r'inv_end_final_(\w+10[ab])_(good|bad)', eid)
        m2 = re.fullmatch(r'inv_end_(desire|virtue)_(good|bad)', eid)
        if hidden:
            hint = ('?', '?')
        elif eid in fixed:
            hint = fixed[eid]
        elif m:
            sid = m.group(1)
            if sid not in doctrine_ids:
                sys.exit(f'{eid}: 없는 최종 스킬')
            ko, en = side['D' if sid.startswith(('lust', 'greed', 'appetite', 'ambition')) else 'V']
            n = names[sid]
            hint = (f"최종 스킬 '{n['name']}' · {ko} 평균이 반대쪽보다 " + ('20 이상 높음' if good else '20 이상 높지 않음'),
                    f"Final skill '{n['name_en']}' · {en} average " + ('at least 20 above the other side.' if good else 'not 20 above the other side.'))
        elif m2:
            ko, en = side['D' if m2.group(1) == 'desire' else 'V']
            hint = (f"최종 스킬 없이 25년 · {ko} 평균이 더 높고 차이 20 {'이상' if good else '미만'}",
                    f"25 years without a final skill · {en} average higher by {'at least' if good else 'less than'} 20.")
        else:
            sys.exit(f'{eid}: 판정 조건을 모르는 엔딩')
        items.append(f'        new("{eid}", EndingKind.{kind}, "{g.esc(g.bi(r, "title"))}", "{g.esc(hint[0] + g.SEP + hint[1])}", '
                     f'"{g.esc(g.bi(r, "text"))}", {str(hidden).lower()}),')
    if len(items) != 41:
        sys.exit(f'거울 세계 엔딩은 41종이어야 함 ({len(items)})')
    text = f'''// 거울 세계 모드 전용 엔딩 41종. Docs/gen_mirror.py로 생성됨 — 직접 고치지 말고 Docs/mirror/endings_mirror.csv 수정 후 다시 생성할 것.
// 판정 규칙은 MirrorRules. 이름·설명·글은 한국어+구분자+영어.
public static class MirrorEndings
{{
    public static readonly Endings.Ending[] All =
    {{
{chr(10).join(items)}
    }};
}}
'''
    open(ENDINGS_OUT, 'w', encoding='utf-8-sig', newline='\n').write(text)
    print(len(items), 'mirror endings')


if __name__ == '__main__':
    main()
