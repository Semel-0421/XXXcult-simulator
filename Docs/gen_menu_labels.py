# 타이틀 메뉴 버튼 글자를 투명 PNG로 굽는다 (TMP 취소선이 고딕 폰트에 없는 '_'를 찾아 경고를 내서 이미지로 바꿨다)
# 사용: python Docs/gen_menu_labels.py  (프로젝트 루트에서). 출력: Assets/Resources/UI/MenuLabels/{key}_{ko|en}[_locked|_lit].png
#   normal: 기본 색 / locked: 회색 + 취소선 / lit: 금색 + 취소선 (잠김 진행도만큼 locked 위에 왼쪽부터 채워 보인다)
#   mirror_mode는 모든 변형을 좌우 반전한다. eye_mode는 잠김(locked·lit)만 반전하고, 열린 글자는 정방향 금색이다.
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FONTS = os.path.join(ROOT, 'Assets', 'TextMesh Pro', 'Fonts')
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'UI', 'MenuLabels')
LATIN = os.path.join(FONTS, 'Fust&Schoeffer-Durandus-GoticoAntiqua118G.otf')
HANGUL = os.path.join(FONTS, 'MapoFlowerIsland.ttf')  # 고딕 폰트의 TMP 폴백과 같다
SIZE = 72  # 화면 36pt의 2배로 굽는다

LABELS = {  # Lang.cs와 같은 글
    'new_game': ('New Game', '새 게임'), 'continue': ('Continue', '이어하기'), 'achievements': ('Achievements', '업적'),
    'settings': ('Settings', '설정'), 'quit': ('Quit', '종료'),
    'god_mode': ('Prepared God', '준비된 신'), 'mirror_mode': ('Mirror World', '거울 세계'),
    'eye_mode': ('The Golden Eye', '황금의 눈동자'),
}
LOCKABLE = {'god_mode', 'mirror_mode', 'eye_mode'}
NORMAL, GRAY, GOLD = (219, 204, 235, 255), (128, 128, 128, 255), (242, 217, 120, 255)


def render(text, font, color, strike):
    asc, desc = font.getmetrics()
    pad = SIZE // 6
    width = int(font.getlength(text)) + pad * 2
    img = Image.new('RGBA', (width, asc + desc + pad), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    draw.text((pad, pad // 2), text, font=font, fill=color)
    if strike:
        y = pad // 2 + asc - int(asc * 0.38)
        draw.line((pad // 2, y, width - pad // 2, y), fill=color, width=max(3, SIZE // 16))
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    fonts = {'en': ImageFont.truetype(LATIN, SIZE), 'ko': ImageFont.truetype(HANGUL, SIZE)}
    count = 0
    for key, (en, ko) in LABELS.items():
        for lang, text in (('en', en), ('ko', ko)):
            variants = [('', GOLD if key == 'eye_mode' else NORMAL, False)]
            if key in LOCKABLE: variants += [('_locked', GRAY, True), ('_lit', GOLD, True)]
            for suffix, color, strike in variants:
                img = render(text, fonts[lang], color, strike)
                if key == 'mirror_mode' or (key == 'eye_mode' and suffix): img = img.transpose(Image.FLIP_LEFT_RIGHT)
                img.save(os.path.join(OUT, f'{key}_{lang}{suffix}.png'))
                count += 1
    assert count == 5 * 2 + 3 * 2 * 3, count
    print(f'{count} labels -> {OUT}')


if __name__ == '__main__':
    main()
