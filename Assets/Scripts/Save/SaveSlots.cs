using System;
using System.IO;
using UnityEngine;

// 저장 칸 100개의 파일 입출력. 칸 번호는 1부터 MaxSlots까지.
public static class SaveSlots
{
    public const int MaxSlots = 100;

    static string directory;

    // 테스트에서 임시 폴더로 바꿀 수 있다
    public static string Directory
    {
        get => directory ??= Path.Combine(Application.persistentDataPath, "saves");
        set => directory = value;
    }

    static string PathOf(int slot)
    {
        if (slot < 1 || slot > MaxSlots) throw new ArgumentOutOfRangeException(nameof(slot), $"slot must be 1..{MaxSlots}");
        return Path.Combine(Directory, $"slot_{slot:000}.json");
    }

    public static bool Exists(int slot) => File.Exists(PathOf(slot));

    // 없거나 깨진 파일이면 null
    public static SaveData Read(int slot)
    {
        var path = PathOf(slot);
        if (!File.Exists(path)) return null;
        try
        {
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            return data != null && data.version <= SaveData.CurrentVersion ? data : null;
        }
        catch (Exception) { return null; }
    }

    // 임시 파일에 먼저 쓰고 바꿔치기해서, 쓰다 끊겨도 기존 저장이 깨지지 않게 한다
    public static void Write(int slot, SaveData data)
    {
        var path = PathOf(slot);
        System.IO.Directory.CreateDirectory(Directory);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonUtility.ToJson(data));
        if (File.Exists(path)) File.Delete(path);
        File.Move(temp, path);
    }

    public static void Delete(int slot)
    {
        var path = PathOf(slot);
        if (File.Exists(path)) File.Delete(path);
    }
}
