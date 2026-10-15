using System;
using NUnit.Framework;

// 화면 없이 Turns로 한 판을 끝까지 진행해 본다: 생애 → 교단 창설 → 교단 운영(해체되거나 40년이 지날 때까지).
// 실제 흐름(LifeFlow/CultFlow)과 같은 규칙 코드를 쓰므로, 진행 중 예외나 값 범위 이탈을 잡아낸다.
public class PlaythroughTests
{
    const int MaxCultTurns = 160;

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void FullRun_StatsStayInRange_AndEventsRun(int seed)
    {
        GameSession.StartNew(seed);
        var rng = GameSession.Rng;

        // 생애: 이벤트(답하면 1턴) → 행동(1턴)을 번갈아 18세 4분기까지
        while (GameSession.Age.Turns < LifeFlow.FoundingTurn)
        {
            AnswerRandomly(Turns.NextLifeEvent().Question, rng);
            GameSession.AdvanceTurn();
            if (GameSession.Age.Turns >= LifeFlow.FoundingTurn) break;
            Turns.Life((LifeAction)rng.Next(5));
        }

        GameSession.FoundCult("시험교단");
        GameSession.Cult.Doctrines.LevelUp(DoctrineSkill.Declaration);

        for (int turn = 0; turn < MaxCultTurns && !Turns.CultDissolved; turn++)
        {
            Turns.Cult((CultAction)rng.Next(5));
            if (Turns.CultDissolved) break;
            var next = Turns.NextCultEvent();
            if (next != null) AnswerRandomly(next.Question, rng);
        }

        Assert.Greater(GameSession.CountOf("life_acts"), 0);
        Assert.Greater(GameSession.CountOf("cult_acts"), 0);
        Assert.Greater(GameSession.CountOf("events"), 0);
        Assert.That(GameSession.Misfortune, Is.InRange(0, Misfortune.MaxStacks));
        foreach (StatId id in Enum.GetValues(typeof(StatId)))
            Assert.That(GameSession.Cult[id].Value, Is.InRange(GameSession.Cult[id].Min, GameSession.Cult[id].Max), id.ToString());
        foreach (PersonalStatId id in Enum.GetValues(typeof(PersonalStatId)))
            Assert.That(GameSession.Personal[id].Value, Is.InRange(GameSession.Personal[id].Min, GameSession.Personal[id].Max), id.ToString());
    }

    [Test]
    public void NegativeEvent_AddsMisfortune()
    {
        GameSession.StartNew(5);
        GameSession.FoundCult("시험교단");
        // 부정적 이벤트가 가능한 연차까지 시간을 보내고, 모든 스탯을 바닥 근처로 내린다
        for (int i = 0; i < CultEvent.NegativeFirstYear * GameClock.QuartersPerYear; i++) GameSession.AdvanceTurn();
        foreach (StatId id in Enum.GetValues(typeof(StatId))) GameSession.Cult[id].Set(GameSession.Cult[id].Min);

        for (int i = 0; i < 200 && GameSession.Misfortune == 0; i++)
        {
            var next = Turns.NextCultEvent();
            if (next != null && next.IsNegative) Assert.AreEqual(1, GameSession.Misfortune);
        }
        Assert.AreEqual(1, GameSession.Misfortune);
    }

    static void AnswerRandomly(Question question, Random rng) =>
        Turns.Answer(question.Choices[rng.Next(question.Choices.Length)]);
}
