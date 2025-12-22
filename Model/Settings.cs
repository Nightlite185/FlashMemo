namespace FlashMemo.Model
{
    public interface IDefaultable
    {
        void ToDefault();
    }
    public class Settings(int userId): IDefaultable
    {
        public int UserId { get; } = userId;
        public void ToDefault()
        {
            throw new NotImplementedException();
        }
    }
}