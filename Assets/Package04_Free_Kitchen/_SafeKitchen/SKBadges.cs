using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// 배지 수집: 6종, PlayerPrefs 영속(게임 껐다 켜도 유지), 획득 팝업 → 우상단 비행, 타이틀 도감 슬롯
public partial class SKMain
{
    static readonly string[] BADGE_SPR = { "BadgeTut", "BadgeFire", "BadgeLeak", "BadgeCheck", "BadgeCombo", "BadgePerfect" };
    static readonly string[] BADGE_NAME = { "소방학교 수료", "화재 대응", "누출 대응", "안전 점검왕", "콤보 마스터", "퍼펙트 가스레인저" };
    static readonly string[] BADGE_RULE = {
        "기초 훈련을 마쳤다 — 이제 실전!",
        "불나면: 밸브 잠그고 소화기로 초기 진압!",
        "가스가 새면: 밸브 잠그고 창문 활짝 환기!",
        "위험 5번 해결 — 사고는 예방이 최고!",
        "연속 5콤보 — 침착하면 다 해낼 수 있어!",
        "모든 배지 획득! 진정한 가스레인저다!",
    };

    bool BadgeHas(int i) { return PlayerPrefs.GetInt("skb" + i, 0) == 1; }

    /// 배지 획득 (이미 있으면 짧은 징글만)
    void BadgeEarn(int i)
    {
        if (BadgeHas(i)) { SKSound.Sfx("sfx_badge", 0.55f); return; }
        PlayerPrefs.SetInt("skb" + i, 1);
        PlayerPrefs.Save();
        runBadges.Add(i);   // 랭크 화면용
        SKSound.Sfx("sfx_badge");
        if (i == 5) SKSound.Vo("vo_badge_all");
        StartCoroutine(BadgePopCo(i));
        // 0~4 전부 모이면 퍼펙트
        if (i != 5)
        {
            bool all = true;
            for (int k = 0; k < 5; k++) if (!BadgeHas(k)) all = false;
            if (all) StartCoroutine(BadgeAllSoon());
        }
    }

    /// 위험 정답 처리 시 호출 — 점검왕(누적 5회)·콤보 마스터(콤보 5)
    void BadgeProgress()
    {
        int n = PlayerPrefs.GetInt("skchk", 0) + 1;
        PlayerPrefs.SetInt("skchk", n);
        if (n == 5) BadgeEarn(3);
        if (combo == 5) BadgeEarn(4);
    }

    IEnumerator BadgeAllSoon()
    {
        yield return new WaitForSeconds(2.8f);   // 앞 배지 팝업이 끝난 뒤
        BadgeEarn(5);
    }

    /// 획득 연출: 중앙 팝(이징 등장) → 1.5초 유지 → 우상단으로 날아가며 축소
    IEnumerator BadgePopCo(int i)
    {
        var root = new GameObject("badge_pop");
        root.transform.SetParent(canvas.transform, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, 46);
        rt.sizeDelta = new Vector2(290, 290);
        var img = root.AddComponent<Image>();
        var spr = Resources.Load<Sprite>("UI/" + BADGE_SPR[i]);
        if (spr != null) { img.sprite = spr; img.preserveAspect = true; }
        else img.color = YELLOW;
        Image pim;
        CPanel(root.transform, 0, -212, 660, 74, new Color(0.10f, 0.13f, 0.19f, 0.92f), out pim);
        var nameT = Label(pim.transform, "★ " + BADGE_NAME[i] + " 배지 획득!", 24, YELLOW, TextAnchor.UpperCenter, true);
        var ruleT = Label(pim.transform, BADGE_RULE[i], 17, Color.white, TextAnchor.LowerCenter);
        var nrt = nameT.GetComponent<RectTransform>();
        nrt.offsetMin = new Vector2(0, 8); nrt.offsetMax = new Vector2(0, -8);
        var rrt = ruleT.GetComponent<RectTransform>();
        rrt.offsetMin = new Vector2(0, 8); rrt.offsetMax = new Vector2(0, -8);

        float t = 0;
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / 0.35f);
            float c1 = 1.70158f, c3 = c1 + 1f;
            float s = 1f + c3 * Mathf.Pow(k - 1f, 3) + c1 * Mathf.Pow(k - 1f, 2);
            root.transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        root.transform.localScale = Vector3.one;
        yield return new WaitForSeconds(1.5f);
        var p0 = rt.anchoredPosition;
        var target = new Vector2(560f, 305f);
        t = 0;
        while (t < 0.45f)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.45f));
            rt.anchoredPosition = Vector2.Lerp(p0, target, k);
            root.transform.localScale = Vector3.one * (1f - 0.82f * k);
            yield return null;
        }
        Destroy(root);
    }

    /// 타이틀 하단 도감 슬롯 6개 — 미획득은 어두운 실루엣
    void BuildTitleBadges(Transform root)
    {
        for (int i = 0; i < 6; i++)
        {
            var go = new GameObject("bslot" + i);
            go.transform.SetParent(root, false);
            var srt = go.AddComponent<RectTransform>();
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0f);
            srt.pivot = new Vector2(0.5f, 0f);
            srt.anchoredPosition = new Vector2((i - 2.5f) * 64f, 122f);
            srt.sizeDelta = new Vector2(56, 56);
            var img = go.AddComponent<Image>();
            var spr = Resources.Load<Sprite>("UI/" + BADGE_SPR[i]);
            if (spr != null) { img.sprite = spr; img.preserveAspect = true; }
            img.color = BadgeHas(i) ? Color.white : new Color(0.08f, 0.10f, 0.14f, 0.45f);
        }
        // 씬 선택 + 훈련 재수강 안내
        var sgo = new GameObject("scenesel");
        sgo.transform.SetParent(root, false);
        var st = sgo.AddComponent<Text>();
        st.font = font; st.fontSize = 15;
        string cur = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        var sb2 = new System.Text.StringBuilder();
        for (int i = 0; i < SCENES.Length; i++)
            sb2.Append((cur == SCENES[i] ? "▶" : "") + "[" + (i + 1) + "] " + SCENE_NAMES[i] + "   ");
        sb2.Append(PlayerPrefs.GetInt("sktut", 0) == 1 ? "· [G] 훈련 다시 받기" : "");
        st.text = sb2.ToString();
        st.color = new Color(0.25f, 0.30f, 0.35f, 0.85f);
        st.alignment = TextAnchor.MiddleCenter;
        st.horizontalOverflow = HorizontalWrapMode.Overflow;
        var strt = sgo.GetComponent<RectTransform>();
        strt.anchorMin = strt.anchorMax = new Vector2(0.5f, 0f);
        strt.pivot = new Vector2(0.5f, 0f);
        strt.anchoredPosition = new Vector2(0, 186);
        strt.sizeDelta = new Vector2(900, 24);
    }
}
