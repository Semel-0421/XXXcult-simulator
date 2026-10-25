# Docs/eye/endings_eye.csv -> Assets/Scripts/Eye/EyeEndings.cs  (황금의 눈동자 모드 전용 엔딩 13종, SYS_DEMIURGE 7절)
# 사용: python -I Docs/gen_eye_endings.py   (프로젝트 루트에서 실행)
# 열: id, kind(good/bad), hidden(0/1), name(_en), description(_en) = 판정 조건, text(_en) = 엔딩 글 ({0} 교단 이름, {1} 교단 운영 연수)
import csv, io, os, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, 'Docs', 'eye', 'endings_eye.csv')
DST = os.path.join(ROOT, 'Assets', 'Scripts', 'Eye', 'EyeEndings.cs')
BS = chr(92)
SEP = BS + 'u001F'
KINDS = {'good': 'EndingKind.Good', 'bad': 'EndingKind.Bad'}


def esc(s):
    return s.replace(BS, BS * 2).replace('"', BS + '"')


def bi(row, col):
    return esc(row[col].strip()) + SEP + esc(row[col + '_en'].strip())


def main():
    raw = open(SRC, 'rb').read().decode('utf-8-sig')
    rows = [r for r in csv.DictReader(io.StringIO(raw)) if r['id'].strip()]
    ids = [r['id'].strip() for r in rows]
    if len(set(ids)) != len(ids): sys.exit('중복 id')
    if len(rows) != 13: sys.exit(f'엔딩 {len(rows)}개 (기대 13개)')
    out = []
    for r in rows:
        kind = r['kind'].strip()
        if kind not in KINDS: sys.exit(f"{r['id']}: kind는 good/bad")
        hidden = 'true' if r['hidden'].strip() == '1' else 'false'
        out.append(f'        new("{r["id"].strip()}", {KINDS[kind]}, "{bi(r, "name")}", "{bi(r, "description")}", "{bi(r, "text")}", {hidden}),')
    cs = ('// 황금의 눈동자 모드 전용 엔딩 13종 (SYS_DEMIURGE 7절). Docs/eye/endings_eye.csv에서 생성됨 — 직접 고치지 말고 CSV 수정 후 다시 생성할 것 (python -I Docs/gen_eye_endings.py).\n'
          '// 판정은 EyeRules.DueEnding. 글의 {0} 교단 이름, {1} 교단 운영 연수.\n'
          'public static class EyeEndings\n{\n    public static readonly Endings.Ending[] All =\n    {\n'
          + '\n'.join(out) +
          '\n    };\n\n    public static Endings.Ending Find(string id) => System.Array.Find(All, e => e.Id == id);\n}\n')
    with open(DST, 'w', encoding='utf-8', newline='\n') as f: f.write(cs)
    print(f'{len(rows)} endings -> {DST}')


if __name__ == '__main__':
    main()
