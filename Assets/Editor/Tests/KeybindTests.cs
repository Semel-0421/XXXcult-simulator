using NUnit.Framework;
using UnityEngine.InputSystem;

public class KeybindTests
{
    [SetUp]
    public void SetUp()
    {
        Keybinds.ResetAll();
        // EditMode에선 frameCount가 안 바뀌어 앞 테스트의 Consume()이 남는다
        typeof(Keybinds).GetField("consumedFrame", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, -1);
    }

    [Test]
    public void Defaults_AreDigits1To5_AndLabelsMatch()
    {
        for (int i = 0; i < Keybinds.ActionCount; i++)
            Assert.AreEqual((i + 1).ToString(), Keybinds.Label(Keybinds.Get(i)));
        Assert.AreEqual("1", Keybinds.Label(Key.Numpad1));
    }

    [Test]
    public void Set_SwapsWhenKeyAlreadyUsed()
    {
        Keybinds.Set(0, Key.Q);
        Assert.AreEqual(Key.Q, Keybinds.Get(0));
        Keybinds.Set(3, Key.Q); // 행동 0이 쓰던 키를 행동 3이 가져가면 맞바꾼다
        Assert.AreEqual(Key.Q, Keybinds.Get(3));
        Assert.AreEqual(Key.Digit4, Keybinds.Get(0));
        Keybinds.ResetAll();
    }

    [Test]
    public void Listening_StopsForMenusRebindingAndConsumedPress()
    {
        Assert.IsTrue(Keybinds.Listening);
        Menus.Opened();
        Assert.IsFalse(Keybinds.Listening, "메뉴가 열려 있으면 단축키를 받지 않는다");
        Menus.Closed();
        Keybinds.BeginRebind(0);
        Assert.IsFalse(Keybinds.Listening, "키 재설정 중에는 받지 않는다");
        Keybinds.CancelRebind();
        Keybinds.Consume();
        Assert.IsFalse(Keybinds.Listening, "같은 프레임에 이미 쓴 키 입력은 다시 쓰지 않는다");
    }

    [Test]
    public void Rebinding_FlagFollowsBeginAndCancel()
    {
        Assert.IsFalse(Keybinds.Rebinding);
        Keybinds.BeginRebind(2);
        Assert.IsTrue(Keybinds.Rebinding);
        Keybinds.CancelRebind();
        Assert.IsFalse(Keybinds.Rebinding);
    }
}
