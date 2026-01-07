namespace FlashMemo.Model
{
    public interface IDefaultable
    {
        void ToDefault();
    }
    public class UserOptions: IDefaultable
    {
        public int UserId { get; }
        public void ToDefault()
        {
            throw new NotImplementedException();
        }
    }
}