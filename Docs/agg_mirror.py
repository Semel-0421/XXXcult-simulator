import re,sys
t=open(sys.argv[1],encoding="utf-8").read(); agg={}
for b in t.split("[거울 ")[1:]:
    name=b.split("]")[0]; g=lambda p: float(re.search(p,b).group(1))
    e={k:float(v) for k,v in re.findall(r"    (\S+): ([\d.]+)%",b)}
    agg.setdefault(name,[]).append(dict(rep=g(r"명망 ([\d.]+)"),fol=g(r"신도 ([\d.]+)"),yr=g(r"교단 기간 ([\d.]+)"),fin=g(r"최종 스킬 엔딩 ([\d.]+)%"),e=e,blk=re.findall(r"\[학습 막힘\] (.+?): (\d+)판",b)))
D=("desire","lust","greed","appetite","ambition"); V=("virtue","temperance","charity","patience","integrity")
allr=[]
for name,rows in agg.items():
    n=len(rows); m=lambda k: sum(r[k] for r in rows)/n; tot={}; allr+=rows
    for r in rows:
        for k,v in r["e"].items(): tot[k]=tot.get(k,0)+v/n
    des=sum(v for k,v in tot.items() if any(x in k for x in D)); vir=sum(v for k,v in tot.items() if any(x in k for x in V))
    blk={}
    for r in rows:
        for k,c in r["blk"]: blk[k]=blk.get(k,0)+int(c)
    print(f"[{name}] 욕망:미덕 {des:.0f}:{vir:.0f} | 명망 {m('rep'):.0f} 신도 {m('fol'):.0f} 기간 {m('yr'):.1f}년 붕괴 {tot.get('inv_end_followers',0):.1f}% 최종스킬 {m('fin'):.1f}% | 막힘 {blk}")
n=len(allr); print("전체: 붕괴 %.1f%% 최종스킬 %.1f%% 신도 %.0f"%(sum(r['e'].get('inv_end_followers',0) for r in allr)/n,sum(r['fin'] for r in allr)/n,sum(r['fol'] for r in allr)/n))
