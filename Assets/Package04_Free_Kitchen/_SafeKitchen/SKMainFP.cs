using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// 1인칭 모드 (V키 토글) — 쿼터뷰와 실시간 전환.
/// 맵·로직은 공유, 카메라·이동·시점만 이중화. 조준(pbody.forward) 체계는 그대로 재사용.
public partial class SKMain
{
    bool fpMode;
    float fpYaw, fpPitch;
    float fpShakeAmp;                 // 지진·타격 셰이크 (FP 전용, 감쇠)
    Vector3 qvCamPos; Quaternion qvCamRot;   // 쿼터뷰 원래 카메라 (복귀용)
    GameObject fpDot;                 // 크로스헤어 점
    Text fpHint;                      // [V] 시점 안내
    const float FP_SENS = 3.0f;
    const float FP_HEAD = 1.42f;

    /// Awake 말미 호출 — 원위치 저장 + UI + 저장된 모드 적용
    void FpInit()
    {
        qvCamPos = cam.transform.position;
        qvCamRot = cam.transform.rotation;
        // 크로스헤어 점 (FP에서만)
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
        var hr = CPanel(canvas.transform, 0, 0, 170, 26, Color.clear, out im);
        hr.anchorMin = hr.anchorMax = new Vector2(1f, 0f);
        hr.pivot = new Vector2(1f, 0f);
        hr.anchoredPosition = new Vector2(-14, 10);
        fpHint = Label(im.transform, "[V] 시점 전환", 15, new Color(1, 1, 1, 0.55f), TextAnchor.MiddleRight);
        if (PlayerPrefs.GetInt("skfp", 0) == 1) SetFp(true);
    }

    void ToggleFp() { SetFp(!fpMode); }

    void SetFp(bool on)
    {
        if (fpMode == on) return;
        fpMode = on;
        PlayerPrefs.SetInt("skfp", on ? 1 : 0);
        if (on)
        {
            // 현재 바라보는 방향에서 시작
            fpYaw = pbody.eulerAngles.y;
            fpPitch = 8f;
            fpShakeAmp = 0f;
        }
        else
        {
            cam.transform.SetPositionAndRotation(qvCamPos, qvCamRot);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        SetRangerVisible(!on);
        if (fpDot != null) fpDot.SetActive(on);
        SKSound.Sfx("sfx_popup", 0.5f);
        Say(on ? "1인칭 시점! 마우스로 둘러보고 [V]로 복귀" : "쿼터뷰 복귀!", 2.2f);
    }

    /// 레인저 모델 표시/숨김 (들고 있는 소화기는 유지)
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

    /// 카메라 추적은 모든 이동·연출 뒤에 (셰이크는 fpShakeAmp로)
    void LateUpdate()
    {
        if (!fpMode || cam == null) return;
        if (quakeState == 1) fpShakeAmp = Mathf.Max(fpShakeAmp, 0.10f);
        var jolt = fpShakeAmp > 0.001f
            ? new Vector3((Random.value - 0.5f) * fpShakeAmp * 2f, (Random.value - 0.5f) * fpShakeAmp, 0)
            : Vector3.zero;
        fpShakeAmp = Mathf.MoveTowards(fpShakeAmp, 0f, Time.deltaTime * 0.5f);
        var head = player.position + new Vector3(0, FP_HEAD, 0);
        cam.transform.position = head + jolt;
        cam.transform.rotation = Quaternion.Euler(fpPitch, fpYaw, 0);
    }
}
