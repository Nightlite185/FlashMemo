namespace FlashMemo.Model
{
    public interface IDefaultable
    {
        void ToDefault();
    }
    public class UserOptions: IDefaultable
    {
        public void ToDefault()
        {
            throw new NotImplementedException();
        }
    }
}