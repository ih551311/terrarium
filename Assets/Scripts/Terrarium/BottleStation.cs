using UnityEngine;

/// <summary>
/// 작업대. 바라보고 E를 누르면 병 꾸미기 패널이 열린다.
/// 작업대에는 병이 한 자리만 있고, 어떤 병을 올릴지는 패널 안에서 고른다.
/// 병은 종류별로 하나씩(처음부터 있는 빈 병 3개)이고, 고른 병만 작업대 위에 보인다.
/// 작업대 오브젝트(Workbench)에 붙이고, 병들(BottleContents)을 연결한다.
/// </summary>
public class BottleStation : MonoBehaviour, IInteractable
{
    [Tooltip("작업대에 올릴 수 있는 병들. 같은 자리에 겹쳐 두고, 고른 것만 켜진다.")]
    [SerializeField] private BottleContents[] bottles;

    [SerializeField] private int selectedIndex;

    public string Prompt => "[E] 병 꾸미기";

    public int Count => bottles.Length;
    public int SelectedIndex => selectedIndex;
    public BottleContents Selected => bottles[selectedIndex];

    /// <summary>
    /// 지금 꾸미는 중인 병(만들었고 아직 완성 안 한 병)의 번호. 없으면 -1.
    /// 꾸미는 중인 병이 있으면 패널은 병 고르기 없이 바로 그 병을 연다.
    /// </summary>
    public int InProgressIndex
    {
        get
        {
            for (int i = 0; i < bottles.Length; i++)
                if (bottles[i].IsCreated && !bottles[i].IsCompleted) return i;
            return -1;
        }
    }

    private void Awake()
    {
        Select(selectedIndex);
    }

    public void Interact(Interactor who)
    {
        GameRoot.Instance.BottlePanel.Open(this);
    }

    public BottleContents Get(int index)
    {
        return bottles[index];
    }

    /// <summary>작업대에 올릴 병을 고른다. 고른 병만 켜고 나머지는 숨긴다.</summary>
    public void Select(int index)
    {
        selectedIndex = Mathf.Clamp(index, 0, bottles.Length - 1);
        for (int i = 0; i < bottles.Length; i++)
            bottles[i].gameObject.SetActive(i == selectedIndex);
    }
}
