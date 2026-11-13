using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 큰 지도 그림을 클릭하면 그 자리로 이동하게 한다. 지도 그림(RawImage)에 붙인다.
/// 실제 이동과 안내는 MapPanel이 한다.
/// </summary>
public class MapClick : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private MapPanel panel;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && panel != null)
            panel.OnMapClicked(eventData.position);
    }
}
