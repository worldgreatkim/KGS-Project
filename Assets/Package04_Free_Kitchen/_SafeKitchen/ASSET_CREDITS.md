# ASSET_CREDITS — 세이프 키친 (Unity판)

공모전 제출 문서용 에셋 출처 기록. 새 에셋 추가 시 반드시 갱신.

## 구매/스토어 에셋

| 에셋 | 출처 | 라이선스 |
|---|---|---|
| Free Kitchen - Cabinets and Equipment (가구 27종) | Unity Asset Store, BOXX-GAMES ASSETS | Unity Asset Store 표준 EULA (프로젝트 임베드 배포 허용) |
| Cartoon FX Remaster Free (VFX: 화재·연기·뿅·바람) | Unity Asset Store, Jean Moreno (JMO) | Unity Asset Store 표준 EULA — 사용 프리팹은 Resources/VFX에 복사본 |
| Fog Particles (가스 누출 안개 — 회색 틴트·알파 곡선 커스텀) | Unity Asset Store | Unity Asset Store 표준 EULA — Whitish Fog를 Resources/VFX/GasFog로 복사 |

## CC0 (퍼블릭 도메인 — 저장소 포함 가능)

| 에셋 | 출처 | 라이선스 |
|---|---|---|
| Assets/Kenney/K_*.glb (캠핑·자연 20종: 텐트·모닥불·나무·바위·울타리 등) | Kenney Survival Kit (kenney.nl) | CC0 1.0 — 텍스처 임베드 변환(gltf-transform) 후 이식 (2026-08-30) |
| SimpleNaturePack (Assets/SimpleNaturePack) | Unity Asset Store, JustCreate | Unity Asset Store 표준 EULA — 재배포 불가, 저장소 제외 |

## 생성형 AI 에셋 (2단계 파이프라인: Gemini 이미지 → Tripo AI image-to-3D)

- 이미지 생성: Google Gemini (나노바나나) — 프롬프트: `TRIPO_프롬프트.md` 참조, 원본 이미지: `refs/` 폴더
- 3D 변환: Tripo AI API (image_to_model, 텍스처 포함) — 2026-08-22 생성
- 위치: `Assets/Models3D/*.glb`

| 파일 | 원본 이미지 | 용도 |
|---|---|---|
| ValveBody.glb | refs/밸브 몸체.png | 가스 중간밸브 몸체 (잠그기 상호작용 고정부) |
| ValveLever.glb | refs/레버 손잡이.png | 밸브 레버 (90도 회전 가동부) |
| StoveStation.glb | refs/가스레인지.png | 오버쿡형 가스레인지 스테이션 |
| CounterBlock.glb | refs/기본 조리대 블록.png | 모듈 조리대 1칸 |
| CounterBlockLong.glb | refs/기본 조리대 블록 2배.png | 모듈 조리대 2칸 |
| SinkStation.glb | refs/싱크대.png | 싱크 스테이션 |
| FridgeMint.glb | refs/냉장고.png | 냉장고 |
| BigPot.glb | refs/대형솥.png | 대형 솥 (끓음 이펙트 대상) |
| WindowBlock.glb | refs/창문 블록.png | 창문 (환기 상호작용) |
| FireExtinguisherKR.glb | refs/소화기.png | 소화기 (한글 라벨) |
| IngredientCrate.glb | refs/재료 상자.png | 장식 소품 |
| PipeStraight.glb / PipeElbow.glb / PipeClamp.glb | refs/직선 파이프·ㄱ자 엘보·벽 고정쇠.png | 가스 배관 모듈 |
| FireExtinguisher_1.glb | (텍스트 프롬프트 직접 생성) | 소화기 테스트본 |
| RangerYellow/Blue/Pink/Red.glb | refs/~Ranger.png ×4 | **KGS 공식 캐릭터 '가스레인저'** — 주인공+스킨. 공모전 FAQ 공식 답변으로 사용 허가 확인(2026-08-18, 스크린샷 증빙 보관). 원본 일러스트를 자세 보정(나노바나나) 후 Tripo 3D 변환 |
| BuiltinCounter.glb | refs/빌트인 선반.png | 일체형 조리대 (화구+싱크 매립) |
| DrawerCounterLong.glb | refs/긴서랍.png | 남쪽 긴 서랍장 |
| HoodKR.glb | refs/후드.png | 레인지 후드 ×2 |
| WindowClosed.glb | refs/닫힌 창문.png | 창문 닫힘 상태 (환기 시 WindowOpen으로 스왑) |

- 미변환 이미지: refs/체크 타일 바닥테스처.png → 바닥 머티리얼 텍스처로 사용 예정
- 폰트: Malgun Gothic (Windows 시스템 폰트, UI 동적 로드)
- tex_window_sky.png / mat_window_sky.mat: 창문 바깥 풍경(하늘·구름·언덕) — 코드로 절차 생성 (외부 에셋 아님)
- UI/MainBg.png · MainKeyVisual.png · TitleLogo.png(원본 title.png 흰 배경 제거): 타이틀 화면 — Google Gemini(나노바나나) 생성, 프롬프트: `AUDIO_프롬프트.md` §1 (2026-08-25)
- UI/MainVideo.mp4: 타이틀 키비주얼 영상 — MainKeyVisual.png를 Google Gemini(베오) 이미지→영상 변환, 프롬프트: `AUDIO_프롬프트.md` §1, 1초 지점부터 루프 재생 (2026-08-25)
- UI/TutFace.png: 튜토리얼 교관 초상화 — refs/Red Ranger.png(KGS 공식 캐릭터, 사용 허가 확인) 머리 부분 크롭·투명화 (코드 가공)
- Audio/sfx_*.mp3 · amb_*.mp3 (효과음 22종): ElevenLabs Sound Effects API 생성 — 유료 플랜(상업 라이선스)에서 재생성, 프롬프트: `AUDIO_프롬프트.md` §3 (2026-08-26)
- Audio/vo_tut_0~8.wav · vo_*.wav (대사 18종): Typecast API TTS — 캐릭터 보이스 '정의로(Justice Roh)', ssfm-v30, 감정 프리셋(happy/toneup/angry) 연출 (2026-08-26). ※ 플랜 상업 사용 조건 확인 필요
- Audio/sfx_rumble.mp3 (지진 예고 럼블): ElevenLabs SFX 추가 생성 (2026-08-27)
- Audio/vo_tut_6·7.wav 재생성(쓸기 분사·밸브 연타 조작 반영) · vo_badge_all.wav (퍼펙트 배지): Typecast '정의로' (2026-08-27)
- UI/ValveCloseupBody.png · ValveCloseupLever.png: 밸브 잠금 미니게임 클로즈업 일러스트 — Google Gemini(나노바나나) 생성, 프롬프트: 2026-08-27 대화 기록, 흰 배경 제거·트림은 SKImgTool(자체 코드) 처리. ※ 몸체에 'KGS 가스' 각인 문구 포함(생성 결과) — 공식 CI 로고 아님
- UI/BadgeTut·BadgeFire·BadgeLeak·BadgeCheck·BadgeCombo·BadgePerfect.png (배지 6종): Google Gemini(나노바나나) 생성 + SKImgTool 투명화 (2026-08-27)
- Models3D/Mod_Counter·Mod_Stove·Mod_Sink·Mod_Corner·Mod_Wall.glb (모듈 킷 5종): Gemini 이미지(기준 유닛 파생 방식, refs/Mod_*.png) → Tripo image-to-model (2026-08-30). SafeKitchen3D_MOD 씬 그리드 조립용 — 스토어 가구 대체. ※ Mod_Stove는 탁상 버너 방식으로, Mod_Sink는 기존 SinkStation(깊은 보울)으로 이후 대체
- Models3D/Mod_Burner.glb (탁상 2구 버너): Gemini 이미지(refs/Mod_Burner.png) → Tripo image-to-model (2026-08-29). Counter 위 거치 방식 — 스토브 유닛 대체
- Models3D/FryingPan.glb (프라이팬): Tripo text-to-model 직접 생성 (2026-08-29, 프롬프트: 오버쿡드풍 네이비 팬+크림 손잡이). 빈 팬 가열 위험 연출 소품
- Models3D/CampTent·CampFirepit·CampButaneCan·CampTable·CampBurner.glb (캠핑 킷 5종): Gemini 파생 생성(refs/Camp_*.png, 기준 Mod_Burner) → Tripo image-to-model (2026-08-30). SafeKitchen3D_CAMP 캠핑 교육 씬용 — CampBurner는 실제 휴대용 가스버너 형태 재생성본
- Models3D/CampTentClosed.glb (닫힌 텐트): Gemini 파생 생성(기준 refs/Camp_Tent.png, 문·환기창 닫힘 상태) → Tripo image-to-model (2026-08-30). 캠핑 환기 시나리오에서 열린 텐트와 교체
- Models3D/GasAlert.glb (일산화탄소 경보기): Gemini 생성(refs/Gas_Alert.png) → Tripo image-to-model (2026-08-31). 텐트 실내 CO 위험 연출용
- refs/Camp_GrassTile.png: 캠핑장 잔디 바닥 텍스처 — Gemini 생성 (2026-08-30)
- Models3D/SafeBasket.glb (안전지대 보관 바구니): Gemini 파생 생성(기준: refs/Mod_Burner.png, 원본 refs/SafeBasket.png·SafeZone.png) → Tripo image-to-model (2026-08-30). 안전지대 매트 위 배치 — 위험물 보관 행동요령 시각화
- Models3D/int_gable·int_sideL·int_sideR.asset + mat_ac_canvas·_bk·mat_ac_pole.mat: 텐트 실내 A프레임(박공 삼각벽·경사 캔버스) — 코드로 절차 생성 (외부 에셋 아님, 2026-08-31)
- Models3D/af_*.asset + mat_ac_canvas_rf·_sd·mat_ac_flap·mat_ac_doorway·mat_ac_deck.mat: 캠핑장 진입 텐트(박공 정면·출입구·양옆 창문·문짝) — 코드로 절차 생성 (외부 에셋 아님, 2026-08-31)
- Models3D/CampTentHouse·CampTentHouseDoor·CampTentHouseOpen.glb (박공 텐트 3단 상태): Gemini 파생 생성(기준 refs/Camp_TentClosed.png → 박공 형태 변형 → 문·창 상태만 순차 수정, refs/Camp_TentHouse*.png) → Tripo image-to-model (2026-08-31). 캠핑 진입 텐트 — 문닫+창닫 / 문열+창닫 / 문열+창열. gltf-transform 다이어트 15MB→5.5MB
- Models3D/CampTentBody·CampTentBodyOpen·CampTentDoor.glb (캠핑 진입 텐트 본체·환기본체·문짝): Gemini 파생 생성(refs/Camp_TentBody*.png, Camp_TentDoor.png — Camp_TentHouse.png에서 한 번에 한 요소씩만 변형) → **Tripo 웹 v3.1 HD 수동 생성**(API 크레딧 소진, 동현님 직접 생성 후 전달) (2026-08-31).
  gltf-transform 감량 57MB→5.1MB(삼각형 193만→15만), 텍스처 2048. 문짝은 좌우 절반으로 분할해 td_L/td_R.asset 생성.
  머티리얼 밝기 보정: 기존 CampTent.glb 캔버스 밝기(144.2) 기준 Body 0.916 / BodyOpen 0.811 / Door 채널별(0.872,0.834,0.970)

## Mod_WallFanBody.glb / Mod_WallFanRotor.glb — 2026-09-01 04:43
- 파일: `Assets/Models3D/Mod_WallFanBody.glb`, `Assets/Models3D/Mod_WallFanRotor.glb`
- 출처: 생성형 AI. 레퍼런스 이미지 = Google Gemini, 3D 변환 = Tripo Studio (웹, `깔끔한 토폴로지` 옵션)
- 라이선스: Tripo 유료 구독(Pro) 산출물. 상업적 이용 가능
- 후처리: gltf-transform weld → simplify(ratio 0.02, error 0.003) → resize 512 → prune
  - 로터 986,728v / 1,932,144tri / 56.1MB → **27,015v / 38,638tri / 1.14MB**
  - 하우징 2.98MB → **349KB** (텍스처 4096 → 512)
- 생성 프롬프트 (하우징):
  > Soft matte 3D render, pastel game-asset style, isolated on pure white. A SQUARE wall-mounted kitchen exhaust fan HOUSING ONLY, with NO fan blades and NO center hub. A thick cream ivory (#F5EDD5) square outer frame with softly rounded corners, and inside it a deep navy blue (#2E3A63) square recessed box that is completely EMPTY in the middle. Shallow depth, like a ventilation fan casing mounted flat on a wall. Straight front view, perfectly square silhouette, centered and fully inside the frame, pure white background, no shadow, no wall, single object only.
- 생성 프롬프트 (로터):
  > Soft matte 3D render, pastel game-asset style, isolated on pure white. A fan ROTOR ONLY: five mint green (#8FE3B5) curved fan blades arranged radially around a small cream ivory (#F5EDD5) center hub. No outer ring, no housing, no frame, no square, no background object. Just the blades and hub floating alone. Straight front view, centered, pure white background, no shadow, single object only.

## Mod_BTS.glb / Mod_BTS2.glb — 부탄가스 소년단 — 2026-09-01 06:19
- 파일: `Assets/Models3D/Mod_BTS.glb`(점프본), `Assets/Models3D/Mod_BTS2.glb`(걷기 애니메이션본)
- 출처: 생성형 AI. 레퍼런스 이미지 = Google Gemini, 3D 변환·리깅·걷기 리타겟 = Tripo Studio (웹)
- 라이선스: Tripo 유료 구독(Pro) 산출물. 상업적 이용 가능
- 후처리: FBX → glTF 변환(fbx2gltf) → gltf-transform weld → simplify → resize 1024 → prune
  - Mod_BTS  1,914,265tri / 62.8MB → **30,626tri / 1.74MB**
  - Mod_BTS2 50,002tri / 34.6MB → **22,500tri / 2.51MB**, 걷기 클립 `preset:biped:walk`(2.33초, 채널 55) 보존
- 디자인 방향: 참고 이미지를 그대로 차용하지 않고 프로젝트 팔레트·소프트 매트 3D 톤으로 재해석
- 생성 프롬프트: Soft matte 3D render, pastel game-asset style, isolated on pure white. A cute but MENACING anthropomorphic BUTANE GAS CAN character. Body: warm red-orange (#E2564A) canister with cream (#F5EDD5) band and silver-grey (#C8CCD4) shoulder with red nozzle cap. Face: black wraparound sunglasses, angry slant, frowning mouth. Limbs clearly SEPARATED from the torso with visible empty space. Full body, centered, pure white background, no shadow, single character only.
