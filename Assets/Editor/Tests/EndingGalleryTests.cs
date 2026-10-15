using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class EndingGalleryTests
{
    [Test]
    public void EveryEnding_HasRiddleAndIllustration()
    {
        // 같은 최종 스킬의 굿/배드는 문양을 공유하고 색으로 구분한다. 그 밖의 엔딩끼리는 삽화가 겹치면 안 된다.
        var textures = new List<(Texture2D texture, string id, string skill)>();
        foreach (var ending in Endings.All)
        {
            var quiz = EndingQuiz.Of(ending);
            Assert.IsFalse(string.IsNullOrEmpty(quiz), ending.Id);
            Assert.AreNotEqual(Lang.Get("quiz_hint"), quiz, ending.Id);
            Assert.IsFalse(quiz.Contains("{"), $"서식이 안 채워짐: {ending.Id} {quiz}");
            var sprite = EndingIllustrations.Get(ending);
            Assert.IsNotNull(sprite, ending.Id);
            var skill = ending.Final?.Id;
            foreach (var (texture, id, otherSkill) in textures)
                if (ReferenceEquals(texture, sprite.texture))
                    Assert.IsTrue(skill != null && skill == otherSkill, $"삽화가 겹침: {ending.Id} = {id}");
            textures.Add((sprite.texture, ending.Id, skill));
        }
    }

    [Test]
    public void Gallery_ListsEveryEnding()
    {
        // 해체 1 + 사망 2 + 최종 스킬 32 + 극단 15
        Assert.AreEqual(50, Endings.All.Count);
    }
}
