using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// 세이프 키친 — 게임필 패키지
/// ① 밸브 잠금 미니게임 (연타→타이밍)  ② 소화기 조준 부채꼴  ③ 타격감(히트스톱·펀치줌·미니셰이크)  ④ 지진 예고
public partial class SKMain
{
    // ---------- 밸브 미니게임 상태 ----------
    // mgKind: 0=튜토리얼, 1=지진1(화재), 2=지진2(누출, 연타만)
    bool mgOpen;
    int mgKind = -1;
    int mgPhase;              // 0=연타 1=타이밍 2=잠금 연출
    float mgGauge, mgNeedle, mgCloseT, mgLeverKick, mgSafeT;
    GameObject pnMg, mgFallback, mgRingGo, mgNeedleGo;
    Image mgBodyImg, mgLeverImg, mgGaugeFill;
    RectTransform mgLeverRt, mgNeedleRt, mgFillRt;
    Text mgTitle, mgHint;

    // 분사(홀드)·쓸기
    bool sprayOn;             // LMB 홀드 분사 중
    float sprayAutoT;         // SPACE 버스트 잔여 시간
    float sprayRetrigT;       // 분사음 재트리거
    float sweepAcc;           // 쓸기 누적량 (마우스 좌우)
    GameObject sprayGo;

    // 조준 부채꼴
    GameObject aimCone;
    Material aimConeMat;

    // 타격감·예고
    bool hitstopOn;
    float camFov0 = -1f, camOrtho0 = -1f;
    Coroutine punchCo;
    bool preQ1, preQ2;        // 지진 예고 1회 재생 플래그

    // ---------- UI 헬퍼 (중앙 앵커) ----------
    RectTransform CPanel(Transform parent, float x, float y, float w, float h, Color c, out Image img)
    {
        var go = new GameObject("p");
        go.transform.SetParent(parent, false);
        img = go.AddComponent<Image>();
        img.color = c;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    static Sprite SprCircle(int n, Color c)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        float h = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = x - h + 0.5f, dy = y - h + 0.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(h - 1f - r);
                tex.SetPixel(x, y, new Color(c.r, c.g, c.b, c.a * a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }

    /// 타이밍 링: 회색 밴드 + 아래(레버 잠금 방향) 초록 구간
    static Sprite SprRing()
    {
        int n = 256;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        float h = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = x - h + 0.5f, dy = y - h + 0.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(124f - r) * Mathf.Clamp01(r - 100f);
                if (a <= 0f) { tex.SetPixel(x, y, Color.clear); continue; }
                float phi = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;   // 0=위, 시계방향+
                if (phi < 0f) phi += 360f;
                bool zone = Mathf.Abs(phi - 180f) <= 24f;
                var c = zone ? new Color(0.35f, 0.95f, 0.45f, 0.95f) : new Color(1f, 1f, 1f, 0.30f);
                tex.SetPixel(x, y, new Color(c.r, c.g, c.b, c.a * a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }

    // ---------- 미니게임 UI ----------
    void BuildMgUI()
    {
        pnMg = new GameObject("mg");
        pnMg.transform.SetParent(canvas.transform, false);
        var rt = pnMg.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var dimImg = pnMg.AddComponent<Image>();
        dimImg.color = new Color(0.06f, 0.09f, 0.13f, 0.60f);

        Image im;
        CPanel(pnMg.transform, 0, 14, 486, 486, new Color(0.93f, 0.89f, 0.79f), out im);   // 크림 테두리
        CPanel(pnMg.transform, 0, 14, 474, 474, new Color(0.10f, 0.13f, 0.19f, 0.97f), out im);
        CPanel(pnMg.transform, 0, 216, 420, 42, Color.clear, out im);
        mgTitle = Label(im.transform, "밸브 잠그기!", 27, YELLOW, TextAnchor.MiddleCenter, true);

        // 축 지점 P: 일러스트 몸체의 상단 스템 위치 (레버·링·바늘 공용 중심)
        float px = -8f, py = 64f;

        // 절차 폴백 그림 (일러스트 이미지가 오면 자동 교체)
        mgFallback = new GameObject("fb");
        mgFallback.transform.SetParent(pnMg.transform, false);
        var frt = mgFallback.AddComponent<RectTransform>();
        frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
        frt.anchoredPosition = Vector2.zero;
        frt.sizeDelta = Vector2.zero;
        CPanel(mgFallback.transform, px, py, 384, 44, new Color(0.62f, 0.65f, 0.70f), out im);   // 배관
        CPanel(mgFallback.transform, px, py, 186, 186, Color.white, out im);
        im.sprite = SprCircle(128, Color.white);
        im.color = new Color(0.80f, 0.64f, 0.30f);   // 황동 몸체
        CPanel(mgFallback.transform, px, py, 58, 58, Color.white, out im);
        im.sprite = SprCircle(64, Color.white);
        im.color = new Color(0.35f, 0.30f, 0.18f);   // 축 너트

        // 일러스트 몸체 (이미지 도착 시 활성) — 스템이 P에 오도록 배치
        var brt = CPanel(pnMg.transform, 0, -8, 430, 430, Color.white, out mgBodyImg);
        mgBodyImg.preserveAspect = true;
        brt.gameObject.SetActive(false);

        // 타이밍 링 + 바늘
        var rrt = CPanel(pnMg.transform, px, py, 356, 356, Color.white, out im);
        im.sprite = SprRing();
        mgRingGo = rrt.gameObject;
        mgRingGo.SetActive(false);
        var nrt = CPanel(pnMg.transform, px, py, 7, 162, YELLOW, out im);
        nrt.pivot = new Vector2(0.5f, 0f);
        nrt.anchoredPosition = new Vector2(px, py);
        mgNeedleRt = nrt;
        mgNeedleGo = nrt.gameObject;
        mgNeedleGo.SetActive(false);

        // 레버 (축 구멍이 P에 걸림, 0°=열림/가로 → -90°=잠김/세로)
        var lrt = CPanel(pnMg.transform, px, py, 280, 86, new Color(0.85f, 0.25f, 0.22f), out mgLeverImg);
        lrt.pivot = new Vector2(0.14f, 0.55f);
        lrt.anchoredPosition = new Vector2(px, py);
        mgLeverRt = lrt;

        // 게이지
        CPanel(pnMg.transform, 0, -176, 344, 26, new Color(1, 1, 1, 0.20f), out im);
        mgFillRt = CPanel(pnMg.transform, -170, -176, 10, 18, MINT, out mgGaugeFill);
        mgFillRt.pivot = new Vector2(0f, 0.5f);
        mgFillRt.anchoredPosition = new Vector2(-170, -176);

        CPanel(pnMg.transform, 0, -204, 460, 32, Color.clear, out im);
        mgHint = Label(im.transform, "", 20, Color.white, TextAnchor.MiddleCenter, true);
        // 타이밍 링·바늘은 레버 위에 그려 초록 구간이 가려지지 않게
        mgRingGo.transform.SetAsLastSibling();
        mgNeedleGo.transform.SetAsLastSibling();
        pnMg.SetActive(false);
    }

    void TryLoadMgArt()
    {
        if (mgBodyImg.sprite == null)
        {
            var s = Resources.Load<Sprite>("UI/ValveCloseupBody");
            if (s != null)
            {
                mgBodyImg.sprite = s;
                mgBodyImg.gameObject.SetActive(true);
                mgFallback.SetActive(false);
            }
        }
        if (mgLeverImg.sprite == null)
        {
            var s = Resources.Load<Sprite>("UI/ValveCloseupLever");
            if (s != null)
            {
                mgLeverImg.sprite = s;
                mgLeverImg.color = Color.white;
                mgLeverImg.preserveAspect = true;
            }
        }
    }

    void SetLever(float ang)
    {
        if (mgLeverRt != null) mgLeverRt.localRotation = Quaternion.Euler(0, 0, ang);
    }

    // ---------- 미니게임 진행 ----------
    void MgStart(int kind)
    {
        if (mgOpen) return;
        mgOpen = true;
        mgKind = kind;
        mgPhase = 0;
        mgGauge = 0f;
        mgNeedle = 0f;
        mgCloseT = 0f;
        mgLeverKick = 0f;
        mgSafeT = 0f;
        TryLoadMgArt();
        SetLever(0f);
        mgRingGo.SetActive(false);
        mgNeedleGo.SetActive(false);
        mgTitle.text = "밸브 잠그기!";
        mgHint.text = kind == 2 ? "[SPACE] 연타! 가스가 새고 있다!" : "[SPACE] 연타로 레버를 돌려라!";
        pnMg.SetActive(true);
        SKSound.Sfx("sfx_popup", 0.8f);
    }

    void MgPress()
    {
        if (mgPhase == 0)
        {
            mgGauge = Mathf.Min(1f, mgGauge + 0.11f);
            mgLeverKick = 6f;
            SKSound.Sfx("sfx_step", 0.5f, 1.15f + mgGauge * 0.5f);   // 래칫 틱
            if (mgGauge >= 1f)
            {
                if (mgKind == 2) MgLock(false);   // 누출: 긴박 — 타이밍 생략
                else
                {
                    mgPhase = 1;
                    mgNeedle = 0f;
                    mgSafeT = 0f;
                    mgRingGo.SetActive(true);
                    mgNeedleGo.SetActive(true);
                    mgHint.text = "초록 구간에서 [SPACE] — 꽉 잠가라!";
                }
            }
        }
        else if (mgPhase == 1)
        {
            float phi = Mathf.Repeat(-mgNeedle, 360f);
            MgLock(Mathf.Abs(phi - 180f) <= 24f);
        }
    }

    void MgLock(bool bonus)
    {
        if (mgPhase == 2) return;
        mgPhase = 2;
        mgCloseT = 0.55f;
        mgLeverKick = 0f;
        SetLever(-90f);
        SKSound.Sfx("sfx_valve");
        Hitstop(0.06f);
        StartCoroutine(ShakeMiniCo(0.05f, 0.12f));
        if (bonus)
        {
            score += 30;
            AddFloat(player.position + new Vector3(0, 1.4f, 0), "+30 타이밍 보너스!");
            SKSound.Sfx("sfx_correct", 0.9f, 1.15f);
            mgHint.text = "쿵! 완벽한 잠금!";
        }
        else mgHint.text = "잠금 완료!";
    }

    void MgUpdate(float dt)
    {
        if (mgPhase == 0)
        {
            mgGauge = Mathf.Max(0f, mgGauge - 0.20f * dt);   // 연타 안 하면 스르륵 풀림
            mgLeverKick = Mathf.MoveTowards(mgLeverKick, 0f, 40f * dt);
            SetLever(-80f * mgGauge - mgLeverKick);
        }
        else if (mgPhase == 1)
        {
            mgNeedle -= 250f * dt;
            if (mgNeedle < -360f) mgNeedle += 360f;
            if (mgNeedleRt != null) mgNeedleRt.localRotation = Quaternion.Euler(0, 0, mgNeedle);
            mgSafeT += dt;
            if (mgSafeT > 7f) MgLock(false);   // 무한 대기 방지
        }
        else
        {
            mgCloseT -= dt;
            if (mgCloseT <= 0f) MgFinish();
        }
        if (mgFillRt != null)
        {
            mgFillRt.sizeDelta = new Vector2(Mathf.Max(6f, 344f * mgGauge), 18f);
            mgGaugeFill.color = Color.Lerp(MINT, YELLOW, mgGauge);
        }
    }

    /// 잠금 성공 → 패널 닫고 시나리오별 후처리
    void MgFinish()
    {
        pnMg.SetActive(false);
        mgOpen = false;
        int k = mgKind;
        mgKind = -1;
        if (k == 0)
        {
            if (valvePivot != null) StartCoroutine(TurnValve(90f));
            if (arrowGo != null) Destroy(arrowGo);
            TutGateClear();
        }
        else if (k == 1) ValveDone();
        else if (k == 2) LockValve2();
    }

    /// 시간 초과 구제·스킵용 — 후처리 없이 즉시 닫기
    void MgForceClose()
    {
        if (pnMg != null) pnMg.SetActive(false);
        mgOpen = false;
        mgKind = -1;
    }

    // ---------- 소화기: 조준 부채꼴 + 홀드 분사 + 쓸기 ----------
    void BuildAimCone()
    {
        var go = new GameObject("aim_cone");
        go.transform.SetParent(pbody, false);
        go.transform.localPosition = new Vector3(0, 0.07f, 0);
        var mesh = new Mesh();
        int segs = 22;
        float half = 25f, R = 4.5f;
        var verts = new Vector3[segs + 2];
        verts[0] = Vector3.zero;
        for (int i = 0; i <= segs; i++)
        {
            float a = (-half + (half * 2f) * i / segs) * Mathf.Deg2Rad;
            verts[i + 1] = new Vector3(Mathf.Sin(a) * R, 0, Mathf.Cos(a) * R);
        }
        var tris = new int[segs * 3];
        for (int i = 0; i < segs; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        go.AddComponent<MeshFilter>().mesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        aimConeMat = Flat(new Color(1f, 1f, 1f, 0.14f));
        mr.material = aimConeMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        aimCone = go;
        go.SetActive(false);
    }

    void UpdateAimCone()
    {
        if (aimCone == null) return;
        bool show = carryExt != null && !mgOpen &&
            (TutGate == TG_SPRAY || (quakeState == 3 && quakeScen == 1 && quizSolved));
        if (aimCone.activeSelf != show) aimCone.SetActive(show);
        if (!show) return;
        bool hot = NearBurningFire() >= 0;
        aimConeMat.color = hot ? new Color(1f, 0.55f, 0.25f, 0.30f) : new Color(1f, 1f, 1f, 0.14f);
    }

    bool AimedAt(int i)
    {
        var fwd = pbody.forward;
        fwd.y = 0;
        fwd.Normalize();
        var to = firePosL[i] - player.position;
        to.y = 0;
        float d = to.magnitude;
        return (d < 4.5f && Vector3.Angle(fwd, to) < 25f) || d < 1.8f;
    }

    void StartJet()
    {
        if (sprayGo != null) return;
        var jet = Resources.Load<GameObject>("VFX/CFXR Smoke Source 3D");
        if (jet == null) return;
        sprayGo = Instantiate(jet);
        sprayGo.name = "spray";
        sprayGo.transform.position = player.position + new Vector3(0, 0.9f, 0);
        sprayGo.transform.localScale = Vector3.one * 0.8f;
        var jps = sprayGo.GetComponent<ParticleSystem>();
        if (jps != null) jps.Play(true);
        SKSound.Sfx("sfx_spray", 0.9f);
        sprayRetrigT = 1.15f;
        spraying = true;
    }

    void StopJet()
    {
        spraying = false;
        sprayOn = false;
        if (sprayGo != null)
        {
            foreach (var p2 in sprayGo.GetComponentsInChildren<ParticleSystem>())
                p2.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(sprayGo, 1.4f);
            sprayGo = null;
        }
    }

    /// SPACE 버스트 분사 (키보드만으로도 플레이 가능)
    void SprayBurst()
    {
        sprayAutoT = 1.6f;
        StartJet();
        spraying = true;
    }

    void SprayUpdate(float dt)
    {
        bool ctx = TutGate == TG_SPRAY || (quakeState == 3 && quakeScen == 1 && quizSolved);
        if (sprayAutoT > 0f) sprayAutoT -= dt;
        bool want = carryExt != null && ctx && !mgOpen && !over
            && ((sprayOn && SKIn.MouseHeld()) || sprayAutoT > 0f);
        if (!want)
        {
            if (spraying || sprayGo != null) StopJet();
            sweepAcc = Mathf.Max(0f, sweepAcc - 3f * dt);
            return;
        }
        spraying = true;
        if (sprayGo == null) StartJet();
        if (sprayGo == null) return;
        // 노즐 위치·조준: 부채꼴 안 최근접 불 방향, 없으면 정면
        var fwd = pbody.forward;
        fwd.y = 0;
        fwd.Normalize();
        var dir = fwd;
        int aim = NearBurningFire();
        if (aim >= 0)
        {
            var to = (firePosL[aim] + new Vector3(0, 0.25f, 0)) - (player.position + new Vector3(0, 0.9f, 0));
            if (to.sqrMagnitude > 0.01f) dir = to.normalized;
        }
        sprayGo.transform.position = player.position + new Vector3(0, 0.9f, 0) + fwd * 0.3f;
        sprayGo.transform.rotation = Quaternion.LookRotation(dir);
        sprayRetrigT -= dt;
        if (sprayRetrigT <= 0f)
        {
            SKSound.Sfx("sfx_spray", 0.8f);
            sprayRetrigT = 1.15f;
        }
        // 쓸기 보너스: 마우스를 좌우로 쓸면 진압 속도 최대 3배
        sweepAcc = Mathf.Max(0f, sweepAcc - 3f * dt) + Mathf.Abs(SKIn.MouseDX());
        float mult = 1f + 2f * Mathf.Clamp01(sweepAcc / 1.6f);
        if (sprayAutoT > 0f) mult = Mathf.Max(mult, 1.6f);
        for (int i = 0; i < firePosL.Count; i++)
        {
            if (fireOutL[i] || !AimedAt(i)) continue;
            fireHpL[i] -= 0.55f * mult * dt;
            float hp = Mathf.Max(0f, fireHpL[i]);
            if (firePsL[i] != null)
                firePsL[i].transform.localScale = Vector3.one * (0.72f * (0.35f + 0.65f * hp));
            if (fireHpL[i] <= 0f) { KillFire(i); break; }
        }
    }

    void KillFire(int i)
    {
        if (fireOutL[i]) return;
        ExtinguishFire(i);
        score += 150;
        AddFloat(firePosL[i], "+150 진압!");
        Hitstop(0.05f);
        PunchZoom();
        bool allOut = true;
        for (int k = 0; k < fireOutL.Count; k++)
            if (!fireOutL[k]) allOut = false;
        if (allOut)
        {
            StopJet();
            sprayAutoT = 0f;
            if (!TutActive && quakeState == 3 && quakeScen == 1) FinalDone();
            // 튜토리얼은 TutUpdate의 TG_SPRAY 게이트가 마무리
        }
        else Say("잘했어! 아직 불이 남았어!", 2.5f);
    }

    // ---------- 타격감 ----------
    void Hitstop(float d)
    {
        if (!hitstopOn) StartCoroutine(HitstopCo(d));
    }

    IEnumerator HitstopCo(float d)
    {
        hitstopOn = true;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(d);
        Time.timeScale = 1f;
        hitstopOn = false;
    }

    void PunchZoom()
    {
        if (cam == null) return;
        if (camFov0 < 0f) camFov0 = cam.fieldOfView;
        if (camOrtho0 < 0f) camOrtho0 = cam.orthographicSize;
        if (punchCo != null) StopCoroutine(punchCo);
        punchCo = StartCoroutine(PunchCo());
    }

    IEnumerator PunchCo()
    {
        float t = 0;
        while (t < 0.22f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.22f);
            float f = (1f - k) * (1f - k);
            if (cam.orthographic) cam.orthographicSize = camOrtho0 - 0.28f * f;
            else cam.fieldOfView = camFov0 - 3f * f;
            yield return null;
        }
        if (cam.orthographic) cam.orthographicSize = camOrtho0;
        else cam.fieldOfView = camFov0;
        punchCo = null;
    }

    IEnumerator ShakeMiniCo(float amp, float dur)
    {
        if (fpMode) { fpShakeAmp = Mathf.Max(fpShakeAmp, amp); yield break; }   // FP는 LateUpdate가 처리
        var p0 = cam.transform.position;
        float t = 0;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            cam.transform.position = p0 + new Vector3((Random.value - 0.5f) * amp * 2f, (Random.value - 0.5f) * amp, 0);
            yield return null;
        }
        cam.transform.position = p0;
    }

    // ---------- 지진 예고 (본지진 1.2초 전) ----------
    IEnumerator PreQuakeFx()
    {
        SKSound.Sfx("sfx_rumble", 0.95f);
        var b = cam.transform.position;
        var lampsGo = GameObject.Find("Lamps");
        Light[] pls = lampsGo != null ? lampsGo.GetComponentsInChildren<Light>() : new Light[0];
        var bases = new float[pls.Length];
        for (int i = 0; i < pls.Length; i++) bases[i] = pls[i].intensity;
        float t = 0;
        while (t < 1.05f)
        {
            t += Time.deltaTime;
            if (quakeState != 0) break;   // 본지진 시작 — 즉시 양보
            if (!fpMode)
                cam.transform.position = b + new Vector3((Random.value - 0.5f) * 0.05f, (Random.value - 0.5f) * 0.03f, 0);
            else
                fpShakeAmp = Mathf.Max(fpShakeAmp, 0.035f);
            bool off = (t > 0.30f && t < 0.42f) || (t > 0.75f && t < 0.92f);
            for (int i = 0; i < pls.Length; i++)
                if (pls[i] != null) pls[i].intensity = bases[i] * (off ? 0.25f : 1f);
            yield return null;
        }
        if (quakeState == 0)
        {
            if (!fpMode) cam.transform.position = b;
            for (int i = 0; i < pls.Length; i++)
                if (pls[i] != null) pls[i].intensity = bases[i];
        }
    }
}
