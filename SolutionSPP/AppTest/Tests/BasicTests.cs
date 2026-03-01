using FrameworkTesting.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using App;
using App.Check;
using App.Engine;
using FrameworkTesting.Assert;

namespace AppTest.Tests
{
    [TestClass]
    public class BasicTests
    {
        private RulesEngine<Model> engine = null!;
        private Model application = null!;

        [TestSetup]
        public void Setup()
        {
            engine = new RulesEngine<Model>();
            engine.AddRules([new AgeCheck2(), new IncomeCheck(), new DebtToIncomeCheck(), new ApprovalCheck()]);

            application = new Model
            {
                ApplicantName = "123",
                Age = 30,
                Income = 80_000m,
                DebtAmount = 5_000m
            };
        }

        [TestTeardown]
        public void Teardown()
        {
            engine = null!;
            application = null!;
        }

        [TestMethod]
        public void BasicTest1() //AgeCheck -> IncomeCheck -> ApprovalCheck
        {
            var result = engine.Execute(application, new DateTime(2024, 6, 1));

            Assert.IsTrue(application.IsApproved, "IsApproved = 1"); // dependsOn: ["AgeCheck", "Income"]
            Assert.GreaterThan(application.CreditAmount, 600, "CreditAmount+");
            Assert.GreaterThan(application.ApprovedAmount, 0m, "ApprovedAmount > 0");
            Assert.AreEqual(240_000m, application.ApprovedAmount, "ApprovedAmount = 3 * Income");
            Assert.IsNotNull(result, "result not null");
            var firedNames = result.FiredRules.Select(r => r.RuleName).ToList();
            Assert.Contains(firedNames, "Approval", "Approval должно сработать");
        }

        [TestMethod]
        public void BasicTest2() //Condition - false
        {
            application.Income = 40_000m; // <50 000
            var result = engine.Execute(application, new DateTime(2024, 6, 1));

            var skippedNames = result.SkippedRules.Select(r => r.RuleName).ToList();

            Assert.Contains(skippedNames, "Income", "Income должен быть пропущен");
            Assert.Contains(skippedNames, "Approval");
            Assert.IsFalse(application.IsApproved, "Кредит не должен быть одобрен без IncomeCheck");
        }

        [TestMethod]
        public void BasicTest3() //AgeCheck -> IncomeCheck -> DebtToIncomeCheck -> ApprovalCheck
        {
            var result = engine.Execute(application, new DateTime(2024, 6, 1));

            Assert.HasCount(result.RuleResults, 4, " 4 пр!!");
        }
    }
}

