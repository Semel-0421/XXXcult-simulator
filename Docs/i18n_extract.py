# 번역할 한국어 문장을 CSV에서 모아 Docs/i18n/<묶음>.json으로 만든다 (중복 제거, 기존 번역 유지).
#   항목: {"ko": 원문, "mt": 기계번역(1차), "en": 다듬은 번역(최종)}
# 실행: python Docs/i18n_extract.py
import csv, json, os, re

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "i18n")
HANGUL = re.compile("[가-힣]")

# 묶음 이름 → [(CSV, 번역할 열들)]
GROUPS = {
    "life": [("life_events.csv", ["text"]), ("life_choices.csv", ["label", "success_text", "fail_text"])],
    "cult": [("cult_events.csv", ["text"]), ("cult_choices.csv", ["label", "success_text", "fail_text"])],
    "god": [("god_events.csv", ["text"]), ("god_choices.csv", ["label", "success_text", "fail_text"])],
    "adult": [("adult_events.csv", ["text"]), ("adult_choices.csv", ["label", "success_text", "fail_text"])],
    "catalog": [("achievements.csv", ["name", "description"]), ("doctrines.csv", ["name", "description"]),
                ("god_skills.csv", ["name", "description"])],
}


def rows(name):
    return list(csv.DictReader(open(os.path.join(HERE, name), encoding="utf-8-sig")))


def gifted_texts():
    # gifted.csv는 두 줄짜리 머리글: 재능명(1열)·플레이버 텍스트(3열)·기타 효과
    rs = list(csv.reader(open(os.path.join(HERE, "gifted.csv"), encoding="utf-8-sig")))
    col = {h: i for i, h in enumerate(rs[1])}
    for r in rs[2:]:
        r = r + [""] * len(rs[1])
        for i in (1, 3, col["기타 효과"]):
            yield r[i].strip()


def main():
    os.makedirs(OUT, exist_ok=True)
    groups = dict(GROUPS)
    groups["gifted"] = None
    for group, sources in groups.items():
        path = os.path.join(OUT, f"{group}.json")
        old = {e["ko"]: e for e in json.load(open(path, encoding="utf-8"))} if os.path.exists(path) else {}
        items, seen = [], set()
        texts = gifted_texts() if sources is None else ((r.get(c) or "").strip() for name, cols in sources for r in rows(name) for c in cols)
        for ko in texts:
            if ko and HANGUL.search(ko) and ko not in seen:
                seen.add(ko)
                items.append(old.get(ko, {"ko": ko, "mt": "", "en": ""}))
        json.dump(items, open(path, "w", encoding="utf-8"), ensure_ascii=False, indent=0)
        print(group, len(items))


if __name__ == "__main__":
    main()
