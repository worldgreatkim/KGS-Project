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
    static readonly string[] SCENES = { "SafeKitchen3D", "SafeKitchen3D_FP", "SafeKitchen3D_MOD" };
    static readonly string[] SCENE_NAMES = { "기본 주방", "넓은 주방", "모듈 주방" };

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
    Text rankGrade, rankScoreT, rankMsgT;

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
        CPanel(pnRank.transform, 0, -180, 560, 30, Color.clear, out im);
        Label(im.transform, "[R] 다시 도전!   [T] 타이틀", 20, YELLOW, TextAnchor.MiddleCenter, true);
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
