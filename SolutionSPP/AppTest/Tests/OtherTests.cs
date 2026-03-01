using App;
using FrameworkTesting.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using App.Check;
using App.Rule;
using FrameworkTesting.Assert;
using App.Engine;

namespace AppTest.Tests
{
    [TestClass]
    public class OthersTests
    {
        [TestMethod]
        [Ignore]
        public void OthersTest1()
        {
            Assert.IsTrue(false, "IsIgnored");
        }

        [TestMethod]
        public void OthersTest2()
        {
            double result = 1.0 / 3.0 * 3.0;

            Assert.AreEqualWithDelta(1.0, result, 1e-10, "+- 1.0");
        }

        [TestMethod]
        public void OthersTest3()
        {
            var engine = new RulesEngine<Model>();
            engine.AddRule(new AgeCheck2());

            var names = engine.GetAllRules().Select(r => r.Name).ToList();

            Assert.DoesNotContain(names, "NonExistentRule",
                "нет пр NonExistentRule");
        }

        [TestMethod]
        public void OthersTest4()
        {
            IRule<Model> rule = new AgeCheck2();

            Assert.IsInstanceOf<RuleBase<Model>>(rule,
                "AgeCheck2 должен быть наследником RuleBase");
        }
    }
}