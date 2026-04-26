using FrameworkTesting.Filtering;

public class TestFilterBuilder
    {
        private readonly List<TestFilterDelegate> _filters = new();
        private bool _useAndMode = true;

        public TestFilterBuilder WithCategory(string category)
        {
            _filters.Add(TestFilters.ByCategory(category));
            return this;
        }

        public TestFilterBuilder WithCategories(params string[] categories)
        {
            _filters.Add(TestFilters.ByCategories(categories));
            return this;
        }

        public TestFilterBuilder WithClassName(string pattern)
        {
            _filters.Add(TestFilters.ByClassName(pattern));
            return this;
        }

        public TestFilterBuilder WithMethodName(string pattern)
        {
            _filters.Add(TestFilters.ByMethodName(pattern));
            return this;
        }

        public TestFilterBuilder ExcludeIgnored()
        {
            _filters.Add(TestFilters.NotIgnored());
            return this;
        }

        public TestFilterBuilder OnlyParameterized()
        {
            _filters.Add(TestFilters.Parameterized());
            return this;
        }

        public TestFilterBuilder OnlyWithTimeout()
        {
            _filters.Add(TestFilters.WithTimeout());
            return this;
        }

        public TestFilterBuilder Custom(Func<TestFilterContext, bool> predicate)
        {
            _filters.Add(TestFilters.Custom(predicate));
            return this;
        }

        public TestFilterBuilder UseAndMode()
        {
            _useAndMode = true;
            return this;
        }

        public TestFilterBuilder UseOrMode()
        {
            _useAndMode = false;
            return this;
        }

        public TestFilterDelegate Build()
        {
            if (_filters.Count == 0)
                return _ => true;

            if (_filters.Count == 1)
                return _filters[0];

            return _useAndMode
                ? TestFilters.AndAll(_filters.ToArray())
                : TestFilters.OrAny(_filters.ToArray());
        }
    }