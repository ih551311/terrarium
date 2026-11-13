using UnityEngine;

/// <summary>
/// 병 안에 놓인 재료 하나. 놓을 때 BottleContents가 만들어서 붙인다.
/// 이 오브젝트의 위치가 바닥재 표면 위의 "선 자리"이고, 크기는 이 오브젝트를 키우고 줄여서 바꾼다.
/// </summary>
public class PlacedMaterial : MonoBehaviour
{
    public const float MinScale = 0.5f;
    public const float MaxScale = 1.8f;

    public MaterialData Material { get; private set; }

    /// <summary>기본 크기를 1로 봤을 때의 배율 (저장할 값)</summary>
    public float ScaleFactor { get; private set; } = 1f;

    /// <summary>위에서 봤을 때 돌아간 각도 (저장할 값)</summary>
    public float Yaw => transform.localEulerAngles.y;

    public void Init(MaterialData material, float yaw, float scaleFactor)
    {
        Material = material;
        transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        SetScaleFactor(scaleFactor);
    }

    /// <summary>자기 축으로 돌린다. 병 전체가 아니라 이 재료만 돈다.</summary>
    public void Rotate(float degrees)
    {
        transform.Rotate(0f, degrees, 0f, Space.Self);
    }

    /// <summary>배율을 곱해 키우거나 줄인다. 범위를 넘지 않는다.</summary>
    public void ScaleBy(float multiplier)
    {
        SetScaleFactor(ScaleFactor * multiplier);
    }

    private void SetScaleFactor(float value)
    {
        ScaleFactor = Mathf.Clamp(value, MinScale, MaxScale);
        transform.localScale = Vector3.one * ScaleFactor;
    }
}
