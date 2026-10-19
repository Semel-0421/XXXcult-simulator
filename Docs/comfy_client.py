# 로컬 ComfyUI(D:\AI\ComfyUI) 서버를 띄우고 워크플로를 돌려 결과 파일을 받아 오는 공용 코드.
# gen_ai_art.py(삽화)와 gen_ai_audio.py(음악·효과음)가 함께 쓴다.
import json, os, subprocess, sys, time, urllib.request

COMFY_DIR = r"D:\AI\ComfyUI"
URL = "http://127.0.0.1:8188"
LOG = r"D:\AI\comfy.log"
# GTX 1070이 화면도 그리고 있어서, GPU 커널 하나가 2초를 넘기면 윈도우가 드라이버를 리셋(TDR)하고 ComfyUI가 죽는다.
# 어텐션을 쪼개 커널을 짧게 하고 동적 VRAM·비동기 오프로드(illegal memory access 원인)를 끈다.
# 파스칼 세대는 fp16 연산에서 드라이버가 리셋되므로 가중치는 fp8로 작게 두고 계산은 fp32로 한다.
SERVER_FLAGS = ["--disable-dynamic-vram", "--disable-pinned-memory", "--use-split-cross-attention", "--disable-async-offload", "--fp8_e4m3fn-unet", "--force-fp32"]
if os.environ.get("COMFY_CPU"):  # GPU 이상 시 CPU로만 생성 (느리지만 VRAM을 안 씀)
    SERVER_FLAGS = ["--cpu", "--disable-dynamic-vram", "--disable-pinned-memory", "--use-split-cross-attention"]


def api(path, payload=None):
    req = urllib.request.Request(URL + path, data=json.dumps(payload).encode() if payload is not None else None,
                                 headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=120) as r:  # CPU에서 언어 모델이 돌면 서버 응답이 수십 초 늦어진다
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
    print("ComfyUI 시작 중...", flush=True)
    subprocess.Popen([os.path.join(COMFY_DIR, ".venv", "Scripts", "python.exe"), "-u", "main.py", "--listen", "127.0.0.1", *SERVER_FLAGS],
                     cwd=COMFY_DIR, stdout=open(LOG, "w", encoding="utf-8"), stderr=subprocess.STDOUT)
    for _ in range(180):
        time.sleep(2)
        try:
            api("/system_stats"); return
        except OSError:
            pass
    sys.exit("ComfyUI가 6분 안에 뜨지 않았음")


def run(workflow, output_node, kind):
    """워크플로를 돌려 output_node가 낸 첫 결과 파일의 바이트를 돌려준다. kind: "images" 또는 "audio"."""
    pid = json.loads(api("/prompt", {"prompt": workflow}))["prompt_id"]
    while True:
        time.sleep(3)
        try:
            hist = json.loads(api(f"/history/{pid}"))
        except TimeoutError:
            continue  # 바쁜 서버의 늦은 응답은 기다린다 (연결이 끊긴 것만 실패로 본다)
        if pid not in hist: continue
        entry = hist[pid]
        if entry.get("status", {}).get("status_str") == "error":
            err = next((m[1].get("exception_message", "") for m in entry["status"].get("messages", []) if m[0] == "execution_error"), "")
            raise RuntimeError(err.strip().splitlines()[0] if err.strip() else "알 수 없는 오류")
        info = entry["outputs"][output_node][kind][0]
        return api(f"/view?filename={info['filename']}&subfolder={info['subfolder']}&type={info['type']}")


def each_with_retry(rows, name_of, make):
    """rows를 하나씩 make(row)로 만든다. 서버가 죽거나 CUDA 오류가 나면 서버를 다시 띄워 최대 3번 시도한다."""
    ensure_server()
    for row in rows:
        for attempt in range(3):
            try:
                make(row); break
            except (OSError, RuntimeError) as e:
                print(f"{name_of(row)}: 실패({e}), 서버 재시작 후 재시도 {attempt + 1}/3", flush=True)
                stop_server(); ensure_server()


def take(args, flag):
    """명령줄 인자에서 '--flag 값'을 꺼내고 지운다. 없으면 None."""
    if flag not in args: return None
    i = args.index(flag); value = args[i + 1]; del args[i:i + 2]
    return value
