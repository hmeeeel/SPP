using App;
using App.Check;
using App.Engine;
using App.Rule;
using FrameworkTesting.Assert;
using FrameworkTesting.Attributes;

    [TestClass(Category = "Performance", Description = "параллелизм и Timeout")]
    public class SlowTests
    {
        private static async Task<Model> LoadApplicantAsync(
            int age = 25, decimal income = 80_000m, decimal debt = 0m)
        {
            await Task.Delay(200);
            return new Model { ApplicantName = $"Applicant_Age{age}", Age = age, Income = income, DebtAmount = debt };
        }
 
        private static async Task<RulesEngine<Model>> LoadEngineAsync()
        {
            await Task.Delay(200);
            var engine = new RulesEngine<Model>();
            engine.AddRules([new AgeCheck2(), new IncomeCheck(), new DebtToIncomeCheck(), new ApprovalCheck()]);
            return engine;
        }
        
        [TestMethod("Slow1: одобрение кредита при хорошем профиле заявителя")]
        [Timeout(2000)]
        public async Task Slow_1()
        {
            var applicant = await LoadApplicantAsync(age: 25, income: 80_000m, debt: 0m);
            var engine    = await LoadEngineAsync();
  
            var result = engine.Execute(applicant, new System.DateTime(2024, 6, 1));
 
            Assert.IsNotNull(result);
            Assert.IsTrue(applicant.IsApproved,                 "Кредит должен быть одобрен");
            Assert.GreaterThan(applicant.ApprovedAmount, 0m,    "Одобренная сумма > 0");
            Assert.AreEqual(240_000m, applicant.ApprovedAmount, "ApprovedAmount = Income * 3");
        }
 
        [TestMethod("Slow2: отказ при доходе ниже порога 50 000")]
        [Timeout(2000)]
        public async Task Slow_2()
        {
            var applicant = await LoadApplicantAsync(age: 30, income: 40_000m, debt: 0m);
            var engine    = await LoadEngineAsync();
 
            var result = engine.Execute(applicant, new System.DateTime(2024, 6, 1));
 
            Assert.IsFalse(applicant.IsApproved, "При доходе < 50 000 кредит не одобряется");
            var skipped = result.SkippedRules.Select(r => r.RuleName).ToList();
            Assert.Contains(skipped, "Income",   "Income пропущен");
            Assert.Contains(skipped, "Approval", "Approval зависит от Income — тоже пропущен");
        }
 
        [TestMethod("Slow3: правило AgeCheck v1 - 21+")]
        [Timeout(2000)]
        public async Task Slow_3()
        {
            var applicant = await LoadApplicantAsync(age: 18, income: 80_000m);
            await Task.Delay(200);
            var engine = new RulesEngine<Model>();
            engine.AddRules([new AgeCheck(), new AgeCheck2(), new IncomeCheck(), new ApprovalCheck()]);
 
            var result    = engine.Execute(applicant, new System.DateTime(2023, 6, 1));
            var ageResult = result.RuleResults.First(r => r.RuleName == "AgeCheck");
 
            Assert.AreEqual(1,     ageResult.RuleVersion, "В 2023 году — версия v1");
            Assert.IsFalse(ageResult.isGood,              "v1: 18 < 21 — не срабатывает");
            Assert.IsFalse(applicant.IsApproved,          "Кредит не одобрен");
        }
 
        [TestMethod("Slow4: правило AgeCheck v2 - 18+")]
        [Timeout(2000)]
        public async Task Slow_4()
        {
            var applicant = await LoadApplicantAsync(age: 18, income: 80_000m);
            await Task.Delay(200);
            var engine = new RulesEngine<Model>();
            engine.AddRules([new AgeCheck(), new AgeCheck2(), new IncomeCheck(), new ApprovalCheck()]);
 
            var result    = engine.Execute(applicant, new System.DateTime(2024, 6, 1));
            var ageResult = result.RuleResults.First(r => r.RuleName == "AgeCheck");
 
            Assert.AreEqual(2,    ageResult.RuleVersion, "В 2024 году — версия v2");
            Assert.IsTrue(ageResult.isGood,              "v2: 18 >= 18 — срабатывает");
            Assert.IsTrue(applicant.IsApproved,          "Кредит одобрен");
        }
 
        [TestMethod("Slow5: DebtToIncomeCheck снижает CreditScore при долге > 50% дохода")]
        [Timeout(2000)]
        public async Task Slow_5()
        {
            var applicant = await LoadApplicantAsync(age: 30, income: 100_000m, debt: 60_000m);
            var engine    = await LoadEngineAsync();
 
            var result     = engine.Execute(applicant, new System.DateTime(2024, 6, 1));
            var debtResult = result.RuleResults.First(r => r.RuleName == "DebtToIncomeCheck");
 
            Assert.IsTrue(debtResult.isGood, "DebtToIncome срабатывает: долг 60% > 50%");
            // CreditScore = 650 + 50 (Income) - 80 (Debt) = 620 < 700  отказ
            Assert.IsFalse(applicant.IsApproved, "CreditScore 620 < 700 — отказ");
        }
 
        [TestMethod("Slow6: доход ровно 50 000 — IncomeCheck не срабатывает (строгое >)")]
        [Timeout(2000)]
        public async Task Slow_6()
        {
            var applicant = await LoadApplicantAsync(age: 25, income: 50_000m);
            var engine    = await LoadEngineAsync();
 
            var result      = engine.Execute(applicant, new System.DateTime(2024, 6, 1));
            var incomeResult = result.RuleResults.First(r => r.RuleName == "Income");
 
            Assert.IsFalse(incomeResult.isGood, "Income = 50 000 не > 50 000 — не срабатывает");
            Assert.IsFalse(applicant.IsApproved, "Кредит не одобрен");
        }
 
        [TestMethod("Slow7: цикл A-B-A при добавлении правил вызывает RuleException")]
        [Timeout(2000)]
        public async Task Slow_7()
        {
            await Task.Delay(200);
            var engine = new RulesEngine<Model>();
            engine.AddRule(new CyclicRuleA());
 
            var ex = Assert.Throws<RuleException>(() => engine.AddRule(new CyclicRuleB()));
 
            Assert.IsNotNull(ex);
            Assert.StringContains(ex.Message, "цикл", "Сообщение содержит слово 'цикл'");
        }
 
        [TestMethod("Slow8: пустой движок — нулевое количество результатов")]
        [Timeout(2000)]
        public async Task Slow_8()
        {
            var applicant = await LoadApplicantAsync();
            await Task.Delay(200);
            var engine = new RulesEngine<Model>();
 
            var result = engine.Execute(applicant, new System.DateTime(2024, 1, 1));
 
            Assert.IsNotNull(result);
            Assert.HasCount(result.RuleResults, 0, "Нет правил — нет результатов");
            Assert.IsFalse(applicant.IsApproved,    "Без правил кредит не одобрен");
        }
 
        [TestMethod("Slow9: полная цепочка — все 4 правила в результате")]
        [Timeout(2000)]
        public async Task Slow_9()
        {
            var applicant = await LoadApplicantAsync(age: 30, income: 80_000m, debt: 5_000m);
            var engine    = await LoadEngineAsync();
 
            var result = engine.Execute(applicant, new System.DateTime(2024, 6, 1));
 
            Assert.HasCount(result.RuleResults, 4, "Должны быть записи для всех 4 правил");
 
            // Долг 5 000 / 80 000 = 6.25% < 50%  DebtToIncome не срабатывает
            var debtResult = result.RuleResults.First(r => r.RuleName == "DebtToIncomeCheck");
            Assert.IsFalse(debtResult.isGood, "Долг < 50% дохода — DebtToIncome не срабатывает");
 
            // CreditScore = 650 + 50 = 700, Approval требует строго > 700
            Assert.IsFalse(applicant.IsApproved, "CreditScore 700 не > 700 — отказ");
        }
 
 
        [TestMethod("Slow10: долг 49% дохода — DebtToIncome не срабатывает, граничный случай")]
        [Timeout(2000)]
        public async Task Slow_10()
        {
            var applicant = await LoadApplicantAsync(age: 25, income: 100_000m, debt: 49_000m);
            var engine    = await LoadEngineAsync();
 
            var result     = engine.Execute(applicant, new System.DateTime(2024, 6, 1));
            var debtResult = result.RuleResults.First(r => r.RuleName == "DebtToIncomeCheck");
 
            Assert.IsFalse(debtResult.isGood, "49% < 50% — DebtToIncome не срабатывает");
            // CreditScore = 650 + 50 = 700, Approval требует > 700
            Assert.IsFalse(applicant.IsApproved, "CreditScore ровно 700, не > 700 — отказ");
        }
 
        [TestMethod("Timeout1: движок завершается за 200мс при лимите 1000мс")]
        [Timeout(1000)]
        public async Task Timeout_1()
        {
            // LoadApplicantAsync занимает 200мс << 1000мс таймаута
            var applicant = await LoadApplicantAsync(age: 25, income: 80_000m);
            var engine = new RulesEngine<Model>();
            engine.AddRules([new AgeCheck2(), new IncomeCheck(), new ApprovalCheck()]);
 
            var result = engine.Execute(applicant, new System.DateTime(2024, 6, 1));
            Assert.IsNotNull(result, "Результат получен до истечения таймаута");
        }
 
        [TestMethod("Timeout2: имитация зависшего внешнего вызова — прерван за 50мс")]
        [Timeout(50)]
        public async Task Timeout_2()
        {
            await Task.Delay(300);
            Assert.IsTrue(true, "Эта строка недостижима — тест должен быть прерван");
        }
 
        [TestMethod("Timeout3: тест укладывается в лимит 500мс")]
        [Timeout(500)]
        public async Task Timeout_3()
        {
            await Task.Delay(100); 
            Assert.IsTrue(true, "Тест завершился до истечения таймаута");
        }
 
        [TestMethod("Timeout4: тест превышает лимит 50мс")]
        [Timeout(50)]
        public async Task Timeout_4()
        {
            await Task.Delay(300); 
            Assert.IsTrue(true, "Эта строка не должна выполниться");
        }

        private class CyclicRuleA : RuleBase<Model>
        {
            public CyclicRuleA() : base("SlowRuleA", 1, 10, System.DateTime.MinValue,
                dependsOn: new[] { "SlowRuleB" }) { }
            public override bool Condition(Model ctx) => true;
            public override void Action(Model ctx) { }
        }
 
        private class CyclicRuleB : RuleBase<Model>
        {
            public CyclicRuleB() : base("SlowRuleB", 1, 20, System.DateTime.MinValue,
                dependsOn: new[] { "SlowRuleA" }) { }
            public override bool Condition(Model ctx) => true;
            public override void Action(Model ctx) { }
        }
    
    }