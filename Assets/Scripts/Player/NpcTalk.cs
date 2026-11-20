using UnityEngine;

/// <summary>
/// 말을 걸 수 있는 NPC. 바라보고 E를 누르면 대사가 화면 아래에 뜬다. (몸통 충돌 상자가 있어야 바라볼 수 있다)
/// 플레이어가 가까이 오면 천천히 그쪽을 바라본다. 대사는 lines에 적고, 누를 때마다 다음 줄이 나온다.
/// 미쿠(MikuSetup이 만든다)가 이 스크립트를 쓴다.
/// </summary>
public class NpcTalk : MonoBehaviour, IInteractable
{
    [SerializeField] private string npcName = "미쿠";
    [SerializeField] private string[] lines = { "안녕 나는 미쿠야" };
    [Tooltip("누구의 대사 모음(NpcDialogue)을 쓸지. 날씨, 시간, 진행 상황에 맞는 말을 한다. lines는 그것을 못 쓸 때만 쓴다")]
    [SerializeField] private NpcPersona persona = NpcPersona.Miku;
    [SerializeField] private bool useDialogue = true;
    [Tooltip("가까이 오면 이만큼(미터) 안에서 플레이어를 바라본다")]
    [SerializeField] private float lookDistance = 7f;

    private int next;
    private readonly System.Collections.Generic.List<string> recent = new System.Collections.Generic.List<string>();

    public string Prompt => "[E] " + npcName + "에게 말 걸기";

    public void Interact(Interactor who)
    {
        if (useDialogue) who.ShowMessage(NpcDialogue.Pick(persona, recent));
        else
        {
            if (lines == null || lines.Length == 0) return;
            who.ShowMessage(lines[next % lines.Length]);
            next++;
        }
        GameAudio.Play(GameAudio.Sfx.Paper, 0.5f);
        LookAt(who.transform.position, 1f);
    }

    private void Update()
    {
        if (GameRoot.Instance == null || GameRoot.Instance.Player == null) return;
        Vector3 p = GameRoot.Instance.Player.position;
        if ((p - transform.position).sqrMagnitude < lookDistance * lookDistance)
            LookAt(p, Time.deltaTime * 3f);
    }

    private void LookAt(Vector3 target, float t)
    {
        Vector3 flat = target - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), Mathf.Clamp01(t));
    }
}
