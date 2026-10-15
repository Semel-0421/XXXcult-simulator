using System;
using System.Collections.Generic;
using UnityEngine;

public static class Lang
{
    public static event Action Changed;
    public static bool IsKorean { get; private set; } = PlayerPrefs.GetString("lang", "ko") == "ko";

    static readonly Dictionary<string, (string en, string ko)> Table = new()
    {
        ["new_game"] = ("New Game", "새 게임"), ["continue"] = ("Continue", "이어하기"), ["achievements"] = ("Achievements", "업적"),
        ["settings"] = ("Settings", "설정"), ["quit"] = ("Quit", "종료"), ["language"] = ("Language", "언어"), ["back"] = ("Back", "뒤로"),
        ["stat_LeaderFaith"] = ("Faith", "믿음"), ["stat_FollowerFaith"] = ("Followers' Faith", "신도의 믿음"), ["stat_Authority"] = ("Authority", "권위"), ["stat_Offerings"] = ("Offerings", "봉헌금"),
        ["stat_Followers"] = ("Followers", "신도"), ["stat_Doctrine"] = ("Doctrine", "교리"), ["stat_Communion"] = ("Communion", "접신"), ["stat_Reputation"] = ("Reputation", "명망"),
        ["resume"] = ("Resume", "게임으로 돌아가기"), ["exit"] = ("Exit", "나가기"),
        ["pstat_Health"] = ("Health", "체력"), ["pstat_Mentality"] = ("Mentality", "정신력"), ["pstat_Piety"] = ("Piety", "신앙심"), ["pstat_Repute"] = ("Repute", "평판"), ["pstat_Wealth"] = ("Wealth", "재산"),
        ["pstat_Lust"] = ("Lust", "음욕"), ["pstat_Greed"] = ("Greed", "재물욕"), ["pstat_Appetite"] = ("Appetite", "식욕"), ["pstat_Ambition"] = ("Ambition", "권력욕"),
        ["pstat_Temperance"] = ("Temperance", "절제"), ["pstat_Charity"] = ("Charity", "자애"), ["pstat_Patience"] = ("Patience", "인내"), ["pstat_Integrity"] = ("Integrity", "청렴"),
        ["desire"] = ("Desire", "욕망"), ["virtue"] = ("Virtue", "덕목"), ["name_hint"] = ("Name", "이름"),
        ["confirm"] = ("Confirm", "확인"), ["choice_hint"] = ("Number", "번호 입력"), ["paused"] = ("Paused", "일시정지"),
        ["talents"] = ("Gifted", "기프티드"), ["talent_title"] = ("Owned Gifted", "보유 기프티드"), ["talent_empty"] = ("No Gifted yet.", "보유한 기프티드가 없습니다."), ["close"] = ("Close", "닫기"),
        ["doctrine_level"] = ("Lv.{0}", "{0}레벨"), ["doctrine_max"] = ("MAX", "MAX레벨"),
        ["gender_prompt"] = ("You look into the mirror and see yourself being washed by the water. What is your gender?\n1. Male    2. Female", "거울을 바라보자 물로 몸이 씻겨지는 당신의 모습이 보입니다. 당신의 성별은?\n1. 남성    2. 여성"),
        ["cult_time"] = ("Cult Year {0}, Q{1}", "교단 {0}년 {1}분기"), ["age"] = ("Age {0}", "{0}세"),
        ["cult_name_prompt"] = ("You resolve to found your own cult. What will it be called?", "당신은 스스로 교단을 세우기로 결심합니다. 교단의 이름은?"), ["cult_name_hint"] = ("Cult name", "교단 이름"), ["cult_founded"] = ("The cult '{0}' has been founded.", "'{0}' 교단이 창설되었습니다."),
        ["doctrine_select"] = ("Doctrines", "교리 선택"), ["doctrine_title"] = ("Doctrines", "교리"), ["doctrine_declaration"] = ("Declaration of Doctrine", "교리 선포"),
        ["doctrine_learn"] = ("Click to learn", "클릭하여 습득"), ["doctrine_tutorial"] = ("Your cult needs a doctrine. Open Doctrines.", "교단에는 교리가 필요합니다. 교리 선택을 여십시오."),
        ["action_Proselytize"] = ("Proselytize", "포교"), ["action_Preach"] = ("Preach", "설교"), ["action_Serve"] = ("Serve", "봉사"), ["action_Desire"] = ("Desire", "욕구"), ["action_Pray"] = ("Pray", "기도"),
        ["life_Exercise"] = ("Exercise", "운동"), ["life_Meditate"] = ("Meditate", "명상"), ["life_Study"] = ("Study", "공부"), ["life_Socialize"] = ("Socialize", "사교활동"), ["life_Pray"] = ("Pray", "기도"),
        ["life_action_prompt"] = ("Age {0}. How will you spend this season?", "{0}세. 이번 분기는 무엇을 하며 보내시겠습니까?"), ["action_success"] = ("Success!", "성공!"), ["action_fail"] = ("Failed...", "실패..."),
        ["life_ok_Exercise"] = ("Your training paid off. Your body grew stronger.", "운동에 힘써 몸이 단단해졌습니다."), ["life_fail_Exercise"] = ("You pushed too hard and got hurt.", "무리하다 그만 다치고 말았습니다."),
        ["life_ok_Meditate"] = ("Meditation brought you calm.", "명상으로 마음이 고요해졌습니다."), ["life_fail_Meditate"] = ("Stray thoughts kept you from focusing.", "잡념에 시달려 집중하지 못했습니다."),
        ["life_ok_Study"] = ("Your studies broadened your mind.", "공부로 견문이 넓어졌습니다."), ["life_fail_Study"] = ("You studied hard but only got a headache.", "책을 붙잡았지만 머리만 아팠습니다."),
        ["life_ok_Socialize"] = ("You made good friends.", "좋은 사람들과 어울렸습니다."), ["life_fail_Socialize"] = ("A careless word cost you a friend.", "말실수로 사람들을 잃었습니다."),
        ["life_ok_Pray"] = ("Your prayers felt answered.", "기도가 닿은 듯한 평온을 느꼈습니다."), ["life_fail_Pray"] = ("Your prayers met only silence.", "기도에 아무 응답도 없었습니다."),
        ["cult_fail_Proselytize"] = ("People scorned you in the streets.", "거리에서 사람들에게 봉변을 당했습니다."), ["cult_fail_Preach"] = ("Your sermon missed the mark.", "설교가 신도들의 마음에 닿지 못했습니다."),
        ["cult_fail_Serve"] = ("Your service only caused trouble.", "봉사가 오히려 일만 키웠습니다."), ["cult_fail_Desire"] = ("Your greed was exposed.", "당신의 탐욕이 사람들에게 드러났습니다."),
        ["cult_fail_Pray"] = ("Your god remained silent.", "신은 끝내 침묵했습니다."),
        ["cult_action_prompt"] = ("Cult year {0}, Q{1}. What will the cult do this season?", "교단 {0}년 {1}분기. 이번 분기에 교단은 무엇을 하시겠습니까?"),
        ["cult_done_Proselytize"] = ("You spread the teachings in the streets.", "거리로 나가 교단의 가르침을 전했습니다."), ["cult_done_Preach"] = ("You expounded the doctrine before the faithful.", "신도들 앞에서 교리를 설파했습니다."),
        ["cult_done_Serve"] = ("You helped your neighbors in the cult's name.", "교단의 이름으로 이웃을 도왔습니다."), ["cult_done_Desire"] = ("You fed your desires with the cult's power.", "교단의 힘으로 당신의 욕망을 채웠습니다."),
        ["cult_done_Pray"] = ("You prayed alone to commune with your god.", "홀로 신과의 교감을 위해 기도했습니다."),
        ["standing_label"] = ("Public view:", "사회적 시선:"), ["standing_Dissolved"] = ("Dissolved", "해체"), ["standing_Persecuted"] = ("Persecuted", "탄압"),
        ["standing_Watched"] = ("Watched", "경계"), ["standing_Neutral"] = ("Neutral", "중립"), ["standing_Respected"] = ("Respected", "존중"), ["standing_Recognized"] = ("Recognized", "공인"),
        ["ending_dissolved"] = ("By order of the state, '{0}' has been dissolved after {1} years. Your followers scatter, and your name fades from the world.", "국가의 명령으로 '{0}' 교단이 창설 {1}년 만에 해체되었습니다. 신도들은 흩어지고 당신의 이름은 세상에서 잊혀 갑니다."),
        ["talent_effects"] = ("Effects", "효과"),
        ["gcat_업적"] = ("Achievement", "업적"),
        ["ach_title"] = ("Achievements", "업적"), ["ach_progress"] = ("{0} / {1} unlocked", "{0} / {1} 달성"),
        ["ach_reward_gifted"] = ("Reward: Gifted '{0}'", "보상: 기프티드 '{0}'"), ["ach_reward_stat"] = ("Reward: starting {0} +{1}", "보상: 시작 {0} +{1}"),
        ["ach_rewards_on"] = ("Rewards: ON", "보상: 켜짐"), ["ach_rewards_off"] = ("Rewards: OFF", "보상: 꺼짐"),
        ["ach_rewards_off_hint"] = ("Rewards are off. Achievements still count, but gifted and starting bonuses are not applied.", "보상이 꺼져 있습니다. 업적은 달성되지만 기프티드와 시작 보너스는 적용되지 않습니다."),
        ["opt_title"] = ("Options", "옵션"), ["opt_music"] = ("Music", "배경음악"), ["opt_sfx"] = ("Sound effects", "효과음"),
        ["opt_language"] = ("Language", "언어"), ["lang_ko"] = ("한국어", "한국어"), ["lang_en"] = ("English", "English"),
        ["misfortune_label"] = ("Misfortune x{0}", "불운 x{0}"),
        ["misfortune_struck"] = ("Misfortune deepens (x{0}). Every season, the cult will wither a little more.", "불운이 한 겹 더 드리웠습니다 (x{0}). 이제 매 분기 교단이 조금씩 쇠락합니다."),
        ["legacy_info"] = ("Legacy Lv.{0} · Misfortune resistance {1}%", "계승 {0}단계 · 불운 저항 {1}%"),
        ["ending_legacy"] = ("Your failure is not in vain. The next leader inherits your resolve.", "당신의 실패는 헛되지 않습니다. 다음 교주는 당신의 결의를 이어받습니다."),
        ["doctrine_points"] = ("Doctrine points: {0}", "교리 포인트: {0}"),
        ["doctrine_points_detail"] = ("Years {0} + achievements {1} + doctrine Lv.{2}x2 - spent {3}", "운영 {0}년 + 업적 {1} + 교리 레벨 {2}x2 - 사용 {3}"),
        ["doctrine_select_hint"] = ("Select a skill to see its effects.", "스킬을 눌러 효과를 확인하세요."),
        ["doctrine_tier"] = ("{0} tier {1}", "{0} {1}단계"), ["doctrine_level_of"] = ("Lv.{0}/{1}", "레벨 {0}/{1}"),
        ["doctrine_cost"] = ("Cost {0}", "비용 {0}"), ["doctrine_effects_per_level"] = ("Effects (per level)", "효과 (레벨당)"),
        ["doctrine_resist"] = ("Misfortune resistance +{0}%", "불운 저항 +{0}%"),
        ["doctrine_learn_btn"] = ("Learn", "배우기"), ["doctrine_maxed"] = ("Max level", "최대 레벨"),
        ["doctrine_need_prev"] = ("Learn the previous tier first", "앞 단계를 먼저 배우세요"),
        ["doctrine_need_declaration"] = ("Learn the Declaration first", "교리 선포를 먼저 익히세요"),
        ["doctrine_need_points"] = ("Not enough points", "포인트 부족"),
        ["doctrine_path"] = ("Path {0} (choosing one path locks the other)", "{0} 갈래 (한 갈래를 고르면 다른 갈래는 막힘)"),
        ["doctrine_path_blocked"] = ("Other path chosen", "다른 갈래를 선택함"),
        ["doctrine_opposite_blocked"] = ("Locked: the opposite doctrine reached tier 6", "정반대 교리 6단계를 찍어 잠김"),
        ["doctrine_final_hint"] = ("Final skill: its emblem replaces the Declaration at the center. Final upgrade there (cost {0}).", "최종 스킬: 배우면 이 문양이 중앙 교리 선포를 대체합니다. 중앙에서 최종 업그레이드할 수 있습니다 (비용 {0})."),
        ["doctrine_final_center"] = ("Upgrade at center", "중앙에서 강화"),
        ["doctrine_final_upgrade"] = ("Ascend", "최종 강화"), ["doctrine_final_done"] = ("Ascended", "최종 완성"),
        ["ach_toast"] = ("Achievement unlocked", "업적 달성"), ["ach_done"] = ("Done", "달성"),
        ["gcat_운명"] = ("Fate", "운명"), ["gcat_성격"] = ("Personality", "성격"), ["gcat_특기"] = ("Skill", "특기"), ["gcat_외모"] = ("Appearance", "외모"),
        ["special_chance"] = ("{0} success rate {1}%p", "{0} 성공률 {1}%p"), ["special_failhalf"] = ("{0}: halves failure penalties", "{0} 실패 시 불이익 절반"),
        ["special_year"] = ("Every year: {0} {1}", "해마다 {0} {1}"), ["action_all"] = ("All actions", "모든 행동"),
        ["save_title"] = ("Save / Load", "저장 / 불러오기"), ["save_hint"] = ("Choose a slot. Overwriting or deleting asks for a second press.", "칸을 고르세요. 덮어쓰기와 삭제는 한 번 더 눌러야 합니다."),
        ["save_hint_locked"] = ("Saving is only possible when choosing an action. Loading is always available.", "저장은 행동을 고르는 시점에만 할 수 있습니다. 불러오기는 언제든 가능합니다."),
        ["save_btn"] = ("Save", "저장"), ["load_btn"] = ("Load", "불러오기"), ["delete_btn"] = ("Delete", "삭제"), ["save_confirm"] = ("Sure?", "확실?"),
        ["save_empty"] = ("- Empty -", "- 비어 있음 -"), ["save_broken"] = ("- Unreadable file -", "- 읽을 수 없는 파일 -"), ["saves"] = ("Save / Load", "저장 / 불러오기"),
        ["intro_prompt"] = ("Your first memory was your parents calling your name. What is your name?", "당신의 첫 기억은 부모가 부르는 당신의 이름이었다. 당신의 이름은?"),
    };

    public static string Get(string key) => key != null && Table.TryGetValue(key, out var text) ? (IsKorean ? text.ko : text.en) : key;

    public static void Set(bool korean)
    {
        IsKorean = korean;
        PlayerPrefs.SetString("lang", korean ? "ko" : "en");
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
