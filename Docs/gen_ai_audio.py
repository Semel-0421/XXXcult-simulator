# Docs/audio_prompts.csv의 프롬프트로 로컬 ComfyUI에서 배경음악(ACE-Step 1.5 turbo)·효과음(Stable Audio Open 1.0)을 뽑아
# Assets/Resources/Audio/Music|Sfx/{키}.ogg로 저장한다. 분위기 기획은 Docs/audio_design.md.
# 실행 (numpy·soundfile이 있는 ComfyUI 가상환경 파이썬으로):
#   D:\AI\ComfyUI\.venv\Scripts\python.exe Docs/gen_ai_audio.py [키 ...] [--seed N] [--out 폴더] [--codes]
# 음악은 끝 4초를 처음 4초에 겹쳐(크로스페이드) 끊김 없는 루프로 만들고, 곡마다 음량(RMS)을 맞춰 전환이 튀지 않게 한다.
import csv, io, os, re, sys, time, zlib
import numpy as np
import soundfile as sf
from comfy_client import each_with_retry, run, take

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.join(HERE, "..")
CHECKPOINT, T5 = "stable-audio-open-1.0.safetensors", "t5-base.safetensors"
NEGATIVE = "low quality, distorted, clipping, noise, vocals with lyrics, speech, electronic drums, pop beat"
LOOP_SECONDS = 4.0          # 루프 이음새 크로스페이드 길이
MUSIC_RMS_DB = -20.0        # 곡끼리 같은 크기로 들리게 맞추는 기준 음량
SFX_PEAK_DB = -3.0
SILENCE_DB = -45.0


def sfx_workflow(prompt, seconds, seed):
    return {
        "1": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": CHECKPOINT}},
        "2": {"class_type": "CLIPLoader", "inputs": {"clip_name": T5, "type": "stable_audio"}},
        "3": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["2", 0], "text": prompt}},
        "4": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["2", 0], "text": NEGATIVE}},
        "5": {"class_type": "ConditioningStableAudio", "inputs": {"positive": ["3", 0], "negative": ["4", 0], "seconds_start": 0.0, "seconds_total": seconds}},
        "6": {"class_type": "EmptyLatentAudio", "inputs": {"seconds": seconds, "batch_size": 1}},
        "7": {"class_type": "KSampler", "inputs": {"model": ["1", 0], "positive": ["5", 0], "negative": ["5", 1], "latent_image": ["6", 0],
                                                    "seed": seed, "steps": 100, "cfg": 7.0, "sampler_name": "dpmpp_3m_sde_gpu", "scheduler": "exponential", "denoise": 1.0}},
        "8": {"class_type": "VAEDecodeAudio", "inputs": {"samples": ["7", 0], "vae": ["1", 2]}},
        "9": {"class_type": "SaveAudio", "inputs": {"audio": ["8", 0], "filename_prefix": "gameaudio"}},
    }


# ACE-Step의 언어 모델(악보 코드 생성)은 GTX 1070에선 fp32로 CPU에서 돌아 150초 곡에 30분 가까이 걸린다.
# 끄면 장당 몇 분이면 되지만 곡 구성이 조금 단순해질 수 있다. 품질을 올리려면 --codes로 켠다.
AUDIO_CODES = False


def music_workflow(prompt, seconds, seed):
    # 곡은 2분 30초짜리 기악곡. 프롬프트의 "NN BPM"과 "in D minor" 같은 조를 그대로 넘긴다
    bpm = int(re.search(r"(\d+) BPM", prompt).group(1)) if re.search(r"(\d+) BPM", prompt) else 70
    key = re.search(r"in ([A-G][#b]? (?:major|minor))", prompt)
    return {
        "1": {"class_type": "UNETLoader", "inputs": {"unet_name": "acestep_v1.5_turbo.safetensors", "weight_dtype": "default"}},
        "2": {"class_type": "DualCLIPLoader", "inputs": {"clip_name1": "qwen_0.6b_ace15.safetensors", "clip_name2": "qwen_1.7b_ace15.safetensors", "type": "ace", "device": "default"}},
        "3": {"class_type": "VAELoader", "inputs": {"vae_name": "ace_1.5_vae.safetensors"}},
        "4": {"class_type": "TextEncodeAceStepAudio1.5", "inputs": {"clip": ["2", 0], "tags": prompt, "lyrics": "[Instrumental]", "seed": seed, "bpm": bpm,
              "duration": seconds, "timesignature": "4", "language": "en", "keyscale": key.group(1) if key else "D minor",
              "generate_audio_codes": AUDIO_CODES, "cfg_scale": 2.0, "temperature": 0.85, "top_p": 0.9, "top_k": 0, "min_p": 0.0}},
        "5": {"class_type": "ConditioningZeroOut", "inputs": {"conditioning": ["4", 0]}},
        "6": {"class_type": "EmptyAceStep1.5LatentAudio", "inputs": {"seconds": seconds, "batch_size": 1}},
        "7": {"class_type": "ModelSamplingAuraFlow", "inputs": {"model": ["1", 0], "shift": 3.0}},
        "8": {"class_type": "KSampler", "inputs": {"model": ["7", 0], "positive": ["4", 0], "negative": ["5", 0], "latent_image": ["6", 0],
              "seed": seed, "steps": 8, "cfg": 1.0, "sampler_name": "euler", "scheduler": "simple", "denoise": 1.0}},
        "9": {"class_type": "VAEDecodeAudio", "inputs": {"samples": ["8", 0], "vae": ["3", 0]}},
        "10": {"class_type": "SaveAudio", "inputs": {"audio": ["9", 0], "filename_prefix": "gamemusic"}},
    }

def db(x): return 10 ** (x / 20)


def make_loop(audio, rate):
    # 끝부분을 앞부분에 등전력 크로스페이드로 겹친다: 마지막 샘플 다음이 자연스럽게 첫 샘플로 이어진다
    x = int(LOOP_SECONDS * rate)
    t = np.linspace(0, np.pi / 2, x)[:, None]
    out = audio[:-x].copy()
    out[:x] = audio[-x:] * np.cos(t) + audio[:x] * np.sin(t)
    return out


def finish_music(audio, rate):
    audio = make_loop(audio, rate)
    rms = np.sqrt(np.mean(audio ** 2)) or 1e-9
    audio *= db(MUSIC_RMS_DB) / rms
    peak = np.max(np.abs(audio))
    return audio * (0.98 / peak) if peak > 0.98 else audio


def finish_sfx(audio, rate, seconds):
    level = np.max(np.abs(audio), axis=1)
    loud = np.where(level > db(SILENCE_DB))[0]
    if len(loud): audio = audio[loud[0]:loud[-1] + 1]
    audio = audio[:int(seconds * rate)]
    fade = min(len(audio), int(0.05 * rate))
    audio[-fade:] *= np.linspace(1, 0, fade)[:, None]
    peak = np.max(np.abs(audio)) or 1e-9
    return audio * (db(SFX_PEAK_DB) / peak)


def write_ogg(path, audio, rate):
    # libsndfile의 OGG 인코더는 한 번에 큰 배열을 쓰면 윈도우에서 스택 오버플로로 죽어서 나눠 쓴다
    with sf.SoundFile(path, "w", rate, audio.shape[1], format="OGG", subtype="VORBIS") as f:
        for i in range(0, len(audio), 4096):
            f.write(audio[i:i + 4096])


def generate(row, seed, out_dir):
    seconds = float(row["seconds"])
    start = time.time()
    if row["type"] == "music":
        data = run(music_workflow(row["prompt"], seconds, seed), "10", "audio")
    else:  # 효과음 모델은 짧은 길이를 잘 못 다뤄 최소 3초로 뽑고 잘라 쓴다
        data = run(sfx_workflow(row["prompt"], max(seconds, 3.0), seed), "9", "audio")
    audio, rate = sf.read(io.BytesIO(data), dtype="float32", always_2d=True)
    audio = finish_music(audio, rate) if row["type"] == "music" else finish_sfx(audio, rate, seconds)
    folder = os.path.join(out_dir, "Music" if row["type"] == "music" else "Sfx")
    os.makedirs(folder, exist_ok=True)
    write_ogg(os.path.join(folder, row["key"] + ".ogg"), audio, rate)
    print(f"{row['key']} 저장 ({len(audio) / rate:.1f}초, {time.time() - start:.0f}초 걸림, seed {seed})", flush=True)


def main():
    args = sys.argv[1:]
    global AUDIO_CODES
    if "--codes" in args: args.remove("--codes"); AUDIO_CODES = True
    seed = take(args, "--seed")
    out_dir = os.path.abspath(take(args, "--out") or os.path.join(ROOT, "Assets", "Resources", "Audio"))
    with open(os.path.join(HERE, "audio_prompts.csv"), encoding="utf-8-sig") as f:
        rows = [r for r in csv.DictReader(f) if not args or r["key"] in args]
    if not rows: sys.exit("맞는 항목이 없음")
    each_with_retry(rows, lambda r: r["key"], lambda r: generate(r, int(seed) if seed else zlib.crc32(r["key"].encode()), out_dir))


if __name__ == "__main__":
    main()
