/// <summary>
/// 받침에 따라 바뀌는 조사를 알맞게 붙인다. 재료 이름 같은 말 뒤에 "을(를)", "이(가)"처럼 쓰면 기계가 만든 글처럼 보여서 이걸 쓴다.
/// 예: Kor.Eul("솔방울") = "솔방울을", Kor.Eul("고사리") = "고사리를", Kor.Iga("이끼") = "이끼가"
/// 한글이 아닌 글자로 끝나면 받침이 없는 것으로 본다.
/// </summary>
public static class Kor
{
    public static bool HasBatchim(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        char c = s[s.Length - 1];
        return c >= 0xAC00 && c <= 0xD7A3 && (c - 0xAC00) % 28 != 0;
    }

    public static string Eul(string s) { return s + (HasBatchim(s) ? "을" : "를"); }
    public static string Iga(string s) { return s + (HasBatchim(s) ? "이" : "가"); }
    public static string Eun(string s) { return s + (HasBatchim(s) ? "은" : "는"); }
}