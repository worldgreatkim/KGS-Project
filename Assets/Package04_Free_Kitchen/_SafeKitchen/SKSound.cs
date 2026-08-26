using System.Collections.Generic;
using UnityEngine;

/// 사운드 재생 헬퍼 — Resources/Audio/ 의 클립을 이름으로 재생 (에디터·빌드 공통)
public static class SKSound
{

    static AudioSource oneShot;      // 효과음
    static AudioSource pitched;      // 효과음 (피치 가변 — 콤보 상승음 등)
    static AudioSource voice;        // 대사 (동시 1개)
    static AudioSource[] loops;      // 0=끓음 1=화구 2=가스누출
    static Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

    public static void Init(GameObject host)
    {
        if (oneShot != null) return;
        oneShot = host.AddComponent<AudioSource>();
        oneShot.playOnAwake = false;
        pitched = host.AddComponent<AudioSource>();
        pitched.playOnAwake = false;
        voice = host.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        voice.volume = 1f;
        loops = new AudioSource[3];
        for (int i = 0; i < 3; i++)
        {
            loops[i] = host.AddComponent<AudioSource>();
            loops[i].playOnAwake = false;
            loops[i].loop = true;
        }
    }

    static AudioClip Load(string name)
    {
        if (cache.ContainsKey(name)) return cache[name];
        var c = Resources.Load<AudioClip>("Audio/" + name);
        cache[name] = c;
        return c;
    }

    public static void Sfx(string name, float vol = 1f)
    {
        if (oneShot == null) return;
        var c = Load(name);
        if (c != null) oneShot.PlayOneShot(c, vol);
    }

    /// 피치 지정 효과음 (콤보 상승음·래칫 틱 등)
    public static void Sfx(string name, float vol, float pitch)
    {
        if (pitched == null) return;
        var c = Load(name);
        if (c == null) return;
        pitched.pitch = pitch;
        pitched.PlayOneShot(c, vol);
    }

    static readonly Queue<AudioClip> voQueue = new Queue<AudioClip>();

    /// 대사 재생 — 이미 말하는 중이면 끊지 않고 큐에 넣어 끝난 뒤 재생
    public static void Vo(string name)
    {
        if (voice == null) return;
        var c = Load(name);
        if (c == null) return;
        if (voice.isPlaying || voQueue.Count > 0)
        {
            if (voQueue.Count < 4) voQueue.Enqueue(c);
            return;
        }
        voice.clip = c;
        voice.Play();
    }

    /// 매 프레임 호출 — 큐에 대기 중인 대사를 이어서 재생
    public static void Tick()
    {
        if (voice == null || voice.isPlaying || voQueue.Count == 0) return;
        voice.clip = voQueue.Dequeue();
        voice.Play();
    }

    public static void VoStop()
    {
        voQueue.Clear();
        if (voice != null) voice.Stop();
    }

    public static void Loop(int ch, string name, float vol)
    {
        if (loops == null || ch < 0 || ch >= loops.Length) return;
        var c = Load(name);
        if (c == null) return;
        loops[ch].clip = c;
        loops[ch].volume = vol;
        if (!loops[ch].isPlaying) loops[ch].Play();
    }

    public static void StopLoop(int ch)
    {
        if (loops == null || ch < 0 || ch >= loops.Length) return;
        loops[ch].Stop();
    }
}
