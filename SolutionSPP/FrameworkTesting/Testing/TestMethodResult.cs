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

            // Для Skipped Duration = Zero — время не выводим
            var time = Status != TestStatus.Skipped
                ? $" ({FormatMs(Duration)})"
                : string.Empty;

            var suffix = Status switch
            {
                TestStatus.Failed => $"\n     FAILED: {ErrorMessage}",
                TestStatus.Skipped => $"\n     SKIPPED: {SkipReason ?? "Skipped"}",
                _ => ""
            };

            return $"{icon}{time} {DisplayName}{suffix}";
        }

        internal static string FormatMs(TimeSpan d) =>
            d.TotalMilliseconds < 1000
                ? $"{(int)d.TotalMilliseconds}мс"
                : $"{d.TotalSeconds:F2}с";
    }
}