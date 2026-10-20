# Docs/gifted_v2.csv -> Assets/Scripts/Talent/GiftedV2Catalog.cs (run from project root: python -I Docs/gen_gifted_v2_cs.py)
import csv

SEP = "\\u001F"  # C# escape; AchievementCatalog uses the same "KO\u001FEN" convention


def e(s):
    return s.replace("\\", "\\\\").replace('"', '\\"').replace("\r", "").replace("\n", "\\n")


def p(s):
    return '"' + e(s) + '"'


def q(ko, en):
    return '"' + e(ko) + SEP + e(en) + '"'


def b(v):
    return "true" if v else "false"


rows = list(csv.DictReader(open("Docs/gifted_v2.csv", encoding="utf-8-sig", newline="")))
lines = [
    "        new(%s, %s, %s, %s, %s, %s, %s, %s),"
    % (
        p(r["id"]), p(r["mode"]), b(r["scope"] == "shared"),
        q(r["name"], r["name_en"]), q(r["flavor"], r["flavor_en"]), q(r["effect"], r["effect_en"]),
        p(r["linked_achievement"]), b(r["gate"] == "adult_option"),
    )
    for r in rows
]

out = '''// 신규 타고난 재능(v2) 목록. Docs/gifted_v2.csv에서 생성됨 — 직접 고치지 말고 CSV 수정 후 Docs/gen_gifted_v2_cs.py로 다시 생성할 것. 읽기 전용 데이터(게임플레이 미연결).
using System.Collections.Generic;
using System.Linq;

public static class GiftedV2Catalog
{
    public sealed class Entry
    {
        public readonly string Id, Mode, Name, Flavor, Effect, LinkedAchievement; // Name/Flavor/Effect: "KO\\u001FEN"
        public readonly bool Shared, AdultGated;

        public Entry(string id, string mode, bool shared, string name, string flavor, string effect, string linkedAchievement, bool adultGated)
        {
            Id = id; Mode = mode; Shared = shared; Name = name; Flavor = flavor; Effect = effect;
            LinkedAchievement = linkedAchievement; AdultGated = adultGated;
        }
    }

    public static readonly IReadOnlyList<Entry> All = new Entry[]
    {
%s
    };

    public static Entry Find(string id) => All.FirstOrDefault(e => e.Id == id);

    public static IEnumerable<Entry> ForMode(string mode) => All.Where(e => e.Mode == mode);
}
''' % "\n".join(lines)
open("Assets/Scripts/Talent/GiftedV2Catalog.cs", "w", encoding="utf-8-sig", newline="\n").write(out)
print(len(rows))
