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

    // 없거나 깨진 파일이면 백업(.bak)을 보고, 그것도 없으면 null
    public static SaveData Read(int slot) =>
        SafeFile.Read(PathOf(slot), text =>
        {
            var data = JsonUtility.FromJson<SaveData>(text);
            return data != null && data.version <= SaveData.CurrentVersion ? data : null;
        });

    public static void Write(int slot, SaveData data) => SafeFile.Write(PathOf(slot), JsonUtility.ToJson(data));

    public static void Delete(int slot) => SafeFile.Delete(PathOf(slot));
}
