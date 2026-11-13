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

    /// <summary>식물의 성장 단계 0 새싹, 1 자란 모습, 2 다 자란 모습. 식물이 아니면 항상 2로 본다. (저장할 값)</summary>
    public int Growth { get; private set; }

    public const int MaxGrowth = 2;
    private static readonly float[] GrowthSizes = { 0.5f, 0.75f, 1f }; // 단계별 크기 (다 자란 모습이 기본 크기)

    public bool IsPlant => Material != null && Material.kind == MaterialKind.Plant;

    /// <summary>
    /// 처음 놓을 때(growth 생략): 식물은 새싹(0)에서 시작하고, 식물이 아닌 것은 항상 다 자란 모습이다.
    /// 저장된 것을 되살릴 때는 저장된 단계를 넣는다.
    /// </summary>
    public void Init(MaterialData material, float yaw, float scaleFactor, int growth = 0)
    {
        Material = material;
        Growth = IsPlant ? Mathf.Clamp(growth, 0, MaxGrowth) : MaxGrowth;
        SetPose(yaw, scaleFactor);
    }

    /// <summary>각도와 배율을 정한다. 배율은 범위(MinScale~MaxScale) 안으로 맞춘다. 보이는 크기에는 성장 단계가 곱해진다.</summary>
    public void SetPose(float yaw, float scaleFactor)
    {
        ScaleFactor = Mathf.Clamp(scaleFactor, MinScale, MaxScale);
        transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        transform.localScale = Vector3.one * (ScaleFactor * GrowthSizes[Growth]);
    }
}
