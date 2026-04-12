namespace FlashMemo.Services;

public class Clock: IClock
{
    public DateTime Now => DateTime.Now;
    public DateTime Today => DateTime.Today;
}