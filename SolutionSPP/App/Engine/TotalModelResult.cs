using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Engine
{
    public class TotalModelResult<TContext>
    {
        public TContext FinalContext { get; set; } = default!;
        public List<RuleModelResult> RuleResults { get; } = [];
        public DateTime ExecutedAt { get; set; }

        public IEnumerable<RuleModelResult> FiredRules =>
            RuleResults.Where(r => r.isGood);

        public IEnumerable<RuleModelResult> SkippedRules =>
            RuleResults.Where(r => !r.isGood);

        public void PrintLog()
        {
            var w = Console.Out;
            foreach (var r in RuleResults)
                w.WriteLine($"  {r}");
            w.WriteLine($"Сработало: {FiredRules.Count()}, пропущено: {SkippedRules.Count()} ---\n");
        }
    }
}
