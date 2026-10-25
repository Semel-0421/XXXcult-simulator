using NUnit.Framework;

public class ResultWindowTests
{
    [Test]
    public void EffectsLine_SkipsZeros_SignsAndSlashes()
    {
        var line = ResultWindow.EffectsLine(new[] { ("신도", 3), ("헌금", 0), ("음욕", -2) }, new[] { "기프티드 획득: 매혹" });
        var plain = System.Text.RegularExpressions.Regex.Replace(line, "<.*?>", "");
        Assert.AreEqual("신도 +3  /  음욕 -2  /  기프티드 획득: 매혹", plain);
    }

    [Test]
    public void TransitionsLine_ShowsBeforeAfterWithArrows()
    {
        var line = ResultWindow.TransitionsLine(new[] { ("신도", 12, 15), ("헌금", 5, 5), ("음욕", 4, 2) }, new[] { "기프티드 획득: 매혹" });
        var plain = System.Text.RegularExpressions.Regex.Replace(line, "<.*?>", "");
        Assert.AreEqual("신도 12 → 15 ▲    음욕 4 → 2 ▼    기프티드 획득: 매혹", plain);
    }

    [Test]
    public void Body_PrefixesResultMark()
    {
        // 성공·실패는 글자 접두어 대신 ✓·× 표시로 구분한다 (SYS_UI_DESIGN 결정 8)
        var korean = Lang.IsKorean;
        try
        {
            Lang.Set(true);
            StringAssert.Contains("✓</color> 좋았다", ResultWindow.Body(true, "좋았다"));
            StringAssert.Contains("×</color> 망했다", ResultWindow.Body(false, "망했다"));
            StringAssert.DoesNotContain("(성공)", ResultWindow.Body(true, "좋았다"));
        }
        finally { Lang.Set(korean); }
    }

    [Test]
    public void AutoAdvance_PersistsInPrefs()
    {
        var was = ResultWindow.AutoAdvance;
        try
        {
            ResultWindow.AutoAdvance = true;
            Assert.IsTrue(ResultWindow.AutoAdvance);
            ResultWindow.AutoAdvance = false;
            Assert.IsFalse(ResultWindow.AutoAdvance);
        }
        finally { ResultWindow.AutoAdvance = was; }
    }
}
