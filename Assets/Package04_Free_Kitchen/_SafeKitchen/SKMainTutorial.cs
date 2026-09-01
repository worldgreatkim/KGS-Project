using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// 세이프 키친 — 가스안전 훈련소 튜토리얼 (레드 레인저 교관)
/// 대화 상자(초상화+이름표+타자 연출) + 실습 게이트(이동→달리기→위험→소화기→밸브)
public partial class SKMain
{
    // 게이트 종류
    const int TG_NONE = 0, TG_MOVE = 1, TG_SPRINT = 2, TG_HAZARD = 3, TG_EXT = 4, TG_SPRAY = 5, TG_VALVE = 6, TG_BTS = 7;

    static readonly string[] TUT_LINES = {
        "안녕! 가스안전 훈련소에 온 걸 환영해!\n나는 너를 가르칠 레드 레인저 교관이다!",
        "여긴 실제 주방과 똑같이 만든 훈련장이야.\n기초부터 하나씩 배워 보자!",
        "먼저 이동 훈련! 방향키(또는 WASD)로\n노란 화살표 지점까지 이동해 봐!",
        "좋아! 위급할 땐 [Shift]를 누른 채 달릴 수 있다.\n이번엔 달려서 다음 지점까지!",
        "잘했어! 주방 곳곳엔 위험이 숨어 있어.\n물음표에 다가가 [SPACE]를 누르고, 올바른 대처를 골라 봐!",
        "완벽해! 이번엔 소화기 훈련이다.\n구석의 소화기로 가서 [SPACE]로 들어 봐!",
        "훈련용 불꽃을 붙였다! 불 쪽을 바라보고\n[마우스 왼쪽]을 꾹 눌러 분사 — 좌우로 쓸면 더 빨리 꺼진다!",
        "다음 훈련! 가스 사고의 기본은 밸브 차단이다.\n가스밸브 앞에서 [SPACE] — 연타로 밸브를 잠가 봐!",
        "마지막 훈련! 부탄캔은 불 근처에서 가열되면 터진다.\n위험지대에 들어가기 전에 [SPACE]로 붙잡아 안전지대로 옮겨놓자!",
        "축하한다, 기초 훈련 수료!\n이제 진짜 주방이다 — 위험을 찾아 해결하고 점수를 모아라!",
    };
    static readonly int[] TUT_GATES = {
        TG_NONE, TG_NONE, TG_MOVE, TG_SPRINT, TG_HAZARD, TG_EXT, TG_SPRAY, TG_VALVE, TG_BTS, TG_NONE,
    };

    int tutStep = -1;            // -1=비활성
    bool tutDone;                // 이번 세션 수료 여부
    bool tutWait;                // 대화 넘김 대기 (SPACE)
    bool tutTyping;              // 타자 연출 중
    float tutCharT;
    int tutCharN;
    string tutText = "";
    bool wasSprinting;           // 이동 코드가 매 프레임 기록
    Vector3 tutTarget;
    GameObject pnTut;
    Text tutTxt, tutNext, tutSkipHint;
    bool tutFireLit;
    bool tutDimmed;   // 훈련 화재 중 화면 어둡게

    bool TutActive { get { return tutStep >= 0; } }
    int TutGate { get { return TutActive && !tutWait ? TUT_GATES[tutStep] : TG_NONE; } }

    // ---------- UI ----------
    void BuildTutUI()
    {
        var root = canvas.transform;
        pnTut = new GameObject("tut");
        pnTut.transform.SetParent(root, false);
        var rt = pnTut.AddComponent<RectTransform>();
        // 하단 중앙 고정 — 어떤 화면비에서도 대화창이 잘리지 않게
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(1280, 720);

        Image im;
        // 대화 상자: 크림 테두리 + 남색 본체 (레퍼런스 스타일)
        Panel(pnTut.transform, 236, 536, 948, 158, new Color(0.93f, 0.89f, 0.79f), out im);
        Panel(pnTut.transform, 242, 542, 936, 146, new Color(0.10f, 0.13f, 0.19f, 0.97f), out im);
        // 본문
        var tgo = new GameObject("tuttext");
        tgo.transform.SetParent(pnTut.transform, false);
        tutTxt = tgo.AddComponent<Text>();
        tutTxt.font = font;
        tutTxt.fontSize = 23;
        tutTxt.color = Color.white;
        tutTxt.alignment = TextAnchor.UpperLeft;
        tutTxt.lineSpacing = 1.25f;
        var trt = tgo.GetComponent<RectTransform>();
        trt.anchorMin = trt.anchorMax = new Vector2(0, 1);
        trt.pivot = new Vector2(0, 1);
        trt.anchoredPosition = new Vector2(278, -560);
        trt.sizeDelta = new Vector2(860, 120);
        // 이름표
        Panel(pnTut.transform, 250, 510, 190, 40, new Color(0.93f, 0.89f, 0.79f), out im);
        var nm = Label(im.transform, "레드 레인저 교관", 19, new Color(0.13f, 0.16f, 0.22f), TextAnchor.MiddleCenter, true);
        // 초상화 (대화 상자 왼쪽에 겹침)
        Sprite face = Resources.Load<Sprite>("UI/TutFace");
        var fgo = new GameObject("tutface");
        fgo.transform.SetParent(pnTut.transform, false);
        var fim = fgo.AddComponent<Image>();
        if (face != null) { fim.sprite = face; fim.preserveAspect = true; }
        else fim.color = Color.clear;
        var frt = fgo.GetComponent<RectTransform>();
        frt.anchorMin = frt.anchorMax = new Vector2(0, 1);
        frt.pivot = new Vector2(0.5f, 0.5f);
        frt.anchoredPosition = new Vector2(150, -600);
        frt.sizeDelta = new Vector2(210, 210);
        // 다음 화살표
        var ngo = new GameObject("tutnext");
        ngo.transform.SetParent(pnTut.transform, false);
        tutNext = ngo.AddComponent<Text>();
        tutNext.font = font;
        tutNext.fontSize = 24;
        tutNext.text = "▶  SPACE";
        tutNext.color = new Color(0.93f, 0.89f, 0.79f);
        tutNext.alignment = TextAnchor.MiddleRight;
        tutNext.fontStyle = FontStyle.Bold;
        var nrt = ngo.GetComponent<RectTransform>();
        nrt.anchorMin = nrt.anchorMax = new Vector2(0, 1);
        nrt.pivot = new Vector2(1, 1);
        nrt.anchoredPosition = new Vector2(1160, -652);
        nrt.sizeDelta = new Vector2(220, 30);
        // 건너뛰기 힌트 (우상단)
        var sgo = new GameObject("tutskip");
        sgo.transform.SetParent(pnTut.transform, false);
        tutSkipHint = sgo.AddComponent<Text>();
        tutSkipHint.font = font;
        tutSkipHint.fontSize = 17;
        tutSkipHint.text = "[F1] 건너뛰기";
        tutSkipHint.color = new Color(1, 1, 1, 0.65f);
        tutSkipHint.alignment = TextAnchor.MiddleRight;
        var srt = sgo.GetComponent<RectTransform>();
        srt.anchorMin = srt.anchorMax = new Vector2(0, 1);
        srt.pivot = new Vector2(1, 1);
        srt.anchoredPosition = new Vector2(1184, -500);   // 대화창 오른쪽 위
        srt.sizeDelta = new Vector2(240, 28);
        pnTut.SetActive(false);
    }

    // ---------- 진행 ----------
    void TutStart()
    {
        tutStep = -1;
        tutDone = false;
        pnTut.SetActive(true);
        if (pnUlt != null) pnUlt.SetActive(false);   // 훈련 대화창과 겹침 방지
        TutGoto(0);
    }

    void TutGoto(int step)
    {
        tutStep = step;
        tutText = TUT_LINES[step];
        tutTxt.text = "";
        tutCharT = 0;
        tutCharN = 0;
        tutTyping = true;
        tutWait = TUT_GATES[step] == TG_NONE;   // 게이트 스텝은 실습 후 자동 진행
        SKSound.Sfx("sfx_popup", 0.6f);
        SKSound.VoStop();   // 넘긴 대사는 끊고 새 대사 즉시 (큐 누적 방지)
        SKSound.Vo("vo_tut_" + step);
        TutSetupGate(TUT_GATES[step]);
    }

    /// 플레이어에서 충분히 먼 통로 지점을 목표로 선정 — "달려가는 훈련" 느낌의 원거리 보장.
    /// 후보는 확정 빈 공간인 통로 그리드(가로 3열×세로 3행). 남쪽(대화창에 가리는 영역)은 제외.
    Vector3 TutPickTarget(float dist, Vector3 fallback)
    {
        float[] gx = { 3.0f, roomW * 0.5f, roomW - 3.0f };       // 서 통로 · 중앙 통로 · 동 통로
        float[] gz = { 2.8f, 6.2f, 9.6f };                        // 북 통로 · 행간 통로 · 남측 통로
        Vector3 best = fallback;
        float bestScore = float.MinValue;
        for (int xi = 0; xi < 3; xi++)
            for (int zi = 0; zi < 3; zi++)
            {
                var pos = new Vector3(gx[xi], 0f, gz[zi]);
                if (Blocked(pos)) continue;
                float d = new Vector2(pos.x - player.position.x, pos.z - player.position.z).magnitude;
                if (d < 3.5f) continue;                           // 너무 가까운 지점 제외
                // dist에 가장 가까운 지점 우대 (초과는 허용, 미달은 감점)
                float score = -Mathf.Abs(d - dist) + (d >= dist * 0.8f ? 50f : 0f);
                if (score > bestScore) { bestScore = score; best = pos; }
            }
        return best;
    }

    void TutSetupGate(int gate)
    {
        if (arrowGo != null) Destroy(arrowGo);
        if (gate == TG_MOVE)
        {
            // 현재 캐릭터 위치에서 확실히 떨어진 지점으로 (고정 좌표는 폴백)
            tutTarget = TutPickTarget(7.0f, Pz(new Vector3(10.6f, 0, 5.6f), new Vector3(10.6f, 0, 10.6f)));
            MakeArrow(tutTarget + new Vector3(0, 1.9f, 0));
        }
        else if (gate == TG_SPRINT)
        {
            tutTarget = TutPickTarget(11.0f, Pz(new Vector3(4.2f, 0, 8.0f), new Vector3(4.2f, 0, 13.0f)));
            MakeArrow(tutTarget + new Vector3(0, 1.9f, 0));
        }
        else if (gate == TG_HAZARD)
        {
            // 연습용 위험: 행주 (시간 제한 없음)
            var def = SKData.EV["towel"];
            var node = SpawnMarker(HzPos("towel"));
            var bang = SpawnBang(node);
            uid++;
            hazards.Add(new Hz { id = uid, type = "towel", def = def, node = node, bang = bang, ttl = 99999f, reach = 2.4f });
        }
        else if (gate == TG_SPRAY)
        {
            if (!tutFireLit)
            {
                tutFireLit = true;
                DimLighting();   // 훈련 화재 동안 화면 어둡게 → 불꽃이 또렷하게
                tutDimmed = true;
                MakeFire(Pz(new Vector3(8.6f, 1.62f, 5.6f), new Vector3(8.6f, 1.62f, 10.6f)));   // 대피 탁자 위 연습 불 (상판에 안 묻히게 띄움)
                SKSound.Sfx("sfx_fire_start");
                SKSound.Loop(2, "amb_fire", 0.5f);
            }
        }
        else if (gate == TG_VALVE)
        {
            if (valvePivot != null) MakeArrow(valvePivot.position + new Vector3(0, 0.55f, 0.2f));
        }
        else if (gate == TG_BTS)
        {
            // 연습용 부탄캔 — 시간 제한 없이 천천히 다가온다
            var def = SKData.EV["bts"];
            var node = SpawnMarker(SKData.TUT_BTS_FROM);   // 훈련은 냉장고 앞 고정 등장
            var bang = SpawnBang(node);
            uid++;
            var hz = new Hz { id = uid, type = "bts", def = def, node = node, bang = bang, reach = 1.7f };
            BtsSetup(hz);
            hz.speed = SKData.BTS_SPEED * SKData.TUT_BTS_SLOW;
            hz.ttl = 99999f;   // 연습이라 놓쳐도 아차가 뜨지 않는다
            hazards.Add(hz);
            StartCoroutine(BtsIntroCo(hz));   // 등장 연출은 튜토리얼에서 한 번만
        }
    }

    /// SPACE — 타자 중이면 즉시 완성, 대기 중이면 다음 스텝
    void TutAdvance()
    {
        if (tutTyping)
        {
            tutTyping = false;
            tutCharN = tutText.Length;
            tutTxt.text = tutText;
            return;
        }
        if (!tutWait) return;
        if (tutStep >= TUT_LINES.Length - 1) { TutFinish(false); return; }
        TutGoto(tutStep + 1);
    }

    void TutGateClear()
    {
        SKSound.Sfx("sfx_correct", 0.8f);
        score += 50;
        AddFloat(player.position + new Vector3(0, 1.4f, 0), "+50 훈련 통과!");
        PunchZoom();
        // (밸브는 TutFinish에서 원위치)
        TutGoto(tutStep + 1);
    }

    void TutUpdate(float dt)
    {
        // 이 분기에서는 본편 해저드 루프가 돌지 않는다 — 부탄캔만 따로 움직여 준다
        for (int i = 0; i < hazards.Count; i++)
        {
            var h = hazards[i];
            if (h.speed > 0f) BtsStep(h, dt);
            if (h.bang != null) h.bang.transform.rotation = cam.transform.rotation;
        }
        // 화살표 둥실
        if (arrowGo != null)
        {
            var ap = arrowGo.transform.position;
            ap.y = arrowBaseY + Mathf.Abs(Mathf.Sin(timeAll * 4f)) * 0.25f;
            arrowGo.transform.position = ap;
            arrowGo.transform.rotation = cam.transform.rotation;
        }
        // 타자 연출
        if (tutTyping)
        {
            tutCharT += dt;
            int n = Mathf.Min(tutText.Length, (int)(tutCharT / 0.028f));
            if (n != tutCharN)
            {
                tutCharN = n;
                tutTxt.text = tutText.Substring(0, n);
            }
            if (n >= tutText.Length) tutTyping = false;
        }
        // 다음 표시 깜빡임
        bool showNext = tutWait && !tutTyping;
        var nc = tutNext.color;
        nc.a = showNext ? 0.55f + 0.45f * Mathf.Sin(timeAll * 5f) : 0f;
        tutNext.color = nc;

        // 게이트 판정
        int gate = TutGate;
        if (gate == TG_MOVE || gate == TG_SPRINT)
        {
            float d = new Vector2(player.position.x - tutTarget.x, player.position.z - tutTarget.z).magnitude;
            if (d < 1.2f && !tutTyping)
            {
                if (gate == TG_MOVE || wasSprinting) { Destroy(arrowGo); TutGateClear(); }
            }
        }
        else if (gate == TG_HAZARD)
        {
            if (hazards.Count == 0 && openEv == null && !tutTyping) TutGateClear();
        }
        else if (gate == TG_EXT)
        {
            if (carryExt != null && !tutTyping) TutGateClear();
        }
        else if (gate == TG_SPRAY)
        {
            bool allOut = firePosL.Count > 0;
            for (int i = 0; i < fireOutL.Count; i++) if (!fireOutL[i]) allOut = false;
            if (allOut && !spraying && !tutTyping)
            {
                // 연습 불 정리
                foreach (var go in FindObjectsByType<GameObject>())
                    if (go.name.StartsWith("quake_fire")) Destroy(go, 2.5f);
                firePsL.Clear(); fireSmokeL.Clear(); fireLightL.Clear(); firePosL.Clear(); fireOutL.Clear(); fireHpL.Clear();
                tutFireLit = false;
                SKSound.StopLoop(2);
                if (tutDimmed) { RestoreLighting(); tutDimmed = false; }
                TutGateClear();
            }
        }
        else if (gate == TG_BTS)
        {
            bool alive = false;
            foreach (var h in hazards) if (h.type == "bts") alive = true;
            if (!alive && !tutTyping) TutGateClear();
        }
        // TG_VALVE: SPACE → 밸브 미니게임(MgStart)이 담당, 성공 시 MgFinish가 TutGateClear 호출
    }

    /// 수료(false=정상 완주) 또는 F1 스킵(true)
    void TutFinish(bool skipped)
    {
        ClearBts();   // F1 로 건너뛰어도 연습용 캔이 남지 않게
        if (pnUlt != null) pnUlt.SetActive(true);   // 훈련 끝 — 게이지 복귀
        // 잔여 연습 요소 정리
        foreach (var hz in hazards) Destroy(hz.node);
        hazards.Clear();
        CloseChoice();
        foreach (var go in FindObjectsByType<GameObject>())
            if (go.name.StartsWith("quake_fire") || go.name == "guide_arrow" || go.name == "spray") Destroy(go);
        firePsL.Clear(); fireSmokeL.Clear(); fireLightL.Clear(); firePosL.Clear(); fireOutL.Clear(); fireHpL.Clear();
        tutFireLit = false;
        MgForceClose();
        StopJet();
        sprayAutoT = 0f;
        SKSound.StopLoop(2);
        if (tutDimmed) { RestoreLighting(); tutDimmed = false; }
        if (carryExt != null)
        {
            carryExt.SetParent(null, true);
            carryExt.localScale = extHomeScale;
            carryExt = null;
        }
        if (valvePivot != null) valvePivot.localEulerAngles = Vector3.zero;
        SKSound.VoStop();
        if (!skipped) { BadgeEarn(0); UnityEngine.PlayerPrefs.SetInt("sktut", 1); UnityEngine.PlayerPrefs.Save(); }   // 수료 배지 + 수료 기억
        pnTut.SetActive(false);
        tutStep = -1;
        tutDone = true;
        // 본편 시작
        score = 0; combo = 0; comboT = 0; acha = 0;
        stageT = 0;
        spawnT = 1.5f;
        Say(skipped ? "훈련 생략! 위험에 다가가 스페이스!" : "훈련 수료! 이제 실전이다 — 물음표를 찾아라!", 4f);
    }
}
