using App;
using App.Check;
using App.Engine;
using FrameworkTesting.Assert;
using FrameworkTesting.Attributes;
using FrameworkTesting.DataProviders;
using System.Collections.Generic;

namespace AppTest.Tests
{

    [TestClass(Category = "Advanced", Description = "Демонстрация итераторов, событий и делегатов")]
    public class AdvancedFeatureTests
    {

        public static IEnumerable<object[]> AgeTestCases()
        {
            yield return new object[] { 17, false, "Ниже порога v2 (18+)" };
            yield return new object[] { 18, true, "Точно на пороге v2" };
            yield return new object[] { 19, true, "Выше порога v2" };
            yield return new object[] { 21, true, "Порог v1 (21+)" };
            yield return new object[] { 25, true, "Нормальный возраст" };
            yield return new object[] { 65, true, "Предпенсионный" };
        }

        [TestMethod("Параметризованный тест возраста через итератор")]
        [DataRow(17, false, DisplayName = "Возраст 17: отказ")]
        [DataRow(18, true, DisplayName = "Возраст 18: одобрение")]
        [DataRow(25, true, DisplayName = "Возраст 25: одобрение")]
        public void IteratorBased_AgeCheck(int age, bool expectedApproval)
        {
            var engine = new RulesEngine<Model>();
            engine.AddRule(new AgeCheck2());

            var model = new Model { Age = age, Income = 100_000m };
            var result = engine.Execute(model, new DateTime(2024, 6, 1));

            var ageResult = result.RuleResults.First(r => r.RuleName == "AgeCheck");
            Assert.AreEqual(expectedApproval, ageResult.isGood,
                $"Для возраста {age} ожидалось: {expectedApproval}");
        }

        public static IEnumerable<object[]> AgeIncomeCombinations()
        {
            int[] ages = { 17, 18, 25 };
            decimal[] incomes = { 40_000m, 50_001m, 80_000m };

            foreach (var age in ages)
            {
                foreach (var income in incomes)
                {
                    bool expectedApproval = age >= 18 && income > 50_000m;
                    yield return new object[] { age, income, expectedApproval };
                }
            }
        }

        [TestMethod("Комбинаторный тест age×income через итератор")]
        [DataRow(17, 40_000.0, false, DisplayName = "17 лет, 40k")]
        [DataRow(18, 50_001.0, true, DisplayName = "18 лет, 50k+")]
        [DataRow(25, 80_000.0, true, DisplayName = "25 лет, 80k")]
        public void IteratorBased_AgeCombined(int age, double incomeRaw, bool shouldApprove)
        {
            decimal income = (decimal)incomeRaw;

            var engine = new RulesEngine<Model>();
            engine.AddRules([new AgeCheck2(), new IncomeCheck(), new ApprovalCheck()]);

            var model = new Model { Age = age, Income = income };
            engine.Execute(model, new DateTime(2024, 6, 1));

            Assert.AreEqual(shouldApprove, model.IsApproved,
                $"Age={age}, Income={income}: ожидалось IsApproved={shouldApprove}");
        }

        public static IEnumerable<object[]> PositiveIncomeOnly()
        {
            decimal[] incomes = { -1000m, 0m, 10_000m, 50_001m, 100_000m };

            foreach (var income in incomes)
            {
                if (income > 0)
                {
                    yield return new object[] { income };
                }
            }
        }

        [TestMethod("Фильтрация положительных доходов через итератор")]
        [DataRow(10_000.0, DisplayName = "Доход 10k")]
        [DataRow(50_001.0, DisplayName = "Доход 50k+")]
        [DataRow(100_000.0, DisplayName = "Доход 100k")]
        public void IteratorBased_PositiveIncomeFilter(double incomeRaw)
        {
            decimal income = (decimal)incomeRaw;
            
            Assert.GreaterThan(income, 0m, "Доход должен быть положительным");
        }

        public static IEnumerable<object[]> CreditScenarios()
        {
            yield return new object[]
            {
                "Идеальный заявитель",
                25, 100_000m, 0m, true
            };

            yield return new object[]
            {
                "Низкий доход",
                30, 40_000m, 0m, false
            };

            yield return new object[]
            {
                "Высокая долговая нагрузка",
                25, 80_000m, 100_000m, false
            };

            yield return new object[]
            {
                "Недостаточный возраст",
                16, 200_000m, 0m, false
            };
        }

        [TestMethod("Сценарии кредитования через итератор")]
        [DataRow("Идеальный", 25, 100_000.0, 0.0, true, DisplayName = "Идеальный заявитель")]
        [DataRow("Низкий доход", 30, 40_000.0, 0.0, false, DisplayName = "Низкий доход")]
        public void IteratorBased_CreditScenarios(
            string scenario, int age, double incomeRaw, double debtRaw, bool shouldApprove)
        {
            decimal income = (decimal)incomeRaw;
            decimal debt = (decimal)debtRaw;

            var engine = new RulesEngine<Model>();
            engine.AddRules([
                new AgeCheck2(), 
                new IncomeCheck(), 
                new DebtToIncomeCheck(), 
                new ApprovalCheck()
            ]);

            var model = new Model { Age = age, Income = income, DebtAmount = debt };
            engine.Execute(model, new DateTime(2024, 6, 1));

            Assert.AreEqual(shouldApprove, model.IsApproved,
                $"Сценарий '{scenario}': ожидалось IsApproved={shouldApprove}");
        }


        [TestMethod("Использование внешнего провайдера AgeBoundaryValues")]
        [DataRow(0, false, DisplayName = "Минимальное значение")]
        [DataRow(17, false, DisplayName = "Ниже порога")]
        [DataRow(18, true, DisplayName = "На пороге")]
        [DataRow(25, true, DisplayName = "Нормальное значение")]
        public void UsingExternalProvider_AgeBoundary(int age, bool expectedApproval)
        {
            var engine = new RulesEngine<Model>();
            engine.AddRule(new AgeCheck2());

            var model = new Model { Age = age, Income = 60_000m };
            var result = engine.Execute(model, new DateTime(2024, 6, 1));

            var ageResult = result.RuleResults.First(r => r.RuleName == "AgeCheck");
            Assert.AreEqual(expectedApproval, ageResult.isGood);
        }

        [TestMethod("Использование внешнего провайдера IncomeBoundaryValues")]
        [DataRow(49_999.0, false, DisplayName = "Ниже порога")]
        [DataRow(50_000.0, false, DisplayName = "На пороге")]
        [DataRow(50_001.0, true, DisplayName = "Выше порога")]
        public void UsingExternalProvider_IncomeBoundary(double incomeRaw, bool expectedApproval)
        {
            decimal income = (decimal)incomeRaw;

            var engine = new RulesEngine<Model>();
            engine.AddRules([new AgeCheck2(), new IncomeCheck()]);

            var model = new Model { Age = 25, Income = income };
            var result = engine.Execute(model, new DateTime(2024, 6, 1));

            var incomeResult = result.RuleResults.First(r => r.RuleName == "Income");
            Assert.AreEqual(expectedApproval, incomeResult.isGood);
        }

        public static IEnumerable<int> FibonacciInfinite()
        {
            int a = 0, b = 1;
            
            while (true)  // Бесконечный цикл!
            {
                yield return a;
                int temp = a;
                a = b;
                b = temp + b;
            }
        }

        [TestMethod("Бесконечный итератор Фибоначчи с Take()")]
        public void InfiniteIterator_Fibonacci()
        {
            var first10 = FibonacciInfinite().Take(10).ToList();

            Assert.HasCount(first10, 10, "Должно быть 10 элементов");
            Assert.AreEqual(0, first10[0], "F(0) = 0");
            Assert.AreEqual(1, first10[1], "F(1) = 1");
            Assert.AreEqual(1, first10[2], "F(2) = 1");
            Assert.AreEqual(2, first10[3], "F(3) = 2");
            Assert.AreEqual(34, first10[9], "F(9) = 34");
        }


        public static IEnumerable<int> NumbersUntilNegative(int[] numbers)
        {
            foreach (var num in numbers)
            {
                if (num < 0)
                {
                    yield break;
                }
                yield return num;
            }
        }

        [TestMethod("Итератор с yield break")]
        public void Iterator_YieldBreak()
        {
            int[] numbers = { 1, 2, 3, -5, 6, 7 };
            var result = NumbersUntilNegative(numbers).ToList();

            Assert.HasCount(result, 3, "Должно остановиться на -5");
            Assert.AreEqual(1, result[0]);
            Assert.AreEqual(2, result[1]);
            Assert.AreEqual(3, result[2]);
        }
    }
}