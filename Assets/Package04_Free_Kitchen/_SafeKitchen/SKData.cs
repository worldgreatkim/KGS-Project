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
    public const float FAN_SPIN = 400f;     // 벽 환풍기 로터 회전 속도(도/초)
    public const float BTS_SPEED = 1.05f;   // 부탄가스 소년단 접근 속도(m/s)
    public static bool BTS_HOP = true;      // true=통통 점프(트랜스폼), false=FBX 걷기 애니메이션
    public const float BTS_HOP_FREQ = 8.0f;  // 점프 주기 (rad/s, 약 2.5회/초)
    public const float BTS_HOP_H = 0.11f;    // 점프 높이(m)
    public const float BTS_SQUASH = 0.12f;   // 착지 눌림 비율
    public const float DANGER_LABEL_Y = 0.95f;  // '위험지대' 라벨 높이
    public const float BTS_BRIEF_1 = 2.2f;   // 등장 안내 1단계
    public const float BTS_BRIEF_2 = 3.2f;   // 위험 설명 2단계
    public const float BTS_BRIEF_3 = 3.6f;   // 조작 안내 3단계
    public const int   BTS_TRY = 400;        // 등장 지점 후보 표본 수
    public const float BTS_EDGE = 1.6f;      // 벽에서 띄울 여유
    public const float BTS_FAR_RATIO = 0.75f;// 가장 먼 거리의 이 비율 이상만 후보로
    public const float BTS_PATH_STEP = 0.4f; // 직선 경로 검사 간격
    public const float FLAME_UNDER = 0.07f;   // 조리기구 밑면에서 불꽃을 이만큼 내려 잡는다
    // --- 앞줄 우측 화구의 파란 불꽃 (가스밸브와 연동) ---
    public static readonly Vector3 STOVE_FLAME_MOD = new Vector3( 7.04f, 1.71f, 12.04f);
    public const float STOVE_FLAME_SIZE_MIN = 0.10f;
    public const float STOVE_FLAME_SIZE_MAX = 0.24f;
    public const float STOVE_FLAME_RATE = 150f;
    public const float STOVE_FLAME_RADIUS = 0.16f;
    public const float STOVE_FLAME_FADE = 0.35f;   // 밸브 조작 시 빛이 사그라드는 시간
    public static readonly Vector3 BTS_GOAL_MOD = new Vector3( 7.04f, 0f, 10.35f);  // 앞줄 좌측 화구 앞 도달 지점
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
        {"bts",    new Vector3(3.0f,  0.0f,  6.6f)},
    };

    // ---- 위험 위치 (MOD 아일랜드 배치 — SafeKitchen3D_MOD.unity, 2026-08-29 스케치 반영) ----
    // 4칸 아일랜드 (서→동) [조리대][버너][소품조리대][싱크]: 버너 A(6.1,4.2) B(17.3,4.2)·냄비 C(6.1,8.2) D(17.3,8.2)
    public static readonly Dictionary<string, Vector3> HZ_MOD = new Dictionary<string, Vector3>
    {
        {"boil",   new Vector3(17.2f, 1.65f, 4.2f)},
        {"yellow", new Vector3(6.1f,  1.65f, 4.2f)},
        {"hood",   new Vector3(20.9f, 2.35f, 1.6f)},
        {"towel",  new Vector3(20.2f, 1.4f,  0.85f)},
        {"oil",    new Vector3(6.1f,  1.5f,  11.42f)},
        {"hose",   new Vector3(17.4f, 1.5f,  1.2f)},
        {"butane", new Vector3(17.3f, 1.5f,  11.42f)},
        {"kid",    new Vector3(10.5f, 0.0f,  9.6f)},
        {"bts",    new Vector3( 3.60f, 0.0f, 10.40f)},   // 등장 위치 (통로 좌측 → 앞줄 좌측 화구로 접근)
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
        {"bts",    new Vector3(3.0f,  0.0f,  6.6f)},
    };

    // ---- 캠핑 교육 스테이지 (SafeKitchen3D_CAMP) — 시간 무제한, 체크리스트 5종 ----
    public static readonly Dictionary<string, Vector3> HZ_CAMP = new Dictionary<string, Vector3>
    {
        {"camp_tent",    new Vector3(4.5f,  0.9f, 3.5f)},    // 텐트 안 버너 (입구 앞)
        {"camp_foil",    new Vector3(7.5f,  1.1f, 9.2f)},    // 호일 감은 삼발이 (테이블1)
        {"camp_pan",     new Vector3(12.2f, 1.1f, 9.2f)},    // 과대불판 (테이블2)
        {"camp_can",     new Vector3(11.9f, 0.6f, 4.7f)},    // 모닥불 옆 부탄캔
        {"camp_dispose", new Vector3(16.5f, 0.5f, 8.7f)},    // 다 쓴 캔 폐기 더미
    };

    public static readonly Dictionary<string, Ev> EV_CAMP = new Dictionary<string, Ev>
    {
        {"camp_tent", new Ev { q = "텐트 안에서 버너를 쓰고 있어!", ttl = 99999f,
            toast = "텐트 밖으로 옮기고 환기까지! 일산화탄소는 무색무취라 더 위험해.",
            icons = new List<Opt> {
                new Opt { t = "버너를 밖으로 옮기고 환기한다", ok = true },
                new Opt { t = "문만 닫고 계속 쓴다", no = "밀폐되면 일산화탄소가 쌓여!" },
                new Opt { t = "잠깐이면 괜찮아", no = "텐트 안 가스기기는 절대 금지!" } } } },
        {"camp_foil", new Ev { q = "삼발이에 은박 호일을 감아놨어!", ttl = 99999f,
            toast = "호일·삼발이커버는 열을 가둬 부탄캔 폭발 위험!",
            icons = new List<Opt> {
                new Opt { t = "호일을 걷어낸다", ok = true },
                new Opt { t = "보온되니 그대로 둔다", no = "열이 캔으로 몰려 폭발 위험!" } } } },
        {"camp_pan", new Ev { q = "캔보다 큰 불판이 올려져 있어!", ttl = 99999f,
            toast = "과대불판은 부탄캔을 데워 파열 위험!",
            icons = new List<Opt> {
                new Opt { t = "작은 팬으로 바꾼다", ok = true },
                new Opt { t = "빨리 구우면 돼", no = "빨리 해도 열은 쌓여!" } } } },
        {"camp_can", new Ev { q = "모닥불 옆에 부탄캔이 있어!", ttl = 99999f,
            toast = "부탄캔은 화기·직사광선에서 멀리 보관!",
            icons = new List<Opt> {
                new Opt { t = "그늘 보관함으로 옮긴다", ok = true },
                new Opt { t = "곧 쓸 거니까 그대로", no = "열 받으면 캔이 터져!" } } } },
        {"camp_dispose", new Ev { q = "다 쓴 부탄캔, 어떻게 버릴까?", ttl = 99999f,
            toast = "환기되는 야외에서 잔가스 배출 후 구멍 뚫어 분리배출!",
            icons = new List<Opt> {
                new Opt { t = "야외에서 잔가스 빼고 구멍 뚫어 배출", ok = true },
                new Opt { t = "그냥 쓰레기통에 버린다", no = "잔가스가 남아 있으면 위험해!" },
                new Opt { t = "모닥불에 던져 태운다", no = "절대 안돼! 폭발해!" } } } },
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
        {"bts", new Ev { q = "부탄가스 소년단이 화구로 다가와!", ttl = 9f,
            toast = "부탄캔은 불 근처 금지 — 서늘하고 통풍되는 곳에!",
            icons = new List<Opt> {
                new Opt { t = "붙잡아 안전한 곳으로", ok = true },
                new Opt { t = "그냥 둔다", no = "가열되면 캔이 터져!" },
                new Opt { t = "물을 뿌려 식힌다", no = "열원에서 떼어내는 게 먼저야!" } } } },
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
