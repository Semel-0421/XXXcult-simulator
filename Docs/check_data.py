import argparse
import csv
from pathlib import Path


def csv_files(root, recursive):
    paths = root.rglob("*.csv") if recursive else root.glob("*.csv")
    return sorted(
        path for path in paths
        if not path.name.startswith("action_")
        and path.name != "itch_stats.csv"
        and "__pycache__" not in path.parts
    )


def check(path):
    issues = []
    with path.open(encoding="utf-8-sig", newline="") as stream:
        reader = csv.reader(stream)
        header = next(reader, None)
        if header is None:
            return issues

        english_columns = [
            (index, name[:-3], header.index(name[:-3]))
            for index, name in enumerate(header)
            if name.endswith("_en") and name[:-3] in header
        ]
        for row in reader:
            if not row or all(not value for value in row):
                continue
            line = reader.line_num
            if len(row) != len(header):
                issues.append(f"{path}:{line} 열 수가 머리줄과 다름 ({len(row)} != {len(header)})")
                continue

            for index, base, base_index in english_columns:
                english = row[index]
                if any("가" <= char <= "힣" for char in english):
                    issues.append(f"{path}:{line} {header[index]}에 한글이 있음")
                if row[base_index] not in ("", "-") and not english:
                    issues.append(f"{path}:{line} {base} 값이 있는데 {header[index]}가 비어 있음")
    return issues


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--path", type=Path)
    args = parser.parse_args()
    root = args.path if args.path is not None else Path(__file__).parent
    issues = [issue for path in csv_files(root, args.path is None) for issue in check(path)]
    for issue in issues:
        print(issue)
    print("FAIL" if issues else "PASS")
    return 1 if issues else 0


if __name__ == "__main__":
    raise SystemExit(main())
