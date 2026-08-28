using System.Net.Http;
using UnityEditor;

/// 타입캐스트 REST 헬퍼 — 보이스 검색 + TTS 생성 (한국어 캐릭터 보이스).
/// 키는 프로젝트 루트/typecast_key.txt (gitignore 대상)
public static class SKTypecast
{
    const string API = "https://api.typecast.ai/v1";
    public const string AUDIO_DIR = "Assets/Package04_Free_Kitchen/_SafeKitchen/Resources/Audio";

    static string _key;
    static string KEY
    {
        get
        {
            if (_key == null)
            {
                // 프로젝트 루트 기준 상대 경로 (에디터 CWD = 프로젝트 루트) — 폴더 이동에 안전
                var p = System.IO.Path.GetFullPath("typecast_key.txt");
                _key = System.IO.File.Exists(p) ? System.IO.File.ReadAllText(p).Trim() : "";
            }
            return _key;
        }
    }

    static HttpClient _c;
    static HttpClient C
    {
        get
        {
            if (_c == null)
            {
                _c = new HttpClient();
                _c.DefaultRequestHeaders.Add("X-API-KEY", KEY);
                _c.Timeout = System.TimeSpan.FromSeconds(120);
            }
            return _c;
        }
    }

    /// 보이스 검색 (이름 부분일치, 원문 JSON 일부 반환)
    public static string FindVoice(string nameContains)
    {
        var r = C.GetAsync(API + "/voices").Result;
        var s = r.Content.ReadAsStringAsync().Result;
        if (!r.IsSuccessStatusCode) return "FAIL " + (int)r.StatusCode + " :: " + s.Substring(0, System.Math.Min(300, s.Length));
        var sb = new System.Text.StringBuilder();
        int i = 0;
        while (true)
        {
            int vi = s.IndexOf(nameContains, i);
            if (vi < 0) break;
            int a = System.Math.Max(0, vi - 220);
            sb.AppendLine("..." + s.Substring(a, System.Math.Min(320, s.Length - a)) + "...");
            i = vi + nameContains.Length;
            if (sb.Length > 2500) break;
        }
        return sb.Length == 0 ? "'" + nameContains + "' 없음 (전체 길이 " + s.Length + ")" : sb.ToString();
    }

    /// TTS 생성 → Resources/Audio/{outName} 저장 (기존 mp3 있으면 제거 후 wav 저장)
    public static string Say(string voiceId, string model, string text, string outName,
        string emotion = null, double intensity = 1.0, double tempo = 1.0)
    {
        var ic = System.Globalization.CultureInfo.InvariantCulture;
        var esc = text.Replace("\\", "").Replace("\"", "\\\"");
        var sb = new System.Text.StringBuilder();
        sb.Append("{\"voice_id\":\"" + voiceId + "\",\"text\":\"" + esc + "\",\"model\":\"" + model + "\",\"language\":\"kor\"");
        if (emotion != null)
            sb.Append(",\"prompt\":{\"emotion_preset\":\"" + emotion + "\",\"emotion_intensity\":" + intensity.ToString(ic) + "}");
        sb.Append(",\"output\":{\"audio_tempo\":" + tempo.ToString(ic) + ",\"audio_format\":\"wav\"}}");
        var content = new StringContent(sb.ToString(), System.Text.Encoding.UTF8, "application/json");
        var r = C.PostAsync(API + "/text-to-speech", content).Result;
        if (!r.IsSuccessStatusCode)
        {
            var err = r.Content.ReadAsStringAsync().Result;
            return "FAIL " + (int)r.StatusCode + " :: " + err.Substring(0, System.Math.Min(300, err.Length));
        }
        var bytes = r.Content.ReadAsByteArrayAsync().Result;
        System.IO.Directory.CreateDirectory(AUDIO_DIR);
        // 같은 이름의 기존 mp3 제거 (Resources 이름 충돌 방지)
        string old = AUDIO_DIR + "/" + outName + ".mp3";
        if (System.IO.File.Exists(old)) AssetDatabase.DeleteAsset(old);
        string path = AUDIO_DIR + "/" + outName + ".wav";
        System.IO.File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);
        return "OK " + outName + ".wav (" + bytes.Length / 1024 + "KB)";
    }
}
