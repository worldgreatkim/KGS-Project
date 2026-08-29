
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

## [구현] 마라톤 P4~P6 — GLB 다이어트·exe v0.3·문서 4종·QA 스냅 — 2026-08-29 11:25
### 프롬프트
맵 스케치는 좀더 고민햅몰게 일단 17번까지 진행해
> 맥락: 마라톤 잔여 P4(빌드)·P5(문서)·P6(QA·푸시)
### 조작 내역
- GLB 다이어트: gltf-transform weld+simplify(ratio 0.35)로 Mod_* 5종 72MB→25MB. 재임포트 후 MOD 씬 시각 무손상 확인. 원본은 샌드박스 백업
- EditorBuildSettings 3씬 등록 누락 발견→재등록(파일 검증). CanvasScaler match 0.5 기존 적용 확인
- exe v0.3: manage_build windows64, 3씬 포함 → Builds/StandaloneWindows64/GasProject.exe (694MB, 170s). DoNotShip 제외 zip 308MB(SafeKitchen_v03_win64.zip). Builds/를 gitignore 추가
- QA 스냅 7종 수집(Docs/shots): 타이틀·기본·확장쿼터뷰·1인칭·모듈·밸브미니게임·랭크 — jpg 축소본 병행
- 문서 4종: 제출문서_세이프키친.docx(7p, 표지+개요+교육매핑+AI활용 기술문서+실행+라이선스, 스냅 5장 삽입), 촬영콘티_1분.md, 기획서_v0.4.md, README 스크린샷 갤러리
### 검증
- zip 무결성: unzip -t 통과. docx: LibreOffice PDF 렌더 7페이지 눈검수(표·이미지·캡션 정상)
- 씬별 플레이 스냅 눈검수: 미니게임 콤팩트/tall 왕복, FP 밝은 컷 재촬영 포함
### 실패와 수정
- 빌드가 지정 경로 무시하고 기본 경로 출력 — 산출물 확인으로 갈음
- mount에서 zip 생성/삭제 불가(lock) — /tmp 생성 후 cp, 잔재 2개는 bat에서 삭제
- FP 1인칭 첫 스냅이 위험 연출로 어두움 — ResetGame+북향 재촬영

## [수정] MOD 씬 모듈 상판 높이 통일 + 냄비·불꽃 재정렬 — 2026-08-29 12:05
### 프롬프트
(스크린샷 첨부) 높낮이가 다르니깐 어색해
### 조작 내역
- 원인: 조립 정규화가 유닛 "전체 높이"를 1.25로 통일 — 스토브는 화구 프레임, 싱크는 수전 돌출까지 포함돼 몸체 상판이 조리대보다 낮았음
- 정점 슬라이스 실측(수전·화구 제외한 몸체 상판 판별): 싱크 0.96, 스토브 1.12 → 홀더 y스케일 보정(싱크 ×1.30, 스토브 ×1.17·1.12 기준 재보정)으로 몸체 상판을 조리대(1.25)에 정렬, 바닥 보존
- SKMain.cs: modKit 분기 추가 — boilPos를 서쪽 화구 중심(x-0.45)으로, 화구 불꽃 flameY 1.34(기본 씬 1.28 유지), 빈 화구 불꽃 x+0.45(화구 정중앙)
- BigPot(씬 오브젝트): 화구 실측 중심(20.75, z2.01)에 그레이트 안착(y1.575) — 위치만 조정
### 검증
- 클로즈업·와이드 스냅 눈검수: 싱크·스토브·조리대 상판 동일선, 화구 프레임만 자연 돌출
- 플레이 스냅: 냄비 아래 불꽃+김 정합, 빈 화구 불꽃 그레이트 정중앙 파란 점화
- 컴파일 Error 0, 기본·FP 씬은 modKit=false로 기존 상수 유지(회귀 없음)
### 실패와 수정
- bounds 기반 안착이 냄비 여유 volume 때문에 공중부양 → 정점 실측·눈맞춤 반복으로 확정
- 측정 중 씬 상태 혼선(플레이/에디트 왕복) — 최종 상태 재실측으로 정리
