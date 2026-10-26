import argparse
from pathlib import Path


DELIMITER = "\\u001F"


def source_strings(line):
    strings = []
    index = 0
    while index < len(line):
        if line.startswith("//", index):
            break
        if line[index] != '"':
            index += 1
            continue

        start = index + 1
        index = start
        while index < len(line):
            if line[index] == "\\":
                index += 2
            elif line[index] == '"':
                strings.append(line[start:index])
                index += 1
                break
            else:
                index += 1
    return strings


def check(path):
    issues = []
    for line_number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        for literal in source_strings(line):
            if DELIMITER not in literal:
                continue
            parts = literal.split(DELIMITER)
            if len(parts) != 2:
                issues.append(f"{path}:{line_number} 구분자로 나뉜 조각이 정확히 두 개가 아님")
                continue
            korean, english = parts
            if korean == "?" and english == "?":
                continue
            if not korean.strip() or not english.strip():
                issues.append(f"{path}:{line_number} 한국어 또는 영어 조각이 비어 있음")
            if not any("가" <= char <= "힣" for char in korean):
                issues.append(f"{path}:{line_number} 한국어 조각에 한글이 없음")
            if any("가" <= char <= "힣" for char in english):
                issues.append(f"{path}:{line_number} 영어 조각에 한글이 있음")
    return issues


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--path", type=Path)
    args = parser.parse_args()
    root = args.path if args.path is not None else Path("Assets/Scripts")
    paths = root.glob("*.cs") if args.path is not None else root.rglob("*.cs")
    files = sorted(path for path in paths if "Shop" not in path.parts)
    issues = [issue for path in files for issue in check(path)]
    for issue in issues:
        print(issue)
    print("FAIL" if issues else "PASS")
    return 1 if issues else 0


if __name__ == "__main__":
    raise SystemExit(main())
