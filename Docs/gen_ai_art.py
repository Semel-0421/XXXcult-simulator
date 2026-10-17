# Docs/adult_art_prompts.csv의 프롬프트로 로컬 ComfyUI(D:\AI\ComfyUI)에서 삽화를 뽑아 Assets/Resources/AdultArt에 저장한다.
# 실행: python Docs/gen_ai_art.py [파일이름 ...] [--seed N] [--model animagine|noobai] [--out 폴더]
#   예: python Docs/gen_ai_art.py ad01.png ad02.png
#       python Docs/gen_ai_art.py --model noobai --out ArtCompare/noobai   (모델 비교용, 게임 폴더를 덮지 않음)
# --model을 안 주면 CSV의 model 열을 따른다(관능이 중요한 장면은 noobai, 상황 묘사가 중요한 장면은 animagine).
# 시드를 안 주면 파일 이름으로 정해서, 모델을 바꿔도 같은 시드로 비교된다. ComfyUI가 꺼져 있으면 띄우고 기다린다.
import csv, json, os, subprocess, sys, time, urllib.request, zlib
from io import BytesIO
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "Assets", "Resources", "AdultArt")
COMFY_DIR = r"D:\AI\ComfyUI"
URL = "http://127.0.0.1:8188"
# GTX 1070이 화면도 그리고 있어서, GPU 커널 하나가 2초를 넘기면 윈도우가 드라이버를 리셋(TDR)하고 ComfyUI가 죽는다.
# 어텐션을 쪼개 커널을 짧게 하고, 동적 VRAM은 끈다. 가중치 일부를 비동기로 오프로드하면 illegal memory access가 나서
# lowvram·async offload도 끈다. VRAM이 모자라면 "--fp8_e4m3fn-unet"을 더한다(화질 약간 손해).
SERVER_FLAGS = ["--disable-dynamic-vram", "--disable-pinned-memory", "--use-split-cross-attention", "--disable-async-offload", "--fp8_e4m3fn-unet", "--force-fp32"]
LOG = r"D:\AI\comfy.log"
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

def api(path, payload=None):
    req = urllib.request.Request(URL + path, data=json.dumps(payload).encode() if payload is not None else None,
                                 headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=30) as r:
        return r.read()

def stop_server():
    # CUDA 오류가 난 서버는 GPU 컨텍스트가 망가져 계속 실패하므로 8188 포트를 잡은 프로세스를 내린다
    subprocess.run(["powershell", "-NoProfile", "-Command",
                    "Get-NetTCPConnection -LocalPort 8188 -State Listen -EA SilentlyContinue | % { Stop-Process -Id $_.OwningProcess -Force }"])
    time.sleep(3)

def ensure_server():
    try:
        api("/system_stats"); return
    except OSError:
        pass
    print("ComfyUI 시작 중...")
    subprocess.Popen([os.path.join(COMFY_DIR, ".venv", "Scripts", "python.exe"), "-u", "main.py", "--listen", "127.0.0.1", *SERVER_FLAGS],
                     cwd=COMFY_DIR, stdout=open(LOG, "w", encoding="utf-8"), stderr=subprocess.STDOUT)
    for _ in range(180):
        time.sleep(2)
        try:
            api("/system_stats"); return
        except OSError:
            pass
    sys.exit("ComfyUI가 6분 안에 뜨지 않았음")

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
    pid = json.loads(api("/prompt", {"prompt": workflow(prompt, seed, negative)}))["prompt_id"]
    start = time.time()
    while True:
        time.sleep(3)
        hist = json.loads(api(f"/history/{pid}"))
        if pid in hist:
            entry = hist[pid]
            if entry.get("status", {}).get("status_str") == "error":
                err = next((m[1].get("exception_message", "") for m in entry["status"].get("messages", []) if m[0] == "execution_error"), "")
                raise RuntimeError(err.strip().splitlines()[0] if err.strip() else "알 수 없는 오류")
            img = entry["outputs"]["7"]["images"][0]
            data = api(f"/view?filename={img['filename']}&subfolder={img['subfolder']}&type={img['type']}")
            Image.open(BytesIO(data)).convert("RGB").resize(SAVE_SIZE, Image.LANCZOS).save(os.path.join(OUT, name))
            print(f"{name} 저장 ({time.time() - start:.0f}초, seed {seed})")
            return

def take(args, flag):
    if flag not in args: return None
    i = args.index(flag); value = args[i + 1]; del args[i:i + 2]
    return value

def main():
    global CHECKPOINT, QUALITY, OUT
    args = sys.argv[1:]
    seed = take(args, "--seed")
    model = take(args, "--model")  # 안 주면 CSV의 model 열(장면별로 고른 모델)을 쓴다
    if model and model not in MODELS: sys.exit(f"모델은 {', '.join(MODELS)} 중 하나")
    OUT = os.path.abspath(take(args, "--out") or OUT)
    with open(os.path.join(HERE, "adult_art_prompts.csv"), encoding="utf-8-sig") as f:
        rows = [r for r in csv.DictReader(f) if not args or r["file"] in args]
    if not rows: sys.exit("맞는 항목이 없음")
    ensure_server()
    os.makedirs(OUT, exist_ok=True)
    rows.sort(key=lambda r: model or r.get("model") or "animagine")  # 같은 모델끼리 묶어 체크포인트 교체를 줄인다
    for r in rows:
        CHECKPOINT, QUALITY = MODELS[model or r.get("model") or "animagine"]
        for attempt in range(3):  # 서버가 죽으면 다시 띄우고 같은 그림을 다시 뽑는다
            try:
                generate(r["file"], r["prompt"], int(seed) if seed else zlib.crc32(r["file"].encode()), r.get("negative") or ""); break
            except (OSError, RuntimeError) as e:
                print(f"{r['file']}: 실패({e}), 서버 재시작 후 재시도 {attempt + 1}/3")
                stop_server(); ensure_server()

if __name__ == "__main__":
    main()
