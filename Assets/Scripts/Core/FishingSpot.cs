using UnityEngine;

/// <summary>
/// 얼어붙은 연못의 낚시 구멍. 바라보고 E로 낚싯줄을 드리우고, 입질이 오면 얼른 E를 눌러 당긴다.
/// 1) 낚시하기 (E): 찌가 구멍에 뜨고 기다린다 (3~8초). 다시 E를 누르면 그만둔다.
/// 2) 입질: 찌가 쏙 들어가고 소리가 난다 (글자 안내는 없다). 2.4초 안에 E로 당기면 물고기가 가방으로 들어온다. 놓쳐도 불이익은 없다.
/// 물고기: 빙어(흔함), 얼음 송어, 금붕어, 밤에만 가끔 오로라 물고기(희귀). 병 속에 놓는 꾸밈 재료이고 우체통에 팔 수도 있다.
/// 구멍과 찌 모양은 FishingHoleBuilder(눈 섬 만들기 또는 낚시 구멍 다시 만들기)가 만든다.
/// </summary>
public class FishingSpot : MonoBehaviour, IInteractable
{
    private enum State { Idle, Waiting, Bite, Missed }

    [Tooltip("나올 수 있는 물고기 재료의 id (흔한 것부터)")]
    [SerializeField] private string[] commonFish = { "smelt", "ice_trout", "goldfish" };
    [SerializeField] private string rareFish = "aurora_fish";
    [Tooltip("찌 (물에 뜨는 작은 공)")]
    [SerializeField] private Transform bobber;

    private State state;
    private float timer;
    private Vector3 bobberRest;

    public string Prompt
    {
        get
        {
            switch (state)
            {
                case State.Waiting: return null; // 기다리는 동안은 안내 글자를 띄우지 않는다 (찌만 보인다)
                case State.Bite: return "[E] 당기기";
                case State.Missed: return null;
                default: return "[E] 낚시하기";
            }
        }
    }

    private void Start()
    {
        if (bobber != null)
        {
            bobberRest = bobber.localPosition;
            bobber.gameObject.SetActive(false);
        }
    }

    public void Interact(Interactor who)
    {
        switch (state)
        {
            case State.Idle:
                state = State.Waiting;
                timer = Random.Range(3f, 8f);
                if (bobber != null) bobber.gameObject.SetActive(true);
                GameAudio.Play(GameAudio.Sfx.Place, 0.7f);
                break;
            case State.Waiting:
                Stop();
                break;
            case State.Bite:
                Catch(who);
                break;
        }
    }

    private void Update()
    {
        if (state == State.Idle) return;
        timer -= Time.deltaTime;

        if (bobber != null)
        {
            float bob = Mathf.Sin(Time.time * 2.2f) * 0.015f;
            float dip = state == State.Bite ? -0.08f + Mathf.Sin(Time.time * 18f) * 0.02f : 0f; // 입질이면 쏙쏙 들어간다
            bobber.localPosition = bobberRest + Vector3.up * (bob + dip);
        }

        if (timer > 0f) return;
        switch (state)
        {
            case State.Waiting:
                state = State.Bite;
                timer = 2.4f;
                GameAudio.Play(GameAudio.Sfx.Chime, 0.7f);
                break;
            case State.Bite:
                state = State.Missed;
                timer = 1.2f;
                break;
            case State.Missed:
                Stop();
                break;
        }
    }

    private void Stop()
    {
        state = State.Idle;
        if (bobber != null) { bobber.gameObject.SetActive(false); bobber.localPosition = bobberRest; }
    }

    private void Catch(Interactor who)
    {
        MaterialData fish = Pick();
        if (fish == null) { Stop(); return; }
        if (!who.Bag.TryAdd(fish))
        {
            who.ShowMessage("가방이 가득 차서 " + fish.displayName + "을(를) 놓아 주었어요");
            Stop();
            return;
        }
        string extra = fish.id == rareFish ? "  (희귀!)" : string.Empty;
        who.ShowMessage(fish.displayName + "을(를) 낚았어요!" + extra);
        GameAudio.Play(GameAudio.Sfx.Pickup, 0.9f);
        Stop();
    }

    /// <summary>물고기 하나를 고른다. 밤(시간 0.78 이후)에는 가끔 오로라 물고기가 걸린다.</summary>
    private MaterialData Pick()
    {
        MaterialDatabase db = MaterialDatabase.Instance;
        if (db == null) return null;
        bool night = GameState.TimeOfDay >= Pickup.NightStart;
        if (night && Random.value < 0.12f)
        {
            MaterialData rare = db.Find(rareFish);
            if (rare != null) return rare;
        }
        // 흔한 것일수록 자주: 빙어 55%, 송어 30%, 금붕어 15%
        float r = Random.value;
        string id = r < 0.55f ? commonFish[0] : r < 0.85f ? commonFish[Mathf.Min(1, commonFish.Length - 1)] : commonFish[Mathf.Min(2, commonFish.Length - 1)];
        return db.Find(id);
    }
}
