using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 배경음악과 효과음을 맡는다. 게임 시작 시 스스로 만들어져 장면이 바뀌어도 남는다.
//  - 메인 화면: menu / 교단 창설 전: life / 교단: 명망 단계 곡 / 준비된 신: 신계·지상 압박 곡 / 엔딩: 결말 곡. 바뀌면 크로스페이드
//  - 교단 창설·압박 단계 상승·승천·사망 효과음도 여기서 낸다
//  - 메뉴가 열려 있는 동안(Menus)은 음악을 작게 낮춘다
//  - 파일이 없으면 조용히 건너뛴다 (파일 규칙은 MusicKeys 참고)
public class AudioManager : MonoBehaviour
{
    public const float MenuDuckFactor = 0.25f;  // 메뉴가 열렸을 때 음악 음량 배율
    const float FadeSeconds = 3f;               // 음악 크로스페이드 시간 (곡끼리 겹쳐 넘어가도록 길게)
    const float SwitchDelay = 2f;               // 단계가 이 시간 넘게 유지돼야 곡을 바꾼다 (경계에서 오락가락 방지)
    const string GameScene = "GameMain";

    const string MusicPref = "volume_music", SfxPref = "volume_sfx", MasterPref = "volume_master";
    const string MusicMutePref = "mute_music", SfxMutePref = "mute_sfx", DuckPref = "duck_on_menu";

    static AudioManager instance;
    static readonly Dictionary<string, AudioClip> Cache = new();

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicPref, 0.7f);
        set => PlayerPrefs.SetFloat(MusicPref, Mathf.Clamp01(value));
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxPref, 1f);
        set => PlayerPrefs.SetFloat(SfxPref, Mathf.Clamp01(value));
    }

    // 전체 음량: 배경음악·효과음 모두에 곱한다
    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(MasterPref, 1f);
        set => PlayerPrefs.SetFloat(MasterPref, Mathf.Clamp01(value));
    }

    public static bool MusicMuted { get => PlayerPrefs.GetInt(MusicMutePref, 0) == 1; set => PlayerPrefs.SetInt(MusicMutePref, value ? 1 : 0); }
    public static bool SfxMuted { get => PlayerPrefs.GetInt(SfxMutePref, 0) == 1; set => PlayerPrefs.SetInt(SfxMutePref, value ? 1 : 0); }
    // 메뉴가 열려 있을 때 배경음악을 작게 할지 (기본 켬)
    public static bool DuckOnMenu { get => PlayerPrefs.GetInt(DuckPref, 1) == 1; set => PlayerPrefs.SetInt(DuckPref, value ? 1 : 0); }

    // 실제로 내는 크기 (음량 × 전체 음량, 끈 쪽은 0)
    public static float EffectiveMusic => MusicMuted ? 0f : MusicVolume * MasterVolume;
    public static float EffectiveSfx => SfxMuted ? 0f : SfxVolume * MasterVolume;

    // 슬라이더를 끌 때마다 쓰지 않도록, 옵션 창을 닫을 때 한 번 저장한다
    public static void SaveVolumes() => PlayerPrefs.Save();

    public static string CurrentMusic => instance == null ? null : instance.currentKey; // 지금 고른 곡 (테스트 봇 기록용)

    public static void PlaySfx(string key)
    {
        if (instance == null) return;
        var clip = Load($"Audio/Sfx/{key}");
        if (clip != null && EffectiveSfx > 0f) instance.sfx.PlayOneShot(clip, EffectiveSfx);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        var go = new GameObject("AudioManager");
        DontDestroyOnLoad(go);
        go.AddComponent<AudioManager>();
    }

    AudioSource[] music;
    AudioSource sfx;
    readonly float[] gain = new float[2];
    int active = -1;
    string currentKey, wantedKey;
    float wantedSince;
    float duck = 1;

    void Awake()
    {
        instance = this;
        music = new AudioSource[2];
        for (int i = 0; i < music.Length; i++)
        {
            music[i] = gameObject.AddComponent<AudioSource>();
            music[i].loop = true;
            music[i].playOnAwake = false;
            music[i].volume = 0;
        }
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        GameSession.Founded += OnFounded;
    }

    static void OnFounded() => PlaySfx("cult_found");

    void OnDestroy()
    {
        GameSession.Founded -= OnFounded;
        if (instance == this) instance = null;
    }

    void Update()
    {
        var dt = Time.unscaledDeltaTime;
        UpdateWantedTrack(dt);
        WatchMoments();

        duck = Mathf.MoveTowards(duck, Menus.AnyOpen && DuckOnMenu ? MenuDuckFactor : 1f, dt * (1f - MenuDuckFactor) / 0.5f);
        for (int i = 0; i < music.Length; i++)
        {
            var target = i == active ? 1f : 0f;
            gain[i] = Mathf.MoveTowards(gain[i], target, dt / FadeSeconds);
            music[i].volume = gain[i] * EffectiveMusic * duck;
            if (gain[i] <= 0f && i != active && music[i].isPlaying) music[i].Stop();
        }
    }

    void UpdateWantedTrack(float dt)
    {
        var key = DesiredKey();
        if (key != wantedKey) { wantedKey = key; wantedSince = 0; }
        else wantedSince += dt;

        if (key == currentKey) return;
        // 처음 곡이거나 충분히 유지됐을 때만 바꾼다
        if (currentKey != null && wantedSince < SwitchDelay) return;
        SwitchTo(key);
    }

    static bool InGame => SceneManager.GetActiveScene().name == GameScene && GameSession.Cult != null;

    static string DesiredKey()
    {
        if (!InGame) return MusicKeys.Menu;
        if (GameSession.Ending != null) return MusicKeys.ForEnding(GameSession.Ending);
        if (GameSession.GodMode)
        {
            var (heaven, earth) = GodStages();
            return MusicKeys.ForGod(heaven, earth);
        }
        if (!GameSession.CultFounded) return MusicKeys.Life;
        return MusicKeys.ForStanding(SocialStanding.Of(GameSession.Cult));
    }

    static (int heaven, int earth) GodStages()
    {
        var pressure = GameSession.God.Pressure;
        return (pressure.Stage(Pressure.Heaven, GameSession.Cult), pressure.Stage(Pressure.Earth, GameSession.Cult));
    }

    // 상태가 바뀌는 순간의 효과음: 압박 단계 상승, 승천·사망 엔딩
    int lastHeaven = -1, lastEarth = -1;
    Endings.Ending lastEnding;

    void WatchMoments()
    {
        if (!InGame) { lastHeaven = lastEarth = -1; lastEnding = null; return; }
        var ending = GameSession.Ending;
        if (ending != null && ending != lastEnding)
        {
            if (GodRules.IsAscension(ending)) PlaySfx("ascend");
            else if (Progress.IsDeath(ending)) PlaySfx("death");
        }
        lastEnding = ending;
        if (!GameSession.GodMode) return;
        var (heaven, earth) = GodStages();
        if (lastHeaven >= 0 && heaven > lastHeaven) PlaySfx("pressure_heaven"); // 불러온 직후(-1)에는 울리지 않는다
        if (lastEarth >= 0 && earth > lastEarth) PlaySfx("pressure_earth");
        lastHeaven = heaven;
        lastEarth = earth;
    }

    void SwitchTo(string key)
    {
        currentKey = key;
        AudioClip clip = null;
        foreach (var candidate in MusicKeys.Candidates(key))
        {
            clip = Load($"Audio/Music/{candidate}");
            if (clip != null) break;
        }

        if (clip == null) { active = -1; return; } // 파일이 없으면 지금 곡을 서서히 끈다

        var next = active == 0 ? 1 : 0;
        music[next].clip = clip;
        music[next].volume = 0;
        gain[next] = 0;
        music[next].Play();
        active = next;
    }

    static AudioClip Load(string path)
    {
        if (Cache.TryGetValue(path, out var cached)) return cached;
        var clip = Resources.Load<AudioClip>(path);
        Cache[path] = clip; // 없음(null)도 기억해 매번 찾지 않는다
        return clip;
    }
}
