# 세이프 키친 (Safe Kitchen)

한국가스안전공사 「가스안전 AI 게임·영상 공모전」 출품작.
오버쿡 스타일 쿼터뷰 주방에서 가스안전 행동요령을 배우는 3D 교육 액션 게임.
주인공은 KGS 공식 캐릭터 **가스레인저** (공모전 FAQ로 사용 허가 확인).

- 엔진: Unity 6 (URP) / 1인 개발 + AI 협업(Claude)
- 플레이: 일상 위험 대처(퀴즈) → 지진 1차: 밸브 차단·잔불 진압 → 지진 2차: 가스 누출·환기
- 재미 요소: 밸브 잠금 미니게임(연타→타이밍), 소화기 조준·쓸기 분사, 배지 수집(6종), 타격감 연출

## AI 제작 파이프라인

| 단계 | 도구 |
|---|---|
| 3D 에셋 | Google Gemini(이미지) → Tripo AI(image-to-3D) → Unity 자동 배치 |
| 캐릭터 리깅·애니 | Tripo 웹 리깅 |
| 대사 | Typecast TTS (캐릭터 보이스 '정의로') |
| 효과음 | ElevenLabs Sound Effects |
| 타이틀 영상 | Gemini 이미지 → 베오 영상 |
| 코드·씬 구성 | Claude + MCP for Unity |

프롬프트 원문: `Assets/Package04_Free_Kitchen/_SafeKitchen/TRIPO_프롬프트.md`, `AUDIO_프롬프트.md`
에셋 출처·라이선스: `Assets/Package04_Free_Kitchen/_SafeKitchen/ASSET_CREDITS.md`

## 빌드 안내 (주의)

유니티 에셋 스토어 패키지(주방 가구 Free Kitchen, Cartoon FX Remaster, Fog Particles)는
**재배포 불가(EULA)라 저장소에서 제외**되어 있다. 클론 후 그대로는 실행되지 않으며,
에셋 스토어에서 해당 무료 패키지 3종을 임포트해야 완전한 씬이 구성된다.
API 키 파일(`tripo_key.txt`, `eleven_key.txt`, `typecast_key.txt`)도 제외 대상 — 프로젝트 루트에 직접 생성.

## 저작권

KGS 가스레인저 캐릭터와 생성 에셋의 사용 범위는 공모전 출품 목적에 한한다. 무단 재사용 금지.
