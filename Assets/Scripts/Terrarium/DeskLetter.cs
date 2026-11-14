using UnityEngine;

/// <summary>
/// 집 책상 위의 편지. 바라보고 E를 누르면 시작 편지를 다시 읽는다.
/// 편지 오브젝트(Collider가 있는 것)에 붙인다. InteriorSetup이 붙여 준다.
/// </summary>
public class DeskLetter : MonoBehaviour, IInteractable
{
    public string Prompt => "[E] 편지 읽기";

    public void Interact(Interactor who)
    {
        if (LetterPanel.Instance != null) LetterPanel.Instance.Show();
    }
}
