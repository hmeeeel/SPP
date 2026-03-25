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
    public class VersioningTests
    {
        private RulesEngine<Model> engine = null!;

        [TestSetup]
        public void Setup()
        {
            engine = new RulesEngine<Model>();
            engine.AddRules([new AgeCheck(), new AgeCheck2(), new IncomeCheck(), new ApprovalCheck()]);
        }

        [TestMethod]
        public void VersioningTest1() // 2023 - v1
        {
            var application = new Model
            {
                Age = 18,
                Income = 80_000m
            };

            var result = engine.Execute(application, new DateTime(2023, 6, 1));

            var ageCheckResult = result.RuleResults.First(r => r.RuleName == "AgeCheck");
            Assert.AreEqual(1, ageCheckResult.RuleVersion, "2023 - V1");
            Assert.IsFalse(ageCheckResult.isGood, "AgeCheck v1 > 18 лет");
            Assert.IsFalse(application.IsApproved, "Кредит не одобрен");
        }

        [TestMethod]
        public void VersioningTest2() // 2024 - v2
        {
            var application = new Model
            {
                Age = 18,
                Income = 80_000m
            };

            var result = engine.Execute(application, new DateTime(2024, 6, 1));

            var ageCheckResult = result.RuleResults.First(r => r.RuleName == "AgeCheck");

            Assert.AreEqual(2, ageCheckResult.RuleVersion, "2024 - V2");
            Assert.IsTrue(ageCheckResult.isGood, "AgeCheck v2 = 18 лет");
        }


        [TestMethod]
        public void VersioningTest3() // 2025 - v1
        {
            var application = new Model { Age = 25, Income = 80_000m };
            var result = engine.Execute(application, new DateTime(2025, 1, 1));

            var ageCheck = result.RuleResults.First(r => r.RuleName == "AgeCheck");

            Assert.AreEqual(2, ageCheck.RuleVersion);
        }
    }
}
