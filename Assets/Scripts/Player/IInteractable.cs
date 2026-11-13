/// <summary>
/// E 키로 상호작용할 수 있는 모든 물건(재료, 작업대, 침대 등)이 구현한다.
/// Collider가 있는 오브젝트나 그 부모에 이 인터페이스를 구현한 스크립트를 붙인다.
/// </summary>
public interface IInteractable
{
    /// <summary>바라볼 때 화면에 보일 안내 글자. 예: "[E] 솔방울 줍기"</summary>
    string Prompt { get; }

    /// <summary>E 키를 눌렀을 때 호출된다. who는 누른 플레이어.</summary>
    void Interact(Interactor who);
}
