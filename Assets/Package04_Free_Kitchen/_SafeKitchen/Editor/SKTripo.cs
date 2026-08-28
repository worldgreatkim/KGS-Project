using System.Net.Http;
using System.Net.Http.Headers;
using UnityEditor;

/// Tripo REST 직접 호출 헬퍼 — 로컬 이미지 업로드 → image-to-model → GLB 다운로드.
/// (MCP 플러그인이 로컬 이미지 업로드를 미지원하여 자체 구현)
public static class SKTripo
{
    // API 키는 저장소에 올리지 않는다 — 프로젝트 루트/tripo_key.txt (gitignore 대상)에서 읽음
    static string _key;
    static string KEY
    {
        get
        {
            if (_key == null)
            {
                var p = System.IO.Path.GetFullPath("tripo_key.txt");   // 프로젝트 루트
                _key = System.IO.File.Exists(p) ? System.IO.File.ReadAllText(p).Trim() : "";
                if (_key == "") UnityEngine.Debug.LogWarning("[SKTripo] tripo_key.txt 없음 — API 호출 불가");
            }
            return _key;
        }
    }
    const string API = "https://api.tripo3d.ai/v2/openapi";
    const string API3 = "https://openapi.tripo3d.ai/v3";
    public const string REFS = @"Assets\Package04_Free_Kitchen\_SafeKitchen\refs\";   // 프로젝트 루트 기준

    static HttpClient _c;
    static HttpClient C
    {
        get
        {
            if (_c == null)
            {
                _c = new HttpClient();
                _c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", KEY);
                _c.Timeout = System.TimeSpan.FromSeconds(180);
            }
            return _c;
        }
    }

    // 단순 JSON 값 추출 (키 다음의 첫 문자열)
    static string Ex(string s, string key)
    {
        var i = s.IndexOf("\"" + key + "\"");
        if (i < 0) return null;
        i = s.IndexOf(':', i) + 1;
        var q1 = s.IndexOf('"', i);
        if (q1 < 0) return null;
        q1 += 1;
        var q2 = s.IndexOf('"', q1);
        return s.Substring(q1, q2 - q1);
    }

    public static string Upload(string path)
    {
        var form = new MultipartFormDataContent();
        var fc = new ByteArrayContent(System.IO.File.ReadAllBytes(path));
        fc.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(fc, "file", System.IO.Path.GetFileName(path));
        var r = C.PostAsync(API + "/upload/sts", form).Result;
        return r.Content.ReadAsStringAsync().Result;
    }

    public static string CreateTask(string imageToken)
    {
        var json = "{\"type\":\"image_to_model\",\"file\":{\"type\":\"png\",\"file_token\":\"" + imageToken + "\"}}";
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var r = C.PostAsync(API + "/task", content).Result;
        return r.Content.ReadAsStringAsync().Result;
    }

    /// refs 폴더의 파일명으로 제출 → task_id 반환 (실패 시 원문 덤프)
    public static string Submit(string fileName)
    {
        var up = Upload(REFS + fileName);
        var tok = Ex(up, "image_token");
        if (tok == null) return "UPLOAD_FAIL " + fileName + " :: " + up;
        var tk = CreateTask(tok);
        var id = Ex(tk, "task_id");
        return id == null ? "TASK_FAIL " + fileName + " :: " + tk : fileName + " => " + id;
    }

    /// v3 파일 업로드
    public static string Upload3(string path)
    {
        var form = new MultipartFormDataContent();
        var fc = new ByteArrayContent(System.IO.File.ReadAllBytes(path));
        fc.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(fc, "file", System.IO.Path.GetFileName(path));
        var r = C.PostAsync(API3 + "/files", form).Result;
        return r.Content.ReadAsStringAsync().Result;
    }

    /// 이미지 편집 생성 (v3 API, banana2) — 비율·자세 보정용
    public static string ImageEdit(string fileName, string prompt)
    {
        var up = Upload3(REFS + fileName);
        var tok = Ex(up, "file_token");
        if (tok == null) tok = Ex(up, "token");
        if (tok == null) tok = Ex(up, "id");
        if (tok == null) return "UPLOAD_FAIL :: " + up;
        var json = "{\"input\":\"" + tok + "\",\"prompt\":\"" + prompt.Replace("\"", "'")
            + "\",\"model\":\"banana2\",\"output_format\":\"png\"}";
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var r = C.PostAsync(API3 + "/generation/image-to-image", content).Result;
        var s = r.Content.ReadAsStringAsync().Result;
        var id = Ex(s, "task_id");
        return id == null ? "TASK_FAIL :: " + s : id;
    }

    public static string RawStatus3(string taskId)
    {
        var r = C.GetAsync(API3 + "/tasks/" + taskId).Result;
        return r.Content.ReadAsStringAsync().Result;
    }

    /// 이미지 작업 결과를 refs/{outName}.png 로 저장 (v3)
    public static string HarvestImage(string taskId, string outName)
    {
        var s = RawStatus3(taskId);
        if (Ex(s, "status") != "success") return "NOT_READY :: " + s.Substring(0, System.Math.Min(400, s.Length));
        string url = null;
        foreach (var key in new string[] { "generated_image_url", "generated_image", "image", "output_image" })
        {
            var v = Ex(s, key);
            if (v != null && v.StartsWith("http")) { url = v; break; }
        }
        if (url == null) return "URL_FAIL :: " + s.Substring(0, System.Math.Min(500, s.Length));
        url = url.Replace("\\u0026", "&").Replace("\\/", "/");
        var bytes = C.GetByteArrayAsync(url).Result;
        System.IO.File.WriteAllBytes(REFS + outName + ".png", bytes);
        AssetDatabase.ImportAsset("Assets/Package04_Free_Kitchen/_SafeKitchen/refs/" + outName + ".png");
        return "OK " + outName + ".png (" + bytes.Length / 1024 + "KB)";
    }

    /// 오토 리깅 (25크레딧)
    public static string CreateRig(string modelTaskId)
    {
        var json = "{\"type\":\"animate_rig\",\"original_model_task_id\":\"" + modelTaskId + "\",\"out_format\":\"glb\"}";
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var r = C.PostAsync(API + "/task", content).Result;
        var s = r.Content.ReadAsStringAsync().Result;
        var id = Ex(s, "task_id");
        return id == null ? "RIG_FAIL :: " + s : id;
    }

    /// 프리셋 애니메이션 리타겟 (10크레딧/개) — preset:walk, preset:run 등
    public static string CreateRetarget(string rigTaskId, string preset)
    {
        var json = "{\"type\":\"animate_retarget\",\"original_model_task_id\":\"" + rigTaskId + "\",\"animation\":\"" + preset + "\",\"out_format\":\"glb\"}";
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var r = C.PostAsync(API + "/task", content).Result;
        var s = r.Content.ReadAsStringAsync().Result;
        var id = Ex(s, "task_id");
        return id == null ? "RETARGET_FAIL :: " + s : id;
    }

    public static string RawStatus(string taskId)
    {
        var r = C.GetAsync(API + "/task/" + taskId).Result;
        return r.Content.ReadAsStringAsync().Result;
    }

    /// 상태 요약: status/progress만
    public static string Poll(string taskId)
    {
        var s = RawStatus(taskId);
        var st = Ex(s, "status");
        var pi = s.IndexOf("\"progress\"");
        var pr = "?";
        if (pi >= 0)
        {
            var c = s.IndexOf(':', pi) + 1;
            var e = c;
            while (e < s.Length && (char.IsDigit(s[e]) || s[e] == '.' || s[e] == ' ')) e++;
            pr = s.Substring(c, e - c).Trim();
        }
        return taskId.Substring(0, 8) + " : " + st + " " + pr + "%";
    }

    /// 성공한 작업의 GLB를 받아 Assets/Models3D/{name}.glb 로 저장+임포트
    public static string Harvest(string taskId, string name)
    {
        var s = RawStatus(taskId);
        if (Ex(s, "status") != "success") return "NOT_READY :: " + s.Substring(0, System.Math.Min(300, s.Length));
        var url = Ex(s, "pbr_model");
        if (url == null || !url.StartsWith("http")) url = Ex(s, "model");
        if (url == null || !url.StartsWith("http")) return "URL_FAIL :: " + s.Substring(0, System.Math.Min(400, s.Length));
        url = url.Replace("\\u0026", "&").Replace("\\/", "/");
        var bytes = C.GetByteArrayAsync(url).Result;
        var path = "Assets/Models3D/" + name + ".glb";
        System.IO.Directory.CreateDirectory("Assets/Models3D");
        System.IO.File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);
        return "OK " + path + " (" + bytes.Length / 1024 + "KB)";
    }
}
