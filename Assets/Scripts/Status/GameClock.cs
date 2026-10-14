using System;

// 턴 단위 시간. 1턴 = 1분기, 4분기 = 1년. 0년 0분기부터 시작한다.
public class GameClock
{
    public const int QuartersPerYear = 4;

    public int Turns { get; private set; }
    public int Years => Turns / QuartersPerYear;
    public int Quarter => Turns % QuartersPerYear;
    public event Action Changed;

    public void Advance()
    {
        Turns++;
        Changed?.Invoke();
    }
}
