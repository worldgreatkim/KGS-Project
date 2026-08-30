using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// UX 패키지: 결과 랭크 화면 · ESC 일시정지 메뉴 · 정답 O/오답 플래시 · 발소리 · 씬 선택
public partial class SKMain
{
    // ---- 랭크 ----
    bool rankShown;
    GameObject pnRank;
    readonly List<int> runBadges = new List<int>();   // 이번 판 새로 획득한 배지

    // ---- 일시정지 ----
    bool paused;
    GameObject pnPause;

    // ---- 연출 ----
    Image fxFlash;        // 오답 붉은 플래시
    Image fxChalkO;       // 정답 분필 O
    float stepT;          // 발소리 타이머

    // ---- 씬 선택 (타이틀) ----
    static readonly string[] SCENES = { "SafeKitchen3D", "SafeKitchen3D_FP", "SafeKitchen3D_MOD", "SafeKitchen3D_CAMP" };
    static readonly string[] SCENE_NAMES = { "기본 주방", "넓은 주방", "모듈 주방", "캠핑장 교육" };

    // ---- 궁극기: 배기통 레인저 "가스 흡수" (KGS 공식 설정 — 손바닥으로 유출 가스를 빨아들임) ----
    int ultGauge;                 // 0~ULT_MAX. 정답·잔불 진압·미니게임 성공으로 충전
    const int ULT_MAX = 4;
    GameObject pnUlt;
    Image ultFill;
    Text ultLabel;
    bool ultBusy;                 // 흡수 연출 중 재발동 방지

    void UltInit()
    {
        // 좌하단 게이지 바 + 라벨
        // 우하단 — 좌하단은 훈련 대화창 초상화와 겹침
        pnUlt = new GameObject("ult");
        pnUlt.transform.SetParent(canvas.transform, false);
        var rt = pnUlt.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-24, 52);
        rt.sizeDelta = new Vector2(230, 54);
        Image im;
        var bg = UP(pnUlt.transform, 0, 0, 230, 26, new Color(0.10f, 0.13f, 0.19f, 0.85f), out im);
        var fillRt = UP(pnUlt.transform, 3, 3, 224, 20, new Color(0.35f, 0.85f, 0.65f, 0.95f), out ultFill);
        fillRt.pivot = new Vector2(0f, 0f);
        fillRt.anchoredPosition = new Vector2(3, 3);
        var lrt = UP(pnUlt.transform, 0, 30, 230, 24, Color.clear, out im);
        ultLabel = Label(lrt.transform, "", 17, Color.white, TextAnchor.MiddleLeft, true);
        UltRefresh();
    }

    // 좌하단 앵커 패널 헬퍼 (CPanel은 중앙 앵커라 별도)
    RectTransform UP(Transform parent, float x, float y, float w, float h, Color c, out Image img)
    {
        var go = new GameObject("u");
        go.transform.SetParent(parent, false);
        img = go.AddComponent<Image>();
        img.color = c;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    void UltRefresh()
    {
        if (ultFill == null) return;
        float k = (float)ultGauge / ULT_MAX;
        ultFill.rectTransform.sizeDelta = new Vector2(Mathf.Max(4f, 224f * k), 20f);
        bool full = ultGauge >= ULT_MAX;
        ultFill.color = full ? new Color(1f, 0.85f, 0.25f, 0.98f) : new Color(0.35f, 0.85f, 0.65f, 0.95f);
        ultLabel.text = full ? "[Q] 필살기: 가스 흡수!" : "필살기 게이지 " + ultGauge + "/" + ULT_MAX;
        ultLabel.color = full ? new Color(1f, 0.9f, 0.4f) : Color.white;
    }

    /// 올바른 행동(정답·진압·미니게임 성공)마다 충전 — "잘 배울수록 강해진다"
    void UltCharge()
    {
        if (ultGauge >= ULT_MAX) return;
        ultGauge++;
        UltRefresh();
        if (ultGauge >= ULT_MAX) SKSound.Sfx("sfx_combo", 0.8f, 1.3f);
    }

    void UltReset() { ultGauge = 0; ultBusy = false; UltRefresh(); }

    /// Q 발동 — 깔린 가스가 있을 때만. 밸브·환기 행동요령은 대체하지 않는다(보조 스킬)
    void UltFire()
    {
        if (ultBusy) return;
        if (ultGauge < ULT_MAX)
        {
            Say("필살기 게이지가 아직! 올바른 행동으로 채우자 (" + ultGauge + "/" + ULT_MAX + ")", 2.5f);
            return;
        }
        if (gasClouds.Count == 0)
        {
            Say("지금은 흡수할 가스가 없어!", 2.2f);
            return;
        }
        ultGauge = 0;
        UltRefresh();
        StartCoroutine(UltAbsorbCo());
    }

    IEnumerator UltAbsorbCo()
    {
        ultBusy = true;
        Say("배기통 레인저! 손바닥으로 가스 흡수!", 3f);
        SKSound.Sfx("sfx_vent", 0.9f, 1.15f);
        PunchZoom();
        // 청록 흡수 웨이브 (플레이어 중심 확장 구체)
        var wave = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        wave.name = "ult_wave";
        Destroy(wave.GetComponent<Collider>());
        // Sprites/Default: 런타임 생성에도 확실한 반투명 (URP Lit은 키워드 없이 투명 전환 안 됨)
        var wm = new Material(Shader.Find("Sprites/Default"));
        wm.color = new Color(0.4f, 0.95f, 0.8f, 0.30f);
        wave.GetComponent<Renderer>().sharedMaterial = wm;
        var hand = player.position + new Vector3(0, 0.9f, 0);
        wave.transform.position = hand;

        // 가스 구름이 손으로 수렴 + 웨이브 확장
        var starts = new List<Vector3>();
        var scales = new List<Vector3>();
        foreach (var c in gasClouds)
        {
            starts.Add(c != null ? c.position : Vector3.zero);
            scales.Add(c != null ? c.localScale : Vector3.one);
        }
        float t = 0, dur = 1.4f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            wave.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 14f, k);
            wm.color = new Color(0.4f, 0.95f, 0.8f, 0.30f * (1f - k));
            for (int i = 0; i < gasClouds.Count; i++)
            {
                var c = gasClouds[i];
                if (c == null) continue;
                float kk = Mathf.Clamp01(k * 1.3f - i * 0.02f);
                c.position = Vector3.Lerp(starts[i], hand, Mathf.SmoothStep(0f, 1f, kk));
                c.localScale = scales[i] * (1f - 0.9f * kk);
            }
            yield return null;
        }
        Destroy(wave);
        foreach (var c in gasClouds) if (c != null) Destroy(c.gameObject);
        gasClouds.Clear();
        score += 300;
        AddFloat(hand + new Vector3(0, 0.6f, 0), "+300 가스 흡수!");
        SKSound.Sfx("sfx_correct", 1f, 1.2f);
        // 교육 포인트: 밸브를 안 잠갔다면 가스는 다시 샌다
        if (gasAdding) Say("가스가 또 새어나와! 밸브부터 잠가야 해!", 3.5f);
        ultBusy = false;
    }

    /// towel 정답: 행주가 안전지대 바구니로 포물선 비행 — 바구니 없는 씬은 조용히 생략
    void FlyTowelToSafeZone(Vector3 from)
    {
        var basket = GameObject.Find("SafeBasket");
        if (basket == null) return;
        var b = RB(basket);
        var to = new Vector3(b.center.x, b.max.y + 0.05f, b.center.z);
        StartCoroutine(FlyTowelCo(from + new Vector3(0, 0.3f, 0), to));
    }

    IEnumerator FlyTowelCo(Vector3 from, Vector3 to)
    {
        // 납작한 분홍 행주 (절차 생성 — 바구니 속 행주와 같은 톤)
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "fly_towel";
        Destroy(go.GetComponent<Collider>());
        go.transform.localScale = new Vector3(0.42f, 0.09f, 0.34f);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.93f, 0.55f, 0.58f);
        mat.SetFloat("_Smoothness", 0.35f);
        go.GetComponent<Renderer>().sharedMaterial = mat;

        float dur = 0.85f, t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            var p = Vector3.Lerp(from, to, k);
            p.y += Mathf.Sin(k * Mathf.PI) * 2.3f;              // 포물선 아치
            go.transform.position = p;
            go.transform.rotation = Quaternion.Euler(k * 540f, k * 180f, 0);   // 빙글빙글
            yield return null;
        }
        // 착지: 뿅 축소 + 효과음 + 안내
        SKSound.Sfx("sfx_popup", 0.8f, 1.25f);
        AddFloat(to + new Vector3(0, 0.4f, 0), "안전지대로!");
        float s = 0f;
        var s0 = go.transform.localScale;
        while (s < 0.22f)
        {
            s += Time.deltaTime;
            go.transform.localScale = s0 * Mathf.Lerp(1f, 0.01f, s / 0.22f);
            yield return null;
        }
        Destroy(go);
    }

    void UxInit()
    {
        // 오답 플래시 (전체 화면, 평소 투명)
        Image im;
        var fr = Panel(canvas.transform, 0, 0, 1280, 720, Color.clear, out im);
        fxFlash = im;
        fxFlash.raycastTarget = false;
        // 정답 분필 O (중앙, 평소 꺼짐)
        var ort = CPanel(canvas.transform, 0, 20, 300, 300, Color.white, out im);
        im.sprite = SprChalkO();
        im.color = new Color(0.55f, 0.95f, 0.55f, 0f);
        fxChalkO = im;
        fxChalkO.raycastTarget = false;
        BuildPauseUI();
        BuildRankUI();
    }

    static Sprite SprChalkO()
    {
        int n = 256;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        float h = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = x - h + 0.5f, dy = y - h + 0.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(116f - r) * Mathf.Clamp01(r - 88f);
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(a)));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }

    // ---------- 정답 O / 오답 플래시 ----------
    void FxCorrect()
    {
        StartCoroutine(FxChalkOCo());
    }

    IEnumerator FxChalkOCo()
    {
        float t = 0;
        while (t < 0.75f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.25f);
            float c1 = 1.70158f, c3 = c1 + 1f;
            float s = 1f + c3 * Mathf.Pow(k - 1f, 3) + c1 * Mathf.Pow(k - 1f, 2);
            fxChalkO.transform.localScale = new Vector3(s, s, 1f);
            float alpha = t < 0.45f ? 0.95f : Mathf.Lerp(0.95f, 0f, (t - 0.45f) / 0.3f);
            var c = fxChalkO.color; c.a = alpha; fxChalkO.color = c;
            yield return null;
        }
        var c2 = fxChalkO.color; c2.a = 0f; fxChalkO.color = c2;
    }

    void FxWrong()
    {
        StartCoroutine(FxFlashCo());
    }

    IEnumerator FxFlashCo()
    {
        float t = 0;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            fxFlash.color = new Color(0.9f, 0.15f, 0.1f, Mathf.Lerp(0.32f, 0f, t / 0.4f));
            yield return null;
        }
        fxFlash.color = Color.clear;
    }

    // ---------- 발소리 (이동 코드에서 호출) ----------
    void StepSfx(float dt, bool sprinting)
    {
        stepT -= dt;
        if (stepT > 0f) return;
        stepT = sprinting ? 0.27f : 0.38f;
        SKSound.Sfx("sfx_step", 0.22f, Random.Range(0.92f, 1.12f));
    }

    // ---------- 일시정지 ----------
    void BuildPauseUI()
    {
        pnPause = new GameObject("pause");
        pnPause.transform.SetParent(canvas.transform, false);
        var rt = pnPause.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var bg = pnPause.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.03f, 0.05f, 0.72f);
        Image im;
        CPanel(pnPause.transform, 0, 30, 460, 300, new Color(0.93f, 0.89f, 0.79f), out im);
        CPanel(pnPause.transform, 0, 30, 448, 288, new Color(0.10f, 0.13f, 0.19f, 0.98f), out im);
        CPanel(pnPause.transform, 0, 130, 300, 50, Color.clear, out im);
        Label(im.transform, "일시정지", 30, new Color(0.95f, 0.93f, 0.85f), TextAnchor.MiddleCenter, true);
        CPanel(pnPause.transform, 0, 55, 380, 36, Color.clear, out im);
        Label(im.transform, "[ESC]  계속하기", 22, Color.white, TextAnchor.MiddleCenter, true);
        CPanel(pnPause.transform, 0, 10, 380, 36, Color.clear, out im);
        Label(im.transform, "[R]  처음부터 다시", 22, Color.white, TextAnchor.MiddleCenter, true);
        CPanel(pnPause.transform, 0, -35, 380, 36, Color.clear, out im);
        Label(im.transform, "[T]  타이틀로", 22, Color.white, TextAnchor.MiddleCenter, true);
        CPanel(pnPause.transform, 0, -95, 420, 30, Color.clear, out im);
        Label(im.transform, "가스안전 꿀팁: 자기 전엔 밸브 확인!", 16, new Color(1f, 0.84f, 0.31f), TextAnchor.MiddleCenter);
        pnPause.SetActive(false);
    }

    bool CanPause()
    {
        return !titleOpen && !over && openEv == null && !quizOpen && !mgOpen && !rankShown;
    }

    void TogglePause()
    {
        if (paused) { ResumeGame(); return; }
        if (!CanPause()) return;
        paused = true;
        pnPause.SetActive(true);
        Time.timeScale = 0f;
        SKSound.Sfx("sfx_popup", 0.6f);
    }

    void ResumeGame()
    {
        paused = false;
        pnPause.SetActive(false);
        Time.timeScale = 1f;
    }

    /// 일시정지 중 입력 (Update에서 호출) — true면 이번 프레임 나머지 입력 스킵
    bool PauseUpdate()
    {
        if (!paused) return false;
        if (SKIn.Down(KeyCode.Escape)) ResumeGame();
        else if (SKIn.Down(KeyCode.R)) { ResumeGame(); ResetGame(); }
        else if (SKIn.Down(KeyCode.T)) { Time.timeScale = 1f; ReloadScene(); }
        return true;
    }

    void ReloadScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    // ---------- 결과 랭크 ----------
    GameObject rankStars, rankBadgeRow;
    Text rankGrade, rankScoreT, rankMsgT, rankNextT;

    // 훈련 맵 → 다음 스테이지
    const string TRAIN_SCENE = "SafeKitchen3D_MOD";
    const string NEXT_SCENE = "SafeKitchen3D_CAMP";

    /// 훈련을 마치면 캠핑장 교육으로 자동 연결 (랭크 화면은 timeScale 0이라 unscaled로 센다)
    IEnumerator NextStageCo()
    {
        float wait = 6f, t = 0f;
        while (t < wait)
        {
            t += Time.unscaledDeltaTime;
            if (rankNextT != null)
                rankNextT.text = "다음 — 캠핑장 교육 (" + Mathf.CeilToInt(wait - t) + "초)   ·   [SPACE] 바로 이동";
            if (SKIn.Down(KeyCode.Space)) break;
            yield return null;
        }
        Time.timeScale = 1f;
        SKSound.VoStop();
        UnityEngine.SceneManagement.SceneManager.LoadScene(NEXT_SCENE);
    }

    void BuildRankUI()
    {
        pnRank = new GameObject("rank");
        pnRank.transform.SetParent(canvas.transform, false);
        var rt = pnRank.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var bg = pnRank.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.03f, 0.05f, 0.8f);
        Image im;
        CPanel(pnRank.transform, 0, 10, 640, 480, new Color(0.45f, 0.30f, 0.17f), out im);
        CPanel(pnRank.transform, 0, 10, 608, 448, new Color(0.15f, 0.20f, 0.18f, 0.99f), out im);
        var tag = CPanel(pnRank.transform, 240, 218, 170, 56, new Color(0.93f, 0.89f, 0.79f), out im);
        tag.localRotation = Quaternion.Euler(0, 0, -7f);
        Label(im.transform, "훈련 결과!", 21, NAVY, TextAnchor.MiddleCenter, true);
        // 등급
        CPanel(pnRank.transform, 0, 130, 300, 110, Color.clear, out im);
        rankGrade = Label(im.transform, "S", 96, YELLOW, TextAnchor.MiddleCenter, true);
        // 별
        CPanel(pnRank.transform, 0, 48, 400, 50, Color.clear, out im);
        rankStars = im.gameObject;
        // 점수
        CPanel(pnRank.transform, 0, -6, 500, 34, Color.clear, out im);
        rankScoreT = Label(im.transform, "", 22, new Color(0.95f, 0.93f, 0.85f), TextAnchor.MiddleCenter, true);
        // 이번 판 배지
        CPanel(pnRank.transform, 0, -66, 520, 70, Color.clear, out im);
        rankBadgeRow = im.gameObject;
        // 메시지 + 재시작
        CPanel(pnRank.transform, 0, -130, 560, 32, Color.clear, out im);
        rankMsgT = Label(im.transform, "", 19, new Color(0.95f, 0.93f, 0.85f), TextAnchor.MiddleCenter);
        CPanel(pnRank.transform, 0, -178, 560, 28, Color.clear, out im);
        Label(im.transform, "[R] 다시 도전!   [T] 타이틀", 20, YELLOW, TextAnchor.MiddleCenter, true);
        // 다음 스테이지 안내 (훈련 맵에서만 채워진다)
        CPanel(pnRank.transform, 0, -208, 560, 26, Color.clear, out im);
        rankNextT = Label(im.transform, "", 17, new Color(0.75f, 0.92f, 1f), TextAnchor.MiddleCenter, true);
        pnRank.SetActive(false);
    }

    void ShowRank()
    {
        if (rankShown) return;
        rankShown = true;
        CloseChoice();
        MgForceClose();
        StopJet();
        // 등급 판정
        string g; Color gc; int stars; string vo; string msg;
        if (score >= 1100 && acha <= 1) { g = "S"; gc = YELLOW; stars = 3; vo = "vo_rank_s"; msg = "가스안전 박사님! 가족에게도 알려주자!"; }
        else if (score >= 700) { g = "A"; gc = new Color(0.55f, 0.95f, 0.55f); stars = 2; vo = "vo_rank_a"; msg = "조금만 더 연습하면 완벽해!"; }
        else { g = "B"; gc = new Color(0.55f, 0.82f, 1f); stars = 1; vo = "vo_rank_b"; msg = "실수하면서 배우는 거야 — 다시 도전!"; }
        rankGrade.text = g;
        rankGrade.color = gc;
        rankScoreT.text = "점수 " + score + "   ·   아차 " + acha + "번";
        rankMsgT.text = msg;
        // 별
        foreach (Transform c in rankStars.transform) Destroy(c.gameObject);
        for (int i = 0; i < 3; i++)
        {
            var sgo = new GameObject("star");
            sgo.transform.SetParent(rankStars.transform, false);
            var t2 = sgo.AddComponent<Text>();
            t2.font = font; t2.fontSize = 40; t2.text = "★";
            t2.color = i < stars ? YELLOW : new Color(1, 1, 1, 0.22f);
            t2.alignment = TextAnchor.MiddleCenter;
            var srt = sgo.GetComponent<RectTransform>();
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = new Vector2((i - 1) * 70f, 0);
            srt.sizeDelta = new Vector2(60, 50);
        }
        // 이번 판 획득 배지
        foreach (Transform c in rankBadgeRow.transform) Destroy(c.gameObject);
        if (runBadges.Count == 0)
        {
            var lgo = new GameObject("none");
            lgo.transform.SetParent(rankBadgeRow.transform, false);
            var lt = lgo.AddComponent<Text>();
            lt.font = font; lt.fontSize = 17; lt.text = "이번 판 새 배지 없음 — 다음엔 모아 보자!";
            lt.color = new Color(1, 1, 1, 0.6f); lt.alignment = TextAnchor.MiddleCenter;
            var lrt = lgo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        }
        else
        {
            for (int i = 0; i < runBadges.Count && i < 6; i++)
            {
                var bgo = new GameObject("b" + i);
                bgo.transform.SetParent(rankBadgeRow.transform, false);
                var bi = bgo.AddComponent<Image>();
                var spr = Resources.Load<Sprite>("UI/" + BADGE_SPR[runBadges[i]]);
                if (spr != null) { bi.sprite = spr; bi.preserveAspect = true; }
                var brt = bgo.GetComponent<RectTransform>();
                brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
                brt.anchoredPosition = new Vector2((i - (runBadges.Count - 1) * 0.5f) * 66f, 0);
                brt.sizeDelta = new Vector2(58, 58);
            }
        }
        pnRank.SetActive(true);
        StartCoroutine(PopInPanel(pnRank.transform));
        Time.timeScale = 0f;
        SKSound.VoStop();
        // 훈련 맵을 마쳤으면 캠핑장 교육으로 이어간다
        if (rankNextT != null)
        {
            bool train = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == TRAIN_SCENE;
            rankNextT.text = "";
            if (train) StartCoroutine(NextStageCo());
        }
        SKSound.Sfx("st_win", 0.8f);
        SKSound.Vo(vo);
        if (fpMode) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }

    /// 랭크 화면 입력 — true면 나머지 입력 스킵
    bool RankUpdate()
    {
        if (!rankShown) return false;
        if (SKIn.Down(KeyCode.R))
        {
            rankShown = false;
            pnRank.SetActive(false);
            Time.timeScale = 1f;
            ResetGame();
        }
        else if (SKIn.Down(KeyCode.T)) { Time.timeScale = 1f; ReloadScene(); }
        return true;
    }
}
