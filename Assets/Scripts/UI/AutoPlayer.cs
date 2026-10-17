#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

// 에디터 전용 실플레이 테스트 봇. 진짜 키 입력(숫자 1~5)을 넣어 행동·선택지·결과 창을 넘기며 판을 계속 돈다.
// 엔딩에 닿으면 기록하고 같은 모드로 새 판을 시작한다. 준비된 신 모드에서는 권능 포인트로 노드를 자동으로 익힌다.
// 시작: AutoPlayer.Begin(godMode, adult) (플레이 모드에서 eval로). 진행 기록은 콘솔에 "[AutoPlay]"로 남는다.
public class AutoPlayer : MonoBehaviour
{
    const float Interval = 0.35f;
    static AutoPlayer instance;
    bool godMode;
    int seed, runs, lastLoggedTurn = -1;
    float wait;
    Key? held;

    public static string Summary => instance == null ? "꺼짐" : $"runs={instance.runs} turn={GameSession.CultTime?.Turns} music={AudioManager.CurrentMusic}";

    public static void Begin(bool godMode, bool adult, int seed = 1)
    {
        PlayerPrefs.SetInt("adult_verified", adult ? 1 : 0);
        PlayerPrefs.SetInt("adult_content", 1);
        if (instance == null)
        {
            var go = new GameObject("AutoPlayer");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AutoPlayer>();
        }
        instance.godMode = godMode;
        instance.seed = seed;
        instance.runs = 0;
        instance.NewRun();
    }

    public static void Stop() { if (instance != null) Destroy(instance.gameObject); }

    void NewRun()
    {
        seed++;
        if (godMode) GodRules.Begin(seed);
        else
        {
            GameSession.StartNew(seed);
            GameSession.Age.Restore(25 * GameClock.QuartersPerYear);
        }
        GameSession.MarkStarted();
        GameSession.FoundCult(godMode ? "자동신전" : "자동교단");
        if (!godMode) GameSession.Cult.Doctrines.Restore(DoctrineSkill.Declaration.Id, DoctrineSkill.Declaration.MaxLevel);
        GameSession.Restore(GameSession.Capture()); // 불러오기와 같은 경로로 씬에 들어간다
        lastLoggedTurn = -1;
        SceneManager.LoadScene("GameMain");
        Debug.Log($"[AutoPlay] 새 판 #{runs + 1} {(godMode ? "준비된 신" : "일반")} seed={seed}");
    }

    void Update()
    {
        if (held is { } key) { Queue(null); held = null; return; } // 누른 다음 프레임에 뗀다
        if (SceneManager.GetActiveScene().name != "GameMain" || GameSession.Cult == null) return;

        if (GameSession.Ending != null)
        {
            runs++;
            Debug.Log($"[AutoPlay] 엔딩 #{runs}: {GameSession.Ending.Id} (교단 {GameSession.CultTime.Years}년, 명망 {GameSession.Cult[StatId.Reputation].Value}, 음악 {AudioManager.CurrentMusic})");
            NewRun();
            return;
        }

        var turn = GameSession.CultTime.Turns;
        if (turn != lastLoggedTurn && turn % 4 == 0)
        {
            lastLoggedTurn = turn;
            var c = GameSession.Cult;
            var pressure = godMode ? $" 신계 {GameSession.God.Pressure.Stage(Pressure.Heaven, c)} 지상 {GameSession.God.Pressure.Stage(Pressure.Earth, c)} 노드 {GameSession.God.Tree.Learned.Count()}" : "";
            Debug.Log($"[AutoPlay] {GameSession.CultTime.Years}년차 신도 {c[StatId.Followers].Value} 신앙 {c[StatId.FollowerFaith].Value} 헌금 {c[StatId.Offerings].Value} 명망 {c[StatId.Reputation].Value}{pressure} 음악 {AudioManager.CurrentMusic}");
            if (godMode) LearnGodSkills();
        }

        wait -= Time.unscaledDeltaTime;
        if (wait > 0) return;
        wait = Interval;
        // 결과 창은 1로 닫고, 그 밖에는 1~5 중 아무 숫자 (질문이면 선택지 수를 넘는 숫자는 무시된다)
        var n = ResultWindow.IsOpen ? 1 : Random.Range(1, 6);
        Queue((Key)((int)Key.Digit1 + n - 1));
    }

    void LearnGodSkills()
    {
        var god = GameSession.God;
        var tree = god.Tree;
        if (!tree.HasNature) tree.ChooseNature(GodSkillCatalog.All.First(s => s.IsOuter));
        foreach (var skill in GodSkillCatalog.All.OrderByDescending(s => s.Ring))
            if (tree.CanLearn(skill, god.Available)) tree.Learn(skill, god.Available);
    }

    void Queue(Key? key)
    {
        var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
        InputSystem.QueueStateEvent(keyboard, key is { } k ? new KeyboardState(k) : new KeyboardState());
        held = key;
    }

    void OnDestroy() { if (instance == this) instance = null; }
}
#endif
