using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// 1인칭 완전 전환 (기본 모드, V키로 쿼터뷰 전환 가능)
/// 마우스룩 + 시선기준 WASD + 크로스헤어 + 스테이션 라벨 + 시야 밖 위험 화살표
/// + 소화기 뷰모델 + FP 전용 남쪽 벽·천장 + 대피 숙이기 카메라
public partial class SKMain
{
    bool fpMode;
    float fpYaw, fpPitch;
    float fpShakeAmp;                 // 지진·타격 셰이크 (FP 전용, 감쇠)
    Vector3 qvCamPos; Quaternion qvCamRot;   // 쿼터뷰 원래 카메라 (복귀용)
    bool qvOrtho; float qvOrthoSize;         // 쿼터뷰 투영 설정 (직교) — FP는 원근으로 전환
    const float FP_FOV = 68f;   // 넓은 시야 — 맵이 한눈에 들어오게
    GameObject fpDot;                 // 크로스헤어 점
    Text fpHint;                      // [V] 시점 안내
    GameObject fpWalls;               // FP 전용 남쪽 벽+천장 (쿼터뷰에선 숨김)
    const float FP_SENS = 3.0f;
    const float FP_HEAD = 1.68f;   // 성인 눈높이 (어린이 시점 느낌 제거)

    // 스테이션 월드 라벨
    class StLabel { public GameObject go; public TextMesh tm; public Transform follow; public Vector3 offset; public Transform ext; }
    readonly List<StLabel> stLabels = new List<StLabel>();

    // 시야 밖 위험 화살표 (FP 전용, 최대 3개)
    readonly Text[] hzArrows = new Text[3];

    /// Awake 말미 호출
    void FpInit()
    {
        qvCamPos = cam.transform.position;
        qvCamRot = cam.transform.rotation;
        qvOrtho = cam.orthographic;
        qvOrthoSize = cam.orthographicSize;
        // 크로스헤어 점
        var go = new GameObject("fp_dot");
        go.transform.SetParent(canvas.transform, false);
        var img = go.AddComponent<Image>();
        img.sprite = SprCircle(32, Color.white);
        img.color = new Color(1f, 1f, 1f, 0.75f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(7, 7);
        fpDot = go;
        fpDot.SetActive(false);
        // 우하단 [V] 안내
        Image im;
        var hr = CPanel(canvas.transform, 0, 0, 190, 26, Color.clear, out im);
        hr.anchorMin = hr.anchorMax = new Vector2(1f, 0f);
        hr.pivot = new Vector2(1f, 0f);
        hr.anchoredPosition = new Vector2(-14, 10);
        fpHint = Label(im.transform, "[V] 시점 전환", 15, new Color(1, 1, 1, 0.55f), TextAnchor.MiddleRight);
        // 시야 밖 위험 화살표 풀
        for (int i = 0; i < 3; i++)
        {
            var ago = new GameObject("hz_arrow" + i);
            ago.transform.SetParent(canvas.transform, false);
            var t = ago.AddComponent<Text>();
            t.font = font; t.fontSize = 30; t.fontStyle = FontStyle.Bold;
            t.color = CORAL; t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var art = ago.GetComponent<RectTransform>();
            art.anchorMin = art.anchorMax = new Vector2(0, 1);
            art.pivot = new Vector2(0.5f, 0.5f);
            art.sizeDelta = new Vector2(90, 40);
            hzArrows[i] = t;
            ago.SetActive(false);
        }
        BuildFpWalls();
        BuildStationLabels();
        // 기본 = 1인칭 (V로 쿼터뷰 전환)
        if (PlayerPrefs.GetInt("skfp", 1) == 1) SetFp(true);
    }

    void ToggleFp() { SetFp(!fpMode); }

    void SetFp(bool on)
    {
        if (fpMode == on) return;
        fpMode = on;
        PlayerPrefs.SetInt("skfp", on ? 1 : 0);
        if (on)
        {
            fpYaw = pbody.eulerAngles.y;
            fpPitch = 8f;
            fpShakeAmp = 0f;
            // 1인칭은 원근 투영 (쿼터뷰는 직교라 그대로 두면 납작한 띠로 보임)
            cam.orthographic = false;
            cam.fieldOfView = FP_FOV;
            camFov0 = FP_FOV;         // 펀치줌 기준 갱신
        }
        else
        {
            cam.orthographic = qvOrtho;
            cam.orthographicSize = qvOrthoSize;
            camOrtho0 = qvOrthoSize;  // 펀치줌 기준 갱신
            cam.transform.SetPositionAndRotation(qvCamPos, qvCamRot);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            // 소화기 들고 있으면 쿼터뷰 표준 파지 자세로 복귀
            if (carryExt != null)
            {
                carryExt.position = player.position + pbody.rotation * new Vector3(0.26f, 0.42f, 0.3f);
                carryExt.rotation = pbody.rotation;
            }
            for (int i = 0; i < 3; i++) if (hzArrows[i] != null) hzArrows[i].gameObject.SetActive(false);
        }
        if (fpWalls != null) fpWalls.SetActive(on);
        SetRangerVisible(!on);
        if (fpDot != null) fpDot.SetActive(on);
        SKSound.Sfx("sfx_popup", 0.5f);
        Say(on ? "1인칭 시점! 마우스로 둘러보기 · [V] 쿼터뷰" : "쿼터뷰 시점! [V] 1인칭", 2.2f);
    }

    /// 레인저 모델 표시/숨김 (들고 있는 소화기·조준 부채꼴은 유지)
    void SetRangerVisible(bool vis)
    {
        foreach (var r in pbody.GetComponentsInChildren<Renderer>(true))
        {
            if (carryExt != null && r.transform.IsChildOf(carryExt)) continue;
            if (aimCone != null && r.transform.IsChildOf(aimCone.transform)) continue;
            r.enabled = vis;
        }
    }

    bool FpCursorFree()
    {
        return openEv != null || quizOpen || mgOpen || over || titleOpen;
    }

    /// 매 프레임 (Update 앞부분): 마우스 시점 + 커서 잠금 관리
    void FpUpdate()
    {
        if (!fpMode) return;
        bool free = FpCursorFree();
        Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = free;
        if (fpDot != null) fpDot.SetActive(!free);
        if (free) return;
        fpYaw += SKIn.MouseDX() * FP_SENS;
        fpPitch = Mathf.Clamp(fpPitch - SKIn.MouseDY() * FP_SENS, -55f, 40f);
        // 몸 방향 = 시선 방향 (조준·분사·상호작용 판정 공유)
        pbody.rotation = Quaternion.Euler(0, fpYaw, 0);
    }

    /// 근접 판정에 얹는 시선 필터 — FP에서는 바라보는 대상만 상호작용
    bool FacingPoint(Vector3 p, float maxAng)
    {
        if (!fpMode) return true;
        var fwd = pbody.forward; fwd.y = 0;
        var to = p - player.position; to.y = 0;
        if (to.sqrMagnitude < 0.04f) return true;   // 발밑은 통과
        return Vector3.Angle(fwd, to) < maxAng;
    }

    /// FP 이동: 시선 기준 WASD (충돌·속도·달리기는 쿼터뷰와 동일 규칙)
    bool FpMoveInput(float dt, ref bool sprinting)
    {
        float ix = 0, iz = 0;
        if (SKIn.Held(KeyCode.RightArrow) || SKIn.Held(KeyCode.D)) ix += 1;
        if (SKIn.Held(KeyCode.LeftArrow) || SKIn.Held(KeyCode.A)) ix -= 1;
        if (SKIn.Held(KeyCode.UpArrow) || SKIn.Held(KeyCode.W)) iz += 1;
        if (SKIn.Held(KeyCode.DownArrow) || SKIn.Held(KeyCode.S)) iz -= 1;
        if (ix == 0 && iz == 0) return false;
        sprinting = SKIn.Held(KeyCode.LeftShift) || SKIn.Held(KeyCode.RightShift);
        float spd = SKData.SPEED * (sprinting ? SKData.RUN_MULT : 1f);
        var dir = Quaternion.Euler(0, fpYaw, 0) * new Vector3(ix, 0, iz);
        dir.y = 0;
        dir.Normalize();
        var p = player.position;
        float nx = Mathf.Clamp(p.x + dir.x * spd * dt, 0.6f, SKData.RW - 0.6f);
        if (!Blocked(new Vector3(nx, 0, p.z))) p.x = nx;
        float nz = Mathf.Clamp(p.z + dir.z * spd * dt, 0.6f, SKData.RD - 0.6f);
        if (!Blocked(new Vector3(p.x, 0, nz))) p.z = nz;
        player.position = p;
        wasSprinting = sprinting;
        return true;
    }

    /// 카메라 추적 + 소화기 뷰모델 (모든 이동·연출 뒤)
    void LateUpdate()
    {
        if (!fpMode || cam == null) return;
        if (quakeState == 1) fpShakeAmp = Mathf.Max(fpShakeAmp, 0.10f);
        var jolt = fpShakeAmp > 0.001f
            ? new Vector3((Random.value - 0.5f) * fpShakeAmp * 2f, (Random.value - 0.5f) * fpShakeAmp, 0)
            : Vector3.zero;
        fpShakeAmp = Mathf.MoveTowards(fpShakeAmp, 0f, Time.deltaTime * 0.5f);
        // 대피 때 몸을 숙이면 카메라도 낮아짐 (pbody 스케일 연동)
        float head = FP_HEAD * Mathf.Clamp(pbody.localScale.y, 0.45f, 1f);
        cam.transform.position = player.position + new Vector3(0, head, 0) + jolt;
        cam.transform.rotation = Quaternion.Euler(fpPitch, fpYaw, 0);
        // 소화기 뷰모델: 화면 우하단 파지
        if (carryExt != null)
        {
            carryExt.position = cam.transform.TransformPoint(new Vector3(0.34f, -0.34f, 0.58f));
            carryExt.rotation = cam.transform.rotation * Quaternion.Euler(-4f, 192f, 6f);
        }
    }

    // ---------- FP 전용 벽 3면(남·동·서) + 천장 ----------
    // 씬의 남·동·서 벽은 쿼터뷰 시야용으로 낮게(0.5~1.5m) 만들어져 있어
    // 1인칭에선 위가 뚫려 보인다 → 바닥 실측 경계에 맞춰 풀높이 벽을 덧댄다 (FP에서만 활성)
    void BuildFpWalls()
    {
        fpWalls = new GameObject("fp_walls");
        var wallC = new Color(0.63f, 0.55f, 0.47f);
        var ceilC = new Color(0.92f, 0.89f, 0.82f);
        // 바닥 실측 경계 (Room/floor) — 없으면 SKData 폴백
        var fb = new Bounds(new Vector3(SKData.RW * 0.5f, 0, SKData.RD * 0.5f),
                            new Vector3(SKData.RW, 0.1f, SKData.RD));
        var room = GameObject.Find("Room");
        if (room != null)
        {
            var ft = room.transform.Find("floor");
            if (ft != null)
            {
                var rs = ft.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    var b = rs[0].bounds;
                    foreach (var r in rs) b.Encapsulate(r.bounds);
                    fb = b;
                }
            }
        }
        float cx = fb.center.x, cz = fb.center.z;
        FpPanel("fp_wall_s", new Vector3(cx, 1.62f, fb.max.z + 0.18f), new Vector3(fb.size.x + 1.0f, 3.3f, 0.25f), wallC);
        FpPanel("fp_wall_w", new Vector3(fb.min.x - 0.18f, 1.62f, cz), new Vector3(0.25f, 3.3f, fb.size.z + 1.0f), wallC);
        FpPanel("fp_wall_e", new Vector3(fb.max.x + 0.18f, 1.62f, cz), new Vector3(0.25f, 3.3f, fb.size.z + 1.0f), wallC);
        FpPanel("fp_ceiling", new Vector3(cx, 3.3f, cz), new Vector3(fb.size.x + 1.2f, 0.2f, fb.size.z + 1.2f), ceilC);
        fpWalls.SetActive(false);
    }

    void FpPanel(string name, Vector3 pos, Vector3 scale, Color col)
    {
        var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(w.GetComponent<Collider>());
        w.name = name;
        w.transform.SetParent(fpWalls.transform, false);
        w.transform.position = pos;
        w.transform.localScale = scale;
        var wr = w.GetComponent<Renderer>();
        wr.material = Lit(col);
        wr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // 실내 밝기 유지
    }

    // ---------- 스테이션 월드 라벨 (김밥지옥식 길찾기 — 양 시점 공통) ----------
    void BuildStationLabels()
    {
        if (valvePivot != null)
            MakeStLabel("가스밸브", new Color(1f, 0.84f, 0.31f), valvePivot, new Vector3(0, 0.5f, 0.15f), null);
        if (windowClosed != null)
        {
            var c = RB(windowClosed).center;
            MakeStLabel("창문", new Color(0.55f, 0.82f, 1f), null, c + new Vector3(0, 1.0f, 0.2f), null);
        }
        foreach (var e in extList)
            if (e != null) MakeStLabel("소화기", new Color(1f, 0.45f, 0.4f), e, new Vector3(0, 0.85f, 0), e);
        var kroot = GameObject.Find("Kitchen");
        if (kroot != null)
            foreach (var t in kroot.GetComponentsInChildren<Transform>(true))
                if (t.name == "ShelterTable")
                {
                    var b = RB(t.gameObject).center;
                    MakeStLabel("대피 탁자", new Color(0.6f, 0.95f, 0.7f), null, b + new Vector3(0, 1.15f, 0), null);
                    break;
                }
    }

    void MakeStLabel(string text, Color col, Transform follow, Vector3 posOrOffset, Transform extRef)
    {
        var go = new GameObject("stlabel_" + text);
        var tm = go.AddComponent<TextMesh>();
        tm.text = "· " + text + " ·";
        tm.font = font;
        tm.fontSize = 72;
        tm.characterSize = 0.075f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = col;
        tm.fontStyle = FontStyle.Bold;
        go.GetComponent<MeshRenderer>().material = font.material;
        var l = new StLabel { go = go, tm = tm, follow = follow, ext = extRef };
        if (follow != null) { l.offset = posOrOffset; go.transform.position = follow.position + posOrOffset; }
        else go.transform.position = posOrOffset;
        stLabels.Add(l);
    }

    /// 매 프레임: 라벨 빌보드·추적 + FP 시야 밖 위험 화살표
    void FpWorldUpdate()
    {
        // 라벨
        foreach (var l in stLabels)
        {
            if (l.go == null) continue;
            bool vis = !(l.ext != null && l.ext == carryExt);   // 들고 있는 소화기 라벨은 숨김
            if (l.go.activeSelf != vis) l.go.SetActive(vis);
            if (!vis) continue;
            if (l.follow != null) l.go.transform.position = l.follow.position + l.offset;
            // 카메라를 정면으로 향하게 (거울 반전 방지, 롤 0 고정)
            var toCam = l.go.transform.position - cam.transform.position;
            toCam.y = 0;
            if (toCam.sqrMagnitude > 0.01f)
                l.go.transform.rotation = Quaternion.LookRotation(toCam);
        }
        // 시야 밖 위험 화살표 (FP 전용)
        int used = 0;
        if (fpMode && !titleOpen)
        {
            foreach (var hz in hazards)
            {
                if (used >= 3 || hz.node == null) continue;
                var wp = hz.node.transform.position + new Vector3(0, 0.6f, 0);
                var vp = cam.WorldToViewportPoint(wp);
                bool behind = vp.z < 0f;
                if (behind) { vp.x = 1f - vp.x; vp.y = 1f - vp.y; }
                bool off = behind || vp.x < 0.04f || vp.x > 0.96f || vp.y < 0.06f || vp.y > 0.94f;
                if (!off) continue;
                float cx = Mathf.Clamp(vp.x, 0.06f, 0.94f) * 1280f;
                float cy = Mathf.Clamp(vp.y, 0.10f, 0.90f) * 720f;
                var t = hzArrows[used];
                t.gameObject.SetActive(true);
                var rt = t.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(cx, -(720f - cy));
                bool horiz = Mathf.Abs(vp.x - 0.5f) > Mathf.Abs(vp.y - 0.5f);
                if (horiz) t.text = vp.x < 0.5f ? "◀ ?" : "? ▶";
                else t.text = vp.y < 0.5f ? "▼ ?" : "▲ ?";
                float bl = 0.6f + 0.4f * Mathf.Sin(timeAll * 6f);
                t.color = new Color(CORAL.r, CORAL.g, CORAL.b, bl);
                used++;
            }
        }
        for (int i = used; i < 3; i++)
            if (hzArrows[i] != null && hzArrows[i].gameObject.activeSelf) hzArrows[i].gameObject.SetActive(false);
    }
}
