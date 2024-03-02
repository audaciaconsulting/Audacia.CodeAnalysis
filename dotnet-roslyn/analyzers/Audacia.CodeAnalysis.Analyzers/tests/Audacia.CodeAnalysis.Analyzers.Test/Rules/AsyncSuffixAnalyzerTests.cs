using Audacia.CodeAnalysis.Analyzers.Rules.AsyncSuffix;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class AsyncSuffixAnalyzerTests : CodeFixVerifier
    {
        [TestMethod]
        public void No_Diagnostics_For_Empty_Code()
        {
            const string testCode = @"";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Synchronous_Class_Method()
        {
            const string testCode = @"
                namespace ConsoleApplication1
                {
                    class TestClass
                    {
                        void TestMethod()
                        {
                            Console.WriteLine(1234567890);
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Synchronous_Interface_Method_Declaration()
        {
            const string testCode = @"
                namespace ConsoleApplication1
                {
                    interface ITestInterface
                    {
                        void TestMethod();
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Synchronous_Method_With_Return_Object_Name_Containing_Task()
        {
            const string testCode = @"
                namespace ConsoleApplication1
                {
                    class FakeTask
                    {
                    }

                    class TestClass
                    {
                        FakeTask TestMethod()
                        {
                            return new FakeTask();
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Synchronous_Method_With_Asynchronous_Operation_Return_Type()
        {
            const string testCode = @"
                namespace ConsoleApplication1
                {
                    class TestClass
                    {
                        Task<int> TestMethod()
                        {
                            return new Task<int>(() => default);
                        }
                    }
                }";

            const string expectedMessage
                = "Asynchronous method name 'TestMethod' is not suffixed with 'Async'";

            var expectedDiagnostic = BuildExpectedResult(expectedMessage, 6, 25);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostic_And_Code_Fix_For_Interface_Method_With_Asynchronous_Operation_Return_Type_Without_Async_Suffix()
        {
            const string testCode = @"
                namespace ConsoleApplication1
                {
                    interface ITestInterface
                    {
                        Task TestMethod();
                    }
                }";

            const string expectedMessage
                = "Asynchronous method name 'TestMethod' is not suffixed with 'Async'";

            var expectedDiagnostic = BuildExpectedResult(expectedMessage, 6, 25);

            VerifyDiagnostic(testCode, expectedDiagnostic);

            const string fixedTestCode = @"
                namespace ConsoleApplication1
                {
                    interface ITestInterface
                    {
                        Task TestMethodAsync();
                    }
                }";
            VerifyCodeFix(testCode, fixedTestCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Asynchronous_Method_With_Async_Suffix()
        {
            const string testCode = @"
                namespace ConsoleApplication1
                {
                    class TestClass
                    {
                        async Task<int> TestMethodAsync()
                        {
                            var task = new Task<int>(() => default);

                            return await task;
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Diagnostic_And_Code_Fix_For_Asynchronous_Method_With_No_Async_Suffix()
        {
            const string testCode = @"
                namespace ConsoleApplication1
                {
                    class TestClass
                    {
                        async Task<int> TestMethod()
                        {
                            var task = new Task<int>(() => default);

                            return await task;
                        }
                    }
                }";

            const string expectedMessage
                = "Asynchronous method name 'TestMethod' is not suffixed with 'Async'";

            var expectedDiagnostic = BuildExpectedResult(expectedMessage, 6, 25);

            VerifyDiagnostic(testCode, expectedDiagnostic);

            const string fixedTestCode = @"
                namespace ConsoleApplication1
                {
                    class TestClass
                    {
                        async Task<int> TestMethodAsync()
                        {
                            var task = new Task<int>(() => default);

                            return await task;
                        }
                    }
                }";
            VerifyCodeFix(testCode, fixedTestCode);
        }

        [TestMethod]
        public void Multiple_Diagnostics_For_Multiple_Asynchronous_Methods_With_No_Async_Suffix_In_Class()
        {
            const string testCode = @"
                namespace ConsoleApplication1
                {
                    class TestClass
                    {
                        async Task<int> TestMethod()
                        {
                            var task = new Task<int>(() => default);

                            return await task;
                        }

                        async Task<int> TestMethod1()
                        {
                            var task = new Task<int>(() => default);

                            return await task;
                        }
                    }
                }";

            const string expectedMessage1
                = "Asynchronous method name 'TestMethod' is not suffixed with 'Async'";

            const string expectedMessage2
                = "Asynchronous method name 'TestMethod1' is not suffixed with 'Async'";

            var expectedDiagnostics
                = new[]
                {
                    BuildExpectedResult(expectedMessage1, 6, 25),
                    BuildExpectedResult(expectedMessage2, 13, 25)
                };

            VerifyDiagnostic(testCode, expectedDiagnostics);
        }

        [TestMethod]
        public void No_Diagnostics_For_Asynchronous_Controller_Get_Action_Method_Without_Async_Suffix()
        {
            const string testCode = @"
                using System.Threading.Tasks;
                using Microsoft.AspNetCore.Mvc;

                namespace ConsoleApplication1
                {
                    public class TestController : ControllerBase
                    {
                        [HttpGet]
                        public async Task<string> Get()
                        {
                            var testTask = new Task<string>(() => string.Empty);

                            return await testTask;
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Asynchronous_Controller_Post_Action_Method_Without_Async_Suffix()
        {
            const string testCode = @"
                using System.Threading.Tasks;
                using Microsoft.AspNetCore.Mvc;

                namespace ConsoleApplication1
                {
                    public class TestController : ControllerBase
                    {
                        [HttpPost]
                        public async Task<string> Post()
                        {
                            var testTask = new Task<string>(() => string.Empty);

                            return await testTask;
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Asynchronous_Controller_Get_Action_Method_Without_HttpGet_Attribute_And_Without_Async_Suffix()
        {
            const string testCode = @"
                using System.Threading.Tasks;
                using Microsoft.AspNetCore.Mvc;

                namespace ConsoleApplication1
                {
                    public class TestController : ControllerBase
                    {
                        public async Task<string> Get()
                        {
                            var testTask = new Task<string>(() => string.Empty);

                            return await testTask;
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Diagnostic_And_Code_Fix_For_Private_Asynchronous_Method_In_Controller_Without_HttpGet_Attribute_And_Without_Async_Suffix()
        {
            const string testCode = @"
                using System.Threading.Tasks;
                using Microsoft.AspNetCore.Mvc;

                namespace ConsoleApplication1
                {
                    public class TestController : ControllerBase
                    {
                        private async Task<string> Get()
                        {
                            var testTask = new Task<string>(() => string.Empty);

                            return await testTask;
                        }
                    }
                }";

            const string expectedMessage
                = "Asynchronous method name 'Get' is not suffixed with 'Async'";

            var expectedDiagnostic = BuildExpectedResult(expectedMessage, 9, 25);

            VerifyDiagnostic(testCode, expectedDiagnostic);

            const string fixedTestCode = @"
                using System.Threading.Tasks;
                using Microsoft.AspNetCore.Mvc;

                namespace ConsoleApplication1
                {
                    public class TestController : ControllerBase
                    {
                        private async Task<string> GetAsync()
                        {
                            var testTask = new Task<string>(() => string.Empty);

                            return await testTask;
                        }
                    }
                }";
            VerifyCodeFix(testCode, fixedTestCode);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
            => new AsyncSuffixAnalyzer();

        protected override CodeFixProvider GetCSharpCodeFixProvider()
            => new AsyncSuffixFixProvider();

        private static DiagnosticResult BuildExpectedResult(string message, int lineNumber, int column)
        {
            var diagnosticResult = new DiagnosticResult
            {
                Id = AsyncSuffixAnalyzer.Id,
                Severity = AsyncSuffixAnalyzer.Severity,
                Message = message,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                    }
            };

            return diagnosticResult;
        }
    }
}