# 성별 분리 이벤트의 반대 성별 그림을 만든다 (ad01_f, ad03_f, ad08_f: 남성 NPC / ad02_m, ad05_m, ad07_m: 여성 NPC)
# 실행: python Docs/gen_split_art.py  →  Assets/Resources/AdultArt에 저장. 기본 그림은 건드리지 않는다.
import csv, os, sys, zlib
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import comfy_client as c
import gen_ai_art as g

JOBS = [
    ("ad01_f.png", "noobai", "nsfw, solo, 1boy, pov, mature male, 40s, businessman, short black hair, unbuttoning shirt, open collar, loosened tie, standing beside office desk, two wine glasses on desk, papers, late night office, city lights through window, seductive smile, looking at viewer, dark gothic, candlelight, crimson and purple palette"),
    ("ad02_m.png", "animagine", "solo, 1girl, pov, mature female, 30s, black masquerade mask, black gown, bare shoulders, leaning close to whisper, hand raised to viewer ear, village festival masquerade ball, dancing crowd blurred in background, lanterns, night, smirk, looking at viewer, dark gothic, candlelight, crimson and purple palette"),
    ("ad03_f.png", "noobai", "nsfw, solo, 1boy, pov, mature male, 30s, rival cult leader, ceremonial robe slipping off shoulders, bare chest, sitting on lap, straddling viewer, hand on viewer chest, seductive smirk, negotiation table with contract papers behind, dark chamber, looking at viewer, dark gothic, candlelight, crimson and purple palette"),
    ("ad05_m.png", "noobai", "solo, 1girl, pov, mature female, 30s, journalist, loosening scarf, unbuttoned blouse, sitting on leather sofa, leaning forward, voice recorder switched off on table, notebook, dim office, desk lamp, night, smoldering gaze, looking at viewer, dark gothic, candlelight, crimson and purple palette"),
    ("ad07_m.png", "animagine", "solo, 1girl, pov, mature female, 30s, elegant woman, evening dress, loosened necklace, sitting at hotel bar counter, sliding room key across counter toward viewer, hand on key, glass of wine, nostalgic longing expression, bottle shelves, pendant lights, night, looking at viewer, dark gothic, candlelight, crimson and purple palette"),
    ("ad08_f.png", "animagine", "nsfw, solo, 1boy, pov, mature male, 40s, painter, nude, wet skin, wet hair, mixed bath onsen, partially submerged, approaching through steam, holding paintbrush, mountain retreat, night, moonlight, lanterns, inviting smile, looking at viewer, dark gothic, purple palette"),
]

# GPU 오류가 나면 서버를 다시 띄우고 해상도·스텝을 줄여 처리량을 낮춰 다시 시도한다 (저장은 어차피 416x608로 줄인다)
LADDER = [(832, 1216, 28), (704, 1024, 24), (576, 832, 20), (512, 768, 16)]

def load_jobs():
    # Docs/split_art_jobs.csv(order 순, 모델별로 묶여 있음)가 있으면 그 목록을 쓴다
    p = os.path.join(os.path.dirname(os.path.abspath(__file__)), "split_art_jobs.csv")
    if not os.path.exists(p): return JOBS
    rows = sorted(csv.DictReader(open(p, encoding="utf-8-sig")), key=lambda r: int(r["order"]))
    return [(r["file"], r["model"], r["prompt"], r.get("negative") or "") for r in rows]

def main():
    only = sys.argv[1:]
    out = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "AdultArt"))
    for job in load_jobs():
        name, model, prompt, negative = (job + ("",))[:4]
        if os.path.exists(os.path.join(out, name)): continue  # 이미 만든 것은 건너뛴다 (중단 후 재개)
        if only and name not in only: continue
        g.CHECKPOINT, g.QUALITY = g.MODELS[model]
        g.OUT = out
        for w, h, steps in LADDER:
            g.WIDTH, g.HEIGHT = w, h
            wf = g.workflow
            def stepped(p, s, n="", wf=wf, steps=steps):
                base = wf(p, s, n)
                return {**base, "5": {**base["5"], "inputs": {**base["5"]["inputs"], "steps": steps}}}
            g.workflow = stepped
            try:
                c.ensure_server()
                g.generate(name, prompt, zlib.crc32(name.encode()), negative)
                print(f"OK {name} {w}x{h} steps={steps}", flush=True)
                break
            except Exception as e:
                print(f"FAIL {name} {w}x{h} steps={steps}: {type(e).__name__}: {str(e)[:160]}", flush=True)
                c.stop_server()
            finally:
                g.workflow = wf
        else:
            print(f"GIVEUP {name}", flush=True)

if __name__ == "__main__":
    main()
