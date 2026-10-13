public static class StatLabel
{
    public static string Of(StatId id) => Lang.Get($"stat_{id}");
}
