public class StatEffect
{
    public PersonalStatId Stat { get; }
    public int Amount { get; }

    public StatEffect(PersonalStatId stat, int amount) { Stat = stat; Amount = amount; }
}

public class Choice
{
    public string Text { get; }
    public StatEffect[] Effects { get; }

    public string[] GrantedGifted { get; private set; } = System.Array.Empty<string>();

    public Choice(string text, params StatEffect[] effects) { Text = text; Effects = effects; }

    // 이 선택지를 고르면 지급할 기프티드 id (예: "no.1")
    public Choice Grants(params string[] giftedIds) { GrantedGifted = giftedIds; return this; }
}

public class Question
{
    public string Text { get; }
    public Choice[] Choices { get; }

    public Question(string text, params Choice[] choices) { Text = text; Choices = choices; }
}
