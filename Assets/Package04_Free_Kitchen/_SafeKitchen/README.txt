세이프 키친 3D — Unity판 (Godot 3D 데모 이식)
================================================

시작하기
1. Unity에서 이 프로젝트(GasProject)를 연다 → 자동 컴파일
2. 상단 메뉴  SafeKitchen → 1. 씬 생성 (주방+게임 전체)
3. Play ▶

조작: 방향키/WASD 이동 · SPACE 살펴보기 · 1/2/3 선택 · R 재시작(종료 후)

폴더 구성
- SKData.cs   : 위험 8종 데이터 + 주방 배치표 (텍스트·밸런싱 수정은 여기)
- SKMain.cs   : 게임 본편 (플레이어·위험·UI·이펙트)
- Editor/SKBootstrap.cs : 씬 생성 메뉴

가구 수정: Hierarchy → Kitchen 아래에서 자유롭게 이동·회전·추가.
프리팹 추가: Package04_Free_Kitchen/FreeKitchen/Prefabs/Version03 에서 드래그.
재질이 분홍이면: 메뉴 SafeKitchen → 0. 재질 파이프라인 맞추기

이 폴더가 에셋 팩 안에 있는 이유: Claude 파일 접근 범위 제약.
원하면 _SafeKitchen 폴더를 Assets 루트로 드래그해도 된다 (Unity가 안전하게 옮김).
