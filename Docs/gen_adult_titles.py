# 성인 이벤트 갤러리 제목: 장면 단위(성별 변형은 같은 제목)로 쓰고 adult_titles.csv 로 내보낸다.
import csv
T = {
"ad01":("자정의 사무실","Midnight Office"),"ad02":("가면무도회의 속삭임","Whispers at the Masquerade"),"ad03":("협상 테이블 아래","Beneath the Negotiating Table"),
"ad04":("합일의 의식","The Rite of Union"),"ad05":("꺼진 녹음기","The Silent Recorder"),"ad06":("탄트라의 초대","The Tantric Invitation"),
"ad07":("호텔 바의 재회","Reunion at the Hotel Bar"),"ad08":("온천의 화가","The Painter in the Hot Spring"),
"ac01":("상담실의 시선","A Gaze in Counseling"),"ac02":("이혼한 신도의 매달림","The Follower's Plea"),"ac03":("빚진 부부의 제안","The Debtors' Offer"),"ac04":("합일 교리의 제안","The Doctrine of Union"),
"xn01":("방송 후 잠긴 문","The Locked Door After the Show"),"xn02":("경매장의 경쟁자","Rival at the Auction"),"xn03":("새벽 요가 강사","The Dawn Yoga Teacher"),
"xn04":("비 오는 밤의 헌책방","Rainy Night at the Bookshop"),"xn05":("시의회 만찬 뒤","After the Council Dinner"),"xn06":("보름달의 무희","Dancer of the Full Moon"),
"xn07":("장례식에서 만난 첫사랑","A Secret Love at the Funeral"),"xn08":("요트 위의 부부","The Couple on the Yacht"),"xn09":("대학 축제의 졸업생","The Graduate at the Campus Fest"),
"xn10":("사진가의 모델","The Photographer's Model"),"xn11":("토론 직후의 대기실","Backstage After the Debate"),"xn12":("산사의 마주침","Crossing Paths at the Monastery"),
"xn13":("카지노 VIP 라운지","The Casino VIP Lounge"),"xn14":("향수 장인의 선물","The Perfumer's Gift"),"xn15":("눈에 갇힌 산장","Snowed In at the Lodge"),
"xn16":("결승선의 포옹","Embrace at the Finish Line"),"xn17":("출판 파티의 편집자","The Editor at the Party"),"xn18":("해변의 명상 부부","The Beach Meditation Couple"),
"xn19":("경찰서장 취임 연회","The Police Chief's Reception"),"xn20":("와이너리의 쌍둥이","The Winery Twins"),"xn21":("학회 뒤풀이","After the Conference"),
"xn22":("순례길의 끝","End of the Pilgrim's Road"),"xn23":("재즈 클럽의 장미","The Jazz Club Rose"),"xn24":("자선 경매의 하룻밤","The Charity Auction Date"),
"xn25":("노천 온천의 여행자","Traveler in the Open-Air Spring"),"xn26":("비서의 은밀한 제안","The Secretary's Private Offer"),"xn27":("스파의 따뜻한 오일","Warm Oil at the Spa"),
"xn28":("등 위의 붓글씨","Calligraphy on the Back"),"xn29":("이십 년 지기의 고백","A Twenty-Year Friend's Confession"),"xn30":("한밤의 정원 클럽","The Midnight Garden Club"),
"xg01":("천상 연회의 꿀술","Mead at the Heavenly Banquet"),"xg02":("폭풍 속의 선장","The Captain in the Storm"),"xg03":("밤의 정령이 든 꿈","A Night Spirit's Dream"),
"xg04":("전쟁의 신과의 내기","A Wager With the God of War"),"xg05":("숲의 사티로스 잔치","The Satyrs' Feast"),"xg06":("여제의 궁전","The Empress's Palace"),
"xg07":("황금신의 유혹","The Gold God's Temptation"),"xg08":("망각의 강 뱃사공","Ferryman of Oblivion"),"xg09":("조각가의 모델","The Sculptor's Model"),
"xg10":("마음을 잃은 천사","The Angel Who Lost Heart"),"xg11":("풍요 축제의 밤","Night of the Fertility Festival"),"xg12":("지혜의 수수께끼","The Riddle of Wisdom"),
"xg13":("사막 오아시스의 성직자","Priest of the Desert Oasis"),"xg14":("별의 관측소","The Starlit Observatory"),"xg15":("마법사 길드장의 청","The Guildmaster's Request"),
"xg16":("오랜 구애의 몽마","The Long-Courting Succubus"),"xg17":("막힌 노래의 음유시인","The Bard's Unfinished Song"),"xg18":("거울신의 얼굴","Face of the Mirror God"),
"xg19":("재산 절반의 대가","Half a Fortune"),"xg20":("계절 사이의 두 신","Two Gods Between Seasons"),"xg21":("금서를 지키는 수도자","Keeper of the Forbidden Text"),
"xg22":("이웃 신의 동맹 제안","The Neighbor God's Alliance"),"xg23":("원래 주인의 꿈","The Vessel's Old Dream"),"xg24":("폭풍 속 세 기사","Three Knights in the Storm"),
"xg25":("세상 최고의 무희","The Greatest Dancer"),"xg26":("인어 왕족의 해변","The Merfolk Royal on the Beach"),"xg27":("화산의 불꽃 왕","The Fire King's Volcano"),
"xg28":("쾌락을 연구하는 학자","Scholar of Pleasure"),"xg29":("지하 세계 군주의 나들이","The Underworld Sovereign's Outing"),"xg30":("신이 된 지 일 년","One Year a God"),
}
rows = list(csv.DictReader(open("adult_events.csv", encoding="utf-8-sig")))
with open("adult_titles.csv", "w", encoding="utf-8-sig", newline="") as f:
    w = csv.writer(f); w.writerow(["id", "title", "title_en"])
    for r in rows:
        ko, en = T[r["id"].split("_")[0]]
        w.writerow([r["id"], ko, en])
print(len(rows), len(T))
