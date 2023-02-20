namespace MultiFactor.SelfService.Windows.Portal.Services.Caching
{
    public class CachedItem<T>
    {
        public T Value { get; set; }

        private bool _isEmpty;
        public bool IsEmpty => _isEmpty;

        public static CachedItem<T> Empty => new CachedItem<T>();

        private CachedItem()
        {
            _isEmpty = true;
        }

        public CachedItem(T value)
        {
            Value = value;
            _isEmpty = false;
        }
    }
}