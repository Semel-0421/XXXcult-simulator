# 번역 다듬기 도구: 원문과 기계번역을 나란히 보고, 다듬은 번역을 다시 넣는다.
#   보기:  python Docs/i18n_batch.py show <묶음> <시작> <개수>     → "번호|원문|기계번역" 줄
#   넣기:  python Docs/i18n_batch.py apply <묶음> <파일>           ← "번호|다듬은 번역" 줄 (UTF-8)
#   현황:  python Docs/i18n_batch.py status
import glob, json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))


def load(group):
    path = os.path.join(HERE, "i18n", f"{group}.json")
    return path, json.load(open(path, encoding="utf-8"))


def main():
    cmd = sys.argv[1]
    sys.stdout.reconfigure(encoding="utf-8")
    if cmd == "status":
        for path in sorted(glob.glob(os.path.join(HERE, "i18n", "*.json"))):
            items = json.load(open(path, encoding="utf-8"))
            print(os.path.basename(path), len(items), "mt", sum(1 for e in items if e["mt"]), "en", sum(1 for e in items if e["en"]))
    elif cmd == "show":
        _, items = load(sys.argv[2])
        start, count = int(sys.argv[3]), int(sys.argv[4])
        for i in range(start, min(len(items), start + count)):
            e = items[i]
            print(f'{i}|{e["ko"]}|{e["mt"]}')
    elif cmd == "apply":
        path, items = load(sys.argv[2])
        n = 0
        for line in open(sys.argv[3], encoding="utf-8"):
            line = line.rstrip("\n")
            if "|" not in line: continue
            idx, en = line.split("|", 1)
            items[int(idx)]["en"] = en.strip(); n += 1
        json.dump(items, open(path, "w", encoding="utf-8"), ensure_ascii=False, indent=0)
        print("applied", n)


if __name__ == "__main__":
    main()
