using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameworkTesting.Testing
{
    public class TestClassResult
    {
        public string ClassName { get; set; } = "";
        public List<TestMethodResult> Results { get; } = [];
        
        public TimeSpan Duration { get; set; }

        public int Total => Results.Count;
        public int Passed => Results.Count(r => r.Status == TestStatus.Passed);
        public int Failed => Results.Count(r => r.Status == TestStatus.Failed);
        public int Skipped => Results.Count(r => r.Status == TestStatus.Skipped);
        public int Errors => Results.Count(r => r.Status == TestStatus.Error);

       public override string ToString() =>
            $"Класс: {ClassName} | {TestMethodResult.FormatMs(Duration)} | " +
            $"Итого: {Total}, ПРОШЛО {Passed}, УПАЛО {Failed}, ПРОПУЩЕНО {Skipped}, ОШИБКА {Errors}";
    }

}
