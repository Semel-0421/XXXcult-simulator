# 성별 반전 변형 이벤트(Docs/i18n/gender/batch*.json)를 adult_events.csv / adult_choices.csv에 넣는다.
# 모드·테마·확률·효과는 원본(base) 이벤트에서 그대로 복사하고, 텍스트와 player(m/f)만 바꾼다. 같은 id가 있으면 교체한다.
# 실행: python Docs/gender_merge.py  →  그다음 python Docs/gen_events.py
import csv, glob, json, os

HERE = os.path.dirname(os.path.abspath(__file__))
EV, CH = os.path.join(HERE, "adult_events.csv"), os.path.join(HERE, "adult_choices.csv")


def load(path):
    rows = list(csv.reader(open(path, encoding="utf-8-sig")))
    return rows[0], rows[1:]


def save(path, header, rows):
    w = csv.writer(open(path, "w", encoding="utf-8-sig", newline=""), lineterminator="\r\n")
    w.writerow(header)
    w.writerows(rows)


def main():
    eh, ev = load(EV)
    ch, cv = load(CH)
    ei, ci = {k: i for i, k in enumerate(eh)}, {k: i for i, k in enumerate(ch)}
    variants = [v for f in sorted(glob.glob(os.path.join(HERE, "i18n", "gender", "batch*.json"))) for v in json.load(open(f, encoding="utf-8"))]
    ids = {v["id"] for v in variants}
    ev = [r for r in ev if r[0] not in ids]
    cv = [r for r in cv if r[0] not in ids]
    for v in variants:
        base = next(r for r in ev if r[0] == v["base"])
        row = list(base)
        row[ei["id"]], row[ei["text"]], row[ei["text_en"]], row[ei["player"]] = v["id"], v["text"], v["text_en"], v["player"]
        ev.insert(ev.index(base) + 1, row)
        base_choices = [r for r in cv if r[0] == v["base"]]
        assert len(base_choices) == len(v["choices"]), f'{v["id"]}: 선택지 수가 원본과 다름'
        at = cv.index(base_choices[-1]) + 1
        for n, (b, c) in enumerate(zip(base_choices, v["choices"])):
            r = list(b)
            r[ci["event_id"]] = v["id"]
            for k in ("label", "label_en", "success_text", "success_text_en", "fail_text", "fail_text_en"):
                r[ci[k]] = c[k]
            cv.insert(at + n, r)
    save(EV, eh, ev)
    save(CH, ch, cv)
    print(f"merged {len(variants)} variants")


if __name__ == "__main__":
    main()
