namespace FlashMemo.Model
{
    public interface IDefaultable
    {
        void ToDefault();
    }
    public class Settings: IDefaultable
    {
        public int UserId { get; }
        public void ToDefault()
        {
            throw new NotImplementedException();
        }
    }
}