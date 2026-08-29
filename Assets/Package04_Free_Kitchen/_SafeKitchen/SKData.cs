using System.Collections.Generic;
using UnityEngine;

/// 세이프 키친 3D — 전 데이터 테이블 (Godot GameData 이식, 주방 스테이지 8종)
/// 밸런싱·텍스트는 이 파일에서만 수정한다.
public static class SKData
{
    public class Opt
    {
        public string t;        // 선택지 텍스트
        public bool ok;         // 정답 여부
        public string no;       // 오답 시 체키 대사
    }

    public class Ev
    {
        public string q;        // 상황 질문
        public string toast;    // 정답 시 안전 지식 토스트
        public float ttl;       // 제한 시간(초)
        public List<Opt> icons; // 선택지 (게임이 섞어서 표시)
    }

    // ---- 수치 ----
    public const float SPEED = 4.2f;        // 이동 속도
    public const float RUN_MULT = 1.5f;     // 쉬프트 달리기 배율
    public const float INTERACT_D = 1.25f;  // 상호작용 거리
    public const float DEMO_DUR = 60f;      // 데모 길이
    public const float COMBO_WINDOW = 4f;   // 콤보 유지 시간
    public const int COMBO_MAX = 4;
    public const int BASE_PTS = 100;
    public const int RETRY_PTS = 40;

    // ---- 방 크기 (2026-08-29 확장: 24×15.8m — 바닥 x 1.1~25.1, z 0~15.8) ----
    public const float RW = 25.0f;
    public const float RD = 15.7f;

    // ---- 위험 발생 위치 (확장 맵 기준 — 소속 가구 이동량 상속) ----
    public static readonly Dictionary<string, Vector3> HZ = new Dictionary<string, Vector3>
    {
        {"boil",   new Vector3(21.2f, 1.65f, 2.0f)},
        {"yellow", new Vector3(9.1f, 1.65f, 2.0f)},
        {"hood",   new Vector3(20.9f, 2.35f, 1.6f)},
        {"towel",  new Vector3(20.2f, 1.4f, 0.85f)},
        {"oil",    new Vector3(4.8f,  1.5f,  13.9f)},
        {"hose",   new Vector3(7.6f,  1.5f,  14.0f)},
        {"butane", new Vector3(11.2f, 1.5f,  13.9f)},
        {"kid",    new Vector3(3.0f,  0.0f,  3.2f)},
    };

    // ---- 위험 위치 (MOD 아일랜드 배치 — SafeKitchen3D_MOD.unity, 2026-08-29 스케치 반영) ----
    // 4칸 아일랜드 (서→동) [조리대][버너][소품조리대][싱크]: 버너 A(6.1,4.2) B(17.3,4.2)·냄비 C(6.1,8.2) D(17.3,8.2)
    public static readonly Dictionary<string, Vector3> HZ_MOD = new Dictionary<string, Vector3>
    {
        {"boil",   new Vector3(17.2f, 1.65f, 4.2f)},
        {"yellow", new Vector3(6.1f,  1.65f, 4.2f)},
        {"hood",   new Vector3(20.9f, 2.35f, 1.6f)},
        {"towel",  new Vector3(20.2f, 1.4f,  0.85f)},
        {"oil",    new Vector3(6.1f,  1.5f,  8.2f)},
        {"hose",   new Vector3(17.4f, 1.5f,  1.2f)},
        {"butane", new Vector3(17.3f, 1.5f,  8.2f)},
        {"kid",    new Vector3(4.5f,  0.0f,  11.0f)},
    };

    // ---- 위험 위치 (원본 맵 19.2×10.8 — SafeKitchen3D.unity 쿼터뷰 씬용) ----
    public static readonly Dictionary<string, Vector3> HZ_OLD = new Dictionary<string, Vector3>
    {
        {"boil",   new Vector3(16.4f, 1.65f, 2.0f)},
        {"yellow", new Vector3(9.1f, 1.65f, 2.0f)},
        {"hood",   new Vector3(16.1f, 2.35f, 1.6f)},
        {"towel",  new Vector3(15.4f, 1.4f, 0.85f)},
        {"oil",    new Vector3(4.8f,  1.5f,  8.9f)},
        {"hose",   new Vector3(7.6f,  1.5f,  9.0f)},
        {"butane", new Vector3(11.2f, 1.5f,  8.9f)},
        {"kid",    new Vector3(3.0f,  0.0f,  3.2f)},
    };

    // ---- 위험 이벤트 (주방 8종) ----
    public static readonly Dictionary<string, Ev> EV = new Dictionary<string, Ev>
    {
        {"boil", new Ev { q = "불이 꺼졌는데 가스는 계속 나와!", ttl = 10f,
            toast = "불이 꺼져도 가스는 나온다 — 환기 후 밸브 잠금!",
            icons = new List<Opt> {
                new Opt { t = "환기하고 밸브 잠그기", ok = true },
                new Opt { t = "스위치를 켠다", no = "안돼! 스위치 불꽃이 튀어!" },
                new Opt { t = "라이터로 불 붙인다", no = "라이터는 폭발 위험!" } } } },
        {"towel", new Ev { q = "불 바로 옆에 행주가 있어!", ttl = 8f,
            toast = "불 옆에는 타기 쉬운 물건 금지!",
            icons = new List<Opt> {
                new Opt { t = "안전한 곳으로 치운다", ok = true },
                new Opt { t = "물만 뿌려 둔다", no = "물보다 먼저, 불에서 멀리!" },
                new Opt { t = "그대로 둔다", no = "그대로 두면 불이 옮아!" } } } },
        {"yellow", new Ev { q = "불꽃 색이 노란색이야!", ttl = 8f,
            toast = "노란 불꽃 = 불완전연소 신호. 환기하고 점검!",
            icons = new List<Opt> {
                new Opt { t = "환기하고 점검 받기", ok = true },
                new Opt { t = "그냥 계속 쓴다", no = "계속 쓰면 CO가 생겨!" },
                new Opt { t = "불을 더 키운다", no = "불을 키우면 더 위험!" } } } },
        {"butane", new Ev { q = "캔보다 큰 불판이 올려져 있어!", ttl = 8f,
            toast = "과대불판은 부탄캔을 데워 파열 위험!",
            icons = new List<Opt> {
                new Opt { t = "불판 치우고 캔 분리", ok = true },
                new Opt { t = "빨리 요리하면 돼", no = "빨리 해도 열은 쌓여!" } } } },
        {"hood", new Ev { q = "환기 후드가 꺼져 있어!", ttl = 11f,
            toast = "조리 중 환기는 필수 — 후드와 창문!",
            icons = new List<Opt> {
                new Opt { t = "후드 켜고 환기하기", ok = true },
                new Opt { t = "그냥 둔다", no = "환기 없인 CO가 쌓여!" } } } },
        {"hose", new Ev { q = "밸브 쪽에서 가스가 새는 것 같아!", ttl = 9f,
            toast = "누출 점검은 비눗물로! 퓨즈콕이 있으면 자동 차단.",
            icons = new List<Opt> {
                new Opt { t = "비눗물로 점검하기", ok = true },
                new Opt { t = "라이터로 확인하기", no = "라이터로 확인은 절대 금지!!" } } } },
        {"oil", new Ev { q = "기름이 과열돼 연기가 나!", ttl = 9f,
            toast = "기름 화재에 물 금지! 불 끄고 뚜껑으로 덮기!",
            icons = new List<Opt> {
                new Opt { t = "불 끄고 뚜껑 덮기", ok = true },
                new Opt { t = "물을 붓는다", no = "기름불에 물은 폭발 확산!!" },
                new Opt { t = "입으로 분다", no = "불씨가 날려서 더 위험!" } } } },
        {"kid", new Ev { q = "아이가 화구 쪽으로 가고 있어!", ttl = 14f,
            toast = "어린이는 가스레인지 근처 금지 — 꼭 어른과 함께!",
            icons = new List<Opt> {
                new Opt { t = "부드럽게 데려온다", ok = true },
                new Opt { t = "소리질러 놀래킨다", no = "놀라면 넘어져서 더 위험해!" } } } },
    };

    // ---- 주방 배치 (Godot Kitchen3D와 동일 좌표) ----
    // {프리팹명, 위치, y회전, 스케일}
    public class Place
    {
        public string f; public Vector3 pos; public float rot; public float scl;
        public string ver;   // 색 = 프리팹 버전 (Version01=핑크, Version02=올리브, Version03=블루)
        public Place(string f, Vector3 pos, float rot = 0f, float scl = 1.5f, string ver = "Version03")
        { this.f = f; this.pos = pos; this.rot = rot; this.scl = scl; this.ver = ver; }
    }

    public static readonly List<Place> KIT = new List<Place>
    {
        new Place("FreeCabinet01",     new Vector3(2.1f, 0, 1.9f)),
        new Place("FreeCabinet03",     new Vector3(3.95f, 0, 1.9f), 0f, 1.5f, "Version01"),
        new Place("FreeCabinet02",     new Vector3(10.6f, 0, 1.95f)),
        new Place("FreeStove",         new Vector3(12.5f, 0, 1.95f)),
        new Place("FreeCabinet02",     new Vector3(14.4f, 0, 1.95f)),
        new Place("FreeExtractorHood", new Vector3(11.7f, 2.18f, 1.75f)),
        new Place("FreeRefrigerator",  new Vector3(16.2f, 0.49f, 1.7f), 0f, 1.0f, "Version01"),
        new Place("FreeToaster",       new Vector3(9.5f, 1.26f, 2.0f), 0f, 1.5f, "Version01"),
        new Place("FreeVase",          new Vector3(13.5f, 1.26f, 9.0f), 0f, 1.5f, "Version01"),
        new Place("FreePotBig",        new Vector3(12.15f, 1.27f, 1.9f)),
        new Place("FreePotSmall",      new Vector3(12.95f, 1.27f, 2.05f), 40f),
        new Place("FreeShelf",         new Vector3(0.78f, 1.5f, 3.4f), 90f),
        new Place("FreeCabinet02",     new Vector3(4.4f, 0, 9.1f)),
        new Place("FreeCabinet02",     new Vector3(6.16f, 0, 9.1f)),
        new Place("FreeCabinet02",     new Vector3(7.92f, 0, 9.1f)),
        new Place("FreeCabinet02",     new Vector3(9.68f, 0, 9.1f)),
        new Place("FreeCabinet02",     new Vector3(11.44f, 0, 9.1f)),
        new Place("FreeCabinet02",     new Vector3(13.2f, 0, 9.1f)),
        new Place("FreeMirkowave",     new Vector3(11.85f, 1.26f, 9.15f)),
        new Place("FreeCoffeeMaker",   new Vector3(4.6f, 1.26f, 9.1f), 0f, 1.5f, "Version02"),
        new Place("FreeBowl01",        new Vector3(8.75f, 1.26f, 9.0f)),
        new Place("FreePlate",         new Vector3(10.3f, 1.26f, 9.0f)),
        new Place("FreeCarpet",        new Vector3(8.6f, 0.02f, 5.6f)),
        new Place("FreeTable",         new Vector3(8.6f, 0, 5.6f), 0f, 1.5f, "Version02"),
        new Place("FreeChair",         new Vector3(7.3f, 0, 5.6f), 90f, 1.5f, "Version01"),
        new Place("FreeChair",         new Vector3(9.9f, 0, 5.6f), -90f, 1.5f, "Version01"),
        new Place("FreeCup",           new Vector3(8.3f, 1.13f, 5.5f)),
    };
}
