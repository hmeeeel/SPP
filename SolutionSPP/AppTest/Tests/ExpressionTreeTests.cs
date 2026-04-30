using FrameworkTesting.Attributes;
using FrameworkTesting.Assert;
using System.Linq.Expressions;
using App;

namespace AppTest.Tests
{
    
    [TestClass(Category = "ExpressionTrees", Description = "Демонстрация деревьев выражений")]
    public class ExpressionTreeTests
    {

        [TestMethod("Пример из лекции")]
        public void ExprTreeConstruction()
        {
            ParameterExpression numParam = Expression.Parameter(typeof(int), "num");
            ConstantExpression five = Expression.Constant(5, typeof(int));
            BinaryExpression numLessThanFive = Expression.LessThan(numParam, five);
            Expression<Func<int, bool>> lambda = Expression.Lambda<Func<int, bool>>(
                numLessThanFive, numParam);
            
            Console.WriteLine($"1. Параметр: {numParam} (тип: {numParam.Type.Name})");
            Console.WriteLine($"2. Константа: {five.Value}");
            Console.WriteLine($"3. Операция: {numLessThanFive.NodeType}");
            Console.WriteLine($"4. Лямбда: {lambda}");
            
            Func<int, bool> function = lambda.Compile();
            
            Console.WriteLine($"\nРезультаты:");
            Console.WriteLine($"function(3) = {function(3)}");
            Console.WriteLine($"function(7) = {function(7)}");
            
            Assert.That(() => function(3));
            Assert.That(() => !function(7));
        }


         [TestMethod("Правила пришли из БД - Age >= 18 AND Income > 50000")]
        public void ExprTree_DynamicRuleCreation()
        {
            var model = new Model { Age = 25, Income = 60_000m };
            
            var param = Expression.Parameter(typeof(Model), "m"); // то что пришло
            
            var ageProperty = Expression.Property(param, nameof(Model.Age));
            var ageCheck = Expression.GreaterThanOrEqual(ageProperty, Expression.Constant(18));

            var incomeProperty = Expression.Property(param, nameof(Model.Income));
            var incomeCheck = Expression.GreaterThan(incomeProperty, Expression.Constant(50_000m));
            
            var combined = Expression.AndAlso(ageCheck, incomeCheck);
            
            var lambda = Expression.Lambda<Func<Model, bool>>(combined, param);
            var compiledRule = lambda.Compile();
            
            bool result = compiledRule(model);
            
            Console.WriteLine($"Правило (динамическое): {lambda}");
           // Console.WriteLine($"Результат: {result}");
            
            Assert.That(() => result == true, "Динамическое правило должно пройти");
        }

        // ПРОВАЛ
        [TestMethod("Комплексное правило с детальной диагностикой")]
        public void ExprTree_ComplexBusinessRule()
        {
            var applicant = new Model 
            { 
                ApplicantName = "Иван Петров",
                Age = 17, // мал
                Income = 75_000m, 
                DebtAmount = 15_000m 
            };
            
            Expression<Func<bool>> creditRule = () =>
                applicant.Age >= 18 &&
                applicant.Age <= 65 && 
                applicant.Income > 50_000m && 
                (applicant.DebtAmount / applicant.Income) < 0.4m &&
                applicant.ApplicantName.Length > 0;
            
            Assert.That(creditRule, "Комплексная проверка заявителя на кредит");
        }

        [TestMethod("Комплексное правило - ручная сборка дерева")]
        public void ExprTree_ComplexBusinessRule_Lecture()
        {
            var applicant = new Model
            {
                ApplicantName = "Иван Петров",
                Age = 17,  
                Income = 75_000m,
                DebtAmount = 15_000m
            };

            //var param = Expression.Parameter(typeof(Model), "a");
            var param = Expression.Constant(applicant);
            var ageProperty = Expression.Property(param, nameof(Model.Age));
            var minAge = Expression.Constant(18);
            var ageCheckMin = Expression.GreaterThanOrEqual(ageProperty, minAge);

            var maxAge = Expression.Constant(65);
            var ageCheckMax = Expression.LessThanOrEqual(ageProperty, maxAge);

            var incomeProperty = Expression.Property(param, nameof(Model.Income));
            var minIncome = Expression.Constant(50_000m);
            var incomeCheck = Expression.GreaterThan(incomeProperty, minIncome);

            // (a.DebtAmount / a.Income) < 0.4
            var debtProperty = Expression.Property(param, nameof(Model.DebtAmount));
            var division = Expression.Divide(debtProperty, incomeProperty);
            var maxRatio = Expression.Constant(0.4m);
            var ratioCheck = Expression.LessThan(division, maxRatio);

            var nameProperty = Expression.Property(param, nameof(Model.ApplicantName));
            var lengthProperty = Expression.Property(nameProperty, nameof(string.Length));
            var zero = Expression.Constant(0);
            var nameCheck = Expression.GreaterThan(lengthProperty, zero);

            var combined =
                Expression.AndAlso(
                    Expression.AndAlso(
                        Expression.AndAlso(
                            Expression.AndAlso(ageCheckMin, ageCheckMax),
                            incomeCheck),
                        ratioCheck),
                    nameCheck);


            var finalExpression = Expression.Lambda<Func<bool>>(combined);
            
            Assert.That(finalExpression, "Комплексная проверка (ручная сборка)");
        }


        [TestMethod("Простое сравнение - успешное")]
        public void ExprTree_SimpleComparison_Success()
        {
            int x = 5;
            int y = 10;
            
            Assert.That(() => x > y);
        }


        [TestMethod("Комбинация с AND")]
        public void ExprTree_ComplexAnd_Failure()
        {
            int age = 16;
            decimal income = 60_000m;
            
            Assert.That(() => age >= 18 && income > 50_000m);
        }
    }
}