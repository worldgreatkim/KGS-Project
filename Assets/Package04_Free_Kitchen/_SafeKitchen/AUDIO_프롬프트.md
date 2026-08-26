# 세이프 키친 — 오디오 & 타이틀 프롬프트북

생성 도구: 제미나이(타이틀 이미지·영상) · 수노(음악) · 일레븐랩스(효과음) · 타입캐스트(대사)

## 진행 상태 (2026-08-26 기준)

| 항목 | 상태 |
|---|---|
| 타이틀 이미지 3종 + 로고 + 키비주얼 영상(베오) | ✅ 생성·게임 적용 완료 |
| 효과음 22종 | ✅ 일레븐랩스 API 생성·배선 완료 (유료 상업 라이선스판) |
| 대사 18줄 | ✅ **타입캐스트 API '정의로(Justice Roh)' ssfm-v30**로 최종 생성 (일레븐랩스 ActionHero는 폐기) |
| BGM 3곡 + 승리 징글 | ⬜ 미제작 — 아래 §2 수노 프롬프트로 생성 예정 |

**대사 감정 연출 매핑 (타입캐스트 emotion_preset):** 튜토리얼·축하 = happy(수료·배지 1.4) /
지진 외침 = angry 1.2 / 화재·누출 안내 = toneup 1.2~1.3 / 안내 = normal. 파일은 vo_*.wav로
Resources/Audio에 저장 (구 mp3 자동 교체).

## 파일 규약 (중요)

- 생성한 파일은 `C:\Users\edwin\OneDrive\Desktop\가스공사 에셋\Audio\` 에 **아래 표의 파일명 그대로** 넣어줘. (타이틀 이미지는 `가스공사 에셋\` 바로 밑에)
- 파일명이 맞으면 내가 한 번에 임포트하고 게임에 연결한다 (다음 [구현] 명령).
- 포맷: BGM = mp3, 효과음 = mp3 또는 wav. ★ = 필수, ☆ = 여유 되면.
- 생성 후 `ASSET_CREDITS.md` 에 기록해야 하니, 어떤 프롬프트로 만들었는지 안 지우고 두면 됨 (기록은 내가 함).

---

## 1) 타이틀 화면 이미지 — 제미나이

> 팁: 프롬프트만 넣지 말고 **게임 스크린샷 1장 + 가스레인저 공식 일러스트**를 참조 이미지로 같이 첨부하면 게임과 톤이 맞는다.

**★ title_key.png — 메인 키비주얼 (로고 자리 비움)**
```
Cheerful 3D cartoon game title key art, cozy stylized kitchen in Overcooked style.
A small heroic red ranger character stands in front, proudly holding a red fire
extinguisher, with yellow, blue and pink ranger friends behind. Mint-green and
cream checkered tile floor, pastel mint counters with a gas stove showing a clean
blue flame, a small window with sunlight. Bright warm lighting, soft shadows,
toy-like rounded shapes, kid-friendly, joyful mood. Leave the upper third mostly
empty sky/wall space for a game logo. 16:9, high quality 3D render look.
```

**★ title_bg.png — 메뉴 배경 (캐릭터 없음, 버튼 깔 자리)**
```
Empty cozy 3D cartoon kitchen background for a game main menu, Overcooked style,
mint-green checkered floor, pastel counters, gas stove with soft blue flame,
boiling pot with steam, gentle depth of field blur, warm morning light from a
window, no characters, no text, calm and inviting. 16:9.
```

**★ title_logo.png — 타이틀 로고 글자 (제목 확정: 세이프 키친)**
```
게임 로고 타이포그래피 디자인. 한글 글자 "세이프 키친"을 통통하고 둥근 3D 카툰
레터링으로. 크림색 글자 몸체에 민트색 두꺼운 테두리, 장난감처럼 살짝 통통 튀는
배치, "키친" 글자 위에 작은 파란 불꽃 장식 하나. 글자 아래에 작게 영문
"SAFE KITCHEN". 순수 흰색 배경, 로고만 단독, 그림자 없음, 고해상도.
글자는 정확히 "세이프 키친" — 오탈자 없이.
```

**☆ title_logo_b.png — 로고 B안 (활기 버전)**
```
게임 로고 타이포그래피 디자인. 한글 글자 "세이프 키친"을 통통한 3D 카툰 레터링으로.
노란색 글자에 빨간 테두리, "세이프"는 위, "키친"은 아래 두 줄 배치, 글자 끝에서
살짝 피어오르는 파란 불꽃 하나. 순수 흰색 배경, 로고만 단독, 그림자 없음, 고해상도.
글자는 정확히 "세이프 키친" — 오탈자 없이.
```

> 로고 팁: 한글이 한 글자라도 깨지면 그 컷은 버리고 재생성 ("글자는 정확히" 문구 유지).
> 흰 배경으로 뽑으면 내가 배경 제거 후 타이틀 화면에 합성한다. 파일명 그대로 `가스공사 에셋\`에.

**★ TitleVisual.mp4 — 타이틀 영상 (제미나이 베오, 이미지→영상)**
MainKeyVisual.png를 첨부하고 아래 프롬프트로 8초 생성:
```
이 이미지를 그대로 살려서 부드럽게 움직이는 애니메이션으로. 네 명의 레인저가
제자리에서 살짝 위아래로 통통 숨쉬듯 움직이고, 노란 레인저는 손을 흔들고,
분홍 레인저는 즐겁게 웃는다. 가스레인지의 파란 불꽃이 일렁이고 냄비에서 김이
피어오른다. 카메라는 아주 천천히 미세하게 줌인. 컷 전환 없음, 캐릭터 디자인·색·
배경 변형 금지, 글자 없음, 부드러운 루프 느낌, 8초.
```
> 팁: 캐릭터가 걷거나 자리를 벗어나는 컷은 버리고 재생성 (제자리 모션만).
> 눈에 보이는 워터마크가 구석에 생기면 말해줘 — 내가 크롭한다.
> 파일명 `TitleVisual.mp4`로 `가스공사 에셋\`에.

**☆ result_bg.png — 결과 화면 배경**
```
Same cozy cartoon kitchen, evening warm light, confetti falling, celebratory mood,
slightly blurred, space in center for score panel, no characters, no text. 16:9.
```

---

## 2) 음악 — 수노 (모두 Instrumental 체크, 가사 없음)

| 파일명 | 용도 | 길이 |
|---|---|---|
| ★ bgm_main.mp3 | 평상시 플레이 (위험 8종 루프) | 1:30~2:00, 루프 |
| ★ bgm_danger.mp3 | 지진~시나리오 해결까지 | 1:00~1:30, 루프 |
| ★ bgm_title.mp3 | 타이틀 화면 | 1:00, 루프 |
| ☆ st_win.mp3 | 배지 획득 순간 징글 | 5~8초 |

**bgm_main** — 스타일 프롬프트:
```
playful cozy cooking game music, upbeat jazzy pop, pizzicato strings, marimba,
glockenspiel, light brass stabs, bouncy rhythm, cheerful kitchen chaos,
Overcooked vibe, instrumental, seamless loop, 118 bpm
```

**bgm_danger** — 스타일 프롬프트:
```
urgent but kid-friendly danger theme for a children's game, fast ticking
percussion, staccato strings, muted alarm bell accents, rising tension,
still playful not scary, instrumental, seamless loop, 140 bpm
```

**bgm_title** — 스타일 프롬프트:
```
warm inviting title theme for a cute cooking safety game, ukulele, whistling
melody, glockenspiel, soft claps, sunny morning feeling, instrumental,
seamless loop, 96 bpm
```

**st_win** — 스타일 프롬프트:
```
short 6 second victory jingle, toy trumpet fanfare, xylophone run up,
confetti celebration feeling, bright ending chord, instrumental
```

> 수노 팁: 루프용은 페이드아웃 없는 구간을 잘라 쓰면 됨(편집은 내가 가능). 같은 프롬프트로 2~3회 뽑아 제일 나은 것 선택.

---

## 3) 효과음 — 일레븐랩스 (Sound Effects, 영어 프롬프트 그대로 입력)

### UI · 피드백
| 파일명 | 게임 내 순간 | 길이 | 프롬프트 |
|---|---|---|---|
| ★ sfx_correct.mp3 | 정답 선택 (+점수) | 1s | bright cheerful ding-dong success chime, game show correct answer, playful, kid friendly |
| ★ sfx_wrong.mp3 | 오답 선택 | 1s | soft comedic gentle buzzer, wrong answer, cartoonish, not harsh |
| ★ sfx_popup.mp3 | 선택지/퀴즈 창 열림 | 0.5s | soft cartoon pop, bubble popping open, UI panel appear |
| ★ sfx_badge.mp3 | 배지 획득 | 2.5s | triumphant sparkly short fanfare with magical shimmer, achievement unlocked, toy orchestra |
| ☆ sfx_combo.mp3 | 콤보 상승 | 0.7s | ascending sparkly xylophone arpeggio, combo bonus, quick |
| ☆ sfx_acha.mp3 | 아차! (위험 놓침) | 1s | comedic slide whistle going down, near miss fail, cartoon |
| ☆ sfx_end.mp3 | 데모 종료 | 1.5s | referee whistle then soft cheerful bell, round over |

### 주방 상시 (루프)
| 파일명 | 게임 내 순간 | 길이 | 프롬프트 |
|---|---|---|---|
| ★ amb_boil.mp3 | 끓는 솥 (상시) | 8s 루프 | gentle bubbling pot of stew boiling softly, cozy kitchen, steady glugs, loopable |
| ★ amb_flame.mp3 | 화구 파란 불꽃 (상시) | 6s 루프 | soft steady gas stove flame hiss, low clean blue flame burning, loopable |
| ☆ sfx_ignite.mp3 | 밸브 재개방·재점화 | 1.2s | gas stove igniter clicking twice then a soft whoomp as flame lights |

### 상호작용
| 파일명 | 게임 내 순간 | 길이 | 프롬프트 |
|---|---|---|---|
| ★ sfx_valve.mp3 | 밸브 잠금/재개방 | 1s | metal gas valve lever turning firmly, short squeak then a solid click |
| ★ sfx_window.mp3 | 창문 열기/닫기 | 1s | sliding window opening with a light wooden rattle and soft thud |
| ★ sfx_pickup.mp3 | 소화기 들기 | 0.6s | quick item pickup, short whoosh with a light metallic clank |
| ☆ sfx_putdown.mp3 | 소화기 내려놓기 | 0.6s | placing a metal canister down gently on tile floor, soft clunk |
| ☆ sfx_soap.mp3 | 비눗물 점검 | 2.5s | soft soap foam bubbling and tiny bubbles popping gently |
| ☆ sfx_step.mp3 | 발걸음 (이동 루프) | 2s 루프 | light quick cartoon footsteps tapping on tile floor, small character, loopable |

### 지진 공통
| 파일명 | 게임 내 순간 | 길이 | 프롬프트 |
|---|---|---|---|
| ★ sfx_quake.mp3 | 흔들림 6초 | 7s | deep earthquake rumble with rattling dishes and creaking shelves, indoor kitchen |
| ★ sfx_blackout.mp3 | 정전 | 1s | electrical power cut thunk, lights shutting down with a fading hum |
| ★ sfx_rumble.mp3 | 지진 예고 (본지진 1.2초 전) | 2s | low deep subterranean rumble slowly building, distant ominous vibration, no impact (2026-08-27 추가) |

### 화재 코스 (1차 지진)
| 파일명 | 게임 내 순간 | 길이 | 프롬프트 |
|---|---|---|---|
| ★ sfx_fire_start.mp3 | 잔불 발화 | 1s | sudden whoosh of a small fire igniting |
| ★ amb_fire.mp3 | 불타는 중 (루프) | 6s 루프 | small fire crackling and burning steadily, loopable |
| ★ sfx_spray.mp3 | 소화기 분사 | 2s | fire extinguisher spraying a strong jet of dry powder, powerful hiss burst |
| ★ sfx_fire_out.mp3 | 불 꺼짐 | 1s | flame extinguished with a wet sizzle and final steam hiss |
| ☆ sfx_pin.mp3 | 퀴즈 정답(핀 뽑기) | 0.5s | metal safety pin pulled from a fire extinguisher, sharp small click |

### 누출 코스 (2차 지진)
| 파일명 | 게임 내 순간 | 길이 | 프롬프트 |
|---|---|---|---|
| ★ sfx_gas_leak.mp3 | 가스 깔리는 중 (루프) | 8s 루프 | continuous soft ominous gas leaking hiss from a pipe, steady, loopable |
| ★ sfx_vent.mp3 | 가스 창문 배출 | 3s | rushing air whoosh as gas is sucked out through a window, wind sweep fading away |

---

## 4) 캐릭터 목소리 — 일레븐랩스 (TTS)

### 체키 (해설위원) — 고정 대사 나레이션
보이스 선택: 한국어 지원 보이스 중 **밝고 또랑또랑한 젊은 여성/중성 톤** (어린이 애니 내레이터 느낌). Stability 낮춰 감정 살리기.

| 파일명 | 대사 (이대로 입력) |
|---|---|
| ★ vo_quake1.mp3 | 지진이다! 탁자 밑으로 숨어! |
| ★ vo_quake2.mp3 | 또 지진이다! 탁자 밑으로 숨어! |
| ★ vo_valve.mp3 | 흔들림이 멈췄어! 지진 뒤엔 먼저 가스밸브를 잠그고 환기해야 해! |
| ★ vo_fires.mp3 | 가스는 잠갔어! 앗, 잔불이 여기저기 붙었다 — 소화기로 꺼! |
| ★ vo_leak.mp3 | 불이 꺼지고 가스가 새서 바닥에 깔리고 있어! 밸브를 잠그고 창문을 열어 환기해! |
| ★ vo_vent.mp3 | 환기 중... 가스가 창문 밖으로 빠져나간다! |
| ★ vo_badge_fire.mp3 | 모든 불 진압 성공! 화재 대응 배지 획득! |
| ★ vo_badge_leak.mp3 | 환기 완료! 가스를 몰아냈어! 누출 대응 배지 획득! |
| ☆ vo_reopen.mp3 | 안전 확인 완료! 밸브를 다시 열었어 — 요리 재개! |
| ☆ vo_good.mp3 | 잘했어! |
| ☆ vo_no.mp3 | 안돼! 그건 위험해! |
| ☆ vo_hurry.mp3 | 서둘러! |

### 가스레인저 (주인공) — 짧은 감탄사 ☆
보이스: 씩씩한 소년 히어로 톤. 한 단어씩 따로 생성.

| 파일명 | 대사 |
|---|---|
| vo_r_yap.mp3 | 얍! |
| vo_r_good.mp3 | 좋아! |
| vo_r_oops.mp3 | 으악! |
| vo_r_phew.mp3 | 휴우~ |

---

## 5) 우선순위 요약

1. **1순위 (★ 22개):** bgm 3곡 + 핵심 효과음 15개 + 체키 대사 8줄 — 이것만 있어도 게임이 산다
2. **2순위 (☆):** 나머지 잔손맛 효과음 + 레인저 감탄사
3. 파일 다 넣고 "오디오 연결해줘" 라고 하면 내가 AudioSource 시스템 구현 + 전 트리거에 배선한다.
