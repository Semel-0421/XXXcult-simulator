using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 배경음악과 효과음을 맡는다. 게임 시작 시 스스로 만들어져 장면이 바뀌어도 남는다.
//  - 메인 화면: menu 음악 / 게임 중: 명망(사회적 시선) 단계에 맞는 음악, 단계가 바뀌면 크로스페이드
//  - 옵션·저장·업적·일시정지 같은 메뉴가 열려 있는 동안은 음악을 작게 낮춘다
//  - 파일이 없으면 조용히 건너뛴다 (파일 규칙은 MusicKeys 참고)
public class AudioManager : MonoBehaviour
{
    public const float MenuDuckFactor = 0.25f;  // 메뉴가 열렸을 때 음악 음량 배율
    const float FadeSeconds = 1.5f;             // 음악 전환·덕킹 시간
    const float SwitchDelay = 2f;               // 단계가 이 시간 넘게 유지돼야 곡을 바꾼다 (경계에서 오락가락 방지)
    const string GameScene = "GameMain";

    const string MusicPref = "volume_music", SfxPref = "volume_sfx";

    static AudioManager instance;
    static int menuDepth;
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

    // 슬라이더를 끌 때마다 쓰지 않도록, 옵션 창을 닫을 때 한 번 저장한다
    public static void SaveVolumes() => PlayerPrefs.Save();

    public static bool MenuOpen => menuDepth > 0;

    // 메뉴 화면이 열리고 닫힐 때 짝으로 부른다. 여러 메뉴가 겹쳐도 모두 닫혀야 원래 크기로 돌아온다.
    public static void MenuOpened() => menuDepth++;
    public static void MenuClosed() => menuDepth = Mathf.Max(0, menuDepth - 1);

    public static void PlaySfx(string key)
    {
        if (instance == null) return;
        var clip = Load($"Audio/Sfx/{key}");
        if (clip != null) instance.sfx.PlayOneShot(clip, SfxVolume);
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
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    void OnDestroy() { if (instance == this) instance = null; }

    // 장면이 바뀌면 열려 있던 메뉴는 모두 사라진 것이다
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => menuDepth = 0;

    void Update()
    {
        var dt = Time.unscaledDeltaTime;
        UpdateWantedTrack(dt);

        duck = Mathf.MoveTowards(duck, MenuOpen ? MenuDuckFactor : 1f, dt * (1f - MenuDuckFactor) / 0.5f);
        for (int i = 0; i < music.Length; i++)
        {
            var target = i == active ? 1f : 0f;
            gain[i] = Mathf.MoveTowards(gain[i], target, dt / FadeSeconds);
            music[i].volume = gain[i] * MusicVolume * duck;
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

    static string DesiredKey()
    {
        if (SceneManager.GetActiveScene().name != GameScene) return MusicKeys.Menu;
        return MusicKeys.ForStanding(SocialStanding.Of(GameSession.Cult));
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
