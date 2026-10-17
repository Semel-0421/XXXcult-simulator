배경음악 파일. 이름이 곡을 정하고 반복 재생됨. 분위기와 생성 프롬프트는 Docs/audio_design.md, Docs/audio_prompts.csv.
생성: D:\AI\ComfyUI\.venv\Scripts\python.exe Docs/gen_ai_audio.py (ACE-Step 1.5, 곡당 약 146초 루프)

menu             타이틀 화면
life             교단 창설 전 성장기
tier_Persecuted  명망 탄압 / tier_Watched 경계 / tier_Neutral 중립 / tier_Respected 존중 / tier_Recognized 공인
tier_Dissolved   교단 해체 결말 (없으면 ending_ruin → tier_Persecuted)
god_main         준비된 신 모드 기본
god_heaven       신계 압박이 더 셀 때 (3단계 이상)
god_earth        지상 압박이 더 셀 때 (3단계 이상)
god_clash        두 압박이 같은 단계일 때 (3단계 이상)
god_ascend       승천 결말
ending_glory     좋은 결말 / ending_ruin 나쁜 결말 / ending_death 죽음 결말

옵션·저장·업적·일시정지 메뉴가 열려 있으면 음악이 자동으로 작아짐. 곡이 바뀔 때 3초 크로스페이드.
