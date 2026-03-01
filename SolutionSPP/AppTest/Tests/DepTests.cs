using App;
using FrameworkTesting.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using App.Check;
using App.Rule;
using App.Engine;
using FrameworkTesting.Assert;

namespace AppTest.Tests
{
    [TestClass]
    public class DependencyTests
    {
        [TestMethod]
        [ExpectedException(typeof(RuleException))]
        public void DependencyTest1() //A -> B -> A
        {
            var engine = new RulesEngine<Model>();

            engine.AddRule(new TestACheck()); 
            engine.AddRule(new TestBCheck()); 
        }

        [TestMethod]
        public void DependencyTest2() // цикл через искл
        {
            var engine = new RulesEngine<Model>();
            engine.AddRule(new TestACheck());
            var ex = Assert.Throws<RuleException>(() =>
            {
                engine.AddRule(new TestBCheck()); 
            });

            Assert.IsNotNull(ex.Message);
            Assert.StringContains(ex.Message, "цикл", "Сообщение о цикле");
        }

        [TestMethod]
        public void DependencyTest3() // AgeCheck FAILED, [Income, Approval] - skipped
        {
            var engine = new RulesEngine<Model>();
            engine.AddRules([ new AgeCheck2(), new IncomeCheck(), new ApprovalCheck()]);

            var app = new Model { Age = 16, Income = 100_000m };
            var result = engine.Execute(app, new DateTime(2024, 6, 1));

            var skipped = result.SkippedRules.Select(r => r.RuleName).ToList();

            Assert.Contains(skipped, "Income");
            Assert.Contains(skipped, "Approval");

            var incomeResult = result.RuleResults.First(r => r.RuleName == "Income");
            Assert.AreEqual(SkipReason.NotDependency, incomeResult.SkipReason);
            Assert.IsFalse(app.IsApproved);
        }


        public class TestACheck : RuleBase<Model>
        {
            public TestACheck() : base(
                name: "RuleA",
                version: 1,
                priority: 10,
                dataStart: DateTime.MinValue,
                dependsOn: new[] { "RuleB" }) 
            { }
            public override bool Condition(Model ctx) => true;
            public override void Action(Model ctx) { }
        }
        public class TestBCheck : RuleBase<Model>
        {
            public TestBCheck() : base(
                name: "RuleB",
                version: 1,
                priority: 20,
                dataStart: DateTime.MinValue,
                dependsOn: new[] { "RuleA" })  
            { }
            public override bool Condition(Model ctx) => true;
            public override void Action(Model ctx) { }
        }
    }
}
