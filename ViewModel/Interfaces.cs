namespace FlashMemo.ViewModel;

public interface ICloseRequest
{
    public event Action? OnCloseRequest;
}
public interface IOnLoadedHandler
{
    public void LoadEventHandler();
}

public interface IViewModel;