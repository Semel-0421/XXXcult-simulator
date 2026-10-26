using System.Text.RegularExpressions;

// 한국어 조사 자리표시("을(를)" 등)를 바로 앞 글자의 받침에 맞게 고른다
public static class Josa
{
    public static string Fix(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return Regex.Replace(text, @"([가-힣])(을\(를\)|이\(가\)|은\(는\)|와\(과\))", (m) =>
        {
            char prev = m.Groups[1].Value[0];
            bool hasBatchim = (prev - '가') % 28 != 0;
            string particle = m.Groups[2].Value[0] switch
            {
                '을' => hasBatchim ? "을" : "를",
                '이' => hasBatchim ? "이" : "가",
                '은' => hasBatchim ? "은" : "는",
                _ => hasBatchim ? "과" : "와",
            };
            return prev + particle;
        });
    }
}
