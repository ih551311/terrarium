using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 저장과 재료 목록 메뉴.
/// - 재료 목록 만들기: 저장 파일이 재료를 이름(id)으로 불러오는 데 쓰는 목록 에셋(Assets/Resources/MaterialDatabase.asset). 재료를 새로 만들면 다시 누른다.
/// - 저장 지우기: 저장 파일을 지워서 다음에 켤 때 새로 시작한다.
/// - 저장 폴더 열기: 저장 파일이 있는 폴더를 연다.
/// </summary>
public static class SaveMenu
{
    private const string DatabasePath = "Assets/Resources/MaterialDatabase.asset";

    [MenuItem("유리병 정원/재료 목록 만들기")]
    public static string BuildMaterialDatabase()
    {
        Directory.CreateDirectory("Assets/Resources");
        MaterialDatabase db = AssetDatabase.LoadAssetAtPath<MaterialDatabase>(DatabasePath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<MaterialDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
        }

        var list = new List<MaterialData>();
        foreach (string guid in AssetDatabase.FindAssets("t:MaterialData"))
        {
            MaterialData m = AssetDatabase.LoadAssetAtPath<MaterialData>(AssetDatabase.GUIDToAssetPath(guid));
            if (m != null && !string.IsNullOrEmpty(m.id)) list.Add(m);
        }
        db.materials = list.ToArray();
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        return "재료 목록을 만들었어요: 재료 " + list.Count + "종";
    }

    [MenuItem("유리병 정원/저장 지우기")]
    public static void DeleteSave()
    {
        bool had = SaveSystem.HasSave;
        SaveSystem.DeleteSave();
        Debug.Log(had ? "저장을 지웠어요. 다음에 Play를 켜면 새로 시작해요." : "지울 저장이 없어요.");
    }

    [MenuItem("유리병 정원/저장 폴더 열기")]
    public static void OpenSaveFolder()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
