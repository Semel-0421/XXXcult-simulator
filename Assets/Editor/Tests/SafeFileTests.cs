using System.IO;
using NUnit.Framework;

public class SafeFileTests
{
    [Test]
    public void CorruptMainFile_FallsBackToBackup()
    {
        var dir = Path.Combine(Path.GetTempPath(), "safefile_" + System.Guid.NewGuid().ToString("N"));
        var path = Path.Combine(dir, "save.json");
        SafeFile.Write(path, "first");
        SafeFile.Write(path, "second"); // first는 .bak으로 남는다
        File.WriteAllText(path, "");    // 본 파일이 깨졌다고 가정
        Assert.AreEqual("first", SafeFile.Read(path, t => t.Length == 0 ? null : t));
        SafeFile.Delete(path);
        Assert.IsNull(SafeFile.Read(path, t => t), "삭제하면 백업까지 지워진다");
        Directory.Delete(dir, true);
    }
}
