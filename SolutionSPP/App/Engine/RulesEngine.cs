using App.Rule;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Engine
{
    public class RulesEngine<TContext>
    {
        private List<IRule<TContext>> rules = [];

        public void AddRule(IRule<TContext> rule)
        {
            rules.Add(rule);
            CheckForCycles();
        }
        public void AddRules(IEnumerable<IRule<TContext>> ruleS)
        {
            foreach (var rule in ruleS)
                rules.Add(rule);

            CheckForCycles();
        }

        private void CheckForCycles()
        {
            var allNames = rules.Select(r => r.Name).Distinct().ToList();
            var deps = rules
                .GroupBy(r => r.Name)
                .ToDictionary(g => g.Key, g => g.SelectMany(r => r.DependsOn).Distinct().ToList());

            var state = allNames.ToDictionary(n => n, _ => 0); 

            foreach (var name in allNames)
                if (state[name] == 0)
                    DfsVisit(name, deps, state, []);
        }

        // 0 - не был. 1 - был. 2 - good
        private static void DfsVisit(string node, Dictionary<string, List<string>> deps, Dictionary<string, int> state, List<string> path)
        {
            state[node] = 1; 
            path.Add(node);

            if (deps.TryGetValue(node, out var neighbors))
            {
                foreach (var neighbor in neighbors)
                {
                    if (!state.ContainsKey(neighbor))
                        continue; 

                    if (state[neighbor] == 1)
                    {
                        var cycle = string.Join(" → ", path.SkipWhile(n => n != neighbor)
                            .Append(neighbor));
                        throw new RuleException($"Обнаружен цикл: {cycle}");
                    }

                    if (state[neighbor] == 0)
                        DfsVisit(neighbor, deps, state, path);
                }
            }

            path.RemoveAt(path.Count - 1);
            state[node] = 2; 
        }

        //для группы уник имени = одна наиб версия 
        private List<IRule<TContext>> SelectActiveVersions(DateTime date)
        {
            return rules
                .Where(r => r.DataStart <= date && (r.DataEnd == null || r.DataEnd >= date))
                .GroupBy(r => r.Name)
                .Select(g => g.OrderByDescending(r => r.Version).First())
                .ToList();
        }


        // Алгоритм Кана - зав-ти всегда  раньше зависимых:
        // 1. Находим все вершины с нулевой входящей степенью (нет зависимостей).
        // 2. Добавляем их в результат (сортируя по приоритету).
        // 3. Удаляем их рёбра и повторяем.
        private static List<IRule<TContext>> KahnSort(List<IRule<TContext>> rules)
        {
            var ruleByName = rules.ToDictionary(r => r.Name);
            var inDegree = rules.ToDictionary(r => r.Name, _ => 0); //сколько правил надо дождаться
            var dependents = rules.ToDictionary(r => r.Name, _ => new List<string>()); //кто ждёт каждое правило

            foreach (var rule in rules)
                foreach (var dep in rule.DependsOn.Where(d => ruleByName.ContainsKey(d)))
                {
                    inDegree[rule.Name]++; // у кого есть DependsOn
                    dependents[dep].Add(rule.Name);
                }

            var queue = new SortedSet<(int priority, string name)>(
                rules.Where(r => inDegree[r.Name] == 0)
                     .Select(r => (r.Priority, r.Name))
            );

            var sorted = new List<IRule<TContext>>();

            // посл ждет всех
            while (queue.Count > 0)
            {
                var (_, name) = queue.Min;
                queue.Remove(queue.Min);

                sorted.Add(ruleByName[name]);

                foreach (var dep in dependents[name])
                {
                    inDegree[dep]--;
                    if (inDegree[dep] == 0)
                        queue.Add((ruleByName[dep].Priority, dep));
                }
            }

            return sorted;
        }
        
        public IReadOnlyList<IRule<TContext>> GetAllRules() => rules.AsReadOnly();


        public TotalModelResult<TContext> Execute(TContext context, DateTime? evaluationDate = null)
        {
            var date = evaluationDate ?? DateTime.UtcNow;
            var result = new TotalModelResult<TContext>
            {
                FinalContext = context,
                ExecutedAt = date
            };

            // 1. версии
            var activeRules = SelectActiveVersions(date);

            // 2. сорт по приоритету
            var orderedRules = KahnSort(activeRules);

            var firedRuleNames = new HashSet<string>();

            foreach (var rule in orderedRules)
            {
                // проверка зав-ти
                var unmetDep = rule.DependsOn.FirstOrDefault(dep => !firedRuleNames.Contains(dep));
                if (unmetDep is not null)
                {
                    result.RuleResults.Add(new RuleModelResult
                    {
                        RuleName = rule.Name,
                        RuleVersion = rule.Version,
                        isGood = false,
                        SkipReason = SkipReason.NotDependency,
                        LogMessage = $"зависимость '{unmetDep}' не сработала"
                    });
                    continue;
                }

                // проверка условие правила
                bool conditionMet;
                try
                {
                    conditionMet = rule.Condition(context);
                }
                catch (Exception ex)
                {
                    result.RuleResults.Add(new RuleModelResult
                    {
                        RuleName = rule.Name,
                        RuleVersion = rule.Version,
                        isGood = false,
                        SkipReason = SkipReason.NotCondition,
                        LogMessage = $"исключение в Condition: {ex.Message}"
                    });
                    continue;
                }

                if (!conditionMet)
                {
                    result.RuleResults.Add(new RuleModelResult
                    {
                        RuleName = rule.Name,
                        RuleVersion = rule.Version,
                        isGood = false,
                        SkipReason = SkipReason.NotCondition,
                        LogMessage = "условие не выполнено"
                    });
                    continue;
                }

                // выпол действ
                rule.Action(context);
                firedRuleNames.Add(rule.Name);

                result.RuleResults.Add(new RuleModelResult
                {
                    RuleName = rule.Name,
                    RuleVersion = rule.Version,
                    isGood = true,
                    LogMessage = "действие выполнено"
                });
            }

            return result;
        }

    }
}
