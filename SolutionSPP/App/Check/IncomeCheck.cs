using App.Rule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Check
{
    public class IncomeCheck : RuleBase<Model>
{
    public IncomeCheck() : base(
        name: "Income",
        version: 1,
        priority: 30,
        dataStart: new DateTime(2020, 1, 1),
        dependsOn: ["AgeCheck"])
    { } 

    public override bool Condition(Model ctx) =>
        ctx.Income > 50000;

    public override void Action(Model ctx)
    {
        ctx.CreditAmount += 50;
    }
}
}
