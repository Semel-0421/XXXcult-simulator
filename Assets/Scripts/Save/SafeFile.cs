using System.IO;

// 저장 파일을 안전하게 쓰고 읽는다: 임시 파일에 쓴 뒤 File.Replace로 한 번에 바꿔치기하고(직전 파일은 .bak으로 남김),
// 읽을 때 본 파일이 없거나 깨졌으면 .bak으로 복구한다. 쓰는 도중 꺼져도 저장이 통째로 사라지지 않는다.
public static class SafeFile
{
    public static void Write(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var temp = path + ".tmp";
        File.WriteAllText(temp, text);
        if (!File.Exists(path)) { File.Move(temp, path); return; }
        try { File.Replace(temp, path, path + ".bak"); }
        catch (System.PlatformNotSupportedException)
        {
            // 웹(IndexedDB 파일시스템) 등 Replace가 없는 곳: 백업을 먼저 만든 뒤 교체한다
            File.Copy(path, path + ".bak", true);
            File.Delete(path);
            File.Move(temp, path);
        }
    }

    // parse가 null을 돌려주거나 예외를 내면 깨진 것으로 보고 백업을 시도한다
    public static T Read<T>(string path, System.Func<string, T> parse) where T : class
    {
        foreach (var candidate in new[] { path, path + ".bak" })
        {
            try
            {
                if (!File.Exists(candidate)) continue;
                var value = parse(File.ReadAllText(candidate));
                if (value != null) return value;
            }
            catch (System.Exception) { /* 다음 후보로 */ }
        }
        return null;
    }

    public static void Delete(string path)
    {
        foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
            if (File.Exists(p)) File.Delete(p);
    }
}
