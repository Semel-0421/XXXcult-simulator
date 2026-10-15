using NUnit.Framework;

public class DoctrineLevelTests
{
    [Test]
    public void Level_FollowsCommonLogCurve_FromZeroToMax()
    {
        Assert.AreEqual(0, DoctrineLevel.Of(0));
        Assert.AreEqual(DoctrineLevel.MaxLevel, DoctrineLevel.Of(ClampedStat.DefaultMax));
        int previousGap = 0;
        for (int level = 1; level <= DoctrineLevel.MaxLevel; level++)
        {
            var at = DoctrineLevel.Threshold(level);
            Assert.AreEqual(level, DoctrineLevel.Of(at), $"{level}레벨 시작점 {at}");
            Assert.AreEqual(level - 1, DoctrineLevel.Of(at - 1), $"{level}레벨 직전 {at - 1}");
            var gap = at - DoctrineLevel.Threshold(level - 1);
            Assert.Greater(gap, previousGap, "레벨이 오를수록 필요한 교리가 늘어난다");
            previousGap = gap;
        }
    }

    [Test]
    public void DoctrineGains_AreDoubled_LossesAreNot()
    {
        var cult = new CultStatus();
        cult.AddWithRemainder(StatId.Doctrine, 15);
        Assert.AreEqual(30, cult[StatId.Doctrine].Value);
        cult.AddWithRemainder(StatId.Doctrine, -10);
        Assert.AreEqual(20, cult[StatId.Doctrine].Value);
    }

    [Test]
    public void FollowerFaithLoss_IsGuardedByDoctrineLevel()
    {
        var cult = new CultStatus();
        cult[StatId.FollowerFaith].Set(500);
        cult.AddWithRemainder(StatId.FollowerFaith, -100);
        Assert.AreEqual(400, cult[StatId.FollowerFaith].Value, "교리 0레벨이면 방어 없음");

        cult[StatId.Doctrine].Set(ClampedStat.DefaultMax); // 10레벨 = 50% 방어
        cult.AddWithRemainder(StatId.FollowerFaith, -100);
        Assert.AreEqual(350, cult[StatId.FollowerFaith].Value);

        cult.AddWithRemainder(StatId.FollowerFaith, 100);
        Assert.AreEqual(450, cult[StatId.FollowerFaith].Value, "증가는 그대로");
    }
}
