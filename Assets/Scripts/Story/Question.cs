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

    public Choice(string text, params StatEffect[] effects) { Text = text; Effects = effects; }
}

public class Question
{
    public string Text { get; }
    public Choice[] Choices { get; }

    public Question(string text, params Choice[] choices) { Text = text; Choices = choices; }
}
