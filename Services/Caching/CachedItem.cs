namespace MultiFactor.SelfService.Windows.Portal.Services.Caching
{
    public class CachedItem<T>
    {
        public T Value { get; set; }

        public bool IsEmpty { get; }

        public static CachedItem<T> Empty => new CachedItem<T>();

        private CachedItem()
        {
            IsEmpty = true;
        }

        public CachedItem(T value)
        {
            Value = value;
            IsEmpty = false;
        }
    }
}