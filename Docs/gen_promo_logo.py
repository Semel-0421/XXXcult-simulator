# itch.io 로고(투명 PNG, 가로형)를 게임 제목 글꼴로 그린다. 결과: Docs/promo/logo.png (+ 흰/검 배경 미리보기)
# 실행: python -I Docs/gen_promo_logo.py
import os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
FONT = os.path.join(HERE, "..", "Assets", "TextMesh Pro", "Fonts", "Fust&Schoeffer-Durandus-GoticoAntiqua118G.otf")
OUT = os.path.join(HERE, "promo")
GOLD_TOP, GOLD_BOTTOM = (250, 222, 140), (176, 128, 42)
INK = (28, 10, 40)  # 짙은 보라 테두리: 흰 배경에서도 글자 윤곽이 보이게

def layer(text, px, stroke):
    font = ImageFont.truetype(FONT, px)
    l, t, r, b = font.getbbox(text, stroke_width=stroke)
    size = (r - l + stroke * 2, b - t + stroke * 2)
    edge = Image.new("RGBA", size, (0, 0, 0, 0))
    ImageDraw.Draw(edge).text((stroke - l, stroke - t), text, font=font, fill=INK + (255,), stroke_width=stroke, stroke_fill=INK + (255,))
    m = Image.new("L", size, 0); ImageDraw.Draw(m).text((stroke - l, stroke - t), text, font=font, fill=255)
    g = Image.new("RGBA", size)  # 세로 금빛 그라데이션
    for y in range(size[1]):
        k = y / max(1, size[1] - 1)
        g.paste(tuple(int(a + (b - a) * k) for a, b in zip(GOLD_TOP, GOLD_BOTTOM)) + (255,), (0, y, size[0], y + 1))
    g.putalpha(m)
    return Image.alpha_composite(edge, g)

title = layer("XXXcult", 220, 10)
sub = layer("Simulator", 96, 6)  # 대문자 블랙레터는 I가 J처럼 보여서 소문자로
pad, gap = 40, 6
W = max(title.width, sub.width) + pad * 2
H = title.height + gap + sub.height + pad * 2
logo = Image.new("RGBA", (W, H), (0, 0, 0, 0))
logo.alpha_composite(title, ((W - title.width) // 2, pad))
logo.alpha_composite(sub, ((W - sub.width) // 2, pad + title.height + gap))
glow = logo.split()[3].filter(ImageFilter.GaussianBlur(14)).point(lambda a: int(a * 0.55))
halo = Image.new("RGBA", (W, H), (120, 60, 170, 0)); halo.putalpha(glow)  # 보라 후광: 어두운 배경에서 떠 보이게
final = Image.alpha_composite(halo, logo)
final.save(os.path.join(OUT, "logo.png"))
prev = Image.new("RGB", (W, H * 2), (255, 255, 255)); prev.paste((12, 6, 20), (0, H, W, H * 2))
prev.paste(final, (0, 0), final); prev.paste(final, (0, H), final)
prev.save(os.path.join(OUT, "logo_preview.png"))
print(final.size)
