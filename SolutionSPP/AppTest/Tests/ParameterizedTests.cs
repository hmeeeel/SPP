using App;
using App.Check;
using App.Engine;
using FrameworkTesting.Assert;
using FrameworkTesting.Attributes;

namespace AppTest.Tests
{
    [TestClass(Category = "Parametrized", Description = "ParameterizedTests_q")]
    public class ParameterizedTests
    {
        [TestMethod("Проверка: пустой набор правил")]
        public void EmptyEngineDoesNotThrow()
        {
            var engine = new RulesEngine<Model>();
            var model = new Model { Age = 25, Income = 60_000m };
            var result = engine.Execute(model, new DateTime(2024, 1, 1));
            Assert.IsNotNull(result);
            Assert.HasCount(result.RuleResults, 0, "Нет правил — нет результатов");
        }

        [TestMethod("Проверка возраста")]
        [DataRow(17, false, 000, DisplayName = "Возраст 17, false - 1")]
        [DataRow(18, true, DisplayName = "Возраст 18, true - 1")]
        [DataRow(25, true, DisplayName = "Возраст 25, true - 1")]
        [DataRow(0, false, DisplayName = "Возраст 0, false - 1")]
        public void AgeCheck_V2_Parameterized(int age, bool expectedFired)
        {
            var engine = new RulesEngine<Model>();
            engine.AddRule(new AgeCheck2());

            var model = new Model { Age = age, Income = 60_000m };
            var result = engine.Execute(model, new DateTime(2024, 6, 1));

            var ageResult = result.RuleResults.First(r => r.RuleName == "AgeCheck");

            Assert.AreEqual(expectedFired, ageResult.isGood,
                $"Возраст {age}: ожидалось isGood={expectedFired}");
        }

        [TestMethod("IncomeCheck: порог 50 000")]
        [DataRow(49_999.0, false, DisplayName = "Доход 49 999, false - 1")]
        [DataRow(50_000.0, false, DisplayName = "Доход 50 000, false - 1")]
        [DataRow(50_001.0, true, DisplayName = "Доход 50 001, true - 1")]
        [DataRow(200_000.0, true, DisplayName = "Доход 200 000, true - 1")]
        public void IncomeCheck_Parameterized(double incomeRaw, bool expectedFired)
        {
            decimal income = (decimal)incomeRaw;

            var engine = new RulesEngine<Model>();
            engine.AddRules([new AgeCheck2(), new IncomeCheck()]);

            var model = new Model { Age = 25, Income = income };
            var result = engine.Execute(model, new DateTime(2024, 6, 1));

            var incomeResult = result.RuleResults.First(r => r.RuleName == "Income");
            Assert.AreEqual(expectedFired, incomeResult.isGood,
                $"Доход {income}: ожидалось isGood={expectedFired}");
        }

        [TestMethod("Итоговое решение по кредиту")]
        [DataRow(25, 80_000.0, 0.0, true, DisplayName = "25, 80_000.0, 0.0, TRUE = IsApproved, но IsApproved = FALSE: 700 > 700 - 0")]
        [DataRow(25, 40_000.0, 0.0, false, DisplayName = "25, 40_000.0, 0.0, false - 1")]
        [DataRow(16, 200_000.0, 0.0, false, DisplayName = "16, 200_000.0, 0.0, false - 1")]
        [DataRow(25, 80_000.0, 100_000.0, false, DisplayName = "25, 80_000.0, 100_000.0, false - 1")]
        public void CreditDecision_Parameterized(int age, double incomeRaw, double debtRaw, bool shouldBeApproved)
        {
            decimal income = (decimal)incomeRaw;
            decimal debt = (decimal)debtRaw;

            var engine = new RulesEngine<Model>();
            engine.AddRules([new AgeCheck2(), new IncomeCheck(), new DebtToIncomeCheck(), new ApprovalCheck()]);

            var model = new Model { Age = age, Income = income, DebtAmount = debt };
            engine.Execute(model, new DateTime(2024, 6, 1));

            Assert.AreEqual(shouldBeApproved, model.IsApproved,
                $"Возраст={age}, Доход={income}, Долг={debt}: ожидалось IsApproved={shouldBeApproved}");
        }
    }
}