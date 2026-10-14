import csv, io, sys
src, dst = sys.argv[1], sys.argv[2]
raw = open(src, 'rb').read()
try: text = raw.decode('utf-8-sig')
except UnicodeDecodeError: text = raw.decode('cp949')
rows = list(csv.reader(io.StringIO(text)))
header = rows[1]
cult = {'믿음':'LeaderFaith','신도의 믿음':'FollowerFaith','권위':'Authority','봉헌금':'Offerings','신도':'Followers','교리':'Doctrine','접신(히든)':'Communion','명망':'Reputation'}
pers = {'체력':'Health','정신력':'Mentality','신앙심':'Piety','평판':'Repute','재산':'Wealth','기적(히든)':'Miracle','음욕':'Lust','재물욕':'Greed','식욕':'Appetite','권력욕':'Ambition','절제':'Temperance','자애':'Charity','인내':'Patience','청렴':'Integrity'}
cols = {i:h for i,h in enumerate(header)}
for h in header[4:-1]:
    assert h in cult or h in pers, h
BS = chr(92)
def esc(s): return s.replace(BS, BS*2).replace('"', BS+'"').replace(chr(10), BS+'n')
out, warn = [], []
for r in rows[2:]:
    if len(r) < 3 or not r[1].strip(): continue
    mods = []
    for i in range(4, len(header)-1):
        v = r[i].strip() if i < len(r) else ''
        if not v: continue
        if not v.endswith('%'): warn.append(f'{r[0]} {header[i]} "{v}" -> {v}%로 처리')
        n = float(v.rstrip('%'))
        num = f'{n:g}f'
        mods.append(f'Cult(StatId.{cult[header[i]]}, {num})' if header[i] in cult else f'Personal(PersonalStatId.{pers[header[i]]}, {num})')
    extra = r[len(header)-1].strip() if len(r) >= len(header) else ''
    body = ',\n            '.join(mods)
    out.append(f'        new("{esc(r[0])}", "{esc(r[1])}", "{esc(r[2])}",\n            "{esc(r[3])}",\n            "{esc(extra)}",\n            {body}),')
cs = f'''// 기프티드 목록. Docs/gifted.csv에서 생성됨 — 직접 고치지 말고 CSV 수정 후 다시 생성할 것.
// 효과 수치는 퍼센트(%) 단위. 기타 효과(ExtraEffect)는 아직 설명 텍스트만 보관한다.
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
print(len(out), 'entries'); print('\n'.join(warn))
