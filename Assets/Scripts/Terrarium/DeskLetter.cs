using UnityEngine;

/// <summary>
/// 집 책상 위의 도감 (예전에는 시작 편지였다). 바라보고 E를 누르면 정원 도감(CollectionBook)이 열린다.
/// 시작 편지는 처음 시작할 때 한 번만 저절로 열린다(LetterPanel).
/// 책상 위 오브젝트(Collider가 있는 것)에 붙인다. InteriorSetup이 붙여 준다. (클래스 이름은 씬 연결 때문에 그대로 둔다)
/// </summary>
public class DeskLetter : MonoBehaviour, IInteractable
{
    public string Prompt => "[E] 도감 보기";

    public void Interact(Interactor who)
    {
        CollectionBook.Show();
    }
}