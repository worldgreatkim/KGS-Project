using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// 캠핑 교육 스테이지 (SafeKitchen3D_CAMP)
/// 시간·점수 없는 교육 전용: 위험 5곳을 찾아 해소 → 체크리스트 → 수료 카드.
/// 폐기 존은 정답 시 KGS 공식 영상 재생 (Resources/UI/CampDispose).
public partial class SKMain
{
    bool campMode;
    int campCleared;
    const int CAMP_TOTAL = 5;
    GameObject pnCampList;
    readonly Dictionary<string, Text> campChecks = new Dictionary<string, Text>();
    static readonly string[] CAMP_KEYS = { "camp_tent", "camp_foil", "camp_pan", "camp_can", "camp_dispose" };
    static readonly string[] CAMP_LABELS = { "텐트 안 버너", "호일 삼발이", "과대 불판", "모닥불 옆 캔", "부탄캔 폐기" };
    bool campDoneShown;

    // 영상 패널
    GameObject pnVideo;
    RawImage videoImg;
    VideoPlayer videoVp;
    bool videoOpen;

    static bool IsCampScene()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.EndsWith("_CAMP");
    }

    void CampInit()
    {
        campMode = IsCampScene();
        if (!campMode) return;
        // 야외 하늘 배경
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.78f, 0.92f);
        }
        // 교육 모드: 타이머·점수·아차 HUD 숨김
        if (uiTimer != null) uiTimer.transform.parent.gameObject.SetActive(false);
        if (uiScore != null) uiScore.transform.parent.gameObject.SetActive(false);
        if (uiAcha != null) uiAcha.transform.parent.gameObject.SetActive(false);
        if (pnUlt != null) pnUlt.SetActive(false);   // 필살기도 교육 모드엔 비노출
        if (fpHint != null) fpHint.gameObject.SetActive(false);   // 캠핑은 쿼터뷰 고정 — 안내 숨김
        // 위험 스폰 (시간 무제한) — camp_tent는 텐트 안에서 별도 등록
        foreach (var key in CAMP_KEYS)
        {
            if (key == "camp_tent") continue;
            var def = SKData.EV_CAMP[key];
            var node = SpawnMarker(SKData.HZ_CAMP[key]);
            var bang = SpawnBang(node);
            uid++;
            hazards.Add(new Hz { id = uid, type = key, def = def, node = node, bang = bang, ttl = def.ttl, reach = 2.2f });
        }
        CampTentInitState();
        CampTentEnterInit();
        BuildCampList();
        BuildVideoPanel();
        Say("캠핑장 곳곳의 위험 5곳을 찾아 고쳐 보자!", 5f);
    }

    // ---------- 체크리스트 UI (좌상단) ----------
    void BuildCampList()
    {
        pnCampList = new GameObject("campList");
        pnCampList.transform.SetParent(canvas.transform, false);
        var rt = pnCampList.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20, -52);   // 상단 안내문("[SPACE] …")과 겹치지 않게 한 줄 내림
        rt.sizeDelta = new Vector2(250, 40 + CAMP_TOTAL * 32);
        var bg = pnCampList.AddComponent<Image>();
        bg.color = new Color(0.10f, 0.13f, 0.19f, 0.78f);

        var trt = new GameObject("t").AddComponent<RectTransform>();
        trt.SetParent(pnCampList.transform, false);
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0, -6);
        trt.sizeDelta = new Vector2(230, 28);
        Label(trt.transform, "캠핑 안전 체크", 20, new Color(1f, 0.9f, 0.4f), TextAnchor.MiddleCenter, true);

        for (int i = 0; i < CAMP_TOTAL; i++)
        {
            var irt = new GameObject("i" + i).AddComponent<RectTransform>();
            irt.SetParent(pnCampList.transform, false);
            irt.anchorMin = irt.anchorMax = new Vector2(0f, 1f);
            irt.pivot = new Vector2(0f, 1f);
            irt.anchoredPosition = new Vector2(14, -38 - i * 32);
            irt.sizeDelta = new Vector2(230, 28);
            var t = Label(irt.transform, "□ " + CAMP_LABELS[i], 18, Color.white, TextAnchor.MiddleLeft, true);
            campChecks[CAMP_KEYS[i]] = t;
        }
    }

    /// Choose 정답 훅 — 체크 갱신 + 타입별 마무리
    void CampOnCorrect(string type)
    {
        if (!campMode || !campChecks.ContainsKey(type)) return;
        int idx = System.Array.IndexOf(CAMP_KEYS, type);
        campChecks[type].text = "✓ " + CAMP_LABELS[idx];
        campChecks[type].color = new Color(0.5f, 0.95f, 0.6f);
        campCleared++;
        if (type == "camp_tent") StartCoroutine(TentExitCo());   // 밖으로 복귀 + 텐트 열림
        if (type == "camp_can") FlyToSafeZone("C_FireCan");
        if (type == "camp_foil") CampRemoveFoil();
        if (type == "camp_pan") CampSwapPan();
        if (type == "camp_dispose") OpenVideo();
        if (campCleared >= CAMP_TOTAL && !campDoneShown)
        {
            campDoneShown = true;
            StartCoroutine(CampDoneCo());
        }
    }

    // ---------- 텐트 진입 시퀀스 ----------
    Transform tentInterior;      // 맵 밖 실내 구역 루트
    Transform tentHeater;        // 실내 가스난로
    GameObject tentDoorMark;     // 텐트 앞 느낌표 마커
    bool inTent;                 // 실내 여부
    bool campCut;                // 진입·퇴장 연출 중 (조작 잠금)
    bool campWalking;            // 연출 중 자동 보행 (걷기 애니·발소리 유지)
    string campPrompt;           // 이번 프레임 표시할 안내 (UpdateUI가 소비)
    Vector3 outsidePos;          // 밖에서의 플레이어 위치 (복귀용)
    bool heaterMoved;            // 난로를 안전지대로 옮겼는가
    Hz heaterHz;                 // 실내 난로 위험 (퀴즈 대상)
    static readonly Vector3 TENT_DOOR = new Vector3(4.5f, 0f, 5.0f);   // 텐트 앞 입구 지점

    void CampTentEnterInit()
    {
        tentInterior = FindKitchenChild("TentInterior");
        if (tentInterior == null) return;
        tentHeater = FindKitchenChild("int_heater");
        tentInterior.gameObject.SetActive(false);   // 평소엔 꺼둠

        // 텐트 앞 느낌표 마커
        tentDoorMark = SpawnMarker(TENT_DOOR + new Vector3(0, 0.2f, 0));
        tentDoorMark.name = "tent_door_mark";
        var bang = SpawnBang(tentDoorMark);
        if (bang != null) bang.text = "!";
    }

    /// 매 프레임 — 입구 근접 시 프롬프트, SPACE 처리
    /// 반환 true면 다른 상호작용(위험 퀴즈)보다 우선 소비
    bool CampTentInteract()
    {
        if (!campMode || tentInterior == null) return false;
        if (quizOpen || openEv != null || videoOpen || campCut) return false;

        if (!inTent)
        {
            if (heaterMoved || tentDoorMark == null) return false;
            float d = Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(TENT_DOOR.x, TENT_DOOR.z));
            if (d > 2.2f) return false;
            campPrompt = "[SPACE] 텐트 들어가기";
            if (SKIn.Down(KeyCode.Space)) StartCoroutine(TentEnterCo());
            return true;
        }
        return false;
    }

    /// 동물의 숲식 진입 연출:
    /// 자동 보행으로 입구를 통과 → 화면 암전 → 원형 아이리스가 열리며 실내 공개
    IEnumerator TentEnterCo()
    {
        campCut = true;
        // 1) 입구까지 걸어 들어간다 (조작 잠금, 걷기 애니·발소리는 그대로)
        if (!camSaved) { camSavePos = cam.transform.position; camSaveRot = cam.transform.rotation; camSaved = true; }
        Vector3 from = player.position;
        Vector3 to = new Vector3(TENT_DOOR.x, from.y, TENT_DOOR.z - 2.1f);   // 텐트 안쪽까지
        Vector3 cam0 = cam.transform.position;
        Vector3 cam1 = cam0 + new Vector3(0, -0.9f, -1.6f);                  // 살짝 다가가는 돌리인
        pbody.rotation = Quaternion.Euler(0, 180f, 0);                       // 텐트 쪽(−Z)을 향해
        // 문이 먼저 양옆으로 열린다
        SKSound.Sfx("sfx_window", 0.55f);
        if (doorL != null) StartCoroutine(DoorSetCo(true, 0.95f));   // 걷기와 동시에 갈라진다
        else if (tentHouseDoor != null) yield return StartCoroutine(TentPhaseCo(tentHouseDoor));
        else yield return StartCoroutine(FlapSetCo(true, 0.40f));
        campWalking = true;
        float wt = 0f, wdur = 1.45f;
        while (wt < wdur)
        {
            wt += Time.deltaTime;
            float k = Mathf.Clamp01(wt / wdur);
            player.position = Vector3.Lerp(from, to, k);
            cam.transform.position = Vector3.Lerp(cam0, cam1, Mathf.SmoothStep(0f, 1f, k));
            yield return null;
        }
        campWalking = false;
        // 절차 텐트일 때만 뒤에서 문을 닫는다 (GLB 방식은 '문 열림'을 유지)
        if (doorL == null && tentHouseDoor == null) StartCoroutine(FlapSetCo(false, 0.35f));

        // 2) 암전 (플레이어는 이미 텐트에 가려져 보이지 않는다)
        yield return StartCoroutine(FadeOut());
        yield return new WaitForSeconds(0.22f);

        outsidePos = from;
        inTent = true;
        tentInterior.gameObject.SetActive(true);
        if (tentDoorMark != null) tentDoorMark.SetActive(false);
        // 플레이어·카메라 실내로
        player.position = tentInterior.position + new Vector3(0, 0, 1.9f);
        pbody.rotation = Quaternion.Euler(0, 180f, 0);
        // 동물의 숲식 실내 구도: 시선을 낮춰(34°) 삼각 박공 벽이 정면에 서고 방 전체가 들어온다
        CampCamTo(tentInterior.position + new Vector3(0, 1.00f, -0.30f), 11.0f, 34f);
        // 실내 위험(난로) 등록 — 기존 퀴즈 시스템 그대로 사용
        if (heaterHz == null && tentHeater != null)
        {
            var def = SKData.EV_CAMP["camp_tent"];
            var node = SpawnMarker(tentHeater.position + new Vector3(0, 0.9f, 0));
            var bang = SpawnBang(node);
            uid++;
            heaterHz = new Hz { id = uid, type = "camp_tent", def = def, node = node, bang = bang, ttl = def.ttl, reach = 2.2f };
            hazards.Add(heaterHz);
        }
        Say("텐트 안이야. 어? 가스난로가 켜져 있어!", 4f);
        // 3) 원형 아이리스가 열리며 실내 전체를 공개 (동물의 숲 입장 연출)
        SKSound.Sfx("sfx_popup", 0.5f);
        yield return StartCoroutine(IrisCo(true, 0.85f));
        campCut = false;
        StartCoroutine(TentCoDangerCo());   // 경보음 + CO 안개 축적
    }

    // ---------- 원형 아이리스 전환 ----------
    Image irisImg; Texture2D irisTex;
    const int IRIS_N = 256;
    void IrisSet(float r)
    {
        if (irisImg == null)
        {
            var go = new GameObject("iris");
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            irisImg = go.AddComponent<Image>();
            irisImg.raycastTarget = false;
            irisTex = new Texture2D(IRIS_N, IRIS_N, TextureFormat.RGBA32, false);
            irisTex.wrapMode = TextureWrapMode.Clamp;
            irisTex.filterMode = FilterMode.Bilinear;
            irisImg.sprite = Sprite.Create(irisTex, new Rect(0, 0, IRIS_N, IRIS_N), new Vector2(0.5f, 0.5f));
        }
        irisImg.gameObject.SetActive(true);
        // 화면 비율을 보정해 정원(正圓)으로 — 안쪽은 투명, 바깥은 검정
        float asp = Screen.width / Mathf.Max(1f, (float)Screen.height);
        float diag = Mathf.Sqrt(asp * asp + 1f);
        var px = new Color32[IRIS_N * IRIS_N];
        for (int y = 0; y < IRIS_N; y++)
        {
            float v = (y + 0.5f) / IRIS_N * 2f - 1f;
            for (int x = 0; x < IRIS_N; x++)
            {
                float u = ((x + 0.5f) / IRIS_N * 2f - 1f) * asp;
                float d = Mathf.Sqrt(u * u + v * v) / diag;
                float a = Mathf.Clamp01((d - r) / 0.035f);
                px[y * IRIS_N + x] = new Color32(0, 0, 0, (byte)(a * 255f));
            }
        }
        irisTex.SetPixels32(px);
        irisTex.Apply(false);
    }
    /// open=true: 검은 화면에서 원이 열림 / false: 원이 닫히며 암전
    IEnumerator IrisCo(bool open, float dur)
    {
        IrisSet(open ? 0f : 1.06f);
        if (open && fadeImg != null) { fadeImg.gameObject.SetActive(false); fadeImg.color = Color.clear; }
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
            IrisSet(open ? k * 1.06f : (1f - k) * 1.06f);
            yield return null;
        }
        if (open) irisImg.gameObject.SetActive(false);
        else
        {
            IrisSet(0f);
            if (fadeImg == null) yield return StartCoroutine(FadeOut());
            else { fadeImg.gameObject.SetActive(true); fadeImg.color = Color.black; }
            irisImg.gameObject.SetActive(false);
        }
    }

    /// 실내에서는 맵 경계 대신 텐트 방 안으로 이동을 제한한다 (SKMain 이동 처리에서 호출)
    void CampMoveBounds(ref float x0, ref float x1, ref float z0, ref float z1)
    {
        if (!campMode || !inTent || tentInterior == null) return;
        var c = tentInterior.position;
        x0 = c.x - 3.5f; x1 = c.x + 3.5f;
        z0 = c.z - 2.3f; z1 = c.z + 2.7f;
    }

    /// 동물의 숲식 숲속 공터 — 맵 둘레를 나무로 두르고 원경은 안개로 흐림
    void CampBuildForest()
    {
        if (GameObject.Find("CampForest") != null) return;
        var root = new GameObject("CampForest").transform;
        string[] kinds = { "Assets/Kenney/K_tree.glb", "Assets/Kenney/K_tree-tall.glb", "Assets/Kenney/K_tree-autumn.glb" };
        var prefabs = new List<GameObject>();
#if UNITY_EDITOR
        foreach (var k in kinds)
        {
            var p = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(k);
            if (p != null) prefabs.Add(p);
        }
#endif
        if (prefabs.Count == 0) return;
        // 맵(가로 19.2 세로 10.8) 둘레 3중 링 — 안쪽은 성기게, 바깥은 빽빽하게
        float cx = 10.7f, cz = 5.4f;
        for (int ring = 0; ring < 3; ring++)
        {
            float rx = 13f + ring * 5.5f, rz = 9.5f + ring * 5.0f;
            int count = 16 + ring * 8;
            for (int i = 0; i < count; i++)
            {
                float a = (i / (float)count) * Mathf.PI * 2f + ring * 0.35f;
                var pos = new Vector3(cx + Mathf.Cos(a) * rx + Random.Range(-1.4f, 1.4f), 0f,
                                      cz + Mathf.Sin(a) * rz + Random.Range(-1.4f, 1.4f));
                var g = Instantiate(prefabs[Random.Range(0, prefabs.Count)], root);
                g.transform.position = pos;
                g.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                float sc = Random.Range(3.2f, 4.6f) + ring * 0.5f;
                g.transform.localScale = Vector3.one * sc;
                // 뒤로 갈수록 하늘색에 물들게 (원경 페이드)
                float fade = 0.18f * ring;
                foreach (var r in g.GetComponentsInChildren<Renderer>())
                {
                    var m = r.material;
                    m.color = Color.Lerp(m.color, new Color(0.60f, 0.82f, 0.95f), fade);
                }
            }
        }
        // 경계선 위장: 맵 가장자리에 풀·바위를 흩뿌려 바닥 이음매를 가림
#if UNITY_EDITOR
        var deco = new List<GameObject>();
        foreach (var k in new[]{ "Assets/Kenney/K_grass.glb", "Assets/Kenney/K_grass-large.glb",
                                 "Assets/Kenney/K_patch-grass.glb", "Assets/Kenney/K_rock-a.glb" })
        {
            var p = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(k);
            if (p != null) deco.Add(p);
        }
        if (deco.Count > 0)
            for (int i = 0; i < 46; i++)
            {
                float a = (i / 46f) * Mathf.PI * 2f;
                float rx = 10.6f + Random.Range(-0.9f, 1.6f), rz = 6.4f + Random.Range(-0.7f, 1.4f);
                var pos = new Vector3(cx + Mathf.Cos(a) * rx, 0.02f, cz + Mathf.Sin(a) * rz);
                var g = Instantiate(deco[Random.Range(0, deco.Count)], root);
                g.transform.position = pos;
                g.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                g.transform.localScale = Vector3.one * Random.Range(1.6f, 2.8f);
            }
#endif
        // 안개: 원경이 하늘색으로 녹아 경계가 사라짐
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.60f, 0.82f, 0.95f);
        RenderSettings.fogStartDistance = 26f;
        RenderSettings.fogEndDistance = 58f;
        // 앰비언트: 회색 → 따뜻한 하늘빛
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.74f, 0.86f);
        RenderSettings.ambientEquatorColor = new Color(0.58f, 0.62f, 0.55f);
        RenderSettings.ambientGroundColor = new Color(0.36f, 0.40f, 0.32f);
    }

    // ---------- 텐트 안 일산화탄소 위험 연출 ----------
    readonly List<Transform> coClouds = new List<Transform>();
    Transform gasAlert;          // CO 경보기 (깜빡임)
    bool coRunning;

    IEnumerator TentCoDangerCo()
    {
        coRunning = true;
        gasAlert = FindKitchenChild("int_alert");
        var prefab = Resources.Load<GameObject>("VFX/GasFog");
        float t = 0f;
        int n = 0;
        Renderer alertR = gasAlert != null ? gasAlert.GetComponentInChildren<Renderer>() : null;
        Color alert0 = alertR != null ? alertR.material.color : Color.white;
        float beep = 0f;
        while (coRunning && inTent)
        {
            t += Time.deltaTime;
            beep -= Time.deltaTime;
            // 경보음 (1.1초 간격) + 경보기 붉은 점멸
            if (beep <= 0f)
            {
                beep = 1.1f;
                SKSound.Sfx("sfx_acha", 0.55f, 1.5f);
                if (alertR != null) StartCoroutine(AlertBlink(alertR, alert0));
            }
            // CO 안개 축적 (2.2초마다 1덩이, 최대 7)
            if (prefab != null && n < 7 && t > 1.0f + n * 2.2f)
            {
                var pos = tentInterior.position + new Vector3(
                    Random.Range(-3.2f, 3.2f), 0.35f, Random.Range(-2.2f, 2.2f));
                var g = Instantiate(prefab);
                g.name = "co_fog" + n;
                g.transform.position = pos;
                var ps = g.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var m = ps.main;
                    m.startSize = new ParticleSystem.MinMaxCurve(1.5f, 2.6f);
                    m.startColor = new ParticleSystem.MinMaxGradient(
                        new Color(0.80f, 0.66f, 0.60f, 0.42f), new Color(0.66f, 0.52f, 0.48f, 0.32f));
                    m.maxParticles = 50;
                    var sh = ps.shape; sh.scale = new Vector3(2.2f, 0.6f, 2.2f);
                    var em = ps.emission; em.rateOverTime = 8f;
                    ps.Clear(); ps.Play();
                }
                coClouds.Add(g.transform);
                n++;
                if (n == 3) Say("일산화탄소 경보기가 울려! 무색무취라 눈엔 안 보여 — 위험해!", 4f);
            }
            yield return null;
        }
    }

    IEnumerator AlertBlink(Renderer r, Color c0)
    {
        if (r == null) yield break;
        r.material.color = new Color(0.95f, 0.25f, 0.20f);
        yield return new WaitForSeconds(0.22f);
        if (r != null) r.material.color = c0;
    }

    /// 환기 성공 → CO 안개가 빨려나가며 소멸 + 경보 정지
    IEnumerator CoClearCo()
    {
        coRunning = false;
        Vector3 to = tentInterior != null ? tentInterior.position + new Vector3(0, 3.5f, 3.5f) : Vector3.zero;
        var starts = new List<Vector3>();
        var scales = new List<Vector3>();
        foreach (var c in coClouds) { starts.Add(c != null ? c.position : Vector3.zero); scales.Add(c != null ? c.localScale : Vector3.one); }
        float t = 0f;
        while (t < 1.1f)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / 1.1f);
            for (int i = 0; i < coClouds.Count; i++)
            {
                var c = coClouds[i];
                if (c == null) continue;
                c.position = Vector3.Lerp(starts[i], to, Mathf.SmoothStep(0f, 1f, k));
                c.localScale = scales[i] * (1f - 0.85f * k);
            }
            yield return null;
        }
        foreach (var c in coClouds) if (c != null) Destroy(c.gameObject);
        coClouds.Clear();
    }

    /// 퀴즈 정답 후: 밖으로 복귀 + 텐트 열림 + 난로가 텐트 앞에 나와 있음
    IEnumerator TentExitCo()
    {
        yield return StartCoroutine(CoClearCo());   // CO 안개 배출 + 경보 정지
        campCut = true;
        yield return StartCoroutine(IrisCo(false, 0.55f));   // 원이 닫히며 암전 (입장의 역순)
        yield return new WaitForSeconds(0.18f);
        inTent = false;
        // 난로를 텐트 앞으로 이동 (실외 오브젝트로 재부모)
        if (tentHeater != null)
        {
            tentHeater.SetParent(FindKitchenChild("Kitchen") ?? tentHeater.parent, true);
            var k = GameObject.Find("Kitchen");
            if (k != null) tentHeater.SetParent(k.transform, true);
            tentHeater.position = new Vector3(4.5f, 0.1f, 5.6f);
            tentHeater.rotation = Quaternion.Euler(0, 180f, 0);
            tentHeater.name = "OutHeater";
        }
        tentInterior.gameObject.SetActive(false);
        player.position = outsidePos;
        CampCamRestore();
        CampSwapTentOpen();          // 텐트 열림 (문·창문 개방)
        SKSound.Sfx("sfx_vent", 0.85f);
        var wind = Resources.Load<GameObject>("VFX/CFXR4 Wind Trails");
        if (wind != null)
        {
            var w = Instantiate(wind);
            w.transform.position = new Vector3(4.5f, 1.0f, 3.6f);
            w.transform.rotation = Quaternion.LookRotation(new Vector3(0, 0.2f, 1f));
            var ps = w.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play(true);
            Destroy(w, 3.5f);
        }
        Say("환기 완료! 이제 난로를 안전지대로 옮기자 — 다가가서 [SPACE]", 5f);
        yield return StartCoroutine(FadeIn());
        campCut = false;
    }

    /// 밖으로 나온 난로 → 안전지대 이송 (SPACE)
    bool CampHeaterCarry()
    {
        if (!campMode || heaterMoved || inTent) return false;
        var oh = GameObject.Find("OutHeater");
        if (oh == null) return false;
        float d = Vector2.Distance(new Vector2(player.position.x, player.position.z),
                                   new Vector2(oh.transform.position.x, oh.transform.position.z));
        if (d > 2.2f) return false;
        campPrompt = "[SPACE] 안전지대로 옮기기";
        if (SKIn.Down(KeyCode.Space))
        {
            heaterMoved = true;
            var basket = GameObject.Find("SafeBasket");
            Vector3 to = basket != null ? RB(basket).center + new Vector3(0.7f, 0.1f, 0) : new Vector3(5.2f, 0.3f, 8.6f);
            StartCoroutine(FlyObjCo(oh.transform, to));
            Say("잘했어! 가스난로는 텐트 밖 안전한 곳에 ★", 4f);
            CampOnCorrect("camp_tent_done");
        }
        return true;
    }

    // 카메라 이동 (쿼터뷰 각도 유지)
    Vector3 camSavePos; Quaternion camSaveRot; bool camSaved;
    void CampCamTo(Vector3 center, float dist, float pitch = 52f)
    {
        if (cam == null) return;
        if (!camSaved) { camSavePos = cam.transform.position; camSaveRot = cam.transform.rotation; camSaved = true; }
        float rad = pitch * Mathf.Deg2Rad;
        cam.transform.position = center + new Vector3(0, Mathf.Sin(rad), Mathf.Cos(rad)) * dist;
        cam.transform.rotation = Quaternion.Euler(pitch, 180f, 0);
        cam.backgroundColor = new Color(0.30f, 0.22f, 0.10f);   // 텐트 속 어둑한 톤
        // 외곽 마루(캠핑장 초록)도 어둡게 — 실내에서 바깥 잔디가 비치지 않게
        var of = GameObject.Find("outer_floor");
        if (of != null)
            foreach (var r in of.GetComponentsInChildren<Renderer>())
                r.material.color = new Color(0.30f, 0.22f, 0.10f);
    }
    void CampCamRestore()
    {
        if (cam == null || !camSaved) return;
        cam.transform.position = camSavePos;
        cam.transform.rotation = camSaveRot;
        cam.backgroundColor = new Color(0.55f, 0.78f, 0.92f);
        var of = GameObject.Find("outer_floor");
        if (of != null)
            foreach (var r in of.GetComponentsInChildren<Renderer>())
                r.material.color = new Color(0.24f, 0.42f, 0.26f);   // 숲 초록 복귀
        camSaved = false;
    }

    // 화면 페이드 (전환 연출)
    Image fadeImg;
    IEnumerator FadeOut()
    {
        if (fadeImg == null)
        {
            var go = new GameObject("fade");
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            fadeImg = go.AddComponent<Image>();
        }
        fadeImg.gameObject.SetActive(true);
        float t = 0f;
        while (t < 0.28f)
        {
            t += Time.deltaTime;
            fadeImg.color = new Color(0, 0, 0, Mathf.Clamp01(t / 0.28f));
            yield return null;
        }
        fadeImg.color = Color.black;
    }
    IEnumerator FadeIn()
    {
        if (fadeImg == null) yield break;
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            fadeImg.color = new Color(0, 0, 0, 1f - Mathf.Clamp01(t / 0.35f));
            yield return null;
        }
        fadeImg.gameObject.SetActive(false);
    }

    // ---------- 해소 연출 ----------
    /// 텐트: 버너를 밖(안전 조리 구역)으로 옮기고 → 측면·후면 플랩을 열어 환기
    IEnumerator CampTentFixCo()
    {
        var burner = GameObject.Find("C_TentBurner");
        if (burner != null)
        {
            var from = burner.transform.position;
            var to = new Vector3(7.5f, 1.05f, 9.2f);   // 야외 조리 테이블 옆
            float t = 0f, dur = 0.9f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                var p = Vector3.Lerp(from, to, k);
                p.y += Mathf.Sin(k * Mathf.PI) * 1.6f;
                burner.transform.position = p;
                burner.transform.Rotate(0, 360f * Time.deltaTime, 0);
                yield return null;
            }
            burner.transform.position = to;
            burner.transform.rotation = Quaternion.Euler(0, -90f, 0);
            SKSound.Sfx("sfx_popup", 0.8f, 1.2f);
        }
        // 환기: 닫힌 텐트 → 열린 텐트로 교체 + 바람 이펙트
        CampSwapTentOpen();
        AddFloat(new Vector3(4.5f, 2.2f, 2.4f), "환기 완료!");
        var wind = Resources.Load<GameObject>("VFX/CFXR4 Wind Trails");
        if (wind != null)
        {
            var w = Instantiate(wind);
            w.transform.position = new Vector3(4.5f, 1.0f, 3.4f);
            w.transform.rotation = Quaternion.LookRotation(new Vector3(0, 0.2f, 1f));
            var ps = w.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play(true);
            Destroy(w, 3.5f);
        }
        SKSound.Sfx("sfx_vent", 0.85f);
    }

    // 텐트 교체 대상 (비활성 오브젝트라 GameObject.Find로는 못 찾음 — 시작 시 캐시)
    Transform tentClosed, tentOpen;

    /// Kitchen 자식에서 이름으로 찾기 (비활성 포함)
    Transform FindKitchenChild(string name)
    {
        var k = GameObject.Find("Kitchen");
        if (k == null) return null;
        foreach (var t in k.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    /// 환기: 문짝 두 장을 양쪽으로 활짝 (절차 생성 텐트),
    /// 없으면 예전 방식(닫힌 텐트 → 열린 텐트 GLB 교체)로 폴백
    void CampSwapTentOpen()
    {
        // 본체+문짝 방식: 창이 열린 본체로 즉시 교체(바람 VFX가 순간을 덮는다) + 문은 계속 열린 채
        if (tentBody != null && tentBodyOpen != null)
        {
            tentBody.gameObject.SetActive(false);
            tentBodyOpen.gameObject.SetActive(true);
            // 문짝은 통째로 끈다 — 바람 VFX가 터지는 순간이라 사라지는 게 보이지 않고,
            // 위험한 '완전 개방'(여유 0)을 아예 하지 않아도 된다
            if (doorL != null) doorL.gameObject.SetActive(false);
            if (doorR != null) doorR.gameObject.SetActive(false);
            return;
        }
        if (tentHouseOpen != null) { StartCoroutine(TentPhaseCo(tentHouseOpen)); return; }
        if (flapL != null && flapR != null) { StartCoroutine(FlapOpenCo()); return; }
        if (tentClosed == null || tentOpen == null) return;
        StartCoroutine(TentSwapCo(tentClosed, tentOpen));
    }

    Transform flapL, flapR, winL, winR;
    Transform tentHouse, tentHouseDoor, tentHouseOpen;   // 문닫·문열(창닫)·문열+창열
    Transform tentBody, tentBodyOpen, doorL, doorR;      // 본체(창닫/창열) + 좌우로 갈린 문짝
    Vector3 doorL0, doorR0;                              // 문짝 닫힘 위치 (씬에 배치된 값)

    // 문짝은 천 표면보다 뒤에 있어, 옆으로 밀면 텐트 천에 가려 사라진다.
    // 캐릭터가 지나갈 만큼만 열면 되므로 0.45 — 천에 가려지는 한계(0.69)까지 0.24 여유.
    // (열린 폭 0.86 vs 캐릭터 충돌 폭 0.52)
    const float DOOR_SLIDE = 0.45f;

    /// u=0 닫힘, u=1 열림. 화면에서 문이 비스듬히 움직이지 않도록 **월드 X 축으로만** 민다
    /// (문짝의 로컬 X는 정면 기울기 16°만큼 z 성분이 섞여 있어 대각선으로 보인다)
    void DoorSlide(float u)
    {
        if (doorL == null || doorR == null) return;
        doorL.position = doorL0 + Vector3.left * (DOOR_SLIDE * u);
        doorR.position = doorR0 + Vector3.right * (DOOR_SLIDE * u);
    }

    IEnumerator DoorSetCo(bool open, float dur)
    {
        if (doorL == null || doorR == null) yield break;
        float u0 = Vector3.Distance(doorL.position, doorL0) / DOOR_SLIDE;
        float u1 = open ? 1f : 0f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            DoorSlide(Mathf.Lerp(u0, u1, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur))));
            yield return null;
        }
        DoorSlide(u1);
    }

    /// 3단 상태 텐트를 to 상태로 교체 — 지금 켜져 있는 것을 찾아 팝 연출로 넘긴다
    IEnumerator TentPhaseCo(Transform to)
    {
        if (to == null) yield break;
        Transform from = null;
        foreach (var t in new[] { tentHouse, tentHouseDoor, tentHouseOpen })
            if (t != null && t != to && t.gameObject.activeSelf) from = t;
        if (from == null) { to.gameObject.SetActive(true); yield break; }
        yield return StartCoroutine(TentSwapCo(from, to));
    }

    IEnumerator FlapOpenCo()
    {
        StartCoroutine(WindowOpenCo());     // 환기니까 양옆 창도 같이 젖힌다
        yield return StartCoroutine(FlapSetCo(true, 0.55f));
    }

    // 열렸을 때 기둥에 남는 천 폭 (0이면 완전히 사라져 '문이 없는 텐트'로 보인다)
    const float FLAP_OPEN_W = 0.10f;

    /// 문짝 두 장 여닫기 — 각 문짝의 원점이 바깥 기둥이라
    /// 가로 폭을 줄이면 천이 기둥 쪽으로 걷히며 접힌다. 살짝 젖혀 접힌 각을 준다.
    IEnumerator FlapSetCo(bool open, float dur)
    {
        if (flapL == null || flapR == null) yield break;
        var a0 = flapL.localRotation; var b0 = flapR.localRotation;
        var p0 = flapL.localScale;    var q0 = flapR.localScale;
        var a1 = Quaternion.Euler(0, open ? 25f : 0f, 0);
        var b1 = Quaternion.Euler(0, open ? -25f : 0f, 0);
        var s1 = new Vector3(open ? FLAP_OPEN_W : 1f, 1f, 1f);
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
            flapL.localRotation = Quaternion.Slerp(a0, a1, k);
            flapR.localRotation = Quaternion.Slerp(b0, b1, k);
            flapL.localScale = Vector3.Lerp(p0, s1, k);
            flapR.localScale = Vector3.Lerp(q0, s1, k);
            yield return null;
        }
        flapL.localRotation = a1; flapR.localRotation = b1;
        flapL.localScale = s1;    flapR.localScale = s1;
    }

    /// 창 덮개 — 위 경첩이라 바깥·위로 들린다 (차양처럼)
    IEnumerator WindowOpenCo()
    {
        if (winL == null || winR == null) yield break;
        var a0 = winL.localRotation; var b0 = winR.localRotation;
        var a1 = Quaternion.Euler(0, 0, -62f);
        var b1 = Quaternion.Euler(0, 0, 62f);
        float t = 0f, dur = 0.6f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
            winL.localRotation = Quaternion.Slerp(a0, a1, k);
            winR.localRotation = Quaternion.Slerp(b0, b1, k);
            yield return null;
        }
        winL.localRotation = a1; winR.localRotation = b1;
    }

    IEnumerator TentSwapCo(Transform closed, Transform open)
    {
        // 닫힌 텐트가 살짝 부푼 뒤 사라지고 열린 텐트가 팝인 — 플랩이 걷히는 인상
        var c0 = closed.localScale;
        float t = 0f;
        while (t < 0.18f)
        {
            t += Time.deltaTime;
            closed.localScale = c0 * Mathf.Lerp(1f, 1.06f, t / 0.18f);
            yield return null;
        }
        closed.gameObject.SetActive(false);
        closed.localScale = c0;

        var o0 = open.localScale;
        open.gameObject.SetActive(true);
        open.localScale = o0 * 0.94f;
        t = 0f;
        while (t < 0.22f)
        {
            t += Time.deltaTime;
            open.localScale = Vector3.Lerp(o0 * 0.94f, o0, t / 0.22f);
            yield return null;
        }
        open.localScale = o0;
    }

    /// 시작 상태 정리 — 닫힌 텐트 노출, 열린 텐트 숨김 (에셋 준비 전엔 무동작)
    void CampTentInitState()
    {
        // 1순위: 본체 + 분리된 문짝 (본체는 그대로 두고 문짝만 걷히므로 화면이 튀지 않는다)
        tentBody = FindKitchenChild("C_TentBody");
        tentBodyOpen = FindKitchenChild("C_TentBodyOpen");
        doorL = FindKitchenChild("C_TentDoorL");
        doorR = FindKitchenChild("C_TentDoorR");
        if (tentBody != null && tentBodyOpen != null && doorL != null && doorR != null)
        {
            // 배치(회전·스케일)는 씬 값을 그대로 쓰고 위치만 기억한다 — 코드가 배치를 덮어쓰지 않게
            doorL0 = doorL.position;
            doorR0 = doorR.position;
            tentBody.gameObject.SetActive(true);
            tentBodyOpen.gameObject.SetActive(false);
            doorL.gameObject.SetActive(true);
            doorR.gameObject.SetActive(true);
            return;
        }

        // 2순위: 3단 상태 GLB 텐트 (문닫·문열·문열+창열)
        tentHouse = FindKitchenChild("C_TentHouse");
        tentHouseDoor = FindKitchenChild("C_TentHouseDoor");
        tentHouseOpen = FindKitchenChild("C_TentHouseOpen");
        if (tentHouse != null && tentHouseDoor != null && tentHouseOpen != null)
        {
            tentHouse.gameObject.SetActive(true);
            tentHouseDoor.gameObject.SetActive(false);
            tentHouseOpen.gameObject.SetActive(false);
            return;
        }

        // 2순위: 절차 생성 텐트(C_TentAF)가 있으면 문짝을 닫힌 각도로 리셋
        flapL = FindKitchenChild("flapL");
        flapR = FindKitchenChild("flapR");
        winL = FindKitchenChild("winL");
        winR = FindKitchenChild("winR");
        if (flapL != null) { flapL.localRotation = Quaternion.identity; flapL.localScale = Vector3.one; }
        if (flapR != null) { flapR.localRotation = Quaternion.identity; flapR.localScale = Vector3.one; }
        if (winL != null) winL.localRotation = Quaternion.identity;
        if (winR != null) winR.localRotation = Quaternion.identity;
        if (flapL != null && flapR != null) return;

        tentClosed = FindKitchenChild("C_CampTentClosed");
        tentOpen = FindKitchenChild("C_CampTentOpen");
        if (tentClosed == null || tentOpen == null) return;
        tentClosed.gameObject.SetActive(true);
        tentOpen.gameObject.SetActive(false);
    }

    /// 모닥불 옆 캔 → 안전지대 바구니로 비행 (towel 연출 재사용 문법)
    void FlyToSafeZone(string objName)
    {
        var go = GameObject.Find(objName);
        if (go == null) return;
        var basket = GameObject.Find("SafeBasket");
        Vector3 to = basket != null ? RB(basket).center + new Vector3(0, 0.3f, 0) : new Vector3(4.3f, 1.0f, 8.6f);
        StartCoroutine(FlyObjCo(go.transform, to));
    }

    IEnumerator FlyObjCo(Transform tr, Vector3 to)
    {
        var from = tr.position;
        float t = 0f, dur = 0.85f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            var p = Vector3.Lerp(from, to, k);
            p.y += Mathf.Sin(k * Mathf.PI) * 2.0f;
            tr.position = p;
            tr.Rotate(0, 420f * Time.deltaTime, 0);
            yield return null;
        }
        tr.position = to;
        SKSound.Sfx("sfx_popup", 0.8f, 1.25f);
        AddFloat(to + new Vector3(0, 0.4f, 0), "안전지대로!");
    }

    /// 호일 링 제거 (구겨지듯 축소)
    void CampRemoveFoil()
    {
        var f = GameObject.Find("C_FoilRing");
        if (f != null) StartCoroutine(ShrinkAway(f.transform));
        AddFloat(SKData.HZ_CAMP["camp_foil"] + new Vector3(0, 0.5f, 0), "호일 제거!");
    }

    /// 과대불판 → 작은 팬으로 교체 (스케일 축소)
    void CampSwapPan()
    {
        var p = GameObject.Find("C_BigPan");
        if (p == null) return;
        StartCoroutine(ScaleTo(p.transform, p.transform.localScale * 0.55f));
        AddFloat(SKData.HZ_CAMP["camp_pan"] + new Vector3(0, 0.5f, 0), "작은 팬으로!");
    }

    IEnumerator ShrinkAway(Transform tr)
    {
        var s0 = tr.localScale;
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            tr.localScale = Vector3.Lerp(s0, s0 * 0.01f, t / 0.35f);
            tr.Rotate(0, 720f * Time.deltaTime, 0);
            yield return null;
        }
        Destroy(tr.gameObject);
    }

    IEnumerator ScaleTo(Transform tr, Vector3 target)
    {
        var s0 = tr.localScale;
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            tr.localScale = Vector3.Lerp(s0, target, t / 0.4f);
            yield return null;
        }
        tr.localScale = target;
    }

    IEnumerator CampDoneCo()
    {
        yield return new WaitForSeconds(videoOpen ? 1.0f : 2.0f);
        while (videoOpen) yield return null;   // 영상 다 보고 나서
        SKSound.Sfx("st_win", 0.9f);
        SKSound.Vo("vo_badge_all");
        Say("캠핑 안전 다섯 가지, 전부 마스터! 가족에게도 알려주자 ★", 6f);
        BadgeProgress();
    }

    // ---------- 폐기 영상 패널 ----------
    void BuildVideoPanel()
    {
        pnVideo = new GameObject("videoPanel");
        pnVideo.transform.SetParent(canvas.transform, false);
        var rt = pnVideo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var dimImg = pnVideo.AddComponent<Image>();
        dimImg.color = new Color(0.04f, 0.06f, 0.09f, 0.88f);

        var vrt = new GameObject("vid").AddComponent<RectTransform>();
        vrt.SetParent(pnVideo.transform, false);
        vrt.anchorMin = vrt.anchorMax = new Vector2(0.5f, 0.5f);
        vrt.pivot = new Vector2(0.5f, 0.5f);
        vrt.anchoredPosition = new Vector2(0, 16);
        vrt.sizeDelta = new Vector2(880, 495);
        videoImg = vrt.gameObject.AddComponent<RawImage>();
        videoImg.color = Color.black;

        var hrt = new GameObject("h").AddComponent<RectTransform>();
        hrt.SetParent(pnVideo.transform, false);
        hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0.5f);
        hrt.pivot = new Vector2(0.5f, 0.5f);
        hrt.anchoredPosition = new Vector2(0, -268);
        hrt.sizeDelta = new Vector2(700, 30);
        Label(hrt.transform, "한국가스안전공사 공식 영상 — [SPACE] 닫기", 19, Color.white, TextAnchor.MiddleCenter, true);
        pnVideo.SetActive(false);
    }

    void OpenVideo()
    {
        var clip = Resources.Load<VideoClip>("UI/CampDispose");
        if (clip == null)
        {
            Say("(폐기 안내 영상은 준비 중!) 야외에서 잔가스를 빼고 배출하자!", 4f);
            return;
        }
        if (videoVp == null)
        {
            videoVp = gameObject.AddComponent<VideoPlayer>();
            videoVp.playOnAwake = false;
            videoVp.renderMode = VideoRenderMode.RenderTexture;
            var tex = new RenderTexture((int)clip.width, (int)clip.height, 0);
            videoVp.targetTexture = tex;
            videoImg.texture = tex;
            videoVp.isLooping = false;
            videoVp.loopPointReached += _ => CloseVideo();
        }
        videoVp.clip = clip;
        pnVideo.SetActive(true);
        videoOpen = true;
        Time.timeScale = 0f;
        videoVp.Play();
    }

    void CloseVideo()
    {
        if (!videoOpen) return;
        videoOpen = false;
        if (videoVp != null) videoVp.Stop();
        pnVideo.SetActive(false);
        Time.timeScale = 1f;
    }

    bool campSkySet;

    /// 캠핑 프레임 갱신 — 물음표 빌보드만 (타이머·아차·지진 없음). true면 기존 게임 루프 스킵
    bool CampUpdate(float dt)
    {
        if (!campMode) return false;
        if (!campSkySet && cam != null)
        {
            // BuildOuterDeco가 덮어쓴 배경을 야외 톤으로 (첫 프레임 1회)
            campSkySet = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.60f, 0.82f, 0.95f);
            // 외곽 마루 → 맵 바닥과 같은 잔디 텍스처(살짝 어둡게) — 경계 이음매 제거
            var of = GameObject.Find("outer_floor");
            var floorGo = GameObject.Find("floor");
            Material floorMat = floorGo != null ? floorGo.GetComponentInChildren<Renderer>().sharedMaterial : null;
            if (of != null)
                foreach (var r in of.GetComponentsInChildren<Renderer>())
                {
                    if (r.material == null) continue;
                    if (floorMat != null && floorMat.mainTexture != null)
                    {
                        r.material.mainTexture = floorMat.mainTexture;
                        r.material.mainTextureScale = new Vector2(30f, 30f);
                        r.material.color = new Color(0.72f, 0.80f, 0.70f);   // 살짝 어둡게 → 원경 느낌
                    }
                    else r.material.color = new Color(0.42f, 0.60f, 0.42f);
                }
            CampBuildForest();
        }
        if (videoOpen)
        {
            if (SKIn.Down(KeyCode.Space) || SKIn.Down(KeyCode.Escape)) CloseVideo();
            return true;
        }
        foreach (var hz in hazards)
        {
            if (hz.bang == null) continue;
            hz.bang.transform.rotation = cam.transform.rotation;
            var lp = hz.bang.transform.localPosition;
            lp.y = 0.6f + Mathf.Sin(timeAll * 3f + hz.id) * 0.08f;
            hz.bang.transform.localPosition = lp;
        }
        // 텐트 입구 마커 빌보드
        if (tentDoorMark != null && tentDoorMark.activeSelf)
        {
            var tb = tentDoorMark.GetComponentInChildren<TextMesh>();
            if (tb != null) tb.transform.rotation = cam.transform.rotation;
        }
        // 텐트 진입 / 난로 이송 상호작용 (위험 퀴즈보다 우선)
        if (CampTentInteract()) { }
        else if (CampHeaterCarry()) { }
        return true;
    }
}
