using FrameworkTesting.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using App;
using App.Check;
using FrameworkTesting.Assert;
using App.Engine;

namespace AppTest.Tests
{
    [TestClass]
    public class AsyncTests
    {
        [TestMethod]
        //CreditAmount < 700
        public async Task AsyncTest1() // LoadAsync, Age: 25, Income: 80_000, DebtAmount: 0, CreditAmount: 700
        {
            var application = await LoadAsync();

            var engine = new RulesEngine<Model>();
            engine.AddRules([new AgeCheck2(), new IncomeCheck(), new ApprovalCheck()]);

            var result = engine.Execute(application, new DateTime(2024, 6, 1));

            Assert.IsNotNull(result);
            Assert.IsTrue(application.IsApproved); // dependsOn: ["AgeCheck", "Income"] 
        }

        [TestMethod]
        public async Task AsyncTest2() // искл
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await ThrowingAsyncMethod();
            });

            Assert.IsNotNull(ex);
            Assert.IsInstanceOf<InvalidOperationException>(ex);
        }

        private static async Task<Model> LoadAsync()
        {
            await Task.Delay(1); 
            return new Model
            {
                ApplicantName = "Async",
                Age = 25,
                Income = 80_000m
            };
        }

        private static async Task ThrowingAsyncMethod()
        {
            await Task.Delay(1);
            throw new InvalidOperationException("async исключение");
        }
    }
}
