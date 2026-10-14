using NUnit.Framework;

public class CultActionTests
{
    [Test]
    public void Preach_RaisesFollowerFaithAndDoctrine()
    {
        var cult = new CultStatus();
        var personal = new PersonalStatus();

        CultActions.Apply(cult, personal, CultAction.Preach);

        Assert.AreEqual(20, cult[StatId.FollowerFaith].Value);
        Assert.AreEqual(10, cult[StatId.Doctrine].Value);
        Assert.AreEqual(1, personal[PersonalStatId.Mentality].Value);
    }

    [Test]
    public void Pray_RaisesCommunionWithinItsCap()
    {
        var cult = new CultStatus();
        var personal = new PersonalStatus();

        for (int i = 0; i < 15; i++) CultActions.Apply(cult, personal, CultAction.Pray);

        Assert.AreEqual(CultStatus.CommunionMax, cult[StatId.Communion].Value);
    }

    [Test]
    public void GiftedCultModifier_AppliesToCultStats()
    {
        var cult = new CultStatus();
        var personal = new PersonalStatus();
        GiftedGranter.Grant(personal.Talents, "no.1"); // 봉헌금 +2%

        CultActions.Apply(cult, personal, CultAction.Desire); // 봉헌금 +30 -> 30.6

        Assert.AreEqual(31, cult[StatId.Offerings].Value);
    }

    [Test]
    public void EveryAction_ChangesSomeCultStat()
    {
        foreach (CultAction action in System.Enum.GetValues(typeof(CultAction)))
            Assert.IsNotEmpty(CultActions.CultEffectsOf(action), action.ToString());
    }
}
