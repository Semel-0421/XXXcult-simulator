// 초기 상태를 정하는 질문 모음. 선택지 옆 효과 수치는 화면에 나오지 않고 스탯에만 반영된다.
public static class IntroQuestions
{
    static StatEffect Wealth(int amount) => new(PersonalStatId.Wealth, amount);

    public static readonly Question First = new(
        "당신은 눈을 뜨고 주변을 둘러봅니다. 작은 아기 침대에 누운 채 눈에 들어오는 것은...",
        new Choice("헤진 벽지와 곰팡이가 슨 벽", Wealth(10)),
        new Choice("작은 유아용 모빌 장난감", Wealth(40)),
        new Choice("자신의 옷을 갈아입히는 유모", Wealth(90)),
        new Choice("바람이 들이차는 천막", Wealth(0)));
}
