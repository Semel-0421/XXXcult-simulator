# Docs/adult_titles.csv -> Assets/Scripts/Story/AdultTitles.cs
import csv, os
root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BS = chr(92)
esc = lambda s: s.replace(BS, BS + BS).replace('"', BS + '"')
rows = list(csv.DictReader(open(os.path.join(root, 'Docs', 'adult_titles.csv'), encoding='utf-8-sig')))
out = ['// Docs/gen_adult_titles_cs.py로 생성됨 (원본: Docs/adult_titles.csv). 직접 고치지 말 것.',
       'public static class AdultTitles', '{',
       '    // id, 한국어, English (_f/_m 변형 포함)',
       '    public static readonly (string id, string ko, string en)[] All =', '    {']
for r in rows:
    out.append(f'        ("{esc(r["id"])}", "{esc(r["title"])}", "{esc(r["title_en"])}"),')
out += ['    };', '', '    public static string Title(string id)', '    {',
        '        foreach (var t in All) if (t.id == id) return Lang.IsKorean ? t.ko : t.en;',
        '        return id;', '    }', '',
        '    public static bool Has(string id)', '    {',
        '        foreach (var t in All) if (t.id == id) return true;',
        '        return false;', '    }', '}', '']
open(os.path.join(root, 'Assets', 'Scripts', 'Story', 'AdultTitles.cs'), 'w', encoding='utf-8-sig', newline='\n').write('\n'.join(out))
print(len(rows), 'rows')
