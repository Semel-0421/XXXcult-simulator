# Docs/eye/aeon_skills.csv -> Assets/Scripts/Eye/AeonSkillCatalog.Data.cs  (황금의 눈동자 모드의 아이온 트리, SYS_DEMIURGE 6절)
# 사용: python -I Docs/gen_aeon_skills.py   (프로젝트 루트에서 실행)
#
# 열: id, sector, level, kind, name(_en), description(_en), effects, calamity, calamity_guard, build
#   sector : yahweh / satan / samael / yaldabaoth
#   level  : 교리 단계 0~10. kind는 0~4 good·evil(단계마다 하나씩), 5~9 gnosis, 10 ascension
#   effects: 권능 트리와 같은 표기 (Docs/gen_god_skills.py 머리말). 공백으로 여러 개
#   calamity       : 익혀 두면 분기마다 쌓는 재앙 카운터 (악한 아이온·그노시스)
#   calamity_guard : 재앙 피해 감소 % (선한 아이온)
#   build          : 익히면 지을 수 있게 되는 건물 (GodBuildingType 이름, 비우면 없음)
import csv, importlib.util, io, os, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, 'Docs', 'eye', 'aeon_skills.csv')
DST = os.path.join(ROOT, 'Assets', 'Scripts', 'Eye', 'AeonSkillCatalog.Data.cs')

# 효과 표기·문자열 처리는 권능 트리 생성기와 같은 것을 쓴다
spec = importlib.util.spec_from_file_location('gen_god_skills', os.path.join(ROOT, 'Docs', 'gen_god_skills.py'))
god = importlib.util.module_from_spec(spec)
spec.loader.exec_module(god)

SECTORS = {'yahweh': 'Yahweh', 'satan': 'Satan', 'samael': 'Samael', 'yaldabaoth': 'Yaldabaoth'}
KINDS = {'good': 'Good', 'evil': 'Evil', 'gnosis': 'Gnosis', 'ascension': 'Ascension'}
LEVELS = {'good': range(0, 5), 'evil': range(0, 5), 'gnosis': range(5, 10), 'ascension': range(10, 11)}
BUILDINGS = {'Statue', 'Temple', 'Library', 'Village', 'Shelter'}


def main():
    rows = [r for r in csv.DictReader(io.StringIO(god.read_text(SRC))) if r['id'].strip()]
    seen, slots = set(), set()
    out = []
    for r in rows:
        sid, sector, kind, level = r['id'].strip(), r['sector'].strip(), r['kind'].strip(), int(r['level'])
        if sid in seen: sys.exit(f'{sid}: 중복 id')
        seen.add(sid)
        if sector not in SECTORS: sys.exit(f'{sid}: 알 수 없는 섹터 {sector}')
        if kind not in KINDS: sys.exit(f'{sid}: 잘못된 kind {kind}')
        if level not in LEVELS[kind]: sys.exit(f'{sid}: {kind}는 단계 {list(LEVELS[kind])}')
        if (sector, level, kind) in slots: sys.exit(f'{sid}: {sector} {level}단계 {kind}가 이미 있음')
        slots.add((sector, level, kind))
        build = r['build'].strip()
        if build and build not in BUILDINGS: sys.exit(f'{sid}: 알 수 없는 건물 {build}')
        effects = god.parse(r['effects'], sid)
        eff_code = f'new[] {{ {", ".join(effects)} }}' if effects else 'System.Array.Empty<GodEffect>()'
        build_code = f'GodBuildingType.{build}' if build else 'null'
        out.append(f'        new("{sid}", EyeSector.{SECTORS[sector]}, {level}, AeonKind.{KINDS[kind]}, '
                   f'"{god.esc(god.bi(r, "name"))}", "{god.esc(god.bi(r, "description"))}", {eff_code}, '
                   f'{int(r["calamity"] or 0)}, {int(r["calamity_guard"] or 0)}, {build_code}),')
    want = len(SECTORS) * 16
    if len(rows) != want: sys.exit(f'노드 {len(rows)}개 (기대 {want}개: 섹터마다 선·악 5단계 + 그노시스 5 + 승천 1)')
    cs = f'''// 아이온 트리 노드 목록. Docs/eye/aeon_skills.csv에서 생성됨 — 직접 고치지 말고 CSV 수정 후 다시 생성할 것 (python -I Docs/gen_aeon_skills.py).
public static partial class AeonSkillCatalog
{{
    static AeonSkill[] Data() => new AeonSkill[]
    {{
{chr(10).join(out)}
    }};
}}
'''
    os.makedirs(os.path.dirname(DST), exist_ok=True)
    with open(DST, 'w', encoding='utf-8', newline='\n') as f: f.write(cs)
    print(f'{len(rows)} nodes -> {DST}')


if __name__ == '__main__':
    main()
