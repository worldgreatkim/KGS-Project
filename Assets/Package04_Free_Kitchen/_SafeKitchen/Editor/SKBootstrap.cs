using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// 세이프 키친 3D — 씬 생성기 (에디터 메뉴)
/// 메뉴: SafeKitchen → "씬 생성"을 누르면 방+주방+게임이 통째로 만들어진다.
/// 생성 후 주방 가구는 Hierarchy의 Kitchen 아래에서 자유롭게 이동·회전·추가.
public static class SKBootstrap
{
    const string PACK = "Assets/Package04_Free_Kitchen/FreeKitchen";
    const string SCENE_PATH = "Assets/SafeKitchen3D.unity";

    static Material Lit(Color c)
    {
        Shader sh = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null
            ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
        var m = new Material(sh);
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        return m;
    }

    static GameObject Box(string name, Vector3 pos, Vector3 size, Color col, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = Lit(col);
        return go;
    }

    [MenuItem("SafeKitchen/0. 재질 파이프라인 맞추기 (분홍 재질 해결)")]
    public static void FixMaterials()
    {
        bool urpProject = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        // 팩 재질 하나를 열어 셰이더 확인
        var guids = AssetDatabase.FindAssets("t:Material", new[] { PACK + "/Materials/Version03" });
        bool urpMats = false;
        if (guids.Length > 0)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (m != null && m.shader != null) urpMats = m.shader.name.Contains("Universal");
        }
        if (urpProject == urpMats)
        {
            Debug.Log("[SafeKitchen] 재질과 파이프라인이 이미 맞음 (URP=" + urpProject + ")");
            return;
        }
        string pkg = PACK + "/SRP UpgradePackage/" + (urpProject ? "URP_Material.unitypackage" : "Built-in_Material.unitypackage");
        if (System.IO.File.Exists(pkg))
        {
            AssetDatabase.ImportPackage(pkg, false);
            Debug.Log("[SafeKitchen] 재질 패키지 적용: " + pkg);
        }
        else Debug.LogWarning("[SafeKitchen] 재질 패키지를 찾지 못함: " + pkg);
    }

    [MenuItem("SafeKitchen/1. 씬 생성 (주방+게임 전체)")]
    public static void BuildScene()
    {
        FixMaterials();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---- 카메라 (고정 쿼터뷰) ----
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.75f;
        cam.transform.position = new Vector3(9.6f, 12f, 14.4f);
        cam.transform.rotation = Quaternion.Euler(52f, 180f, 0f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.44f, 0.53f, 0.63f);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 60f;
        camGo.AddComponent<AudioListener>();

        // ---- 조명 (확정 무드: 낮은 전역광 + 따뜻한 실내등 웅덩이) ----
        var sunGo = new GameObject("Sun");
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 0.75f;
        sun.color = new Color(0.98f, 0.97f, 0.94f);
        sun.shadows = LightShadows.Soft;
        sunGo.transform.rotation = Quaternion.Euler(50f, 145f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.46f, 0.48f, 0.53f);
        var lampRoot = new GameObject("Lamps");
        foreach (var lp in new[] { new Vector3(12.5f, 2.9f, 2.6f), new Vector3(8.6f, 3.0f, 5.6f),
                                   new Vector3(8.8f, 2.9f, 8.3f), new Vector3(3.0f, 2.9f, 2.6f) })
        {
            var lgo = new GameObject("Lamp");
            lgo.transform.SetParent(lampRoot.transform, false);
            lgo.transform.position = lp;
            var l = lgo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.88f, 0.68f);
            l.intensity = 2.6f;
            l.range = 8f;
        }

        // ---- 방 (바닥+벽+비상구+창문) ----
        var room = new GameObject("Room").transform;
        float RW = SKData.RW, RD = SKData.RD;
        Box("floor", new Vector3(RW / 2f, -0.15f, RD / 2f), new Vector3(RW, 0.3f, RD), new Color(0.93f, 0.88f, 0.77f), room);
        var wallC = new Color(0.62f, 0.55f, 0.47f);
        Box("wall_n", new Vector3(RW / 2f, 1.5f, -0.15f), new Vector3(RW + 0.6f, 3f, 0.3f), wallC, room);
        Box("wall_s", new Vector3(RW / 2f, 0.25f, RD + 0.15f), new Vector3(RW + 0.6f, 0.5f, 0.3f), wallC, room);
        Box("wall_w", new Vector3(-0.15f, 0.75f, RD / 2f), new Vector3(0.3f, 1.5f, RD), wallC, room);
        Box("wall_e", new Vector3(RW + 0.15f, 0.75f, RD / 2f), new Vector3(0.3f, 1.5f, RD), wallC, room);
        Box("door_exit", new Vector3(17.6f, 1f, 1.15f), new Vector3(1.2f, 2f, 0.25f), new Color(0.35f, 0.63f, 0.37f), room);
        // (그레이박스 창문 제거 — Tripo WindowBlock으로 대체)

        // ---- 주방 가구 (FreeKitchen 프리팹, Version03=블루) ----
        var kitchen = new GameObject("Kitchen").transform;
        var counts = new Dictionary<string, int>();
        var missing = new List<string>();
        int placed = 0;
        // Tripo 세트로 교체된 FreeKitchen 가구는 제외 (보조 소품만 유지)
        var skip = new HashSet<string> { "FreeCabinet01", "FreeCabinet02", "FreeCabinet03",
            "FreeStove", "FreeRefrigerator", "FreePotBig", "FreePotSmall" };
        foreach (var e in SKData.KIT)
        {
            if (skip.Contains(e.f)) continue;
            GameObject prefab = null;
            foreach (var ver in new[] { e.ver, "Version03", "Version01", "Version02" })
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PACK + "/Prefabs/" + ver + "/" + e.f + ".prefab");
                if (prefab != null) break;
            }
            if (prefab == null) { missing.Add(e.f); continue; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            go.transform.SetParent(kitchen, false);
            go.transform.position = e.pos;
            go.transform.rotation = Quaternion.Euler(0, e.rot, 0);
            go.transform.localScale = Vector3.one * e.scl;
            string baseName = e.f.Replace("Free", "");
            int n = counts.ContainsKey(baseName) ? counts[baseName] + 1 : 1;
            counts[baseName] = n;
            go.name = n > 1 ? baseName + n : baseName;
            placed++;
        }

        // ---- 오버쿡형 주방 (Tripo 세트) ----
        BuildTripoKitchen(kitchen);

        // ---- 가스안전 소품 (프로시저럴 잔여분) ----
        GasProps();

        // ---- 게임 ----
        var game = new GameObject("Game");
        game.AddComponent<SKMain>();

        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        Debug.Log("[SafeKitchen] 씬 생성 완료 — 가구 " + placed + "개 배치, 저장: " + SCENE_PATH
            + (missing.Count > 0 ? " / 못 찾은 프리팹: " + string.Join(", ", missing) : ""));
    }

    // ---------- Tripo 3D 에셋 헬퍼 ----------
    public static Bounds WBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    /// Models3D의 GLB를 목표 높이로 스케일해 바닥(baseY) 위에 배치
    public static GameObject Model3D(string name, Vector3 pos, float rotY, float targetH, Transform parent, float baseY = 0f)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models3D/" + name + ".glb");
        if (asset == null) { Debug.LogWarning("[SafeKitchen] 모델 없음: " + name); return null; }
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        inst.name = name;
        inst.transform.SetParent(parent, false);
        var b = WBounds(inst);
        float sc = targetH / Mathf.Max(0.001f, b.size.y);
        inst.transform.localScale = Vector3.one * sc;
        inst.transform.rotation = Quaternion.Euler(0, rotY, 0);
        var b2 = WBounds(inst);
        inst.transform.position = new Vector3(pos.x, baseY - b2.min.y + 0.001f, pos.z)
            + new Vector3(inst.transform.position.x - b2.center.x, 0, inst.transform.position.z - b2.center.z);
        return inst;
    }

    /// 밸브 조립: 몸체(폭 기준) + 레버(몸체 폭에 비례) + 회전 피벗
    static void ValveAssembly(Vector3 at, Transform parent)
    {
        var bodyAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models3D/ValveBody.glb");
        var leverAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models3D/ValveLever.glb");
        if (bodyAsset == null || leverAsset == null) { Debug.LogWarning("[SafeKitchen] 밸브 에셋 없음"); return; }
        var root = new GameObject("ValveAssembly");
        root.transform.SetParent(parent, false);
        root.transform.position = at;

        var body = (GameObject)PrefabUtility.InstantiatePrefab(bodyAsset);
        body.name = "valve_body";
        body.transform.SetParent(root.transform, false);
        var b0 = WBounds(body);
        float bs = 0.55f / Mathf.Max(0.001f, b0.size.x);
        body.transform.localScale = Vector3.one * bs;
        var b1 = WBounds(body);
        body.transform.position += at - b1.center;
        b1 = WBounds(body);

        var pivot = new GameObject("valve_pivot");
        pivot.transform.SetParent(root.transform, false);
        pivot.transform.position = new Vector3(at.x, b1.max.y - 0.015f, at.z);

        var lever = (GameObject)PrefabUtility.InstantiatePrefab(leverAsset);
        lever.name = "valve_lever";
        lever.transform.SetParent(pivot.transform, false);
        var l0 = WBounds(lever);
        float ls = 0.48f / Mathf.Max(0.001f, l0.size.x);
        lever.transform.localScale = Vector3.one * ls;
        var l1 = WBounds(lever);
        // 구멍(축) 쪽 끝을 피벗에 맞춤: -x 끝에서 살짝 안쪽
        var holeWorld = new Vector3(l1.min.x + l1.size.x * 0.10f, l1.center.y, l1.center.z);
        lever.transform.position += pivot.transform.position - holeWorld;
        // 초기 상태: 배관과 평행(열림)
        pivot.transform.localEulerAngles = new Vector3(0, 0, 0);
    }

    /// 오버쿡형 주방 (Tripo 세트 전면 배치)
    static void BuildTripoKitchen(Transform kitchen)
    {
        // 북쪽: 빌트인 일체형 조리대 (+90=문 정면, 화구는 +x 끝. 높이 1.74=수도꼭지 포함 → 상판 1.25)
        Model3D("BuiltinCounter", new Vector3(10.2f, 0, 1.9f), 90f, 1.74f, kitchen);
        // 화구 앵커 (끓음 이펙트 기준점 — 렌더러 없는 빈 노드, 이름에 Stove 포함)
        var anchor = new GameObject("StoveAnchor");
        anchor.transform.SetParent(kitchen, false);
        anchor.transform.position = new Vector3(11.7f, 1.62f, 1.9f);
        // 화구 위 대형솥
        Model3D("BigPot", new Vector3(11.7f, 0, 1.85f), 0f, 0.34f, kitchen, 1.26f);
        Model3D("FridgeMint", new Vector3(16.3f, 0, 1.8f), 0f, 2.0f, kitchen);
        // 서쪽 창문 (벽면, 방 안쪽을 보게)
        Model3D("WindowBlock", new Vector3(0.5f, 0, 5.6f), -90f, 1.25f, kitchen, 0.65f);
        // 남쪽 조리대 라인: 긴 서랍장 2연장
        Model3D("DrawerCounterLong", new Vector3(6.3f, 0, 9.1f), 90f, 1.25f, kitchen);
        Model3D("DrawerCounterLong", new Vector3(12.0f, 0, 9.1f), 90f, 1.25f, kitchen);
        // 소품
        Model3D("FireExtinguisherKR", new Vector3(17.3f, 0, 8.4f), 0f, 0.9f, kitchen);
        Model3D("IngredientCrate", new Vector3(1.3f, 0, 8.5f), 0f, 0.5f, kitchen);
        // 밸브 조립 (배관 라인 위, 화구 근처)
        ValveAssembly(new Vector3(13.2f, 1.66f, 0.42f), kitchen);
        Debug.Log("[SafeKitchen] 오버쿡 주방 배치 완료 (Tripo 세트)");
    }

    /// 근접 QA 스크린샷 (임시 카메라)
    public static void SnapAt(Vector3 pos, Vector3 look, string file)
    {
        var go = new GameObject("qa_cam");
        var c = go.AddComponent<Camera>();
        c.transform.position = pos;
        c.transform.LookAt(look);
        c.fieldOfView = 35f;
        c.nearClipPlane = 0.05f;
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = new Color(0.3f, 0.35f, 0.42f);
        var rt = new RenderTexture(960, 720, 24);
        var req = new UnityEngine.Rendering.RenderPipeline.StandardRequest();
        req.destination = rt;
        UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(c, req);
        RenderTexture.active = rt;
        var tex = new Texture2D(960, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 960, 720), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        System.IO.File.WriteAllBytes(file, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(go);
    }

    // ---------- 가스안전 임시 소품 (프리미티브 조립, 카툰 단색) ----------
    static GameObject Prim(PrimitiveType t, string name, Transform parent, Vector3 pos, Vector3 scale, Color c)
    {
        var go = GameObject.CreatePrimitive(t);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = Lit(c);
        return go;
    }

    static void Tube(Transform parent, string name, Vector3 a, Vector3 b, float r, Color c)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = (a + b) * 0.5f;
        go.transform.rotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
        go.transform.localScale = new Vector3(r * 2f, (b - a).magnitude * 0.5f, r * 2f);
        go.GetComponent<Renderer>().sharedMaterial = Lit(c);
    }

    static void GasProps()
    {
        var root = new GameObject("GasProps").transform;
        var pipeC = new Color(0.63f, 0.65f, 0.70f);
        var brass = new Color(0.80f, 0.63f, 0.30f);
        var leverY = new Color(1f, 0.80f, 0.16f);

        // 1) 가스 배관(북벽) — 중간밸브는 Tripo ValveAssembly로 대체됨
        Tube(root, "gas_pipe", new Vector3(9.8f, 1.66f, 0.42f), new Vector3(15.8f, 1.66f, 0.42f), 0.055f, pipeC);
        Tube(root, "gas_pipe_drop", new Vector3(12.5f, 1.66f, 0.42f), new Vector3(12.5f, 1.10f, 0.42f), 0.05f, pipeC);

        // 2) 부탄버너 + 과대불판 + 부탄캔 (남쪽 조리대, butane 위험 목표)
        var bb = new Vector3(11.2f, 1.26f, 8.9f);
        Prim(PrimitiveType.Cube, "butane_base", root, bb + new Vector3(0, 0.07f, 0), new Vector3(0.52f, 0.14f, 0.36f), new Color(0.22f, 0.28f, 0.38f));
        Prim(PrimitiveType.Cube, "butane_grill", root, bb + new Vector3(0, 0.155f, 0), new Vector3(0.44f, 0.03f, 0.30f), new Color(0.45f, 0.47f, 0.50f));
        Prim(PrimitiveType.Cylinder, "butane_bigpan", root, bb + new Vector3(0, 0.21f, 0), new Vector3(0.72f, 0.035f, 0.72f), new Color(0.30f, 0.30f, 0.32f));
        var can = Prim(PrimitiveType.Cylinder, "butane_can", root, bb + new Vector3(0.34f, 0.08f, 0.05f), new Vector3(0.12f, 0.13f, 0.12f), new Color(0.95f, 0.55f, 0.20f));
        can.transform.localEulerAngles = new Vector3(0, 0, 90);

        // 3) 프라이팬 (남쪽 조리대, oil 위험 지점)
        var pp = new Vector3(4.8f, 1.27f, 8.95f);
        Prim(PrimitiveType.Cylinder, "pan", root, pp, new Vector3(0.36f, 0.028f, 0.36f), new Color(0.25f, 0.25f, 0.27f));
        Prim(PrimitiveType.Cube, "pan_handle", root, pp + new Vector3(0.26f, 0.01f, 0), new Vector3(0.22f, 0.03f, 0.05f), new Color(0.18f, 0.18f, 0.20f));

        // 4) 노후 호스 (배관→탁상 가스레인지 뒤, hose 위험 목표)
        var hoseC = new Color(0.35f, 0.42f, 0.30f);
        Tube(root, "hose_a", new Vector3(12.5f, 1.10f, 0.5f), new Vector3(12.3f, 1.05f, 1.0f), 0.045f, hoseC);
        Tube(root, "hose_b", new Vector3(12.3f, 1.05f, 1.0f), new Vector3(12.1f, 1.28f, 1.4f), 0.045f, hoseC);

        // 5) 행주 (가스레인지 옆 조리대, towel 위험 목표)
        var tw = Prim(PrimitiveType.Cube, "towel", root, new Vector3(10.6f, 1.27f, 2.15f), new Vector3(0.30f, 0.025f, 0.40f), new Color(0.93f, 0.45f, 0.40f));
        tw.transform.localEulerAngles = new Vector3(0, 18f, 0);
        Debug.Log("[SafeKitchen] 가스 소품 배치 (임시 — Tripo 교체 대상): 배관+밸브, 부탄버너 세트, 팬, 호스, 행주");
    }

    /// 플레이 모드에서 게임 화면을 파일로 저장 (Claude 자가검수용)
    public static void Snap()
    {
        string path = "Assets/Package04_Free_Kitchen/_SafeKitchen/qa_shot.png";
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log("[SafeKitchen] QA 스크린샷 예약: " + path);
    }
}
