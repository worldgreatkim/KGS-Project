
## [구현] 마라톤 P1~P3 — 오디오·랭크·일시정지·씬선택·미니게임 확장 — 2026-08-29 10:55
### 프롬프트
1번부터 17번 진행하자. (일레븐랩스 API 전달) 추가로 필요한거 있니?
> 맥락: 6시간 부재 위임 마라톤 17항목 중 P1(오디오)·P2(게임 기능 A)·P3(게임 기능 B)
### 조작 내역
- SKMainUX.cs 신규: 랭크 화면(칠판 스타일, S/A/B, 별·이번판 배지·대사), ESC 일시정지(꿀팁 포함), 정답 분필 O·오답 빨강 플래시 연출, 발소리, 씬 목록 상수, ReloadScene
- SKMain.cs: BGM 배선(bgm_title/main/danger, 파일 도착 시 자동 작동), 게임오버→랭크 화면, Choose/QuizChoice에 정답·오답 연출, 훈련 수료 기억(sktut — 재방문 시 훈련 생략, 타이틀 [G] 재수강), 타이틀 1/2/3 씬 전환
- SKSound.cs: 음악 채널(loops[3]) + Music()/StopMusic()
- SKMinigame.cs: kinds 3(창문 열기)·4(비눗물 점검) — 밸브 아트 숨김+콤팩트 패널(MgLayout), 래칫 틱 피치 상승. 지진2 창문·비눗물 점검을 미니게임으로 연결
- SKMainTutorial.cs: 수료 시 sktut 저장 / SKBadges.cs: runBadges + 타이틀 씬선택 라벨
- 오디오 생성: st_win(승리 징글)·sfx_ratchet(래칫)·vo_rank_s/a/b(랭크 대사, Typecast 정의로). BGM 3곡은 ElevenLabs music 권한 없음(401)으로 보류 — 배선만 완료
- EditorBuildSettings 3씬 등록(기본/FP/MOD)
### 검증
- 컴파일 Error 0. 플레이 QA 스냅: S랭크 화면(별3·배지2·대사), 일시정지(ESC/R/T·꿀팁), 재개 후 라벨 정상, 창문 미니게임 콤팩트 패널, 밸브 미니게임 tall 복원, 타이틀 씬선택([1][2]▶[3]·[G] 재수강)
- EditorBuildSettings.asset에 3씬 기록 확인 (최초 등록 누락 발견→재등록)
### 실패와 수정
- ShowRank 첫 호출 TargetInvocationException — 컴파일 전 시작된 스테일 플레이 세션. stop 후 재플레이로 해결
- 콤팩트 패널 미적용 — refresh_unity가 리컴파일 미실행. CompilationPipeline.RequestScriptCompilation으로 강제 후 정상
- 모달(랭크·일시정지) 중 스테이션 라벨 빌보드 동결로 거울처럼 보임 — 게임 중 정상, 딤 아래 배경이라 무해 판정
