using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameworkTesting.Testing
{
    public enum TestStatus
    {
        Passed,
        Failed,
        Skipped,
        Error
    }

    public class TestMethodResult
    {
        public string ClassName { get; set; } = "";
        public string MethodName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public TestStatus Status { get; set; }
        public TimeSpan Duration { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SkipReason { get; set; }
        public Exception? Exception { get; set; }

        public override string ToString()
        {
            var icon = Status switch
            {
                TestStatus.Passed => "ПРОШЕЛ",
                TestStatus.Failed => "УПАЛ",
                TestStatus.Skipped => "ПРОПУЩЕН",
                TestStatus.Error => "ОШИБКА",
                _ => "?"
            };

            var suffix = Status switch
            {
                TestStatus.Failed => $"\n     FAILED: {ErrorMessage}",
                TestStatus.Skipped => $"\n     SKIPPED: {SkipReason ?? "Skipped"}",
                _ => ""
            };
            return $"{icon} {DisplayName}{suffix}";
        }
    }
}

