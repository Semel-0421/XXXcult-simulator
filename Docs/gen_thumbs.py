# itch.io 커버(630x500) 시안을 로컬 ComfyUI로 뽑고 게임 제목을 얹는다. 실행: python Docs/gen_thumbs.py
import os, sys
from io import BytesIO
from PIL import Image, ImageDraw, ImageFont, ImageFilter
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gen_ai_art as g
from comfy_client import run

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "Temp", "thumbs")
FONT = os.path.join(HERE, "..", "Assets", "TextMesh Pro", "Fonts", "Fust&Schoeffer-Durandus-GoticoAntiqua118G.otf")
SFW = ", nsfw, nude, cleavage"
CONCEPTS = {
    "A_leader": "1man, cult leader, purple and gold robes, arms raised, standing on altar, crowd of hooded followers kneeling, candles, dark cathedral, god rays, dramatic lighting, from below, wide shot, dark fantasy",
    "B_grimoire": "no humans, ancient purple book with golden winged caduceus emblem, floating, glowing, swirling purple galaxy, stars, dark background, magic circle, gold particles, still life, centered",
    "C_crossroads": "1man, robed preacher, back view, standing between halo light and pile of gold coins, throne, stained glass, purple and gold palette, dark church interior, cinematic, wide shot",
}

def title(img):
    # 고딕체의 X는 장식 글자라 알아보기 어려워 XXX만 Georgia Bold로 쓴다
    parts = [("XXX", ImageFont.truetype("C:/Windows/Fonts/georgiab.ttf", 56)), ("cult Simulator", ImageFont.truetype(FONT, 58))]
    d = ImageDraw.Draw(img)
    w = sum(d.textlength(t, font=f) for t, f in parts) + 6
    x, y = (img.width - w) / 2, img.height - 110
    shadow = Image.new("RGBA", img.size, (0, 0, 0, 0)); sd = ImageDraw.Draw(shadow)
    for t, f in parts:
        dy = -6 if f.size == 56 else 0
        sd.text((x, y + dy), t, font=f, fill=(0, 0, 0, 230))
        d.text((x, y + dy), t, font=f, fill=(232, 200, 110), stroke_width=2, stroke_fill=(40, 10, 50))
        x += d.textlength(t, font=f) + 6
    base = img.copy(); base.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(6))); base.alpha_composite(img)
    return base

def main():
    os.makedirs(OUT, exist_ok=True)
    g.CHECKPOINT, g.QUALITY = g.MODELS["animagine"]
    g.WIDTH, g.HEIGHT = 1152, 896
    only = sys.argv[1:]
    for i, (name, prompt) in enumerate(CONCEPTS.items()):
        if only and name not in only: continue
        data = run(g.workflow(prompt, 4242 + i, SFW.strip(", ")), "7", "images")
        im = Image.open(BytesIO(data)).convert("RGBA")
        im = im.resize((643, 500), Image.LANCZOS).crop((6, 0, 636, 500))  # 630x500 비율로 맞춤
        title(im).convert("RGB").save(os.path.join(OUT, name + ".png"))
        print(name, "ok", flush=True)

if __name__ == "__main__":
    main()
