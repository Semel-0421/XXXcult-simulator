# itch.io 홍보 이미지 원본(와이드 7:3)을 로컬 ComfyUI로 뽑는다. 결과는 Docs/promo/master_<이름>.png
# 실행: python -I Docs/gen_promo_art.py <이름> <시드> "<프롬프트>"
import os, sys, time
from io import BytesIO
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from comfy_client import run, ensure_server

W, H = 1792, 768  # 7:3 = 21:9
QUALITY = ", masterpiece, high score, great score, absurdres"
NEG = ("lowres, bad anatomy, bad hands, extra fingers, text, watermark, signature, logo, jpeg artifacts, blurry, "
       "child, loli, nsfw, nude, cropped head, out of frame" + os.environ.get("PROMO_NEG", ""))

def workflow(prompt, seed):
    return {
        "1": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": "animagine-xl-4.0.safetensors"}},
        "2": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["1", 1], "text": prompt + QUALITY}},
        "3": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["1", 1], "text": NEG}},
        "4": {"class_type": "EmptyLatentImage", "inputs": {"width": W, "height": H, "batch_size": 1}},
        "5": {"class_type": "KSampler", "inputs": {"model": ["1", 0], "positive": ["2", 0], "negative": ["3", 0], "latent_image": ["4", 0],
                                                    "seed": seed, "steps": 28, "cfg": 5.0, "sampler_name": "euler_ancestral", "scheduler": "normal", "denoise": 1.0}},
        "6": {"class_type": "VAEDecode", "inputs": {"samples": ["5", 0], "vae": ["1", 2]}},
        "7": {"class_type": "SaveImage", "inputs": {"images": ["6", 0], "filename_prefix": "promo"}},
    }

if __name__ == "__main__":
    name, seed, prompt = sys.argv[1], int(sys.argv[2]), sys.argv[3]
    ensure_server()
    t = time.time()
    data = run(workflow(prompt, seed), "7", "images")
    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "promo", f"master_{name}.png")
    Image.open(BytesIO(data)).convert("RGB").save(out)
    print(f"{out} ({time.time() - t:.0f}초, seed {seed})")
