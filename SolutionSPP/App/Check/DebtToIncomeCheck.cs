using App.Rule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Check
{
    public class DebtToIncomeCheck : RuleBase<Model>
    {
        public DebtToIncomeCheck() : base(
            name: "DebtToIncomeCheck",
            version: 1,
            priority: 35,
            dataStart: new DateTime(2020, 1, 1),
            dependsOn: ["AgeCheck"])
        { }

        public override bool Condition(Model ctx) =>
             ctx.Income > 0 && (ctx.DebtAmount / ctx.Income) > 0.5m;

        public override void Action(Model ctx)
        {
            ctx.CreditAmount -= 80;
        }
    }
}
