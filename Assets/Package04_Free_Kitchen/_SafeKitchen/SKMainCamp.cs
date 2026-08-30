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
        // 위험 5곳 스폰 (시간 무제한)
        foreach (var key in CAMP_KEYS)
        {
            var def = SKData.EV_CAMP[key];
            var node = SpawnMarker(SKData.HZ_CAMP[key]);
            var bang = SpawnBang(node);
            uid++;
            hazards.Add(new Hz { id = uid, type = key, def = def, node = node, bang = bang, ttl = def.ttl, reach = 2.2f });
        }
        CampTentInitState();
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
        rt.anchoredPosition = new Vector2(20, -20);
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
        if (type == "camp_tent") StartCoroutine(CampTentFixCo());
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

    /// 환기: 닫힌 텐트 → 열린 텐트 교체 (에셋 없으면 조용히 생략)
    void CampSwapTentOpen()
    {
        if (tentClosed == null || tentOpen == null) return;
        StartCoroutine(TentSwapCo(tentClosed, tentOpen));
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
            cam.backgroundColor = new Color(0.55f, 0.78f, 0.92f);
            // 외곽 마루(주방용 다크우드) → 짙은 숲 초록
            var of = GameObject.Find("outer_floor");
            if (of != null)
                foreach (var r in of.GetComponentsInChildren<Renderer>())
                    if (r.material != null) r.material.color = new Color(0.24f, 0.42f, 0.26f);
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
        return true;
    }
}
