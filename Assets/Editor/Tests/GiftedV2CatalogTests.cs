using System.Linq;
using NUnit.Framework;

public class GiftedV2CatalogTests
{
    [Test]
    public void Catalog_Has120UniqueEntries()
    {
        Assert.AreEqual(120, GiftedV2Catalog.All.Count);
        Assert.AreEqual(120, GiftedV2Catalog.All.Select(e => e.Id).Distinct().Count());
        Assert.IsNotNull(GiftedV2Catalog.Find("v2n001"));
    }

    [Test]
    public void EachMode_Has10SharedAnd30Exclusive()
    {
        foreach (var mode in new[] { "normal", "god", "mirror" })
        {
            var list = GiftedV2Catalog.ForMode(mode).ToList();
            Assert.AreEqual(10, list.Count(e => e.Shared), mode);
            Assert.AreEqual(30, list.Count(e => !e.Shared), mode);
        }
    }

    [Test]
    public void OneAdultGated_AllLinkedToAchievement()
    {
        Assert.AreEqual(1, GiftedV2Catalog.All.Count(e => e.AdultGated));
        Assert.IsTrue(GiftedV2Catalog.All.All(e => !string.IsNullOrEmpty(e.LinkedAchievement)));
    }
}
