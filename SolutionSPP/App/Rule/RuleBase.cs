using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Rule
{
    public abstract class RuleBase<TContext> : IRule<TContext>
    {
        public string Name { get; }
        public int Version { get; }
        public int Priority { get; }
        public DateTime DataStart { get; }
        public DateTime? DataEnd { get; }
        public IReadOnlyList<string> DependsOn { get; }

        protected RuleBase(string name, int version, int priority, DateTime dataStart, 
            DateTime? dataEnd = null, IEnumerable<string>? dependsOn = null)
        {
            Name = name;
            Version = version;
            Priority = priority;
            DataStart = dataStart;
            DataEnd = dataEnd;
            var list = new List<string>();
            if (dependsOn != null) list.AddRange(dependsOn);
            DependsOn = list;
        }

        public abstract bool Condition(TContext context);
        public abstract void Action(TContext context);
    }
}
