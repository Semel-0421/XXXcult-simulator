using UnityEngine;
using UnityEngine.SceneManagement;

// 옵션·저장·업적·교리·일시정지 같은 메뉴가 열려 있는지 센다. 열고 닫을 때 짝으로 부른다.
// 메뉴가 열려 있는 동안 음악을 낮추고(AudioManager) 진행 단축키를 막는다(ActionBar·QuestionPanel).
public static class Menus
{
    static int depth;

    public static bool AnyOpen => depth > 0;

    // 여러 메뉴가 겹쳐도 모두 닫혀야 열린 상태가 풀린다
    public static void Opened() { depth++; AudioManager.PlaySfx("menu_open"); }
    public static void Closed() => depth = Mathf.Max(0, depth - 1);

    // 메뉴가 열린 채로 장면이 바뀌면(타이틀로 나가기·불러오기) 창이 사라져 짝이 맞지 않으므로 새 장면에서 다시 센다
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Subscribe() => SceneManager.sceneLoaded += (_, __) => depth = 0;
}
