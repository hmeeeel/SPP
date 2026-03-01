using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Engine
{
    public enum SkipReason
    {
        None,
        NotCondition,
        NotDependency,
        NoVersion,
        NotContext
    }

    public class RuleModelResult
    {
        public string RuleName { get; set; } = "";
        public int RuleVersion { get; set; }
        public bool isGood { get; set; }
        public SkipReason SkipReason { get; set; }
        public string LogMessage { get; set; } = "";

        public override string ToString()
        {
            if (isGood)
                return $"[СРАБОТАЛО v{RuleVersion}] {RuleName}: {LogMessage}";

            string reason = SkipReason switch
            {
                SkipReason.NotCondition => "условие не выполнено",
                SkipReason.NotDependency => "зависимость не сработала",
                SkipReason.NoVersion => "нет  версии",
                SkipReason.NotContext => "контекст неподходящий",
                _ => "?"
            };
            return $"[ПРОПУЩЕНО] {RuleName}: {reason}";
        }
    }
}
