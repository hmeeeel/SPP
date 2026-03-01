using FrameworkTesting.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrameworkTesting.Assert;

namespace AppTest.Tests
{
    [TestClass]
    [SharedContext] 
    public class SharedContextTests
    {
        private readonly List<string> _executionLog = []; // global

        [TestMethod]
        public void SharedContextTest1()
        {
            _executionLog.Add("Test1");
            Assert.HasCount(_executionLog, 1, "После первого теста в логе 1 запись");
        }

        [TestMethod]
        public void SharedContextTest2()
        {
            _executionLog.Add("Test2");

            Assert.HasCount(_executionLog, 2, "Shared context: оба теста должны быть в логе");
            Assert.Contains(_executionLog, "Test1", "Запись из первого теста должна быть видна");
            Assert.Contains(_executionLog, "Test2");
        }
    }
}