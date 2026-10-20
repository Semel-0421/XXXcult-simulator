# Docs/adult_art_prompts.csv의 프롬프트로 로컬 ComfyUI(D:\AI\ComfyUI)에서 삽화를 뽑아 Assets/Resources/AdultArt에 저장한다.
# 실행: python Docs/gen_ai_art.py [파일이름 ...] [--seed N] [--model animagine|noobai] [--out 폴더]
#   예: python Docs/gen_ai_art.py ad01.png ad02.png
#       python Docs/gen_ai_art.py --model noobai --out ArtCompare/noobai   (모델 비교용, 게임 폴더를 덮지 않음)
# --model을 안 주면 CSV의 model 열을 따른다(관능이 중요한 장면은 noobai, 상황 묘사가 중요한 장면은 animagine).
# 시드를 안 주면 파일 이름으로 정해서, 모델을 바꿔도 같은 시드로 비교된다. ComfyUI가 꺼져 있으면 띄우고 기다린다.
import csv, os, sys, time, zlib
from io import BytesIO
from PIL import Image
from comfy_client import each_with_retry, run, take

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "Assets", "Resources", "AdultArt")
# 모델 이름 → (체크포인트, 품질 태그). 둘 다 SDXL 애니풍이고 danbooru 태그를 쓴다. noobai는 성인 묘사를 학습한 모델.
MODELS = {
    "animagine": ("animagine-xl-4.0.safetensors", ", masterpiece, high score, great score, absurdres"),
    "noobai": ("noobai-xl-1.1.safetensors", ", masterpiece, best quality, newest, absurdres, highres"),
}
CHECKPOINT, QUALITY = MODELS["animagine"]
WIDTH, HEIGHT, SAVE_SIZE = 832, 1216, (416, 608)  # SDXL 세로 비율로 뽑고 절반으로 줄여 저장
# 미성년으로 보이는 표현 차단은 성인 콘텐츠라 빼지 않는다
NEGATIVE = ("lowres, bad anatomy, bad hands, extra fingers, missing fingers, text, watermark, signature, jpeg artifacts, blurry, "
            "child, loli, shota, teen, young, petite, flat chest, school uniform")

def workflow(prompt, seed, negative=""):
    return {
        "1": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": CHECKPOINT}},
        "2": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["1", 1], "text": prompt + QUALITY}},
        "3": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["1", 1], "text": NEGATIVE + (", " + negative if negative else "")}},
        "4": {"class_type": "EmptyLatentImage", "inputs": {"width": WIDTH, "height": HEIGHT, "batch_size": 1}},
        "5": {"class_type": "KSampler", "inputs": {"model": ["1", 0], "positive": ["2", 0], "negative": ["3", 0], "latent_image": ["4", 0],
                                                    "seed": seed, "steps": 28, "cfg": 5.0, "sampler_name": "euler_ancestral", "scheduler": "normal", "denoise": 1.0}},
        "6": {"class_type": "VAEDecode", "inputs": {"samples": ["5", 0], "vae": ["1", 2]}},
        "7": {"class_type": "SaveImage", "inputs": {"images": ["6", 0], "filename_prefix": "adultart"}},
    }

def generate(name, prompt, seed, negative=""):
    start = time.time()
    data = run(workflow(prompt, seed, negative), "7", "images")
    Image.open(BytesIO(data)).convert("RGB").resize(SAVE_SIZE, Image.LANCZOS).save(os.path.join(OUT, name))
    print(f"{name} 저장 ({time.time() - start:.0f}초, seed {seed})", flush=True)

def main():
    global CHECKPOINT, QUALITY, OUT
    args = sys.argv[1:]
    seed = take(args, "--seed")
    model = take(args, "--model")  # 안 주면 CSV의 model 열(장면별로 고른 모델)을 쓴다
    if model and model not in MODELS: sys.exit(f"모델은 {', '.join(MODELS)} 중 하나")
    OUT = os.path.abspath(take(args, "--out") or OUT)
    csv_name = take(args, "--csv") or "adult_art_prompts.csv"  # 다른 프롬프트 모음(예: action_art_prompts.csv)도 쓸 수 있게
    with open(os.path.join(HERE, csv_name), encoding="utf-8-sig") as f:
        rows = [r for r in csv.DictReader(f) if not args or r["file"] in args]
    if not rows: sys.exit("맞는 항목이 없음")
    os.makedirs(OUT, exist_ok=True)
    rows.sort(key=lambda r: model or r.get("model") or "animagine")  # 같은 모델끼리 묶어 체크포인트 교체를 줄인다

    def make(r):
        global CHECKPOINT, QUALITY
        CHECKPOINT, QUALITY = MODELS[model or r.get("model") or "animagine"]
        generate(r["file"], r["prompt"], int(seed) if seed else zlib.crc32(r["file"].encode()), r.get("negative") or "")
    each_with_retry(rows, lambda r: r["file"], make)

if __name__ == "__main__":
    main()
