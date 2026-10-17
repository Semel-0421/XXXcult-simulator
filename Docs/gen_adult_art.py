# 성인 콘텐츠 삽화(Assets/Resources/AdultArt/*.png)를 코드로 그린다.
# 노골적인 묘사 없이 실루엣·촛불·김으로 암시만 한다(페이드 아웃 수위). 목록은 Docs/adult_art_prompts.csv와 맞춘다.
# 실행: python Docs/gen_adult_art.py  (Pillow 필요)
import math, os, random
from PIL import Image, ImageDraw, ImageFilter, ImageChops

W, H, S = 420, 600, 2  # 결과 크기, 슈퍼샘플링 배율
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "AdultArt")
INK = (14, 8, 20)

def P(x, y): return (x * S, y * S)

class Canvas:
    def __init__(self, top, bottom):
        self.img = Image.new("RGB", (W * S, H * S))
        d = ImageDraw.Draw(self.img)
        for y in range(H * S):
            t = y / (H * S)
            d.line([(0, y), (W * S, y)], fill=tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)))
        self.d = d

    def glow(self, x, y, r, color, strength=1.0):
        layer = Image.new("RGB", self.img.size)
        ImageDraw.Draw(layer).ellipse([P(x - r, y - r), P(x + r, y + r)], fill=tuple(int(c * strength) for c in color))
        self.img = ImageChops.screen(self.img, layer.filter(ImageFilter.GaussianBlur(r * S * 0.45)))
        self.d = ImageDraw.Draw(self.img)

    def mist(self, blobs, color=(200, 190, 220), alpha=110, blur=18):
        layer = Image.new("RGBA", self.img.size, (0, 0, 0, 0))
        ld = ImageDraw.Draw(layer)
        for x, y, rx, ry in blobs:
            ld.ellipse([P(x - rx, y - ry), P(x + rx, y + ry)], fill=color + (alpha,))
        layer = layer.filter(ImageFilter.GaussianBlur(blur * S))
        self.img = Image.alpha_composite(self.img.convert("RGBA"), layer).convert("RGB")
        self.d = ImageDraw.Draw(self.img)

    def poly(self, pts, fill): self.d.polygon([P(*p) for p in pts], fill=fill)
    def rect(self, x0, y0, x1, y1, fill): self.d.rectangle([P(x0, y0), P(x1, y1)], fill=fill)
    def oval(self, x, y, rx, ry, fill): self.d.ellipse([P(x - rx, y - ry), P(x + rx, y + ry)], fill=fill)
    def line(self, pts, width, fill):
        self.d.line([P(*p) for p in pts], fill=fill, width=int(width * S), joint="curve")
        for p in (pts[0], pts[-1]): self.oval(p[0], p[1], width / 2, width / 2, fill)

    def save(self, name):
        img = self.img
        # 비네트
        v = Image.new("L", img.size, 0)
        ImageDraw.Draw(v).ellipse([P(-60, -40), P(W + 60, H + 40)], fill=255)
        v = v.filter(ImageFilter.GaussianBlur(70 * S))
        img = Image.composite(img, Image.new("RGB", img.size, (6, 2, 10)), v)
        img.resize((W, H), Image.LANCZOS).save(os.path.join(OUT, name))
        print("wrote", name)

# ── 인물 실루엣 ────────────────────────────────────────────
def figure(c, x, g, h, pose="stand", lean=0, long_hair=False, dress=False, robe=False,
           arms=None, rim=(255, 120, 150), color=INK, mask=None, hood=False):
    """x: 몸 중심, g: 바닥(발) 높이, h: 키. arms: [(어깨쪽 'L'/'R', 팔꿈치, 손)] 절대 좌표."""
    def draw(col, grow):
        hip_y = {"stand": g - .48 * h, "sit": g - .30 * h, "kneel": g - .30 * h}[pose]
        top = hip_y - .34 * h
        hx, hy, hr = x + lean, top - .1 * h, .066 * h
        sh = .11 * h
        # 상체
        c.poly([(x + lean - sh - grow, top + .02 * h), (x + lean + sh + grow, top + .02 * h),
                (x + .07 * h + grow, hip_y - .12 * h), (x + .1 * h + grow, hip_y + grow),
                (x - .1 * h - grow, hip_y + grow), (x - .07 * h - grow, hip_y - .12 * h)], col)
        c.line([(x + lean, top + .03 * h), (hx, hy)], .07 * h + 2 * grow, col)  # 목
        # 하체
        if robe or dress:
            spread = .2 * h if robe else .15 * h
            if pose == "stand":
                c.poly([(x - .1 * h - grow, hip_y - 2), (x + .1 * h + grow, hip_y - 2), (x + spread + grow, g + grow), (x - spread - grow, g + grow)], col)
            elif pose == "sit":
                c.poly([(x - .1 * h - grow, hip_y - 4), (x + .1 * h + grow, hip_y - 4), (x + .3 * h + grow, hip_y + .05 * h), (x + .3 * h + grow, g + grow), (x + .18 * h, g + grow), (x - .12 * h - grow, hip_y + .08 * h)], col)
            else:
                c.poly([(x - .12 * h - grow, hip_y - 4), (x + .1 * h + grow, hip_y - 4), (x + .2 * h + grow, g + grow), (x - .22 * h - grow, g + grow)], col)
        else:
            lw = .075 * h + 2 * grow
            if pose == "stand":
                c.line([(x - .05 * h, hip_y), (x - .06 * h, g - .02 * h)], lw, col)
                c.line([(x + .05 * h, hip_y), (x + .06 * h, g - .02 * h)], lw, col)
            elif pose == "sit":
                for dx in (-.04, .04):
                    c.line([(x + dx * h, hip_y), (x + .25 * h, hip_y + .02 * h), (x + .25 * h + dx * h, g - .02 * h)], lw, col)
            else:
                c.line([(x, hip_y), (x - .15 * h, g - .04 * h), (x + .12 * h, g - .03 * h)], lw, col)
        # 팔
        for side, elbow, hand in arms or [("L", (x - .14 * h, hip_y - .14 * h), (x - .15 * h, hip_y + .02 * h)),
                                          ("R", (x + .14 * h, hip_y - .14 * h), (x + .15 * h, hip_y + .02 * h))]:
            shx = x + lean + (-sh if side == "L" else sh) * .85
            c.line([(shx, top + .05 * h), elbow, hand], .055 * h + 2 * grow, col)
        # 머리
        c.oval(hx, hy, hr + grow, hr * 1.12 + grow, col)
        if long_hair:
            c.poly([(hx - hr * 1.15 - grow, hy - hr * .2), (hx + hr * 1.15 + grow, hy - hr * .2), (hx + hr * 1.5 + grow, hy + hr * 3.2 + grow), (hx - hr * 1.5 - grow, hy + hr * 3.2 + grow)], col)
        if hood:
            c.poly([(hx, hy - hr * 1.7 - grow), (hx + hr * 1.6 + grow, hy + hr * 1.6), (hx - hr * 1.6 - grow, hy + hr * 1.6)], col)
        return hx, hy, hr
    draw(rim, 1.6)
    hx, hy, hr = draw(color, 0)
    if mask:
        c.poly([(hx - hr * .95, hy - hr * .35), (hx + hr * .95, hy - hr * .35), (hx + hr * .7, hy + hr * .25), (hx, hy + hr * .05), (hx - hr * .7, hy + hr * .25)], mask)
    return hx, hy, hr

# ── 소품 ──────────────────────────────────────────────────
def candle(c, x, y, h=26, lit=True, size=1.0):
    c.rect(x - 4 * size, y - h, x + 4 * size, y, (225, 210, 190))
    if lit:
        c.glow(x, y - h - 6, 34 * size, (255, 160, 70), .9)
        c.poly([(x, y - h - 14 * size), (x + 4 * size, y - h - 4), (x - 4 * size, y - h - 4)], (255, 230, 150))
    else:
        c.mist([(x + 3, y - h - 16, 4, 12)], (150, 150, 160), 120, 4)

def wine_glass(c, x, y, wine=(150, 20, 50)):
    c.poly([(x - 8, y - 34), (x + 8, y - 34), (x + 5, y - 20), (x - 5, y - 20)], (210, 200, 220))
    c.poly([(x - 7, y - 30), (x + 7, y - 30), (x + 5, y - 21), (x - 5, y - 21)], wine)
    c.line([(x, y - 20), (x, y - 3)], 2, (210, 200, 220))
    c.oval(x, y - 2, 7, 2, (210, 200, 220))

def lantern(c, x, y, color=(255, 120, 60)):
    c.glow(x, y, 30, color, .8)
    c.oval(x, y, 10, 13, color)
    c.rect(x - 5, y - 16, x + 5, y - 12, INK)

def crowd(c, y, n, color=(30, 18, 40), rim=(140, 90, 160), raised=0.0, seed=1):
    rnd = random.Random(seed)
    for i in range(n):
        x = 15 + i * (W - 30) / (n - 1) + rnd.uniform(-8, 8)
        h = rnd.uniform(130, 170)
        yy = y + rnd.uniform(-6, 10)
        arms = None
        if rnd.random() < raised:
            arms = [("L", (x - 18, yy - h * .62), (x - 14, yy - h * .9)), ("R", (x + 18, yy - h * .62), (x + 14, yy - h * .9))]
        figure(c, x, yy, h, robe=True, arms=arms, rim=rim, color=color)

def rays(c, x, y, n, length, color, strength=.5, spread=math.pi):
    layer = Image.new("RGB", c.img.size)
    ld = ImageDraw.Draw(layer)
    for i in range(n):
        a = -math.pi / 2 - spread / 2 + spread * (i + .5) / n
        ld.polygon([P(x, y), P(x + math.cos(a - .05) * length, y + math.sin(a - .05) * length), P(x + math.cos(a + .05) * length, y + math.sin(a + .05) * length)],
                   fill=tuple(int(v * strength) for v in color))
    c.img = ImageChops.screen(c.img, layer.filter(ImageFilter.GaussianBlur(6 * S)))
    c.d = ImageDraw.Draw(c.img)

def floor(c, y, color): c.rect(0, y, W, H, color)

# ── 성인 이벤트 ────────────────────────────────────────────
def ad01():  # 늦은 밤 사무실, 와인 두 잔, 블라우스 단추를 푸는 후원자
    c = Canvas((22, 10, 40), (60, 16, 40))
    c.rect(250, 70, 390, 260, (40, 30, 80)); c.glow(330, 120, 40, (180, 180, 255), .7); c.oval(330, 120, 20, 20, (230, 230, 250))
    c.rect(316, 70, 322, 260, INK); c.rect(250, 162, 390, 168, INK)
    floor(c, 470, (30, 12, 26))
    figure(c, 170, 470, 330, dress=True, long_hair=True, lean=-4,
           arms=[("L", (130, 240), (162, 216)), ("R", (212, 240), (176, 222))], rim=(255, 110, 140))
    c.rect(220, 380, 420, 400, (70, 35, 30)); c.rect(235, 400, 250, 520, (50, 25, 22)); c.rect(390, 400, 405, 520, (50, 25, 22))
    for px, py, a in ((300, 378, -8), (340, 376, 6)):
        c.poly([(px - 18, py), (px + 18, py + a / 4), (px + 16, py - 4), (px - 20, py - 4)], (220, 215, 200))
    wine_glass(c, 260, 380); wine_glass(c, 372, 380)
    candle(c, 400, 380, 30)
    c.save("ad01.png")

def ad02():  # 가면무도회 밤, 뒷골목 여관으로 손을 이끄는 가면 쓴 남자
    c = Canvas((12, 8, 34), (50, 14, 44))
    c.poly([(0, 0), (120, 0), (140, 600), (0, 600)], (24, 12, 30)); c.poly([(300, 0), (420, 0), (420, 600), (280, 600)], (24, 12, 30))
    c.rect(190, 250, 250, 420, (255, 170, 90)); c.glow(220, 330, 70, (255, 140, 60), .8)
    c.rect(180, 236, 262, 250, (40, 20, 30))
    for i in range(7): lantern(c, 40 + i * 57, 70 + 18 * math.sin(i * 1.3), [(255, 90, 90), (255, 170, 60), (220, 90, 200)][i % 3])
    c.line([(0, 60), (420, 80)], 1.5, (60, 40, 50))
    floor(c, 470, (22, 10, 22))
    figure(c, 260, 480, 300, lean=-6, mask=(235, 225, 210), rim=(220, 120, 255),
           arms=[("L", (200, 360), (140, 420)), ("R", (290, 330), (300, 270))])
    c.line([(0, 600), (60, 520), (138, 424)], 22, (35, 20, 40))  # 이끌려 가는 쪽(보는 사람)의 손
    c.save("ad02.png")

def ad03():  # 협상 탁자 위에 걸터앉은 경쟁 교단 지도자
    c = Canvas((30, 8, 30), (70, 14, 30))
    for x in (60, 360): c.rect(x - 22, 40, x + 22, 470, (45, 15, 35))
    c.poly([(150, 60), (270, 60), (250, 200), (170, 200)], (90, 20, 40)); c.oval(210, 120, 22, 22, (200, 160, 60))
    floor(c, 470, (34, 10, 22))
    c.rect(40, 400, 380, 420, (60, 25, 25)); c.rect(60, 420, 76, 540, (45, 18, 20)); c.rect(344, 420, 360, 540, (45, 18, 20))
    figure(c, 190, 405, 300, pose="sit", robe=True, long_hair=True, lean=6, rim=(255, 90, 110),
           arms=[("L", (150, 320), (150, 392)), ("R", (240, 290), (232, 254))])
    c.poly([(290, 398), (350, 394), (352, 390), (288, 392)], (220, 215, 200))
    candle(c, 80, 400, 28); candle(c, 330, 400, 24)
    c.save("ad03.png")

def ad04():  # 촛불 의식방, 바닥에 펼친 문헌, 포옹하는 연인
    c = Canvas((18, 6, 30), (56, 12, 40))
    floor(c, 430, (40, 14, 34))
    c.d.ellipse([P(60, 450), P(360, 560)], outline=(200, 80, 120), width=3 * S)
    c.d.ellipse([P(90, 465), P(330, 545)], outline=(150, 60, 110), width=2 * S)
    figure(c, 180, 480, 320, long_hair=True, dress=True, lean=10, rim=(255, 120, 150),
           arms=[("L", (150, 250), (222, 214)), ("R", (225, 250), (250, 230))])
    figure(c, 240, 480, 340, lean=-10, rim=(255, 150, 120),
           arms=[("L", (205, 260), (172, 232)), ("R", (280, 270), (260, 320))])
    c.poly([(150, 540), (210, 530), (210, 560), (150, 572)], (225, 210, 175)); c.poly([(210, 530), (270, 540), (270, 572), (210, 560)], (215, 200, 165))
    for i in range(8):
        a = i / 8 * 2 * math.pi
        candle(c, 210 + math.cos(a) * 170, 505 + math.sin(a) * 60, 22, size=.8)
    c.save("ad04.png")

def ad05():  # 녹음기를 끄고 넥타이를 푸는 기자
    c = Canvas((16, 12, 30), (40, 20, 40))
    c.rect(40, 80, 160, 300, (30, 24, 48)); c.rect(60, 100, 140, 280, (24, 30, 60))
    c.glow(340, 230, 60, (255, 180, 100), .8); c.poly([(310, 210), (370, 210), (355, 180), (325, 180)], (230, 180, 110)); c.line([(340, 210), (340, 400)], 4, INK)
    floor(c, 470, (28, 16, 26))
    c.rect(40, 350, 330, 440, (70, 25, 40)); c.rect(40, 300, 70, 440, (60, 20, 34)); c.rect(300, 300, 330, 440, (60, 20, 34)); c.rect(40, 280, 330, 360, (64, 22, 38))
    figure(c, 170, 440, 290, pose="sit", lean=-6, rim=(255, 170, 110),
           arms=[("L", (130, 300), (160, 248)), ("R", (220, 300), (176, 250))])
    c.line([(166, 240), (172, 300)], 6, (140, 20, 40))  # 느슨해진 넥타이
    c.rect(220, 480, 400, 494, (60, 30, 26)); c.rect(260, 466, 300, 480, (40, 40, 44)); c.oval(292, 473, 3, 3, (80, 30, 30))  # 꺼진 녹음기
    c.save("ad05.png")

def ad06():  # 향 피어오르는 수행방, 손을 내미는 부부
    c = Canvas((26, 10, 30), (60, 20, 30))
    for r, col in ((150, (90, 30, 50)), (115, (60, 20, 40)), (80, (100, 40, 50))):
        c.d.ellipse([P(210 - r, 220 - r), P(210 + r, 220 + r)], outline=col, width=4 * S)
    floor(c, 470, (38, 16, 24))
    c.mist([(120 + i * 30, 360 - i * 50, 30, 60) for i in range(6)], (210, 180, 200), 70, 14)
    figure(c, 150, 480, 310, long_hair=True, dress=True, lean=4, rim=(255, 140, 120),
           arms=[("L", (110, 330), (130, 380)), ("R", (190, 290), (210, 330))])
    figure(c, 270, 480, 330, lean=-4, rim=(255, 140, 120),
           arms=[("L", (230, 300), (214, 338)), ("R", (310, 330), (300, 380))])
    c.oval(212, 334, 9, 7, (60, 30, 40))  # 맞잡아 내미는 두 손
    for x in (60, 360):
        c.rect(x - 10, 450, x + 10, 470, (120, 80, 50)); c.mist([(x, 400 - k * 30, 6, 30) for k in range(4)], (220, 210, 220), 120, 5)
    c.save("ad06.png")

def ad07():  # 호텔 바, 카운터 위로 밀어 주는 방 열쇠
    c = Canvas((14, 8, 26), (46, 14, 30))
    for row in range(3):
        c.rect(30, 90 + row * 70, 390, 96 + row * 70, (60, 30, 30))
        for i in range(10):
            col = [(90, 40, 60), (60, 90, 70), (120, 80, 40)][(i + row) % 3]
            c.rect(42 + i * 35, 50 + row * 70, 56 + i * 35, 90 + row * 70, col)
    for x in (110, 310): c.line([(x, 0), (x, 60)], 2, INK); c.glow(x, 66, 40, (255, 190, 110), .9); c.oval(x, 64, 14, 8, (255, 210, 150))
    figure(c, 290, 560, 340, pose="sit", lean=-14, rim=(255, 180, 120),
           arms=[("L", (240, 420), (190, 398)), ("R", (330, 400), (300, 360))])
    c.rect(0, 400, W, 420, (80, 40, 30)); c.rect(0, 420, W, H, (40, 18, 18))
    c.glow(180, 398, 22, (255, 220, 140), .7)
    c.oval(176, 396, 7, 5, (230, 190, 90)); c.line([(182, 396), (206, 396)], 3, (230, 190, 90)); c.rect(196, 396, 201, 402, (230, 190, 90))
    wine_glass(c, 90, 400, (200, 140, 40)); wine_glass(c, 360, 400, (200, 140, 40))
    c.save("ad07.png")

def ad08():  # 산속 혼욕 온천, 김 너머 붓을 든 화가 (김으로 가려짐)
    c = Canvas((30, 26, 54), (70, 40, 70))
    c.poly([(0, 260), (90, 150), (180, 230), (280, 120), (420, 240), (420, 320), (0, 320)], (40, 34, 60))
    c.oval(330, 90, 18, 18, (240, 230, 220)); c.glow(330, 90, 40, (200, 200, 255), .5)
    c.rect(0, 320, W, H, (40, 50, 80))
    for x, y, rx, ry in ((40, 330, 70, 30), (380, 340, 80, 36), (210, 560, 260, 50)): c.oval(x, y, rx, ry, (28, 24, 34))
    figure(c, 200, 560, 320, long_hair=True, lean=6, rim=(255, 190, 170),
           arms=[("L", (150, 380), (170, 430)), ("R", (250, 360), (272, 318))])
    c.line([(272, 318), (290, 280)], 3, (160, 110, 70)); c.oval(291, 278, 3, 4, (180, 40, 60))  # 붓
    c.rect(0, 420, W, H, (36, 48, 86))
    c.mist([(x, 420 + (i % 3) * 30, 90, 40) for i, x in enumerate(range(-20, 460, 60))], (220, 220, 240), 170, 16)
    c.mist([(100, 300, 120, 80), (320, 340, 120, 90)], (220, 220, 240), 70, 24)
    c.rect(360, 250, 368, 420, (110, 80, 50)); c.poly([(330, 260), (400, 250), (404, 330), (334, 340)], (225, 215, 195))
    c.save("ad08.png")

# ── 행동 결과 (교단 지도자) ─────────────────────────────────
GOLD, BLUE = (255, 200, 110), (110, 130, 200)

def leader(c, x, g, h, ok, arms, pose="stand"):
    return figure(c, x, g, h, pose=pose, robe=True, hood=not ok, rim=GOLD if ok else BLUE, arms=arms)

def result_bg(ok): return Canvas((50, 24, 50), (90, 30, 40)) if ok else Canvas((14, 14, 30), (26, 22, 40))

def cult_pray(ok):
    c = result_bg(ok)
    floor(c, 470, (40, 20, 30) if ok else (20, 18, 28))
    c.rect(220, 360, 380, 470, (70, 30, 40) if ok else (34, 28, 40)); c.rect(210, 350, 390, 364, (100, 50, 50) if ok else (40, 34, 50))
    c.line([(300, 260), (300, 350)], 8, (200, 160, 70) if ok else (70, 70, 90)); c.line([(276, 288), (324, 288)], 8, (200, 160, 70) if ok else (70, 70, 90))
    for x in (236, 364): candle(c, x, 350, 26, lit=ok)
    if ok: rays(c, 300, 0, 9, 480, (255, 220, 150), .55, .6)
    leader(c, 150, 480, 280, ok, [("L", (120, 330), (190, 300)), ("R", (180, 330), (196, 300))] if ok
           else [("L", (130, 360), (140, 420)), ("R", (170, 360), (180, 420))], pose="kneel")
    if not ok: c.mist([(236, 300, 6, 30), (364, 300, 6, 30)], (150, 150, 170), 80, 6)
    c.save(f"CultPray_{'ok' if ok else 'fail'}.png")

def preach(ok):
    c = result_bg(ok)
    c.rect(150, 250, 270, 420, (80, 40, 40) if ok else (34, 30, 44))
    if ok: c.glow(210, 160, 110, (255, 190, 110), .6)
    leader(c, 210, 300, 220, ok, [("L", (170, 150), (150, 100)), ("R", (250, 200), (262, 240))] if ok
           else [("L", (180, 210), (184, 250)), ("R", (240, 210), (236, 250))])
    crowd(c, 640, 9, raised=.7 if ok else 0, rim=(200, 140, 90) if ok else (60, 60, 90), seed=3)
    c.save(f"Preach_{'ok' if ok else 'fail'}.png")

def proselytize(ok):
    c = result_bg(ok)
    floor(c, 430, (40, 22, 34) if ok else (22, 20, 30))
    if ok:
        c.glow(210, 150, 80, (255, 230, 160), 1.0); c.oval(210, 150, 26, 26, (255, 245, 210)); rays(c, 210, 150, 14, 220, (255, 220, 140), .5, 2 * math.pi)
    else:
        for i in range(6): c.oval(190 + i * 9, 170 + (i % 2) * 14, 2, 2, (150, 150, 170))
        c.mist([(210, 160, 30, 20)], (120, 120, 140), 90, 8)
    leader(c, 210, 450, 280, ok, [("L", (160, 220), (186, 180)), ("R", (260, 220), (234, 180))] if ok
           else [("L", (170, 260), (180, 300)), ("R", (250, 260), (240, 300))])
    crowd(c, 660, 8, raised=.5 if ok else 0, rim=(200, 150, 100) if ok else (60, 60, 90), seed=5)
    c.save(f"Proselytize_{'ok' if ok else 'fail'}.png")

def serve(ok):
    c = result_bg(ok)
    floor(c, 470, (40, 24, 30) if ok else (20, 18, 28))
    leader(c, 150, 480, 300, ok, [("L", (180, 340), (230, 360)), ("R", (200, 330), (240, 350))] if ok
           else [("L", (130, 330), (140, 380)), ("R", (180, 330), (170, 380))])
    if ok:
        c.glow(240, 360, 50, (255, 230, 160), .9)
        figure(c, 270, 480, 240, pose="kneel", rim=(255, 210, 150), color=(40, 30, 40))
    else:
        figure(c, 330, 480, 260, lean=6, rim=(80, 80, 110), color=(30, 26, 40))  # 등 돌린 사람
        c.oval(250, 466, 14, 6, (60, 50, 50))  # 엎어진 그릇
    c.save(f"Serve_{'ok' if ok else 'fail'}.png")

def desire(ok):
    c = result_bg(ok) if not ok else Canvas((60, 10, 30), (100, 20, 40))
    c.poly([(40, 0), (120, 0), (90, 360), (20, 380)], (110, 20, 50) if ok else (40, 26, 44)); c.poly([(300, 0), (380, 0), (400, 380), (330, 360)], (110, 20, 50) if ok else (40, 26, 44))
    floor(c, 470, (50, 14, 30) if ok else (22, 18, 28))
    if ok:
        figure(c, 110, 470, 280, long_hair=True, dress=True, lean=10, rim=(255, 120, 150), arms=[("L", (90, 320), (110, 380)), ("R", (150, 280), (176, 250))])
        figure(c, 310, 470, 290, lean=-10, rim=(255, 120, 150), arms=[("L", (270, 280), (246, 250)), ("R", (330, 320), (320, 380))])
    leader(c, 210, 430, 280, ok, [("L", (170, 220), (180, 180)), ("R", (250, 220), (240, 180))] if ok
           else [("L", (180, 300), (200, 360)), ("R", (240, 300), (220, 360))], pose="sit" if not ok else "stand")
    c.rect(60, 420, 360, 440, (90, 40, 30) if ok else (40, 30, 34))
    if ok:
        for x, col in ((110, (200, 60, 60)), (150, (240, 180, 60)), (270, (120, 40, 140)), (310, (200, 60, 60))): c.oval(x, 410, 12, 10, col)
        wine_glass(c, 200, 420); wine_glass(c, 230, 420)
        c.glow(210, 380, 120, (255, 90, 120), .4)
    else:
        c.poly([(170, 418), (210, 410), (212, 418), (172, 424)], (150, 150, 160)); c.oval(220, 432, 26, 4, (90, 20, 40))  # 넘어진 잔, 쏟아진 술
    c.save(f"Desire_{'ok' if ok else 'fail'}.png")

def consecrate(ok):
    c = result_bg(ok)
    floor(c, 470, (40, 24, 30) if ok else (22, 20, 30))
    col = (210, 180, 140) if ok else (80, 76, 90)
    c.poly([(220, 140), (380, 230), (60, 230)], col); c.rect(90, 230, 350, 250, col)
    for x in (110, 170, 270, 330): c.rect(x - 10, 250, x + 10, 440, col)
    c.rect(80, 440, 360, 460, col)
    if ok:
        rays(c, 220, 180, 12, 300, (255, 220, 150), .55, 2 * math.pi); c.glow(220, 330, 70, (255, 200, 120), .7)
    else:
        c.line([(270, 250), (260, 320), (276, 380), (266, 440)], 3, INK); c.poly([(380, 230), (330, 226), (350, 250)], (22, 20, 30))
        c.mist([(300, 470, 80, 20)], (130, 126, 140), 110, 10)
    leader(c, 220, 520, 230, ok, [("L", (180, 380), (166, 330)), ("R", (260, 380), (274, 330))] if ok
           else [("L", (190, 420), (196, 460)), ("R", (250, 420), (244, 460))])
    c.save(f"Consecrate_{'ok' if ok else 'fail'}.png")

if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for f in (ad01, ad02, ad03, ad04, ad05, ad06, ad07, ad08): f()
    for f in (cult_pray, preach, proselytize, serve, desire, consecrate):
        f(True); f(False)
