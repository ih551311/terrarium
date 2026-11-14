using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>저장 파일(JSON)에 들어가는 내용. 재료는 이름(id)으로 적는다.</summary>
[Serializable]
public class SaveData
{
    // 2: 병은 사용자가 골라서 전시한다 (1에서는 완성하면 선반에 자동으로 올라갔다)
    public int version = 2;

    // 하루와 지갑
    public int day = 1;
    public int coins;
    public int arrivedCoins;
    public int axeLevel;
    public int pickaxeLevel;
    public int hoeLevel;

    // 우체통: 보낸 재료, 주문한 물건, 도착해서 받기를 기다리는 물건
    public List<string> outbox = new List<string>();
    public List<string> orders = new List<string>();
    public List<string> arrivedItems = new List<string>();
    public int freeBottles = 1;                           // 아직 안 쓴 공짜 병 (처음 시작할 때 1개. 예전 저장에도 1개를 준다)
    public List<int> upgradeOrders = new List<int>();    // 주문한 도구 강화 (AcquireMethod 번호)
    public List<int> arrivedUpgrades = new List<int>();  // 도착한 도구 강화

    // 가방
    public List<BagEntrySave> bag = new List<BagEntrySave>();

    // 병들
    public List<BottleSave> bottles = new List<BottleSave>();
}

[Serializable]
public class BagEntrySave
{
    public string id;
    public int count;
}

[Serializable]
public class BottleSave
{
    public string bottleId;
    public bool created;
    public bool completed;
    public int soilLevel = 1;
    public string groundId;                 // 비어 있으면 흙
    public int shelfSlot = -1;              // 선반의 몇 번째 칸 (-1이면 선반에 없다)
    public string typeId;                   // 병 인벤토리의 병이면 병 종류 번호(예: bottle0). 작업대 병이면 비어 있다.
    public string name;                     // 사용자가 지어 준 병 이름. 비어 있으면 "종류 #번호"
    public List<PlacedSave> items = new List<PlacedSave>();
}

[Serializable]
public class PlacedSave
{
    public string id;
    public Vector3 position;
    public float yaw;
    public float scale = 1f;
}
