using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 게임 저장과 불러오기. 침대에서 자면 저장하고(Save), 게임을 켜면 저장 파일이 있을 때 자동으로 불러온다.
/// 저장 파일: (내 컴퓨터의 게임 데이터 폴더)/save.json  — Unity 에디터에서는 메뉴 "유리병 정원 > 저장 지우기"로 지운다.
/// 저장하는 것: 날짜, 코인, 도구 단계, 우체통 상태, 가방, 병(종류, 흙의 양, 바닥재, 놓은 재료의 위치·회전·크기, 완성 여부)
/// 불러오는 순서: 장면이 열리기 전에 날짜, 코인, 병 기록을 먼저 채우고(LoadWorld), 가방은 GameRoot가 만들어질 때 채운다(ApplyBag).
/// </summary>
public static class SaveSystem
{
    private static SaveData loaded; // 불러온 내용 (가방은 나중에 GameRoot가 받아 간다)

    public static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");
    private static string BackupPath => FilePath + ".bak"; // 바로 전 저장 (저장 파일이 깨졌을 때 이것으로 이어 간다)

    public static bool HasSave => File.Exists(FilePath);

    /// <summary>저장 파일이 있는데 읽지 못했는가. 이때는 자동 저장이 읽지 못한 파일을 덮어쓰지 않는다.</summary>
    public static bool LoadFailed { get; private set; }

    /// <summary>지금 상태를 저장한다. 성공하면 true.</summary>
    public static bool Save(Bag bag)
    {
        try
        {
            var data = new SaveData();
            GameState.Export(data);

            if (bag != null)
            {
                foreach (Bag.Slot slot in bag.Slots)
                    data.bag.Add(new BagEntrySave { id = slot.material.id, count = slot.count });
            }

            foreach (var pair in BottleStore.All)
            {
                BottleRecord r = pair.Value;
                var b = new BottleSave
                {
                    bottleId = pair.Key,
                    created = r.created,
                    completed = r.completed,
                    soilLevel = r.soilLevel,
                    shelfSlot = r.shelfSlot,
                    typeId = r.typeId,
                    name = r.name,
                    paidCoins = r.paidCoins,
                    usedFree = r.usedFree,
                    groundId = r.ground != null ? r.ground.id : string.Empty
                };
                foreach (PlacedData p in r.items)
                    b.items.Add(new PlacedSave { id = p.material.id, position = p.localPosition, yaw = p.yaw, scale = p.scaleFactor });
                data.bottles.Add(b);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

            // 파일을 바로 덮어쓰면 쓰는 도중 꺼졌을 때 저장이 깨진다. 임시 파일에 먼저 다 쓰고, 이전 저장은 백업으로 남긴 뒤 바꾼다.
            string temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(data, true));
            if (File.Exists(FilePath)) File.Copy(FilePath, BackupPath, true);
            File.Copy(temp, FilePath, true);
            File.Delete(temp);

            loaded = data; // 메인 화면에서 다시 시작해도 방금 저장한 가방으로 채워진다
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("저장하지 못했어요: " + e.Message);
            return false;
        }
    }

    /// <summary>저장 파일을 지운다. (새로 시작하고 싶을 때)</summary>
    public static void DeleteSave()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
    }

    /// <summary>가방을 저장된 내용으로 채운다. GameRoot가 처음 만들어질 때 부른다.</summary>
    public static void ApplyBag(Bag bag)
    {
        if (loaded == null || bag == null) return;
        MaterialDatabase db = MaterialDatabase.Instance;
        if (db == null) return;

        var entries = new System.Collections.Generic.List<(MaterialData, int)>();
        foreach (BagEntrySave e in loaded.bag)
        {
            MaterialData m = db.Find(e.id);
            if (m != null) entries.Add((m, e.count));
        }
        bag.Restore(entries);
    }

    // 장면이 열리기 전에: 날짜, 코인, 병 기록을 저장된 것으로 채운다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadWorld()
    {
        loaded = null;
        LoadFailed = false;
        if (!HasSave) return;
        LoadFailed = true; // 끝까지 읽으면 아래에서 false로 바꾼다

        try
        {
            SaveData data = ReadSaveFile();
            MaterialDatabase db = MaterialDatabase.Instance;
            if (data == null || db == null)
            {
                Debug.LogWarning("저장 파일이나 재료 목록(MaterialDatabase)을 읽지 못해서 새로 시작해요. 메뉴 '유리병 정원 > 재료 목록 만들기'를 눌렀는지 확인하세요.");
                return;
            }

            GameState.Import(data, db);

            foreach (BottleSave b in data.bottles)
            {
                var record = new BottleRecord
                {
                    created = b.created,
                    completed = b.completed,
                    soilLevel = b.soilLevel,
                    shelfSlot = data.version >= 2 ? b.shelfSlot : -1, // 예전 저장은 자동으로 올라가 있던 병을 모두 내린 상태로 연다
                    typeId = string.IsNullOrEmpty(b.typeId) ? null : b.typeId,
                    name = string.IsNullOrEmpty(b.name) ? null : b.name,
                    paidCoins = b.paidCoins,
                    usedFree = b.usedFree,
                    ground = db.Find(b.groundId)
                };
                foreach (PlacedSave p in b.items)
                {
                    MaterialData m = db.Find(p.id);
                    if (m == null) continue;
                    record.items.Add(new PlacedData { material = m, localPosition = p.position, yaw = p.yaw, scaleFactor = p.scale });
                }
                BottleStore.Set(b.bottleId, record);
            }

            // 예전 저장: 작업대 병이 완성된 채로 남아 있으면 병 인벤토리로 옮긴다 (선반 전시 상태는 그대로 이어진다)
            var legacy = new System.Collections.Generic.List<string>();
            foreach (var pair in BottleStore.All)
                if (pair.Value.completed && pair.Value.typeId == null) legacy.Add(pair.Key);
            foreach (string typeId in legacy) BottleStore.Archive(typeId);

            loaded = data;
            LoadFailed = false;
            Debug.Log("[저장] " + data.day + "일째 저장을 불러왔어요.");
        }
        catch (Exception e)
        {
            Debug.LogError("저장 파일을 불러오지 못해서 새로 시작해요: " + e.Message);
        }
    }

    /// <summary>
    /// 저장 파일을 읽는다. 깨져서 못 읽으면 바로 전 저장(백업)을 읽는다. 둘 다 못 읽으면 깨진 파일은 지우지 않고 따로 옮겨 두고
    /// (save.json.broken), 새로 시작해서 다시 저장할 수 있게 한다. (읽지 못한 파일이 있다고 영영 저장이 안 되는 일이 없게)
    /// </summary>
    private static SaveData ReadSaveFile()
    {
        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            if (data != null && data.day >= 1) return data;
        }
        catch (Exception e)
        {
            Debug.LogWarning("저장 파일을 읽지 못했어요: " + e.Message);
        }

        if (File.Exists(BackupPath))
        {
            try
            {
                SaveData backup = JsonUtility.FromJson<SaveData>(File.ReadAllText(BackupPath));
                if (backup != null && backup.day >= 1)
                {
                    Debug.LogWarning("[저장] 저장 파일이 깨져서 바로 전 저장(백업)으로 이어 가요.");
                    return backup;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("백업 저장도 읽지 못했어요: " + e.Message);
            }
        }

        // 둘 다 못 읽는다: 깨진 파일을 따로 보관하고 새로 시작한다 (지우지 않는다)
        try
        {
            string broken = FilePath + ".broken";
            if (File.Exists(broken)) File.Delete(broken);
            File.Move(FilePath, broken);
            Debug.LogError("저장 파일이 깨져서 " + broken + " 로 옮겨 두고 새로 시작해요.");
            LoadFailed = false; // 새 저장은 가능하다
        }
        catch (Exception e)
        {
            Debug.LogError("깨진 저장 파일을 옮기지 못했어요: " + e.Message);
        }
        return null;
    }

    // Play를 껐다 켤 때 기억해 둔 것을 버린다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        loaded = null;
        LoadFailed = false;
    }
}
