using TMPro;
using UnityEngine;

/// <summary>
/// 화면에 항상 떠 있는 코인 표시 (오른쪽 위, 작은 지도 아래). 코인이 바뀌면 숫자가 바뀌고, 늘면 잠깐 커졌다 돌아온다.
/// 패널이 아니라서 가방이나 병 꾸미기를 열어도 그대로 보인다. 모양은 UISetup이 만든다.
/// </summary>
public class CoinHud : MonoBehaviour
{
    [SerializeField] private TMP_Text amountText;
    [Tooltip("코인이 늘 때 잠깐 커지는 효과를 줄 오브젝트")]
    [SerializeField] private RectTransform pulseTarget;

    private int shown = -1;
    private float pulse; // 1에서 0으로 줄어드는 값

    private void OnEnable()
    {
        GameState.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        GameState.Changed -= Refresh;
    }

    private void Update()
    {
        // 저장을 불러오는 등으로 값이 바뀌었는데 알림을 못 받은 경우를 위해, 값이 다르면 다시 맞춘다
        if (shown != GameState.Coins) Refresh();

        if (pulse > 0f)
        {
            pulse = Mathf.Max(0f, pulse - Time.unscaledDeltaTime * 3f);
            if (pulseTarget != null) pulseTarget.localScale = Vector3.one * (1f + 0.2f * pulse);
        }
    }

    private void Refresh()
    {
        int coins = GameState.Coins;
        if (shown >= 0 && coins > shown) pulse = 1f;
        shown = coins;
        if (amountText != null) amountText.text = coins.ToString();
    }
}
