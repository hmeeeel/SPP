using System.Collections.Generic;

namespace FrameworkTesting.DataProviders
{
    public static class TestDataProviders
    {
        public static IEnumerable<object[]> AgeBoundaryValues()
        {
            // Граничные случаи для возраста
            yield return new object[] { 0, false };      // Минимальное значение
            yield return new object[] { 17, false };     // Ниже порога v2
            yield return new object[] { 18, true };      // Порог v2 (>=18)
            yield return new object[] { 19, true };      // Выше порога v2
            yield return new object[] { 20, true };      // Ниже порога v1
            yield return new object[] { 21, true };      // Порог v1 (>=21)
            yield return new object[] { 25, true };      // Нормальное значение
            yield return new object[] { 65, true };      // Старший возраст
            yield return new object[] { 100, true };     // Экстремальное значение
            yield return new object[] { -1, false };     // Некорректное значение
        }

        public static IEnumerable<object[]> IncomeBoundaryValues()
        {
            yield return new object[] { 0.0, false };           // Нулевой доход
            yield return new object[] { 49_999.0, false };      // Ниже порога
            yield return new object[] { 50_000.0, false };      // Точно на пороге (> 50k)
            yield return new object[] { 50_001.0, true };       // Выше порога
            yield return new object[] { 100_000.0, true };      // Нормальный доход
            yield return new object[] { 1_000_000.0, true };    // Высокий доход
            yield return new object[] { -1000.0, false };       // Некорректное значение
        }

        public static IEnumerable<object[]> CreditScenarios()
        {
            //  +
            yield return new object[]
            {
                "Идеальный заявитель",
                25,          // Age
                100_000.0,   // Income
                0.0,         // Debt
                true         // ShouldBeApproved
            };

            // Низкий доход
            yield return new object[]
            {
                "Низкий доход",
                30,
                40_000.0,
                0.0,
                false
            };

            yield return new object[]
            {
                "Высокая долговая нагрузка",
                25,
                80_000.0,
                100_000.0,  // Долг > 50% дохода
                false
            };

            // Мал по возрасту
            yield return new object[]
            {
                "Недостаточный возраст",
                16,
                200_000.0,
                0.0,
                false
            };

            yield return new object[]
            {
                "Граничный CreditScore",
                25,
                80_000.0,
                0.0,
                false  // CreditScore = 650 + 50 = 700, но нужно >700
            };

            yield return new object[]
            {
                "Превышение порога",
                30,
                90_000.0,
                0.0,
                true  // CreditScore = 650 + 50 = 700 + бонусы
            };
        }

        public static IEnumerable<object[]> AgeCombined()
        {
            int[] ages = { 17, 18, 25, 30 };
            double[] incomes = { 40_000.0, 50_001.0, 80_000.0 };

            foreach (var age in ages)
            {
                foreach (var income in incomes)
                {

                    bool expectedApproval = age >= 18 && income > 50_000.0;
                    yield return new object[] { age, income, expectedApproval };
                }
            }
        }

        public static IEnumerable<object[]> PositiveIncomeCases()
        {
            var allIncomes = new[]
            {
                -1000.0, 0.0, 10_000.0, 50_001.0, 100_000.0, 1_000_000.0
            };

            foreach (var income in allIncomes)
            {
                if (income > 0)
                {
                    yield return new object[] { income };
                }
            }
        }

        public static IEnumerable<object[]> RandomScenariosInfinite()
        {
            var random = new Random(42);
            
            while (true)
            {
                int age = random.Next(15, 70);
                double income = random.Next(20_000, 200_000);
                double debt = random.Next(0, 100_000);
                
                yield return new object[] { age, income, debt };
            }
        }
    }
}