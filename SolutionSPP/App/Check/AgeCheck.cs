using App.Rule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Check
{
    public class AgeCheck : RuleBase<Model>
    {
        public AgeCheck() : base(
            name: "AgeCheck",
            version: 1,
            priority: 10,
            dataStart: new DateTime(2020, 1, 1),
            dataEnd: new DateTime(2023, 12, 31))
        { }

        public override bool Condition(Model ctx) => ctx.Age >= 21;

        public override void Action(Model ctx)
        {
            //прошло Condition и попало в firedRuleNames
        }
    }

}
