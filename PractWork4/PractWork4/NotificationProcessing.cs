namespace NotificationLibrary
{
    public interface ISpecification<T>
    {
        bool IsSatisfiedBy(T item);
    }

    public interface INotificationFilter
    {
        IEnumerable<Notification> Filter(
            IEnumerable<Notification> notifications,
            NotificationFilterOptions options);
    }

    public interface INotificationSorter
    {
        IEnumerable<Notification> Sort(
            IEnumerable<Notification> notifications,
            NotificationFilterOptions options);
    }

    public interface INotificationProcessor
    {
        IEnumerable<Notification> FilterAndSort(
            IEnumerable<Notification> notifications,
            NotificationFilterOptions options);
    }

    public class ReadStatusSpecification : ISpecification<Notification>
    {
        private readonly bool _isRead;
        public ReadStatusSpecification(bool isRead) => _isRead = isRead;
        public bool IsSatisfiedBy(Notification n) => n.IsRead == _isRead;
    }

    public class TypeSpecification : ISpecification<Notification>
    {
        private readonly NotificationType[] _types;
        public TypeSpecification(NotificationType[] types) => _types = types;
        public bool IsSatisfiedBy(Notification n) => _types.Contains(n.Type);
    }

    public class SearchTextSpecification : ISpecification<Notification>
    {
        private readonly string _search;
        public SearchTextSpecification(string search) => _search = search.Trim();
        public bool IsSatisfiedBy(Notification n) =>
            (n.Title?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (n.Content?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    public class MinPrioritySpecification : ISpecification<Notification>
    {
        private readonly int _min;
        public MinPrioritySpecification(int min) => _min = min;
        public bool IsSatisfiedBy(Notification n) => n.Priority >= _min;
    }

    public class NotificationFilter : INotificationFilter
    {
        public IEnumerable<Notification> Filter(
            IEnumerable<Notification> notifications,
            NotificationFilterOptions options)
        {
            if (notifications == null) throw new ArgumentNullException(nameof(notifications));
            options ??= new NotificationFilterOptions();

            var result = notifications;
            foreach (var spec in BuildSpecifications(options))
                result = result.Where(spec.IsSatisfiedBy);
            return result;
        }

        private static IEnumerable<ISpecification<Notification>> BuildSpecifications(
            NotificationFilterOptions options)
        {
            if (options.IsRead.HasValue)
                yield return new ReadStatusSpecification(options.IsRead.Value);
            if (options.Types != null && options.Types.Length > 0)
                yield return new TypeSpecification(options.Types);
            if (!string.IsNullOrWhiteSpace(options.SearchText))
                yield return new SearchTextSpecification(options.SearchText);
            if (options.MinPriority.HasValue)
                yield return new MinPrioritySpecification(options.MinPriority.Value);
        }
    }

    public class NotificationSorter : INotificationSorter
    {
        private static readonly Dictionary<
            SortNotificationBy,
            Func<IEnumerable<Notification>, bool, IEnumerable<Notification>>> Strategies = new()
            {
                [SortNotificationBy.Date] = (q, desc) => desc
                    ? q.OrderByDescending(n => n.CreatedAt)
                    : q.OrderBy(n => n.CreatedAt),
                [SortNotificationBy.Priority] = (q, desc) => desc
                    ? q.OrderByDescending(n => n.Priority)
                    : q.OrderBy(n => n.Priority),
                [SortNotificationBy.Title] = (q, desc) => desc
                    ? q.OrderByDescending(n => n.Title)
                    : q.OrderBy(n => n.Title)
            };

        public IEnumerable<Notification> Sort(
            IEnumerable<Notification> notifications,
            NotificationFilterOptions options)
        {
            if (notifications == null) throw new ArgumentNullException(nameof(notifications));
            options ??= new NotificationFilterOptions();
            if (!options.SortBy.HasValue) return notifications;
            if (!Strategies.TryGetValue(options.SortBy.Value, out var strategy)) return notifications;
            return strategy(notifications, options.Descending);
        }
    }

    public class NotificationProcessor : INotificationProcessor
    {
        private readonly INotificationFilter _filter;
        private readonly INotificationSorter _sorter;

        public NotificationProcessor(INotificationFilter filter, INotificationSorter sorter)
        {
            _filter = filter ?? throw new ArgumentNullException(nameof(filter));
            _sorter = sorter ?? throw new ArgumentNullException(nameof(sorter));
        }

        public IEnumerable<Notification> FilterAndSort(
            IEnumerable<Notification> notifications,
            NotificationFilterOptions options)
        {
            if (notifications == null) throw new ArgumentNullException(nameof(notifications));
            var filtered = _filter.Filter(notifications, options);
            return _sorter.Sort(filtered, options);
        }
    }
}