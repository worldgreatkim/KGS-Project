using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Video;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

/// 입력 헬퍼 — 구/신 입력 시스템 양쪽 지원
public static class SKIn
{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    static ButtonControl B(KeyCode k)
    {
        var kb = Keyboard.current;
        if (kb == null) return null;
        switch (k)
        {
            case KeyCode.LeftArrow: return kb.leftArrowKey;
            case KeyCode.RightArrow: return kb.rightArrowKey;
            case KeyCode.UpArrow: return kb.upArrowKey;
            case KeyCode.DownArrow: return kb.downArrowKey;
            case KeyCode.A: return kb.aKey;
            case KeyCode.D: return kb.dKey;
            case KeyCode.W: return kb.wKey;
            case KeyCode.S: return kb.sKey;
            case KeyCode.R: return kb.rKey;
            case KeyCode.Space: return kb.spaceKey;
            case KeyCode.Return: return kb.enterKey;
            case KeyCode.Alpha1: return kb.digit1Key;
            case KeyCode.Alpha2: return kb.digit2Key;
            case KeyCode.Alpha3: return kb.digit3Key;
            case KeyCode.Keypad1: return kb.numpad1Key;
            case KeyCode.Keypad2: return kb.numpad2Key;
            case KeyCode.Keypad3: return kb.numpad3Key;
            case KeyCode.LeftShift: return kb.leftShiftKey;
            case KeyCode.RightShift: return kb.rightShiftKey;
            case KeyCode.F1: return kb.f1Key;
            case KeyCode.V: return kb.vKey;
            case KeyCode.Escape: return kb.escapeKey;
            case KeyCode.T: return kb.tKey;
            case KeyCode.G: return kb.gKey;
        }
        return null;
    }
    public static bool Held(KeyCode k) { var b = B(k); return b != null && b.isPressed; }
    public static bool Down(KeyCode k) { var b = B(k); return b != null && b.wasPressedThisFrame; }
    public static bool MouseDown() { var m = Mouse.current; return m != null && m.leftButton.wasPressedThisFrame; }
    public static bool MouseHeld() { var m = Mouse.current; return m != null && m.leftButton.isPressed; }
    public static float MouseDX() { var m = Mouse.current; return m != null ? m.delta.ReadValue().x * 0.05f : 0f; }
    public static float MouseDY() { var m = Mouse.current; return m != null ? m.delta.ReadValue().y * 0.05f : 0f; }
#else
    public static bool Held(KeyCode k) { return Input.GetKey(k); }
    public static bool Down(KeyCode k) { return Input.GetKeyDown(k); }
    public static bool MouseDown() { return Input.GetMouseButtonDown(0); }
    public static bool MouseHeld() { return Input.GetMouseButton(0); }
    public static float MouseDX() { return Input.GetAxis("Mouse X"); }
    public static float MouseDY() { return Input.GetAxis("Mouse Y"); }
#endif
}

/// 세이프 키친 3D — 본편 (Godot main3d.gd + ui3d.gd 이식)
/// 씬의 주방 배치는 에디터에서 수정, 게임 로직·UI·이펙트는 이 스크립트가 소유.
public partial class SKMain : MonoBehaviour
{
    class Hz
    {
        public int id; public string type; public SKData.Ev def;
        public GameObject node; public TextMesh bang;
        public float t; public float ttl; public bool retry;
        public float reach = SKData.INTERACT_D;   // 상호작용 거리 (밸브 등 개별 조정)
        public List<SKData.Opt> opts;
    }

    // 상태
    Transform player, pbody;
    Camera cam;
    readonly List<Hz> hazards = new List<Hz>();
    int uid, score, combo; float comboT;
    int acha;
    float stageT, spawnT = 1.5f, timeAll;
    bool over;
    Hz openEv;
    string msg = ""; float msgT;
    Transform boilFoam; float boilBaseY; Vector3 boilPos;
    Transform valvePivot;   // 가스밸브 레버 축 (0=열림, 90=잠김)
    GameObject windowOpen, windowClosed;   // 창문 개폐 스왑
    Coroutine windowCo;
    // 캐릭터 애니메이션 (Tripo 리깅: 0=대기 1=걷기 2=뛰기)
    Animator pAnim;
    UnityEngine.Playables.PlayableGraph pGraph;
    UnityEngine.Animations.AnimationClipPlayable pClipPlayable;
    AnimationClip clipIdle, clipWalk, clipRun;   // 임포터에서 분할한 루프 클립
    bool hasAnim, graphAlive;
    int animState = -1;
    float curClipLen;
    // 소화기 들기
    readonly List<Transform> extList = new List<Transform>();
    Transform carryExt;
    float carryY;
    Vector3 extHomeScale;
    // 화구 불꽃 (0=냄비 아래, 1=빈 화구 — yellow 위험 대상)
    readonly ParticleSystem[] flames = new ParticleSystem[2];
    readonly ParticleSystem[] flameSmoke = new ParticleSystem[2];
    readonly Light[] flameLights = new Light[2];
    // 지진 시퀀스: 0=대기 1=흔들림(대피) 2=밸브(가스화재) 3=소화기(일반화재) 4=완료
    // 타이틀 화면
    bool titleOpen = true;
    GameObject titleGo;
    RectTransform titleLogoRt;
    Text titleStart;
    float titleT;                 // 타이틀 경과 시간 (연출 타이밍)
    VideoPlayer titleVp;          // 키비주얼 영상 (1초 지점부터 루프)
    RawImage titleVidImg;
    Image titleStartBg;
    Text titleCredit;
    const float VID_START = 1.0f; // 영상 시작·루프 지점(초)

    int quakeState; float quakeT; bool quakeDone;
    int quakeScen;                 // 1=화재 시나리오(1차), 2=가스누출 시나리오(2차)
    bool quake2Done;               // 2차 지진(누출) 완료
    bool qValveLocked, qWindowOpen; // 누출 시나리오 과제 2종
    readonly List<Transform> gasClouds = new List<Transform>();   // 바닥 가스 구름들 (CFXR)
    bool gasAdding;                // 누출 진행 중 (밸브 잠그면 중단)
    Vector3 camBase, shelterPos;
    GameObject arrowGo; float arrowBaseY;
    ParticleSystem dustPs;
    readonly List<ParticleSystem> firePsL = new List<ParticleSystem>();
    readonly List<ParticleSystem> fireSmokeL = new List<ParticleSystem>();
    readonly List<Light> fireLightL = new List<Light>();
    readonly List<Vector3> firePosL = new List<Vector3>();
    bool quizOpen, quizSolved, spraying;
    readonly List<bool> fireOutL = new List<bool>();
    readonly List<float> fireHpL = new List<float>();   // 불 체력 (쓸기 분사로 감소)
    // 정전 연출 백업
    Color ambBackup; Color camBgBackup;
    Light sunLight; float sunBackup = -1f;
    readonly List<Light> lampL = new List<Light>();
    readonly List<float> lampBackup = new List<float>();

    // UI
    Font font;
    Canvas canvas;
    Text uiTimer, uiScore, uiCombo, uiAcha, uiToast, uiQ, uiPrompt;
    GameObject pnCombo, pnToast, pnPrompt, pnChoice, dim;
    readonly Text[] optTexts = new Text[3];
    readonly GameObject[] optRows = new GameObject[3];
    class Fl { public Text t; public float life; public Vector3 world; }
    readonly List<Fl> floats = new List<Fl>();

    static Material Lit(Color c)
    {
        Shader sh = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null
            ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
        var m = new Material(sh);
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        return m;
    }

    static Material Flat(Color c)
    {
        var m = new Material(Shader.Find("Sprites/Default"));
        m.color = c;
        return m;
    }

    void Awake()
    {
        cam = Camera.main;
        DetectMap();   // 씬(맵 버전) 판정 — 좌표·시점 분기의 기준
        bool modKit = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.EndsWith("_MOD");
        font = LoadFont();
        BuildPlayer();
        BuildUI();
        var stv = FindKitchen("Stove", "Hood");
        if (stv != null)
        {
            var rs = stv.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                // MOD 모듈 스토브는 화구가 좌우 2구 — 김·불꽃을 서쪽 화구 중심에
                boilPos = modKit
                    ? new Vector3(b.center.x - 0.45f, b.max.y + 0.10f, b.center.z + 0.06f)
                    : new Vector3(b.center.x - 0.25f, b.max.y + 0.16f, b.center.z - 0.05f);
            }
            else boilPos = stv.position;   // 빈 앵커 노드(StoveAnchor)면 그 위치 그대로
        }
        else boilPos = Pz(new Vector3(11.35f, 1.85f, 1.9f), new Vector3(16.15f, 1.85f, 1.9f));
        BuildBoilFx(boilPos);
        var vp = GameObject.Find("valve_pivot");
        if (vp != null) valvePivot = vp.transform;
        // 창문: 평소 닫힘, 환기 정답 시 열림 (비활성 포함 탐색)
        var kroot = GameObject.Find("Kitchen");
        if (kroot != null)
        {
            foreach (var t in kroot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "WindowOpen") windowOpen = t.gameObject;
                else if (t.name == "WindowClosed") windowClosed = t.gameObject;
            }
            if (windowOpen != null && windowClosed != null)
            {
                windowOpen.SetActive(false);
                windowClosed.SetActive(true);
            }
        }
        BuildColliders();
        // 소화기 목록 (들기 대상)
        var kk = GameObject.Find("Kitchen");
        if (kk != null)
            foreach (Transform t in kk.transform)
                if (t.name.Contains("FireExtinguisher")) extList.Add(t);
        // 화구 불꽃: 냄비 아래 + 빈 화구 (yellow 위험이 이 화구를 노랗게 만든다)
        // MOD 씬은 모듈 스토브의 화구 트레이가 높아(상판 정렬 보정) 불꽃도 따라 올림
        float flameY = modKit ? 1.34f : 1.28f;
        BuildFlame(0, new Vector3(boilPos.x, flameY, boilPos.z - 0.05f));
        BuildFlame(1, new Vector3(HzPos("yellow").x + (modKit ? 0.45f : 0f), flameY, 1.9f));
        SetFlame(0, false);
        SetFlame(1, false);
        BuildTitle();
        BuildTutUI();
        BuildMgUI();
        BuildAimCone();
        FpInit();
        UxInit();
        SKSound.Init(gameObject);
        SKSound.Music("bgm_title", 0.45f);
        SKSound.Loop(0, "amb_boil", 0.30f);
        SKSound.Loop(1, "amb_flame", 0.18f);
        // (부팅 안내는 타이틀·튜토리얼이 담당 — 대화창과 겹치는 토스트 제거)
    }

    static Font LoadFont()
    {
        try { var f = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 24); if (f != null) return f; }
        catch { }
        try { var f2 = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); if (f2 != null) return f2; }
        catch { }
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    Transform FindKitchen(string contains, string exclude)
    {
        var kitchen = GameObject.Find("Kitchen");
        if (kitchen == null) return null;
        foreach (Transform c in kitchen.transform)
            if (c.name.Contains(contains) && (exclude == null || !c.name.Contains(exclude)))
                return c;
        return null;
    }

    // ---------- 빌드 ----------
    /// 주방 가구에 자동 충돌 박스 생성 (대피 탁자·카펫·벽부착물 제외)
    void BuildColliders()
    {
        var kroot = GameObject.Find("Kitchen");
        if (kroot == null) return;
        foreach (Transform child in kroot.transform)
        {
            var n = child.name;
            if (n.Contains("ShelterTable") || n.Contains("Carpet") || n.Contains("Window")
                || n.Contains("Hood") || n.Contains("Valve") || n.Contains("StoveAnchor")
                || n.Contains("Stool") || n.Contains("Cup") || n.Contains("FireExtinguisher")) continue;
            var rs = child.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) continue;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            if (b.size.y < 0.8f || b.size.x < 0.3f || b.size.z < 0.3f) continue;   // 소품 스킵
            if (b.min.y > 0.85f) continue;   // 벽 상부 부착물 스킵
            var cgo = new GameObject("col_" + n);
            cgo.transform.position = b.center;
            var bc = cgo.AddComponent<BoxCollider>();
            bc.size = new Vector3(b.size.x * 0.94f, b.size.y, b.size.z * 0.94f);
        }
    }

    /// 이동 차단 판정 (허리 높이 박스)
    bool Blocked(Vector3 at)
    {
        return Physics.CheckBox(at + new Vector3(0, 0.55f, 0), new Vector3(0.26f, 0.34f, 0.26f), Quaternion.identity);
    }

    void BuildPlayer()
    {
        var root = new GameObject("Player");
        player = root.transform;
        player.position = Pz(new Vector3(6f, 0f, 6.2f), new Vector3(12f, 0f, 8.5f));
        pbody = new GameObject("Body").transform;
        pbody.SetParent(player, false);

        // KGS 가스레인저 모델이 있으면 그걸 사용 (콩콩 애니는 pbody가 담당)
        GameObject charPrefab = null;
        Texture2D charTex = null;
        // Resources에서 로드 (에디터·빌드 공통)
        charPrefab = Resources.Load<GameObject>("Char/RangerRed");
        foreach (var o in Resources.LoadAll("Char/RangerRed"))
        {
            var c = o as AnimationClip;
            if (c == null || c.name.StartsWith("__preview")) continue;
            if (c.name == "SegA") clipRun = c;        // 제작 순서: 달리기
            else if (c.name == "SegB") clipWalk = c;  // 걷기
            else if (c.name == "SegC") clipIdle = c;  // 대기
        }
        foreach (var t in Resources.LoadAll<Texture2D>("Char/RangerRedTex"))
        {
            charTex = t;
            if (t.name.ToLower().Contains("rgb")) break;
        }
        if (charPrefab != null)
        {
            var inst = Instantiate(charPrefab, pbody);
            inst.name = "ranger";
            inst.transform.localEulerAngles = new Vector3(0, 0f, 0);   // FBX 정면 보정 (0=정면)
            var rs = inst.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            float sc = 1.5f / Mathf.Max(0.001f, b.size.y);   // 3등신 원본이라 균등 스케일
            inst.transform.localScale = Vector3.one * sc;
            var rs2 = inst.GetComponentsInChildren<Renderer>();
            var b2 = rs2[0].bounds;
            foreach (var r in rs2) b2.Encapsulate(r.bounds);
            inst.transform.position += new Vector3(player.position.x - b2.center.x, -b2.min.y, player.position.z - b2.center.z);
            // FBX 재질 미연결 대응: 추출한 텍스처로 URP 재질 직접 입힘
            if (charTex != null)
            {
                var mat = Lit(Color.white);
                mat.mainTexture = charTex;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", charTex);
                foreach (var r in rs2) r.material = mat;
            }
            pAnim = inst.GetComponentInChildren<Animator>();
            if (pAnim == null) pAnim = inst.AddComponent<Animator>();
            pAnim.applyRootMotion = false;
            hasAnim = clipWalk != null;
            return;
        }

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Destroy(body.GetComponent<Collider>());
        body.name = "torso";
        body.transform.SetParent(pbody, false);
        body.transform.localPosition = new Vector3(0, 0.55f, 0);
        body.transform.localScale = new Vector3(0.68f, 0.5f, 0.68f);
        body.GetComponent<Renderer>().material = Lit(new Color(0.50f, 0.82f, 0.71f));

        var helm = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        helm.name = "helmet";
        helm.transform.SetParent(pbody, false);
        helm.transform.localPosition = new Vector3(0, 1.12f, 0);
        helm.transform.localScale = new Vector3(0.6f, 0.42f, 0.6f);
        helm.GetComponent<Renderer>().material = Lit(new Color(0.95f, 0.76f, 0.31f));

        for (int i = 0; i < 2; i++)
        {
            var eye = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eye.name = "eye";
            eye.transform.SetParent(pbody, false);
            eye.transform.localPosition = new Vector3(i == 0 ? -0.12f : 0.12f, 0.82f, 0.3f);
            eye.transform.localScale = new Vector3(0.07f, 0.12f, 0.05f);
            eye.GetComponent<Renderer>().material = Lit(new Color(0.17f, 0.17f, 0.17f));
        }
    }

    void BuildBoilFx(Vector3 at)
    {
        var go = new GameObject("steam");
        go.transform.position = at + new Vector3(0, 0.1f, 0);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 1.6f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.24f);
        main.startColor = new Color(1, 1, 1, 0.38f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = 15f;
        var shp = ps.shape; shp.shapeType = ParticleSystemShapeType.Cone;
        shp.angle = 9f; shp.radius = 0.13f;
        var psr = go.GetComponent<ParticleSystemRenderer>();
        psr.material = Flat(new Color(1, 1, 1, 0.4f));

        var foam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        foam.name = "boil_foam";
        Destroy(foam.GetComponent<Collider>());
        foam.transform.position = at;
        foam.transform.localScale = new Vector3(0.38f, 0.025f, 0.38f);
        foam.GetComponent<Renderer>().material = Flat(new Color(0.97f, 0.97f, 1f, 0.95f));
        boilFoam = foam.transform;
        boilBaseY = at.y;
    }

    /// 화구 불꽃 링 + 연기 + 조명 생성
    void BuildFlame(int idx, Vector3 at)
    {
        var go = new GameObject("flame" + idx);
        go.transform.position = at;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.28f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = 110f;
        var shp = ps.shape;
        shp.shapeType = ParticleSystemShapeType.Circle;
        shp.radius = 0.15f;
        shp.radiusThickness = 0.25f;
        shp.rotation = new Vector3(-90f, 0, 0);
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);
        var psr = go.GetComponent<ParticleSystemRenderer>();
        psr.material = Flat(Color.white);
        flames[idx] = ps;
        // 연기 (노란 불꽃일 때만)
        var sgo = new GameObject("flame_smoke" + idx);
        sgo.transform.position = at + new Vector3(0, 0.15f, 0);
        var sp = sgo.AddComponent<ParticleSystem>();
        var sm = sp.main;
        sm.startLifetime = 1.3f;
        sm.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        sm.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        sm.startColor = new Color(0.2f, 0.2f, 0.22f, 0.5f);
        var sem = sp.emission; sem.rateOverTime = 10f;
        var ssh = sp.shape; ssh.shapeType = ParticleSystemShapeType.Sphere; ssh.radius = 0.08f;
        var spr = sgo.GetComponent<ParticleSystemRenderer>();
        spr.material = Flat(new Color(0.2f, 0.2f, 0.22f, 0.5f));
        flameSmoke[idx] = sp;
        // 조명
        var lgo = new GameObject("flame_light" + idx);
        lgo.transform.position = at + new Vector3(0, 0.2f, 0);
        var l = lgo.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 1.3f;
        l.intensity = 1.1f;
        flameLights[idx] = l;
    }

    /// 불꽃 상태 전환: false=파랑(정상) true=노랑(불완전연소)
    void SetFlame(int idx, bool yellow)
    {
        if (flames[idx] == null) return;
        var main = flames[idx].main;
        main.startColor = yellow
            ? new ParticleSystem.MinMaxGradient(new Color(1f, 0.75f, 0.2f), new Color(1f, 0.45f, 0.1f))
            : new ParticleSystem.MinMaxGradient(new Color(0.3f, 0.55f, 1f), new Color(0.15f, 0.35f, 0.95f));
        main.startSize = yellow
            ? new ParticleSystem.MinMaxCurve(0.07f, 0.15f)
            : new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
        flameLights[idx].color = yellow ? new Color(1f, 0.65f, 0.25f) : new Color(0.4f, 0.6f, 1f);
        var sem = flameSmoke[idx].emission;
        sem.enabled = yellow;
        if (!yellow) flameSmoke[idx].Clear();
    }

    // 위험 표시는 떠 있는 물음표 하나만 (상자·바닥판 없음)
    GameObject SpawnMarker(Vector3 at)
    {
        var root = new GameObject("hazard");
        root.transform.position = at;
        return root;
    }

    TextMesh SpawnBang(GameObject root)
    {
        var tgo = new GameObject("bang");
        tgo.transform.SetParent(root.transform, false);
        tgo.transform.localPosition = new Vector3(0, 0.6f, 0);
        var tm = tgo.AddComponent<TextMesh>();
        tm.text = "?";
        tm.font = font;
        tm.fontSize = 90;
        tm.characterSize = 0.18f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = new Color(0.90f, 0.38f, 0.35f);
        tm.fontStyle = FontStyle.Bold;
        var mr = tgo.GetComponent<MeshRenderer>();
        mr.material = font.material;
        return tm;
    }

    // ---------- UI ----------
    RectTransform Panel(Transform parent, float x, float y, float w, float h, Color c, out Image img)
    {
        var go = new GameObject("panel");
        go.transform.SetParent(parent, false);
        img = go.AddComponent<Image>();
        img.color = c;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    Text Label(Transform parent, string s, int size, Color c, TextAnchor align = TextAnchor.MiddleCenter, bool bold = false)
    {
        var go = new GameObject("label");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.text = s;
        t.fontSize = size;
        t.color = c;
        t.alignment = align;
        t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return t;
    }

    static readonly Color NAVY = new Color(0.20f, 0.28f, 0.36f);
    static readonly Color CORAL = new Color(0.90f, 0.38f, 0.35f);
    static readonly Color MINT = new Color(0.74f, 0.91f, 0.85f);
    static readonly Color YELLOW = new Color(1f, 0.84f, 0.31f);

    /// 타이틀 화면: 배경 + 로고(투명화) + 시작 안내. SPACE로 게임 시작
    void BuildTitle()
    {
        var cgo = new GameObject("SKTitle");
        var cv = cgo.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 50;
        var sc = cgo.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1280, 720);
        sc.matchWidthOrHeight = 0.5f;
        titleGo = cgo;
        var root = cgo.transform;

        Sprite bg = Resources.Load<Sprite>("UI/MainBg");
        Sprite logo = Resources.Load<Sprite>("UI/TitleLogo");
        // 배경 — 화면 꽉 채움
        var bgo = new GameObject("bg");
        bgo.transform.SetParent(root, false);
        var bim = bgo.AddComponent<Image>();
        if (bg != null) bim.sprite = bg;
        else bim.color = new Color(0.85f, 0.93f, 0.90f);
        var brt = bgo.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;
        // 키비주얼 영상 — 배경 위에서 재생 (1초 지점부터, 없으면 정지 배경 유지)
        VideoClip vclip = Resources.Load<VideoClip>("UI/MainVideo");
        if (vclip != null)
        {
            var vgo = new GameObject("vid");
            vgo.transform.SetParent(root, false);
            titleVidImg = vgo.AddComponent<RawImage>();
            titleVidImg.color = new Color(1, 1, 1, 0);   // 재생 시작되면 페이드 인
            var vrt = vgo.GetComponent<RectTransform>();
            vrt.anchorMin = vrt.anchorMax = new Vector2(0.5f, 0.5f);
            var arf = vgo.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            arf.aspectRatio = (float)vclip.width / vclip.height;
            var vtex = new RenderTexture((int)vclip.width, (int)vclip.height, 0);
            titleVp = vgo.AddComponent<VideoPlayer>();
            titleVp.clip = vclip;
            titleVp.renderMode = VideoRenderMode.RenderTexture;
            titleVp.targetTexture = vtex;
            titleVp.audioOutputMode = VideoAudioOutputMode.None;
            titleVp.isLooping = false;
            titleVp.playOnAwake = false;
            titleVp.loopPointReached += OnTitleVideoEnd;
            titleVp.prepareCompleted += OnTitleVideoReady;
            titleVidImg.texture = vtex;
            titleVp.Prepare();
        }
        // 로고 — 상단 중앙, 둥실둥실
        var lgo = new GameObject("logo");
        lgo.transform.SetParent(root, false);
        var lim = lgo.AddComponent<Image>();
        if (logo != null) { lim.sprite = logo; lim.preserveAspect = true; }
        else lim.color = Color.clear;
        titleLogoRt = lgo.GetComponent<RectTransform>();
        titleLogoRt.anchorMin = titleLogoRt.anchorMax = new Vector2(0.5f, 0.68f);
        titleLogoRt.sizeDelta = new Vector2(680, 380);
        titleLogoRt.localScale = Vector3.zero;   // 1.2초에 통통 등장
        // 시작 안내 (하단 기준 앵커 — 어떤 화면비에서도 안 잘리게)
        Image im;
        var spr = Panel(root, 0, 0, 340, 58, new Color(0.16f, 0.22f, 0.30f, 0f), out im);
        spr.anchorMin = spr.anchorMax = new Vector2(0.5f, 0f);
        spr.pivot = new Vector2(0.5f, 0f);
        spr.anchoredPosition = new Vector2(0, 52);
        titleStartBg = im;
        titleStart = Label(im.transform, "[SPACE] 시작하기!", 24, new Color(1, 1, 1, 0), TextAnchor.MiddleCenter, true);
        // 하단 크레딧 (하단 기준 앵커)
        var crt = Panel(root, 0, 0, 900, 22, Color.clear, out im);
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0f);
        crt.pivot = new Vector2(0.5f, 0f);
        crt.anchoredPosition = new Vector2(0, 16);
        titleCredit = Label(im.transform, "한국가스안전공사 「가스안전 AI 게임·영상 공모전」 — 세이프 키친", 13, new Color(0.25f, 0.30f, 0.35f, 0f));
        BuildTitleBadges(root);   // 배지 도감 슬롯 (수집 현황)
    }

    void OnTitleVideoReady(VideoPlayer v)
    {
        v.time = VID_START;
        v.Play();
    }

    /// 영상 끝 → 1초 지점으로 되감아 루프
    void OnTitleVideoEnd(VideoPlayer v)
    {
        if (!titleOpen) return;
        v.time = VID_START;
        v.Play();
    }

    void BuildUI()
    {
        var cgo = new GameObject("SKCanvas");
        canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = cgo.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1280, 720);
        sc.matchWidthOrHeight = 0.5f;
        var root = canvas.transform;

        Image im;
        Panel(root, 20, 14, 170, 44, NAVY, out im);
        uiTimer = Label(im.transform, "60초", 22, Color.white, TextAnchor.MiddleCenter, true);
        Panel(root, 1280 - 240, 14, 220, 44, new Color(1, 1, 1, 0.92f), out im);
        uiScore = Label(im.transform, "점수 0", 21, CORAL, TextAnchor.MiddleCenter, true);
        var rc = Panel(root, 1280 - 240, 64, 130, 38, new Color(1f, 0.54f, 0.50f), out im);
        pnCombo = rc.gameObject;
        uiCombo = Label(im.transform, "콤보 ×2", 19, Color.white, TextAnchor.MiddleCenter, true);
        pnCombo.SetActive(false);
        Panel(root, 20, 64, 150, 38, new Color(1, 1, 1, 0.85f), out im);
        uiAcha = Label(im.transform, "아차 0", 18, new Color(0.54f, 0.59f, 0.65f));
        // (하단 개발용 안내 문구 제거 — 제출용 화면 정리)

        // ---------- 안전 퀴즈: 오버쿡드 칠판 스타일 (일시정지 + 강딤 + 칠판 패널) ----------
        var dr = Panel(root, 0, 0, 1280, 720, new Color(0.02f, 0.03f, 0.05f, 0.78f), out im);
        dim = dr.gameObject;
        var pc = new GameObject("choice");
        pc.transform.SetParent(root, false);
        var pcr = pc.AddComponent<RectTransform>();
        pcr.anchorMin = pcr.anchorMax = new Vector2(0.5f, 0.5f);
        pcr.pivot = new Vector2(0.5f, 0.5f);
        pcr.anchoredPosition = Vector2.zero;
        pcr.sizeDelta = new Vector2(1280, 720);
        pnChoice = pc;
        var CHALK = new Color(0.95f, 0.93f, 0.85f);          // 분필색
        var BOARD = new Color(0.15f, 0.20f, 0.18f, 0.99f);   // 칠판
        var WOOD = new Color(0.45f, 0.30f, 0.17f);           // 나무 테두리
        var CREAM = new Color(0.93f, 0.89f, 0.79f);
        CPanel(pc.transform, 0, 8, 760, 540, WOOD, out im);          // 나무 프레임
        CPanel(pc.transform, 0, 8, 728, 508, BOARD, out im);         // 칠판
        // 종이 태그 "안전 퀴즈!" (우상단, 살짝 기울임)
        var tagRt = CPanel(pc.transform, 292, 236, 170, 60, CREAM, out im);
        tagRt.localRotation = Quaternion.Euler(0, 0, -7f);
        Label(im.transform, "안전 퀴즈!", 22, NAVY, TextAnchor.MiddleCenter, true);
        // 질문 (분필 글씨) + 밑줄
        CPanel(pc.transform, 0, 190, 660, 60, Color.clear, out im);
        uiQ = Label(im.transform, "", 27, CHALK, TextAnchor.MiddleCenter, true);
        CPanel(pc.transform, 0, 152, 340, 4, new Color(CHALK.r, CHALK.g, CHALK.b, 0.75f), out im);
        // 선택지 카드 3장
        for (int i = 0; i < 3; i++)
        {
            var row = CPanel(pc.transform, 0, 66 - i * 92, 580, 76, CREAM, out im);
            optRows[i] = row.gameObject;
            Image bim;
            var badge = CPanel(row, -252, 0, 48, 48, Color.white, out bim);
            bim.sprite = SprCircle(64, Color.white);
            bim.color = YELLOW;
            Label(badge, (i + 1).ToString(), 24, NAVY, TextAnchor.MiddleCenter, true);
            var txt = new GameObject("t");
            txt.transform.SetParent(row, false);
            optTexts[i] = txt.AddComponent<Text>();
            optTexts[i].font = font; optTexts[i].fontSize = 21; optTexts[i].color = NAVY;
            optTexts[i].alignment = TextAnchor.MiddleLeft; optTexts[i].fontStyle = FontStyle.Bold;
            optTexts[i].horizontalOverflow = HorizontalWrapMode.Overflow;
            var trt = txt.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(78, 0); trt.offsetMax = Vector2.zero;
        }
        CPanel(pc.transform, 0, -222, 500, 28, Color.clear, out im);
        Label(im.transform, "숫자키  1 · 2 · 3  으로 골라 봐!", 19, new Color(CHALK.r, CHALK.g, CHALK.b, 0.9f), TextAnchor.MiddleCenter, true);
        dim.SetActive(false);
        pnChoice.SetActive(false);

        // 토스트(체키 메시지)
        var tr = Panel(root, 280, 620, 720, 46, new Color(1, 1, 1, 0.95f), out im);
        pnToast = tr.gameObject;
        uiToast = Label(im.transform, "", 20, NAVY);
        pnToast.SetActive(false);

        // 프롬프트
        var pr = Panel(root, 0, 0, 240, 40, new Color(0.2f, 0.28f, 0.36f, 0.9f), out im);
        pnPrompt = pr.gameObject;
        uiPrompt = Label(im.transform, "[SPACE] 살펴보기", 19, YELLOW, TextAnchor.MiddleCenter, true);
        pnPrompt.SetActive(false);
    }

    Vector2 ToUI(Vector3 world)
    {
        var sp = cam.WorldToScreenPoint(world);
        float s = canvas.scaleFactor;
        return new Vector2(sp.x / s, -(Screen.height - sp.y) / s);
    }

    void AddFloat(Vector3 world, string s)
    {
        var go = new GameObject("float");
        go.transform.SetParent(canvas.transform, false);
        var t = go.AddComponent<Text>();
        t.font = font; t.text = s;
        // 점수 팝 크기: 랜덤 + 콤보 보정 (타격감)
        t.fontSize = Mathf.RoundToInt(26f * Random.Range(0.95f, 1.3f)) + Mathf.Min(combo, 5) * 2;
        t.color = CORAL;
        t.fontStyle = FontStyle.Bold; t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(200, 40);
        floats.Add(new Fl { t = t, life = 1.4f, world = world + new Vector3(0, 1.2f, 0) });
    }

    void Say(string m, float t = 2.6f) { msg = m; msgT = t; }

    // ---------- 게임 로직 ----------
    Hz Nearest()
    {
        Hz best = null; float bd = float.MaxValue;
        foreach (var hz in hazards)
        {
            var p = hz.node.transform.position;
            float d = new Vector2(player.position.x - p.x, player.position.z - p.z).magnitude;
            if (d < hz.reach && d < bd && FacingPoint(p, 62f)) { bd = d; best = hz; }
        }
        return best;
    }

    void TrySpawn()
    {
        if (hazards.Count >= 3) return;
        var cand = new List<string>();
        foreach (var k in SKData.EV.Keys)
        {
            bool active = false;
            foreach (var hz in hazards) if (hz.type == k) active = true;
            if (!active && SKData.HZ.ContainsKey(k)) cand.Add(k);
        }
        if (cand.Count == 0) return;
        string type = cand[Random.Range(0, cand.Count)];
        var def = SKData.EV[type];
        Vector3 at = HzPos(type);
        float reach = SKData.INTERACT_D;
        // 가스 누출(비눗물 점검)은 실제 가스밸브 위치에서 상호작용
        // 밸브가 벽 위(y≈1.9)라 ?가 화면 밖으로 안 나가게 살짝 아래·앞에 띄운다
        if (type == "hose" && valvePivot != null)
        { at = valvePivot.position + new Vector3(0, -0.55f, 0.25f); reach = 2.0f; }
        else if (type == "towel") reach = 2.4f;   // 행주가 조리대 안쪽 — 앞에서도 닿게
        else if (type == "hood") reach = 2.0f;    // 후드는 벽 쪽이라 여유
        else if (type == "boil" || type == "yellow") reach = 1.7f;
        var node = SpawnMarker(at);
        var bang = SpawnBang(node);
        uid++;
        hazards.Add(new Hz { id = uid, type = type, def = def, node = node, bang = bang, ttl = def.ttl + 3f, reach = reach });
        if (type == "yellow") SetFlame(1, true);
    }

    void OpenChoice(Hz hz)
    {
        openEv = hz;
        var list = new List<SKData.Opt>(hz.def.icons);
        for (int i = list.Count - 1; i > 0; i--)
        { int j = Random.Range(0, i + 1); var tmp = list[i]; list[i] = list[j]; list[j] = tmp; }
        hz.opts = list;
        uiQ.text = hz.def.q;
        for (int i = 0; i < 3; i++)
        {
            bool on = i < list.Count;
            optRows[i].SetActive(on);
            if (on) optTexts[i].text = list[i].t;
        }
        dim.SetActive(true);
        pnChoice.SetActive(true);
        if (TutActive && pnTut != null) pnTut.SetActive(false);   // 칠판과 대화창 겹침 방지
        Time.timeScale = 0f;   // 애들이 천천히 읽도록 일시정지 (타이머·위험도 정지)
        StartCoroutine(PopInPanel(pnChoice.transform));
        SKSound.Sfx("sfx_popup", 0.7f);
    }

    /// 칠판 팝인 연출 (일시정지 중에도 도는 unscaled 애니)
    IEnumerator PopInPanel(Transform tr)
    {
        float t = 0;
        while (t < 0.28f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.28f);
            float c1 = 1.70158f, c3 = c1 + 1f;
            float s = 1f + c3 * Mathf.Pow(k - 1f, 3) + c1 * Mathf.Pow(k - 1f, 2);
            tr.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        tr.localScale = Vector3.one;
    }

    void CloseChoice()
    {
        openEv = null;
        dim.SetActive(false);
        pnChoice.SetActive(false);
        if (TutActive && pnTut != null) pnTut.SetActive(true);   // 대화창 복원
        Time.timeScale = 1f;   // 일시정지 해제
    }

    void Choose(int i)
    {
        if (quizOpen) { QuizChoice(i); return; }
        if (openEv == null || i >= openEv.opts.Count) return;
        var ic = openEv.opts[i];
        if (ic.ok)
        {
            if (openEv.type == "boil" && valvePivot != null) StartCoroutine(TurnValve(90f));
            if (openEv.type == "yellow") SetFlame(1, false);
            if (openEv.type == "hose") MgStart(4);   // 비눗물 점검 미니게임 (연타)
            // 환기 계열 정답 → 창문 열림 (6초 후 자동 닫힘)
            if ((openEv.type == "boil" || openEv.type == "yellow" || openEv.type == "hood")
                && windowOpen != null && windowClosed != null)
            {
                if (windowCo != null) StopCoroutine(windowCo);
                windowCo = StartCoroutine(VentWindow());
            }
            int pts = openEv.retry ? SKData.RETRY_PTS : SKData.BASE_PTS;
            combo = comboT > 0f ? combo + 1 : 1;
            comboT = SKData.COMBO_WINDOW;
            pts *= Mathf.Min(combo, SKData.COMBO_MAX);
            score += pts;
            AddFloat(openEv.node.transform.position, "+" + pts);
            // 콤보가 쌓일수록 정답음이 높아진다 + 펀치줌
            SKSound.Sfx("sfx_correct", 1f, Mathf.Min(1.35f, 1f + 0.06f * Mathf.Min(combo, 6)));
            PunchZoom();
            FxCorrect();
            BadgeProgress();   // 점검왕·콤보 배지 진행
            Say(openEv.def.toast, 3f);
            StartCoroutine(Pop(openEv.node));
            hazards.Remove(openEv);
            CloseChoice();
        }
        else
        {
            openEv.retry = true;
            FxWrong();
            SKSound.Sfx("sfx_wrong");
            Say(string.IsNullOrEmpty(ic.no) ? "다시!" : ic.no, 2.2f);
            StartCoroutine(Shake());
        }
    }

    IEnumerator Pop(GameObject n)
    {
        float t = 0;
        var s0 = n.transform.localScale;
        while (t < 0.12f) { t += Time.deltaTime; n.transform.localScale = s0 * Mathf.Lerp(1f, 1.6f, t / 0.12f); yield return null; }
        t = 0;
        while (t < 0.18f) { t += Time.deltaTime; n.transform.localScale = s0 * Mathf.Lerp(1.6f, 0.01f, t / 0.18f); yield return null; }
        Destroy(n);
    }

    static Bounds RB(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    /// 비눗물 점검 연출: 밸브 이음새에 하얀 비눗방울이 보글보글 (2.2초)
    IEnumerator SoapCheck()
    {
        if (valvePivot == null) yield break;
        SKSound.Sfx("sfx_soap", 0.8f);
        var go = new GameObject("soap_fx");
        go.transform.position = valvePivot.position + new Vector3(0, 0.02f, 0.08f);
        var ps = go.AddComponent<ParticleSystem>();
        var m = ps.main;
        m.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.2f);
        m.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.22f);
        m.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.16f);   // 쿼터뷰에서도 보이게
        m.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 1f, 0.9f), new Color(0.85f, 0.95f, 1f, 0.8f));
        m.gravityModifier = -0.03f;   // 살짝 떠오름
        var em = ps.emission; em.rateOverTime = 42f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.11f;
        go.GetComponent<ParticleSystemRenderer>().material = Flat(new Color(1f, 1f, 1f, 0.85f));
        yield return new WaitForSeconds(2.2f);
        var e2 = ps.emission; e2.enabled = false;
        yield return new WaitForSeconds(1.3f);
        Destroy(go);
    }

    /// 창문 개폐 스왑: 열림 6초 → 자동 닫힘. 열린 창문을 닫힌 창문 위치·방향에 정렬 후 교체
    IEnumerator VentWindow()
    {
        SKSound.Sfx("sfx_window");
        windowOpen.transform.rotation = windowClosed.transform.rotation;
        windowOpen.SetActive(true);
        var cb = RB(windowClosed);
        var ob = RB(windowOpen);
        windowOpen.transform.position += cb.center - ob.center;
        windowClosed.SetActive(false);
        yield return new WaitForSeconds(6f);
        windowOpen.SetActive(false);
        windowClosed.SetActive(true);
        windowCo = null;
    }

    /// 가장 가까운 소화기 (들고 있는 것 제외, FP에선 바라보는 것만)
    Transform NearestExt()
    {
        Transform best = null;
        float bd = 1.8f;
        foreach (var e in extList)
        {
            if (e == null || e == carryExt) continue;
            float d = new Vector2(player.position.x - e.position.x, player.position.z - e.position.z).magnitude;
            if (d < bd && FacingPoint(e.position, 62f)) { bd = d; best = e; }
        }
        return best;
    }

    /// 소화기 들기/내려놓기 토글
    void TryToggleExtinguisher()
    {
        if (carryExt != null)
        {
            carryExt.SetParent(null, true);
            carryExt.localScale = extHomeScale;   // 원래 크기 복원
            var fwd = pbody.rotation * Vector3.forward;
            carryExt.position = new Vector3(player.position.x + fwd.x * 0.55f, carryY, player.position.z + fwd.z * 0.55f);
            carryExt.rotation = Quaternion.Euler(0, pbody.eulerAngles.y, 0);
            carryExt = null;
            SKSound.Sfx("sfx_putdown");
            Say("소화기를 내려놨어", 1.5f);
            return;
        }
        var e2 = NearestExt();
        if (e2 == null) return;
        carryY = e2.position.y;
        extHomeScale = e2.localScale;
        carryExt = e2;
        carryExt.SetParent(pbody, true);
        carryExt.localScale *= 0.55f;             // 들었을 땐 자연스럽게 축소
        carryExt.position = player.position + pbody.rotation * new Vector3(0.26f, 0.42f, 0.3f);
        carryExt.rotation = pbody.rotation;
        SKSound.Sfx("sfx_pickup");
        Say("소화기를 들었어! 불이 나면 이걸로!", 2.5f);
        // 지진 소화 페이즈(화재 시나리오): 드는 순간 사용법 퀴즈
        if (quakeState == 3 && quakeScen == 1 && !quizSolved) ShowQuiz();
    }

    /// 애니메이션 상태 전환 (0=대기 1=걷기 2=뛰기)
    void SetAnimState(int s)
    {
        if (!hasAnim) return;
        // 플레이 중 리컴파일 등으로 그래프가 무효화됐으면 상태 리셋 후 재생성
        if (graphAlive && !pGraph.IsValid()) { graphAlive = false; animState = -1; }
        if (s == animState) return;
        animState = s;
        if (graphAlive)
        {
            pGraph.Destroy();
            graphAlive = false;
        }
        // Tripo 걷기 클립은 미완성 반쪽 사이클(0.4초 후 정지)이라 사용하지 않는다.
        // 걷기·뛰기 모두 뛰기 클립을 쓰고 속도만 다르게 (걷기=0.8배속 조깅 느낌).
        var clip = s == 0 ? clipIdle : (clipRun != null ? clipRun : clipWalk);
        if (clip == null) clip = clipWalk;
        curClipLen = clip.length;
        if (clip == clipRun) curClipLen = 1.21f;        // 측정된 무결 사이클 (시작-끝 포즈차 0.03)
        else if (clip == clipWalk) curClipLen = 0.4f;   // 폴백용
        pGraph = PlayableGraph.Create("ranger_anim");
        var output = AnimationPlayableOutput.Create(pGraph, "out", pAnim);
        pClipPlayable = AnimationClipPlayable.Create(pGraph, clip);
        pClipPlayable.SetDuration(double.MaxValue);   // '완료'로 멈추지 않게 — 루프는 수동 되감기가 담당
        pClipPlayable.SetSpeed(s == 1 ? 0.8 : 1.0);   // 걷기는 뛰기 클립 0.8배속
        output.SetSourcePlayable(pClipPlayable);
        pGraph.Play();
        graphAlive = true;
    }

    void OnDestroy()
    {
        if (graphAlive && pGraph.IsValid()) pGraph.Destroy();
    }

    // ---------- 지진 시퀀스 (1차=화재, 2차=가스누출) ----------
    void StartQuake(int scen)
    {
        quakeScen = scen;
        quakeState = 1;
        quakeT = 0;
        qValveLocked = false;
        qWindowOpen = false;
        CloseChoice();
        camBase = fpMode ? qvCamPos : cam.transform.position;   // FP 중엔 쿼터뷰 원위치 기준
        SKSound.Music("bgm_danger", 0.4f);
        SKSound.Sfx("sfx_quake");
        SKSound.Sfx("sfx_blackout", 0.8f);
        SKSound.VoStop();   // 긴급 경보는 무조건 끼어듦
        SKSound.Vo("vo_quake" + scen);
        Say(scen == 1 ? "지진이다! 탁자 밑으로 숨어!" : "또 지진이다! 탁자 밑으로 숨어!", 4f);
        // 누출 시나리오: 위험 이벤트로 열린 창문이 있으면 닫고 시작
        if (scen == 2 && windowOpen != null && windowClosed != null)
        {
            if (windowCo != null) { StopCoroutine(windowCo); windowCo = null; }
            windowOpen.SetActive(false);
            windowClosed.SetActive(true);
        }
        Transform shelter = null;
        var kroot = GameObject.Find("Kitchen");
        if (kroot != null)
            foreach (var t in kroot.GetComponentsInChildren<Transform>(true))
                if (t.name == "ShelterTable") shelter = t;
        if (shelter != null)
        {
            var rs = shelter.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            shelterPos = new Vector3(b.center.x, 0, b.center.z);
        }
        else shelterPos = Pz(new Vector3(8.6f, 0, 5.6f), new Vector3(8.6f, 0, 10.6f));
        MakeArrow(shelterPos + new Vector3(0, 1.9f, 0));
        var dgo = new GameObject("quake_dust");
        dgo.transform.position = Pz(new Vector3(9.6f, 2.8f, 5.4f), new Vector3(13.1f, 2.8f, 7.9f));
        dustPs = dgo.AddComponent<ParticleSystem>();
        var dm = dustPs.main;
        dm.startLifetime = 1.6f;
        dm.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        dm.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.16f);
        dm.startColor = new Color(0.75f, 0.7f, 0.62f, 0.6f);
        dm.gravityModifier = 0.6f;
        var de = dustPs.emission; de.rateOverTime = 60f;
        var ds = dustPs.shape;
        ds.shapeType = ParticleSystemShapeType.Box;
        ds.scale = Pz(new Vector3(17f, 0.1f, 9f), new Vector3(22f, 0.1f, 14f));
        dgo.GetComponent<ParticleSystemRenderer>().material = Flat(new Color(0.75f, 0.7f, 0.62f, 0.6f));
        DimLighting();
    }

    /// 정전/화재 연출: 전역광·실내등·태양광 급감 (RestoreLighting으로 복구)
    void DimLighting()
    {
        ambBackup = RenderSettings.ambientLight;
        RenderSettings.ambientLight = ambBackup * 0.3f;
        camBgBackup = cam.backgroundColor;
        cam.backgroundColor = camBgBackup * 0.35f;
        lampL.Clear();
        lampBackup.Clear();
        var lamps = GameObject.Find("Lamps");
        if (lamps != null)
            foreach (var l in lamps.GetComponentsInChildren<Light>())
            {
                lampL.Add(l);
                lampBackup.Add(l.intensity);
                l.intensity = 0f;
            }
        var sunGo = GameObject.Find("Sun");
        if (sunGo != null)
        {
            sunLight = sunGo.GetComponent<Light>();
            if (sunLight != null) { sunBackup = sunLight.intensity; sunLight.intensity = 0.12f; }
        }
    }

    /// 정전 복구
    void RestoreLighting()
    {
        RenderSettings.ambientLight = ambBackup;
        cam.backgroundColor = camBgBackup;
        for (int i = 0; i < lampL.Count; i++)
            if (lampL[i] != null) lampL[i].intensity = lampBackup[i];
        if (sunLight != null && sunBackup > 0f) sunLight.intensity = sunBackup;
    }

    void MakeArrow(Vector3 at)
    {
        arrowGo = new GameObject("guide_arrow");
        arrowGo.transform.position = at;
        var tm = arrowGo.AddComponent<TextMesh>();
        tm.text = "▼";
        tm.font = font;
        tm.fontSize = 120;
        tm.characterSize = 0.13f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = new Color(1f, 0.84f, 0.2f);
        tm.fontStyle = FontStyle.Bold;
        arrowGo.GetComponent<MeshRenderer>().material = font.material;
        arrowBaseY = at.y;
    }

    bool PlayerNearValve()
    {
        if (valvePivot == null) return false;
        var p = valvePivot.position;
        return new Vector2(player.position.x - p.x, player.position.z - (p.z + 0.7f)).magnitude < 1.7f
            && FacingPoint(p, 75f);
    }

    void QuakeUpdate(float dt)
    {
        quakeT += dt;
        if (arrowGo != null)
        {
            var ap = arrowGo.transform.position;
            ap.y = arrowBaseY + Mathf.Abs(Mathf.Sin(timeAll * 4f)) * 0.25f;
            arrowGo.transform.position = ap;
            arrowGo.transform.rotation = cam.transform.rotation;
        }
        bool nearShelter = new Vector2(player.position.x - shelterPos.x, player.position.z - shelterPos.z).magnitude < 1.5f;
        if (quakeState == 1)
        {
            if (!fpMode)
                cam.transform.position = camBase + new Vector3((Random.value - 0.5f) * 0.24f, (Random.value - 0.5f) * 0.12f, 0);
            // (FP 셰이크는 LateUpdate의 fpShakeAmp가 담당)
            var sc = pbody.localScale;
            sc.y = Mathf.Lerp(sc.y, nearShelter ? 0.62f : 1f, 10f * dt);
            pbody.localScale = sc;
            if (quakeT >= 6f)
            {
                if (!fpMode) cam.transform.position = camBase;
                if (dustPs != null) { var de = dustPs.emission; de.enabled = false; }
                if (nearShelter)
                {
                    score += 150;
                    AddFloat(shelterPos + new Vector3(0, 1.2f, 0), "+150 잘 숨었어!");
                }
                else
                {
                    acha++;
                    SKSound.Sfx("sfx_acha");
                    AddFloat(player.position + new Vector3(0, 1.2f, 0), "아차! 흔들릴 땐 몸부터!");
                }
                quakeState = 2;
                quakeT = 0;
                if (arrowGo != null && valvePivot != null)
                {
                    arrowGo.transform.position = valvePivot.position + new Vector3(0, 0.55f, 0.2f);
                    arrowBaseY = arrowGo.transform.position.y;
                }
                if (quakeScen == 1)
                {
                    // 화재 시나리오: 안내가 먼저, 불은 밸브 잠근 뒤에 발생
                    SKSound.Vo("vo_valve");
                    Say("흔들림이 멈췄어! 지진 뒤엔 먼저 가스밸브를 잠그고 환기해야 해!", 4.5f);
                }
                else
                {
                    // 누출 시나리오: 불이 꺼지고 가스가 바닥에 깔린다
                    KillStoveFlames();
                    StartGasLeak();
                    SKSound.Vo("vo_leak");
                    Say("불이 꺼지고 가스가 새서 바닥에 깔리고 있어! 밸브를 잠그고 창문을 열어 환기해!", 5f);
                }
            }
        }
        else if (quakeState == 2)
        {
            var sc = pbody.localScale;
            sc.y = Mathf.Lerp(sc.y, 1f, 10f * dt);
            pbody.localScale = sc;
            // 밸브 잠금은 SPACE → 미니게임 (Update 입력부에서 진입)
            // 누출 시나리오: 화살표는 남은 과제로 (밸브 → 창문)
            if (quakeScen == 2 && arrowGo != null)
            {
                Vector3 aim = (!qValveLocked && valvePivot != null)
                    ? valvePivot.position + new Vector3(0, 0.55f, 0.2f)
                    : WindowAim();
                arrowGo.transform.position = new Vector3(aim.x, arrowGo.transform.position.y, aim.z);
                arrowBaseY = aim.y;
            }
            // 시간 초과 구제
            if (quakeScen == 1 && quakeT > 30f) { if (mgOpen) MgForceClose(); ValveDone(); }
            if (quakeScen == 2 && quakeT > 45f)
            {
                acha++;
                if (mgOpen) MgForceClose();
                if (!qValveLocked) LockValve2();
                if (!qWindowOpen) OpenQuakeWindow();
            }
        }
        else if (quakeState == 3 && quakeScen == 1)
        {
            // 화살표 안내: 소화기 → (들었으면) 가장 가까운 남은 불
            if (arrowGo != null)
            {
                Vector3 aim = arrowGo.transform.position;
                if (carryExt == null)
                {
                    var e = NearestExtAny();
                    if (e != null) aim = e.position + new Vector3(0, 1.1f, 0);
                }
                else
                {
                    float bd = 999f;
                    for (int i = 0; i < firePosL.Count; i++)
                    {
                        if (fireOutL[i]) continue;
                        float d = new Vector2(player.position.x - firePosL[i].x, player.position.z - firePosL[i].z).magnitude;
                        if (d < bd) { bd = d; aim = firePosL[i] + new Vector3(0, 0.9f, 0); }
                    }
                }
                arrowGo.transform.position = new Vector3(aim.x, arrowGo.transform.position.y, aim.z);
                arrowBaseY = aim.y;
            }
            if (quakeT > 60f)
            {
                for (int i = 0; i < fireOutL.Count; i++)
                    if (!fireOutL[i]) ExtinguishFire(i);
                FinalDone();
            }
        }
    }

    Transform NearestExtAny()
    {
        Transform best = null;
        float bd = 999f;
        foreach (var e in extList)
        {
            if (e == null || e == carryExt) continue;
            float d = new Vector2(player.position.x - e.position.x, player.position.z - e.position.z).magnitude;
            if (d < bd) { bd = d; best = e; }
        }
        return best;
    }

    /// 잔불 3곳 발생 (밸브 잠근 뒤): 후보 4곳(화구 2 + 남쪽 서랍장 2)에서 랜덤 3곳 — 매판 다른 배치
    void StartFires()
    {
        var cands = new List<Vector3>
        {
            new Vector3(HzPos("yellow").x, 1.3f, 1.9f),
            new Vector3(HzPos("boil").x, 1.3f, 1.9f),
        };
        var kroot = GameObject.Find("Kitchen");
        if (kroot != null)
            foreach (Transform t in kroot.transform)
            {
                if (!t.name.Contains("DrawerCounterLong")) continue;
                var rs = t.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0) continue;
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                // 상판 위 소품(재료상자 등)에 불꽃이 파묻히지 않게 넉넉히 띄움
                cands.Add(new Vector3(b.center.x, b.max.y + 0.42f, b.center.z));
                if (cands.Count >= 4) break;
            }
        while (cands.Count < 4) cands.Add(new Vector3(6.3f + cands.Count * 2.5f, 1.35f, Pf(9.0f, 14.0f)));
        for (int i = cands.Count - 1; i > 0; i--)
        { int j = Random.Range(0, i + 1); var tmp = cands[i]; cands[i] = cands[j]; cands[j] = tmp; }
        for (int i = 0; i < 3; i++) MakeFire(cands[i]);
        SKSound.Sfx("sfx_fire_start");
        SKSound.Loop(2, "amb_fire", 0.55f);
    }

    void MakeFire(Vector3 at)
    {
        int idx = firePsL.Count;
        // 카툰 FX 팩 불꽃 (Cartoon FX Remaster Free)
        ParticleSystem firePs = null;
        ParticleSystem fireSmoke = null;
        var prefab = Resources.Load<GameObject>("VFX/CFXR Fire");
        var fgo = Instantiate(prefab);
        fgo.name = "quake_fire" + idx;
        fgo.transform.position = at;
        fgo.transform.localScale = Vector3.one * 0.72f;   // 불꽃 크게 (가시성)
        firePs = fgo.GetComponent<ParticleSystem>();
        // 프리웜 끄고 빈 상태에서 시작 → 연기가 서서히 차오름
        foreach (var cps in fgo.GetComponentsInChildren<ParticleSystem>(true))
        {
            var cm = cps.main;
            cm.prewarm = false;
        }
        firePs.Clear(true);
        firePs.Play(true);   // CFXR 프리팹은 명시 재생 필요
        // 프리팹 내장 라이트(강한 애니메이션 광원)는 끄고, 은은한 자체 광원으로 대체
        foreach (var fl in fgo.GetComponentsInChildren<Light>(true))
            fl.gameObject.SetActive(false);
        var lgo = new GameObject("quake_fire_light" + idx);
        lgo.transform.position = at + new Vector3(0, 0.9f, 0);
        var fireLight = lgo.AddComponent<Light>();
        fireLight.type = LightType.Point;
        fireLight.color = new Color(1f, 0.6f, 0.3f);
        fireLight.intensity = 0.75f;   // 살짝만 번지게
        fireLight.range = 2.6f;
        firePsL.Add(firePs);
        fireSmokeL.Add(fireSmoke);
        fireLightL.Add(fireLight);
        firePosL.Add(at);
        fireOutL.Add(false);
        fireHpL.Add(1f);
    }

    /// 특정 화재 진화 (파티클 정지 + 연기 잔류 후 소멸 + 조명 페이드)
    void ExtinguishFire(int idx)
    {
        if (idx >= firePsL.Count) return;
        fireOutL[idx] = true;
        SKSound.Sfx("sfx_fire_out");
        if (firePsL[idx] != null) firePsL[idx].Stop(true, ParticleSystemStopBehavior.StopEmitting);
        // 꺼질 때 뿅 연출
        var poof = Resources.Load<GameObject>("VFX/CFXR Magic Poof");
        if (poof != null)
        {
            var pgo = Instantiate(poof);
            pgo.name = "quake_poof";
            pgo.transform.position = firePosL[idx];
            pgo.transform.localScale = Vector3.one * 0.6f;
            var pps = pgo.GetComponent<ParticleSystem>();
            if (pps != null) pps.Play(true);
            Destroy(pgo, 3f);
        }
        if (fireSmokeL[idx] != null) StartCoroutine(StopSmokeSoon(fireSmokeL[idx]));
        if (fireLightL[idx] != null) StartCoroutine(FadeFireLight(fireLightL[idx]));
    }

    /// 화구 불꽃·김 일괄 소등
    void KillStoveFlames()
    {
        for (int i = 0; i < 2; i++)
        {
            if (flames[i] != null) { var fe = flames[i].emission; fe.enabled = false; }
            if (flameSmoke[i] != null) { var fs = flameSmoke[i].emission; fs.enabled = false; }
            if (flameLights[i] != null) flameLights[i].intensity = 0f;
        }
        if (boilFoam != null) boilFoam.gameObject.SetActive(false);
        var steamGo = GameObject.Find("steam");
        if (steamGo != null) { var se = steamGo.GetComponent<ParticleSystem>().emission; se.enabled = false; }
    }

    /// [화재 시나리오] 밸브 잠금 완료 → 그제야 잔불 3곳 발생, 소화기 페이즈로
    void ValveDone()
    {
        quakeState = 3;
        quakeT = 0;
        if (valvePivot != null) StartCoroutine(TurnValve(90f));
        KillStoveFlames();
        score += 150;
        AddFloat(Pz(new Vector3(12.6f, 1.6f, 0.8f), new Vector3(17.4f, 1.6f, 0.8f)), "+150 가스 차단!");
        StartFires();
        SKSound.Vo("vo_fires");
        Say("가스는 잠갔어! 앗, 잔불이 여기저기 붙었다 — 소화기로 꺼!", 4.5f);
    }

    /// [누출 시나리오] 밸브 잠금 → 새 가스 공급 차단
    void LockValve2()
    {
        if (qValveLocked) return;
        qValveLocked = true;
        if (valvePivot != null) StartCoroutine(TurnValve(90f));
        gasAdding = false;        // 새 가스 구름 생성 중단
        SKSound.StopLoop(2);      // 밸브 잠금 = 누출음 즉시 정지
        score += 150;
        AddFloat(Pz(new Vector3(12.6f, 1.6f, 0.8f), new Vector3(17.4f, 1.6f, 0.8f)), "+150 가스 차단!");
        if (!qWindowOpen) Say("밸브 잠금! 이제 창문을 열어 환기해!", 4f);
        TryFinishVent();
    }

    bool NearQuakeWindow()
    {
        if (windowClosed == null) return false;
        var c = RB(qWindowOpen && windowOpen != null ? windowOpen : windowClosed).center;
        return new Vector2(player.position.x - c.x, player.position.z - (c.z + 0.8f)).magnitude < 1.9f
            && FacingPoint(c, 75f);
    }

    Vector3 WindowAim()
    {
        var w = (qWindowOpen && windowOpen != null) ? windowOpen : windowClosed;
        if (w == null) return new Vector3(9.6f, 2.0f, 0.9f);
        var c = RB(w).center;
        return new Vector3(c.x, c.y + 0.75f, c.z + 0.3f);
    }

    /// [누출 시나리오] 창문 열기 (열림 상태 유지, 환기 끝나고 닫음)
    void OpenQuakeWindow()
    {
        if (qWindowOpen) return;
        SKSound.Sfx("sfx_window");
        if (windowOpen != null && windowClosed != null)
        {
            windowOpen.transform.rotation = windowClosed.transform.rotation;
            windowOpen.SetActive(true);
            var cb = RB(windowClosed);
            var ob = RB(windowOpen);
            windowOpen.transform.position += cb.center - ob.center;
            windowClosed.SetActive(false);
            AddFloat(cb.center, "+150 창문 개방!");
        }
        qWindowOpen = true;
        score += 150;
        if (!qValveLocked) Say("창문 열림! 이제 가스밸브를 잠가!", 4f);
        TryFinishVent();
    }

    /// 밸브+창문 둘 다 완료 → 가스 배출 연출로
    void TryFinishVent()
    {
        if (qValveLocked && qWindowOpen && quakeState == 2)
        {
            quakeState = 3;
            quakeT = 0;
            StartCoroutine(VentOutGas());
        }
    }

    /// 바닥에 깔리는 가스: 독구름 프리팹을 누출점부터 온 바닥으로 점점 추가 (누출 지점은 두 화구 중 랜덤)
    void StartGasLeak()
    {
        var src = Random.value < 0.5f ? HzPos("yellow") : HzPos("boil");
        SKSound.Loop(2, "sfx_gas_leak", 0.7f);
        gasAdding = true;
        StartCoroutine(GasSpread(new Vector3(src.x, 0.25f, src.z + 0.5f)));
    }

    IEnumerator GasSpread(Vector3 origin)
    {
        // Fog Particles 팩: 초록 틴트 안개가 바닥에 낮게 깔림
        var prefab = Resources.Load<GameObject>("VFX/GasFog");
        int n = 0;
        while (gasAdding && n < 11)
        {
            // 갈수록 넓은 반경에 배치 → 화구 주변에서 온 바닥으로 확산
            float rad = Mathf.Min(Pf(8f, 12f), 0.3f + n * 0.9f);
            float ang = Random.value * Mathf.PI * 2f;
            var pos = origin + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * (Random.value * rad);
            pos.x = Mathf.Clamp(pos.x, 1.2f, roomW - 1.2f);
            pos.z = Mathf.Clamp(pos.z, 1.2f, roomD - 1.2f);
            pos.y = 0.3f;
            var go = Instantiate(prefab);
            go.name = "quake_gas" + n;
            go.transform.position = pos;
            go.transform.localScale = Vector3.one;
            var ps = go.GetComponent<ParticleSystem>();
            var m = ps.main;
            m.prewarm = false;
            m.startSize = new ParticleSystem.MinMaxCurve(1.6f, 2.9f);
            // 회색 가스 — 실제 KGS 안전 영상 톤
            m.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.78f, 0.78f, 0.80f, 0.50f), new Color(0.60f, 0.60f, 0.64f, 0.38f));
            m.maxParticles = 60;
            var sh = ps.shape;
            sh.scale = new Vector3(2.6f, 0.5f, 2.6f);   // 낮고 넓게 — 바닥에 깔리는 형태
            var em = ps.emission;
            em.rateOverTime = 9f;
            // 프리팹의 수명별 알파 곡선이 너무 옅어(3/255) 안 보임 → 또렷한 곡선으로 교체
            var col = ps.colorOverLifetime;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.55f, 0.25f),
                        new GradientAlphaKey(0.55f, 0.75f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);
            ps.Clear();
            ps.Play();   // 빈 상태에서 서서히 차오름
            gasClouds.Add(go.transform);
            n++;
            yield return new WaitForSeconds(n < 3 ? 0.5f : 1.3f);
        }
    }

    /// 가스 구름들이 창문으로 빨려나가는 연출 → 완료
    IEnumerator VentOutGas()
    {
        Say("환기 중... 가스가 창문 밖으로 빠져나간다!", 3.5f);
        SKSound.StopLoop(2);
        SKSound.Sfx("sfx_vent");
        SKSound.Vo("vo_vent");
        if (arrowGo != null) Destroy(arrowGo);
        Vector3 wpos = windowOpen != null ? RB(windowOpen).center : new Vector3(9.6f, 1.9f, 0.4f);
        // 창문 바람 연출
        var wind = Resources.Load<GameObject>("VFX/CFXR4 Wind Trails");
        if (wind != null)
        {
            var wgo = Instantiate(wind);
            wgo.name = "quake_wind";
            wgo.transform.position = wpos + new Vector3(0, -0.3f, 0.8f);
            wgo.transform.rotation = Quaternion.LookRotation(new Vector3(0, 0.2f, -1f));
            var wps = wgo.GetComponent<ParticleSystem>();
            if (wps != null) wps.Play(true);
            Destroy(wgo, 3.5f);
        }
        // 구름 이동·축소 (월드 시뮬이라 기존 입자는 잔향처럼 남아 자연스럽게 사라짐)
        var starts = new List<Vector3>();
        var scales = new List<Vector3>();
        foreach (var c in gasClouds)
        {
            starts.Add(c != null ? c.position : Vector3.zero);
            scales.Add(c != null ? c.localScale : Vector3.one);
        }
        float t = 0;
        while (t < 2.8f)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / 2.8f);
            for (int i = 0; i < gasClouds.Count; i++)
            {
                var c = gasClouds[i];
                if (c == null) continue;
                float kk = Mathf.Clamp01(k * 1.35f - i * 0.025f);
                c.position = Vector3.Lerp(starts[i], wpos, Mathf.SmoothStep(0f, 1f, kk));
                c.localScale = scales[i] * (1f - 0.85f * kk);
            }
            yield return null;
        }
        foreach (var c in gasClouds) if (c != null) Destroy(c.gameObject);
        gasClouds.Clear();
        FinalDone();
    }

    /// 시나리오 완료 (화재 진압 or 환기 성공)
    void FinalDone()
    {
        quakeState = 4;
        if (quakeScen == 1)
        {
            quakeDone = true;
            SKSound.Vo("vo_badge_fire");
            Say("모든 불 진압 성공! ★ 화재 대응 배지 획득!", 5f);
            BadgeEarn(1);
        }
        else
        {
            quake2Done = true;
            SKSound.Vo("vo_badge_leak");
            Say("환기 완료! 가스를 몰아냈어 ★ 누출 대응 배지 획득!", 5f);
            BadgeEarn(2);
        }
        pbody.localScale = Vector3.one;
        pbody.localEulerAngles = new Vector3(0, pbody.localEulerAngles.y, 0);
        animState = -1;
        SKSound.StopLoop(2);
        SKSound.Music("bgm_main", 0.3f);
        RestoreLighting();
        if (arrowGo != null) Destroy(arrowGo);
        StartCoroutine(ReopenValve());
    }

    IEnumerator StopSmokeSoon(ParticleSystem sp)
    {
        yield return new WaitForSeconds(2.5f);
        if (sp != null) { var e = sp.emission; e.enabled = false; }
    }

    IEnumerator FadeFireLight(Light fl)
    {
        float t = 0;
        float i0 = fl.intensity;
        while (t < 1.5f)
        {
            t += Time.deltaTime;
            if (fl == null) yield break;
            fl.intensity = Mathf.Lerp(i0, 0, t / 1.5f);
            yield return null;
        }
    }

    // ---------- 소화기 사용법 퀴즈 + 분사 ----------
    static readonly string[] QUIZ_OPTS = {
        "안전핀 뽑기 → 불 쪽 겨누기 → 손잡이 쥐고 쓸기",
        "손잡이 쥐기 → 아무 데나 분사 → 안전핀 뽑기",
        "불 속에 던지기 → 멀리 도망가기",
    };

    void ShowQuiz()
    {
        quizOpen = true;
        uiQ.text = "소화기 사용 순서로 맞는 것은?";
        for (int i = 0; i < 3; i++)
        {
            optRows[i].SetActive(true);
            optTexts[i].text = QUIZ_OPTS[i];
        }
        dim.SetActive(true);
        pnChoice.SetActive(true);
        if (TutActive && pnTut != null) pnTut.SetActive(false);
        Time.timeScale = 0f;
        StartCoroutine(PopInPanel(pnChoice.transform));
        SKSound.Sfx("sfx_popup", 0.7f);
    }

    void QuizChoice(int i)
    {
        if (i == 0)
        {
            quizOpen = false;
            quizSolved = true;
            dim.SetActive(false);
            pnChoice.SetActive(false);
            if (TutActive && pnTut != null) pnTut.SetActive(true);
            Time.timeScale = 1f;
            score += 100;
            AddFloat(player.position + new Vector3(0, 1.4f, 0), "+100 정답!");
            SKSound.Sfx("sfx_correct");
            FxCorrect();
            Say("좋아! 불을 향해 [마우스 왼쪽] 꾹 — 좌우로 쓸면서 분사!", 4f);
        }
        else
        {
            SKSound.Sfx("sfx_wrong");
            FxWrong();
            Say(i == 1 ? "안전핀을 먼저 뽑아야 약제가 나와!" : "소화기는 던지는 게 아니야!", 2.5f);
            StartCoroutine(Shake());
        }
    }

    /// 조준된 화재 인덱스 (-1 없음): 바라보는 방향 ±25도 & 4.5m 이내, 또는 1.8m 초근접
    int NearBurningFire()
    {
        int best = -1;
        float bd = 999f;
        var fwd = pbody.forward;
        fwd.y = 0;
        fwd.Normalize();
        for (int i = 0; i < firePosL.Count; i++)
        {
            if (fireOutL[i]) continue;
            var to = firePosL[i] - player.position;
            to.y = 0;
            float d = to.magnitude;
            bool aimed = d < 4.5f && Vector3.Angle(fwd, to) < 25f;
            bool close = d < 1.8f;
            if ((aimed || close) && d < bd) { bd = d; best = i; }
        }
        return best;
    }

    // (분사는 SKMinigame.cs의 SprayUpdate가 담당 — 홀드 분사 + 쓸기 보너스)

    /// 지진 종료 수 초 후: 안전 확인 → 밸브 재개방 + 화구 재점화
    IEnumerator ReopenValve()
    {
        yield return new WaitForSeconds(4.5f);
        SKSound.Vo("vo_reopen");
        Say("안전 확인 완료! 밸브를 다시 열었어 — 요리 재개!", 3.5f);
        if (valvePivot != null) StartCoroutine(TurnValve(0f));
        for (int i = 0; i < 2; i++)
        {
            if (flames[i] != null) { var fe = flames[i].emission; fe.enabled = true; }
            if (flameLights[i] != null) flameLights[i].intensity = 1.1f;
        }
        SetFlame(0, false);
        SetFlame(1, false);
        if (boilFoam != null) boilFoam.gameObject.SetActive(true);
        var steamGo = GameObject.Find("steam");
        if (steamGo != null) { var se = steamGo.GetComponent<ParticleSystem>().emission; se.enabled = true; }
        // 누출 시나리오에서 열었던 창문은 환기가 끝났으니 닫기
        if (qWindowOpen && windowOpen != null && windowClosed != null)
        {
            windowClosed.SetActive(true);
            windowOpen.SetActive(false);
            qWindowOpen = false;
        }
        quakeState = 0;   // 일상 복귀 (다음 지진 대기)
    }

    /// 밸브 레버 회전 연출 (90=잠김, 0=열림)
    IEnumerator TurnValve(float to)
    {
        SKSound.Sfx("sfx_valve");
        float from = valvePivot.localEulerAngles.y;
        if (from > 180f) from -= 360f;
        float t = 0;
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / 0.35f));
            valvePivot.localEulerAngles = new Vector3(0, a, 0);
            yield return null;
        }
        valvePivot.localEulerAngles = new Vector3(0, to, 0);
    }

    IEnumerator Shake()
    {
        // 오답 흔들림 — 일시정지 중에도 동작 (리얼타임), FP는 셰이크 앰프로 위임
        if (fpMode) { fpShakeAmp = Mathf.Max(fpShakeAmp, 0.09f); yield break; }
        var p0 = cam.transform.position;
        cam.transform.position = p0 + new Vector3(0.15f, 0, 0);
        yield return new WaitForSecondsRealtime(0.04f);
        cam.transform.position = p0 + new Vector3(-0.15f, 0, 0);
        yield return new WaitForSecondsRealtime(0.04f);
        cam.transform.position = p0;
    }

    void ResetGame()
    {
        foreach (var hz in hazards) Destroy(hz.node);
        hazards.Clear();
        CloseChoice();
        score = 0; combo = 0; comboT = 0; acha = 0;
        stageT = 0; spawnT = 1.5f; over = false;
        player.position = Pz(new Vector3(6f, 0f, 6.2f), new Vector3(12f, 0f, 8.5f));
        // 지진 상태 초기화
        foreach (var go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            if (go.name.StartsWith("quake_") || go.name == "guide_arrow" || go.name == "spray")
                Destroy(go);
        firePsL.Clear(); fireSmokeL.Clear(); fireLightL.Clear(); firePosL.Clear(); fireOutL.Clear(); fireHpL.Clear();
        if (quakeState >= 1 && quakeState <= 3) RestoreLighting();
        MgForceClose();
        StopJet();
        sprayAutoT = 0f; sweepAcc = 0f;
        preQ1 = false; preQ2 = false;
        quizOpen = false; quizSolved = false; spraying = false;
        rankShown = false; if (pnRank != null) pnRank.SetActive(false);
        if (paused) ResumeGame();
        runBadges.Clear();
        SKSound.Music("bgm_main", 0.3f);
        if (carryExt != null) { carryExt.SetParent(null, true); carryExt.localScale = extHomeScale; carryExt = null; }
        quakeState = 0; quakeDone = false;
        quakeScen = 0; quake2Done = false; qValveLocked = false; qWindowOpen = false;
        gasAdding = false;
        gasClouds.Clear();
        SKSound.StopLoop(2);
        SKSound.VoStop();
        if (windowCo != null) { StopCoroutine(windowCo); windowCo = null; }
        if (windowOpen != null && windowClosed != null) { windowClosed.SetActive(true); windowOpen.SetActive(false); }
        pbody.localScale = Vector3.one;
        // 불꽃·김 재점화
        for (int i = 0; i < 2; i++)
        {
            if (flames[i] != null) { var fe = flames[i].emission; fe.enabled = true; }
            if (flameLights[i] != null) flameLights[i].intensity = 1.1f;
        }
        if (boilFoam != null) boilFoam.gameObject.SetActive(true);
        var steamGo = GameObject.Find("steam");
        if (steamGo != null) { var se = steamGo.GetComponent<ParticleSystem>().emission; se.enabled = true; }
        SetFlame(0, false);
        SetFlame(1, false);
        if (valvePivot != null) valvePivot.localEulerAngles = Vector3.zero;
        Say("다시 시작! 위험에 다가가 스페이스!", 3f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        timeAll += dt;
        SKSound.Tick();   // 대기 중인 대사 이어 재생

        // 타이틀 화면: 영상 페이드 인 → 로고 통통 등장 → 안내 깜빡임, SPACE로 시작
        if (titleOpen)
        {
            titleT += dt;
            if (titleVidImg != null && titleVp != null && titleVp.isPlaying)
            {
                var vc = titleVidImg.color;
                vc.a = Mathf.MoveTowards(vc.a, 1f, dt * 2.5f);
                titleVidImg.color = vc;
            }
            if (titleLogoRt != null)
            {
                float k = Mathf.Clamp01((titleT - 1.2f) / 0.5f);
                float c1 = 1.70158f, c3 = c1 + 1f;
                float s = k <= 0f ? 0f : 1f + c3 * Mathf.Pow(k - 1f, 3) + c1 * Mathf.Pow(k - 1f, 2);
                titleLogoRt.localScale = new Vector3(s, s, 1f);
                titleLogoRt.anchoredPosition = new Vector2(0, Mathf.Sin(timeAll * 2.2f) * 9f);
            }
            float vis = Mathf.Clamp01((titleT - 2.0f) / 0.4f);
            if (titleStartBg != null)
            {
                var bc = titleStartBg.color; bc.a = 0.92f * vis; titleStartBg.color = bc;
            }
            if (titleStart != null)
            {
                var tc = titleStart.color;
                tc.a = vis * (0.65f + 0.35f * Mathf.Sin(timeAll * 5f));
                titleStart.color = tc;
            }
            if (titleCredit != null)
            {
                var cc = titleCredit.color; cc.a = 0.8f * vis; titleCredit.color = cc;
            }
            if (titleT > 0.4f && SKIn.Down(KeyCode.G))
            {
                PlayerPrefs.SetInt("sktut", 0);   // 훈련 다시 받기
            }
            for (int si = 0; si < 3; si++)
                if (titleT > 0.4f && SKIn.Down(si == 0 ? KeyCode.Alpha1 : si == 1 ? KeyCode.Alpha2 : KeyCode.Alpha3)
                    && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != SCENES[si])
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(SCENES[si]);
                    return;
                }
            if (titleT > 0.4f && (SKIn.Down(KeyCode.Space) || SKIn.Down(KeyCode.Return)))
            {
                titleOpen = false;
                if (titleVidImg != null && titleVidImg.texture != null) Destroy(titleVidImg.texture);
                Destroy(titleGo);
                stageT = 0;
                spawnT = 1.5f;
                SKSound.Sfx("sfx_correct", 0.7f);
                SKSound.Music("bgm_main", 0.3f);
                // 수료 이력이 있으면 훈련 생략하고 바로 실전 (G로 재수강 가능)
                bool tutDoneBefore = PlayerPrefs.GetInt("sktut", 0) == 1;
                if (!tutDone && !tutDoneBefore) TutStart();
                else { tutDone = true; Say("위험에 다가가 스페이스! 물음표를 따라가!", 4f); }
            }
            return;
        }

        // 튜토리얼 건너뛰기
        if (TutActive && SKIn.Down(KeyCode.F1)) { TutFinish(true); }

        // 랭크·일시정지 (모달 입력 우선)
        if (RankUpdate()) return;
        if (SKIn.Down(KeyCode.Escape)) TogglePause();
        if (PauseUpdate()) return;

        // 1인칭 ↔ 쿼터뷰 전환 + FP 시점 처리
        if (SKIn.Down(KeyCode.V)) ToggleFp();
        FpUpdate();

        // 키 입력
        if (SKIn.Down(KeyCode.Space) || SKIn.Down(KeyCode.Return))
        {
            if (mgOpen)
            {
                MgPress();   // 밸브 미니게임 입력 최우선
            }
            else if (TutActive && (tutWait || tutTyping))
            {
                TutAdvance();   // 튜토리얼 대화 넘김
            }
            else if (TutGate == TG_VALVE && PlayerNearValve())
            {
                MgStart(0);   // 밸브 훈련: 미니게임 진입
            }
            else if (!over && openEv == null && !quizOpen && (quakeState == 0 || quakeState == 4))
            {
                var hz = Nearest();
                bool drill = !spraying && TutGate == TG_SPRAY && carryExt != null && NearBurningFire() >= 0;
                if (hz != null) OpenChoice(hz);
                else if (drill) SprayBurst();   // 훈련: SPACE로도 분사
                else TryToggleExtinguisher();
            }
            else if (!over && openEv == null && !quizOpen && quakeState == 2
                && (quakeScen == 1 || !qValveLocked) && PlayerNearValve())
            {
                MgStart(quakeScen == 1 ? 1 : 2);   // 지진: 밸브 잠금 미니게임 (누출은 연타만)
            }
            else if (!over && openEv == null && !quizOpen && !spraying && quakeState == 3 && quakeScen == 1)
            {
                bool fi = carryExt != null && quizSolved && NearBurningFire() >= 0;
                if (fi) SprayBurst();   // 불 조준 스페이스 = 버스트 분사
                else TryToggleExtinguisher();
            }
            else if (!over && openEv == null && !quizOpen && quakeState == 2 && quakeScen == 2
                && !qWindowOpen && NearQuakeWindow())
            {
                MgStart(3);   // 누출: 창문 열기 미니게임 (연타)
            }
        }
        // 소화기 좌클릭 홀드: 화재 진압 페이즈·소화 훈련에만 분사 — 평상시엔 장난 금지 교육 경고
        if (SKIn.MouseDown() && !over && !mgOpen && openEv == null && !quizOpen && carryExt != null)
        {
            bool fireDrill = TutGate == TG_SPRAY;   // 튜토리얼 소화 훈련
            bool fireQuake = quakeState == 3 && quakeScen == 1;
            if (fireQuake || fireDrill)
            {
                if (fireQuake && !quizSolved) Say("먼저 사용 순서 퀴즈를 풀어야 해!", 2.2f);
                else { sprayOn = true; StartJet(); }
            }
            else Say("소화기는 장난으로 쓰면 안 돼! 불이 났을 때만 사용하는 거야!", 2.8f);
        }
        if (openEv != null || quizOpen)
        {
            if (SKIn.Down(KeyCode.Alpha1) || SKIn.Down(KeyCode.Keypad1)) Choose(0);
            else if (SKIn.Down(KeyCode.Alpha2) || SKIn.Down(KeyCode.Keypad2)) Choose(1);
            else if (SKIn.Down(KeyCode.Alpha3) || SKIn.Down(KeyCode.Keypad3)) Choose(2);
        }
        if (over && SKIn.Down(KeyCode.R)) ResetGame();

        // 타이머류
        comboT -= dt; if (comboT < 0) combo = 0;
        msgT -= dt;

        // 이동 + 콩콩
        bool moving = false;
        bool sprinting = false;
        if (!over && openEv == null && !quizOpen && !mgOpen)
        {
            if (fpMode)
            {
                moving = FpMoveInput(dt, ref sprinting);   // 1인칭: 시선 기준 WASD
            }
            else
            {
            var d = Vector2.zero;
            // 카메라가 -Z를 보므로(yaw 180) 화면 왼쪽 = 월드 +X
            if (SKIn.Held(KeyCode.LeftArrow) || SKIn.Held(KeyCode.A)) d.x += 1;
            if (SKIn.Held(KeyCode.RightArrow) || SKIn.Held(KeyCode.D)) d.x -= 1;
            if (SKIn.Held(KeyCode.UpArrow) || SKIn.Held(KeyCode.W)) d.y -= 1;
            if (SKIn.Held(KeyCode.DownArrow) || SKIn.Held(KeyCode.S)) d.y += 1;
            if (d != Vector2.zero)
            {
                d.Normalize();
                // 쉬프트 = 달리기
                sprinting = SKIn.Held(KeyCode.LeftShift) || SKIn.Held(KeyCode.RightShift);
                float spd = SKData.SPEED * (sprinting ? SKData.RUN_MULT : 1f);
                var p = player.position;
                // 축 분리 이동: 막힌 축만 멈추고 나머지 축으로 미끄러짐
                float nx = Mathf.Clamp(p.x + d.x * spd * dt, 0.6f, roomW - 0.6f);
                if (!Blocked(new Vector3(nx, 0, p.z))) p.x = nx;
                float nz = Mathf.Clamp(p.z + d.y * spd * dt, 0.6f, roomD - 0.6f);
                if (!Blocked(new Vector3(p.x, 0, nz))) p.z = nz;
                player.position = p;
                moving = true;
                wasSprinting = sprinting;   // 튜토리얼 달리기 판정용
                float yaw = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
                // 일정 각속도 회전 — 대각 전환도 즉답 (Slerp의 넓은 회전 반경 제거)
                pbody.rotation = Quaternion.RotateTowards(pbody.rotation, Quaternion.Euler(0, yaw, 0), 780f * dt);
            }
            }
        }
        if (moving) StepSfx(dt, sprinting);
        if (hasAnim)
        {
            // 리깅 애니메이션: 정지=대기, 이동=걷기, 쉬프트·지진 중=뛰기
            int target = moving ? ((sprinting || (quakeState >= 1 && quakeState <= 3)) ? 2 : 1) : 0;
            SetAnimState(target);
            // 루프 보증 (클립 설정과 무관하게 되감기)
            if (graphAlive && pGraph.IsValid() && curClipLen > 0.01f)
            {
                double tt = pClipPlayable.GetTime();
                if (tt >= curClipLen) pClipPlayable.SetTime(tt % curClipLen);
            }
        }
        else
        {
            var bp = pbody.localPosition;
            var be = pbody.localEulerAngles;
            if (moving)
            {
                bp.y = Mathf.Abs(Mathf.Sin(timeAll * 11f)) * 0.14f;
                be.z = Mathf.Sin(timeAll * 11f) * 3.4f;
            }
            else
            {
                bp.y = Mathf.Lerp(bp.y, 0, 10f * dt);
                be.z = 0;
            }
            pbody.localPosition = bp;
            pbody.localEulerAngles = be;
        }

        // 미니게임·분사·조준 부채꼴·월드 라벨
        if (mgOpen) MgUpdate(dt);
        SprayUpdate(dt);
        UpdateAimCone();
        FpWorldUpdate();

        // 끓는 수면
        if (boilFoam != null)
        {
            boilFoam.localScale = new Vector3(0.38f, 0.025f, 0.38f) * (1f + 0.09f * Mathf.Sin(timeAll * 9f));
            var fp = boilFoam.position;
            fp.y = boilBaseY + 0.02f * Mathf.Sin(timeAll * 13f);
            boilFoam.position = fp;
        }

        // 진행
        if (!over)
        {
            if (TutActive)
            {
                TutUpdate(dt);   // 튜토리얼 중엔 스폰·타이머·지진 정지
            }
            else
            {
            // 지진 예고: 본지진 1.2초 전 럼블 + 미세 진동 + 조명 깜빡
            if (!quakeDone && quakeState == 0 && !preQ1 && stageT >= 28.8f)
            { preQ1 = true; StartCoroutine(PreQuakeFx()); }
            if (quakeDone && !quake2Done && quakeState == 0 && !preQ2 && stageT >= 46.8f)
            { preQ2 = true; StartCoroutine(PreQuakeFx()); }
            if (!quakeDone && quakeState == 0 && stageT >= 30f) StartQuake(1);
            else if (quakeDone && !quake2Done && quakeState == 0 && stageT >= 48f) StartQuake(2);
            if (quakeState >= 1 && quakeState <= 3)
            {
                QuakeUpdate(dt);
            }
            else
            {
            stageT += dt;
            if (stageT > SKData.DEMO_DUR)
            {
                over = true;
                ShowRank();
            }
            spawnT -= dt;
            if (spawnT <= 0f) { TrySpawn(); spawnT = 4f + Random.value * 2f; }
            for (int i = hazards.Count - 1; i >= 0; i--)
            {
                var hz = hazards[i];
                hz.t += dt;
                if (hz.bang != null)
                {
                    hz.bang.transform.rotation = cam.transform.rotation;
                    var lp = hz.bang.transform.localPosition;
                    lp.y = 0.6f + Mathf.Sin(timeAll * 3f + hz.id) * 0.08f;
                    hz.bang.transform.localPosition = lp;
                }
                if (hz.t > hz.ttl)
                {
                    acha++;
                    SKSound.Sfx("sfx_acha");
                    AddFloat(hz.node.transform.position, "아차!");
                    Say("아슬아슬! 체키가 처리했어", 2f);
                    if (hz.type == "yellow") SetFlame(1, false);
                    if (openEv == hz) CloseChoice();
                    Destroy(hz.node);
                    hazards.RemoveAt(i);
                }
            }
            }
            }
        }

        UpdateUI(dt);
    }

    void UpdateUI(float dt)
    {
        uiTimer.text = Mathf.Max(0, Mathf.CeilToInt(SKData.DEMO_DUR - stageT)) + "초";
        uiScore.text = "점수 " + score;
        uiAcha.text = "아차 " + acha;
        pnCombo.SetActive(combo > 1);
        if (combo > 1) uiCombo.text = "콤보 ×" + combo;

        pnToast.SetActive(msgT > 0 && msg != "");
        if (msgT > 0) uiToast.text = msg;
        // 튜토리얼 대화창이 하단을 차지할 땐 토스트를 상단으로 (겹침 방지)
        var toastRt = pnToast.GetComponent<RectTransform>();
        toastRt.anchoredPosition = (TutActive && pnTut != null && pnTut.activeSelf)
            ? new Vector2(280, -96) : new Vector2(280, -620);

        // 프롬프트 (지진 중엔 밸브 안내로 전환)
        bool showPrompt;
        string promptTxt = "[SPACE] 살펴보기";
        if (quakeState == 1) showPrompt = false;
        else if (quakeState == 2)
        {
            bool valveTask = quakeScen == 1 || !qValveLocked;
            if (valveTask && PlayerNearValve())
            {
                showPrompt = true;
                promptTxt = "[SPACE] 밸브 잠그기";
            }
            else if (quakeScen == 2 && !qWindowOpen && NearQuakeWindow())
            {
                showPrompt = true;
                promptTxt = "[SPACE] 창문 열기";
            }
            else showPrompt = false;
        }
        else if (quakeState == 3 && quakeScen == 2) showPrompt = false;   // 가스 배출 연출 중
        else if (quakeState == 3)
        {
            if (carryExt == null)
            {
                showPrompt = NearestExt() != null;
                promptTxt = "[SPACE] 소화기 들기";
            }
            else if (!quizOpen && !spraying)
            {
                bool nearFire = NearBurningFire() >= 0;
                showPrompt = true;
                promptTxt = nearFire ? "[클릭 꾹] 분사 — 좌우로 쓸어!" : "남은 불로 가자!";
            }
            else showPrompt = false;
        }
        else if (TutGate == TG_VALVE)
        {
            showPrompt = PlayerNearValve();
            promptTxt = "[SPACE] 밸브 잠그기";
        }
        else
        {
            bool nearHz = Nearest() != null;
            bool extNear = NearestExt() != null;
            showPrompt = !over && openEv == null && (nearHz || extNear || carryExt != null);
            if (!nearHz && showPrompt)
                promptTxt = carryExt != null ? "[SPACE] 소화기 내려놓기" : "[SPACE] 소화기 들기";
        }
        pnPrompt.SetActive(showPrompt && !mgOpen);
        if (showPrompt)
        {
            uiPrompt.text = promptTxt;
            var rt = pnPrompt.GetComponent<RectTransform>();
            if (fpMode)
            {
                // 1인칭: 크로스헤어 아래 고정 (머리 위 좌표는 카메라 뒤라 못 씀)
                rt.anchoredPosition = new Vector2(520, -455);
            }
            else
            {
                var ui = ToUI(player.position + new Vector3(0, 1.9f, 0));
                rt.anchoredPosition = new Vector2(ui.x - 120, ui.y + 20);
            }
        }

        // 월드 플로트
        for (int i = floats.Count - 1; i >= 0; i--)
        {
            var f = floats[i];
            f.life -= dt;
            if (f.life <= 0) { Destroy(f.t.gameObject); floats.RemoveAt(i); continue; }
            var ui = ToUI(f.world);
            var rt = f.t.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(ui.x, ui.y + (1.4f - f.life) * 40f);
            var c = f.t.color; c.a = Mathf.Min(1f, f.life); f.t.color = c;
        }
    }
}
