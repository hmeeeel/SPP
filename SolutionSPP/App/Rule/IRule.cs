using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Rule
{
    public interface IRule<TContext>
    {
        string Name { get; } // уник имя
        int Version { get; } // версионность правила
        int Priority { get; } // приоритет
        DateTime DataStart { get; } // дата начала 
        DateTime? DataEnd { get; } // оконч. null = бессрочно
        IReadOnlyList<string> DependsOn { get; } // пр до этого пр
        bool Condition(TContext context); // условия изм контекста
        void Action(TContext context); // измение в зав-ти от усл
    }

}
