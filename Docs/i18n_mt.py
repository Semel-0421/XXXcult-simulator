# 1차 기계번역: Docs/i18n/*.json에서 mt가 빈 항목을 로컬 모델(Helsinki-NLP/opus-mt-ko-en, Apache-2.0)로 번역해 채운다.
# 결과는 초안일 뿐이고, 최종 번역(en)은 원문과 비교해 다듬어 넣는다.
# 실행 (transformers가 있는 ComfyUI 가상환경): D:\AI\ComfyUI\.venv\Scripts\python.exe Docs/i18n_mt.py
import glob, json, os, sys, time
import torch
from transformers import MarianMTModel, MarianTokenizer

MODEL = "Helsinki-NLP/opus-mt-ko-en"
HERE = os.path.dirname(os.path.abspath(__file__))


def main():
    tok = MarianTokenizer.from_pretrained(MODEL)
    model = MarianMTModel.from_pretrained(MODEL).eval()
    device = "cuda" if torch.cuda.is_available() else "cpu"
    model.to(device)
    for path in sorted(glob.glob(os.path.join(HERE, "i18n", "*.json"))):
        items = json.load(open(path, encoding="utf-8"))
        todo = [e for e in items if not e["mt"]]
        start = time.time()
        for i in range(0, len(todo), 16):
            batch = todo[i:i + 16]
            enc = tok([e["ko"] for e in batch], return_tensors="pt", padding=True, truncation=True, max_length=400).to(device)
            with torch.no_grad():
                out = model.generate(**enc, max_new_tokens=400, num_beams=4)
            for e, t in zip(batch, tok.batch_decode(out, skip_special_tokens=True)):
                e["mt"] = t
            if i % 320 == 0:
                json.dump(items, open(path, "w", encoding="utf-8"), ensure_ascii=False, indent=0)
        json.dump(items, open(path, "w", encoding="utf-8"), ensure_ascii=False, indent=0)
        print(os.path.basename(path), len(todo), f"{time.time() - start:.0f}s", flush=True)


if __name__ == "__main__":
    main()
