using App.Rule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Check
{
    public class ApprovalCheck : RuleBase<Model>
    {
        public ApprovalCheck() : base(
            name: "Approval",
            version: 1,
            priority: 100,
            dataStart: new DateTime(2020, 1, 1),
            dependsOn: ["AgeCheck", "Income"])
        { } 

        public override bool Condition(Model ctx) =>
            ctx.CreditAmount > 700;

        public override void Action(Model ctx)
        {
            ctx.IsApproved = true;
            ctx.ApprovedAmount = ctx.Income * 3; 
        }
    }
}
