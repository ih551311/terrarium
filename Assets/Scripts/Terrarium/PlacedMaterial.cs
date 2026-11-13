using UnityEngine;

/// <summary>
/// 병 안에 놓인 재료 하나. 놓을 때 BottleContents가 만들어서 붙인다.
/// 이 오브젝트의 위치가 바닥재 표면 위의 "선 자리"이고, 크기는 이 오브젝트를 키우고 줄여서 바꾼다.
/// 돌리기와 크기 조절은 병 밖으로 나가지 않는지 검사해야 해서 BottleContents.TryRotate / TryScale로 한다.
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
        SetPose(yaw, scaleFactor);
    }

    /// <summary>각도와 배율을 정한다. 배율은 범위(MinScale~MaxScale) 안으로 맞춘다.</summary>
    public void SetPose(float yaw, float scaleFactor)
    {
        ScaleFactor = Mathf.Clamp(scaleFactor, MinScale, MaxScale);
        transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        transform.localScale = Vector3.one * ScaleFactor;
    }
}
