using System.Net.Http;
using UnityEditor;

/// 일레븐랩스 REST 헬퍼 — 보이스 목록 조회 + TTS 생성(mp3 저장).
/// 키는 프로젝트 루트/eleven_key.txt (gitignore 대상)에서 읽는다.
public static class SKEleven
{
    const string API = "https://api.elevenlabs.io/v1";
    public const string AUDIO_DIR = "Assets/Package04_Free_Kitchen/_SafeKitchen/Resources/Audio";

    static string _key;
    static string KEY
    {
        get
        {
            if (_key == null)
            {
                var p = @"C:\Users\edwin\OneDrive\Desktop\GasProject\eleven_key.txt";
                _key = System.IO.File.Exists(p) ? System.IO.File.ReadAllText(p).Trim() : "";
                if (_key == "") UnityEngine.Debug.LogWarning("[SKEleven] eleven_key.txt 없음");
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
                _c.DefaultRequestHeaders.Add("xi-api-key", KEY);
                _c.Timeout = System.TimeSpan.FromSeconds(120);
            }
            return _c;
        }
    }

    /// 보이스 목록: 이름 | id | 라벨 요약
    public static string Voices()
    {
        var s = C.GetAsync(API + "/voices").Result.Content.ReadAsStringAsync().Result;
        var sb = new System.Text.StringBuilder();
        int i = 0;
        while (true)
        {
            int vi = s.IndexOf("\"voice_id\"", i);
            if (vi < 0) break;
            string id = Ex(s, vi, "voice_id");
            string name = Ex(s, vi, "name");
            // labels 블록 요약 (voice_id 다음 500자 안에서)
            int li = s.IndexOf("\"labels\"", vi);
            string lab = "";
            if (li > 0 && li < vi + 2000)
            {
                int le = s.IndexOf('}', li);
                if (le > 0) lab = s.Substring(li + 9, System.Math.Min(le - li - 9, 160)).Replace("\"", "");
            }
            sb.AppendLine(name + " | " + id + " | " + lab);
            i = vi + 10;
        }
        return sb.ToString();
    }

    static string Ex(string s, int from, string key)
    {
        int i = s.IndexOf("\"" + key + "\"", from);
        if (i < 0) return null;
        i = s.IndexOf(':', i) + 1;
        int q1 = s.IndexOf('"', i) + 1;
        int q2 = s.IndexOf('"', q1);
        return s.Substring(q1, q2 - q1);
    }

    /// 효과음 생성 → Audio/{outName}.mp3 저장
    public static string SoundFx(string prompt, float seconds, string outName)
    {
        var json = "{\"text\":\"" + prompt.Replace("\"", "'") +
            "\",\"duration_seconds\":" + seconds.ToString(System.Globalization.CultureInfo.InvariantCulture) +
            ",\"prompt_influence\":0.35}";
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var r = C.PostAsync(API + "/sound-generation", content).Result;
        if (!r.IsSuccessStatusCode)
        {
            var err = r.Content.ReadAsStringAsync().Result;
            return "FAIL " + (int)r.StatusCode + " :: " + err.Substring(0, System.Math.Min(200, err.Length));
        }
        var bytes = r.Content.ReadAsByteArrayAsync().Result;
        System.IO.Directory.CreateDirectory(AUDIO_DIR);
        string path = AUDIO_DIR + "/" + outName + ".mp3";
        System.IO.File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);
        return "OK " + outName + " (" + bytes.Length / 1024 + "KB)";
    }

    /// TTS 생성 → Audio/{outName}.mp3 저장. <break time="0.3s" /> 태그로 일시정지 제어 가능
    public static string Say(string voiceId, string text, string outName,
        float stability = 0.62f, float style = 0.18f, float speed = 0.95f)
    {
        var ic = System.Globalization.CultureInfo.InvariantCulture;
        var esc = text.Replace("\\", "").Replace("\"", "\\\"");
        var json = "{\"text\":\"" + esc +
            "\",\"model_id\":\"eleven_multilingual_v2\"," +
            "\"voice_settings\":{\"stability\":" + stability.ToString(ic) +
            ",\"similarity_boost\":0.8,\"style\":" + style.ToString(ic) +
            ",\"speed\":" + speed.ToString(ic) + "}}";
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var r = C.PostAsync(API + "/text-to-speech/" + voiceId, content).Result;
        if (!r.IsSuccessStatusCode)
        {
            var err = r.Content.ReadAsStringAsync().Result;
            return "FAIL " + (int)r.StatusCode + " :: " + err.Substring(0, System.Math.Min(300, err.Length));
        }
        var bytes = r.Content.ReadAsByteArrayAsync().Result;
        System.IO.Directory.CreateDirectory(AUDIO_DIR);
        string path = AUDIO_DIR + "/" + outName + ".mp3";
        System.IO.File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);
        return "OK " + path + " (" + bytes.Length / 1024 + "KB)";
    }
}
