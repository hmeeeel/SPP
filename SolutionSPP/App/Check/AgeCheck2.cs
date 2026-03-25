using App.Rule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Check
{
    public class AgeCheck2 : RuleBase<Model>
    {
        public AgeCheck2() : base(
            name: "AgeCheck",
            version: 2,
            priority: 10,
            dataStart: new DateTime(2024, 1, 1),
            dataEnd: null)
        { } 

        public override bool Condition(Model ctx) => ctx.Age >= 18;

        public override void Action(Model ctx)
        {
        }
    }
}
