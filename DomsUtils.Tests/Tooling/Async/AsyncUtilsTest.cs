using DomsUtils.Tooling.Async;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace DomsUtils.Tests.Tooling.Async
{
    [TestClass]
    [TestSubject(typeof(AsyncUtils))]
    public class AsyncUtilsTest
    {
        [TestMethod]
        public void ConfigureErrorHandler_ValidHandler_SetsHandler()
        {
            var invocationCount = 0;
            var waitHandle = new ManualResetEventSlim(false);
            
            Action<Exception> handler = _ => 
            {
                invocationCount++;
                waitHandle.Set();
            };

            AsyncUtils.ConfigureErrorHandler(handler);

            // Invoke the default error handler indirectly through FireAndForget
            AsyncUtils.FireAndForget(() => throw new Exception("Test Exception"), null);
            
            // Wait for the error handler to be called with a timeout
            bool signaled = waitHandle.Wait(TimeSpan.FromSeconds(1));
            
            Assert.IsTrue(signaled, "Error handler was not called within the timeout period");
            Assert.AreEqual(1, invocationCount);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConfigureErrorHandler_NullHandler_ThrowsException()
        {
            AsyncUtils.ConfigureErrorHandler(null);
        }

        [TestMethod]
        public void RunSync_Action_ExecutesActionSynchronously()
        {
            var executed = false;
            void Action() => executed = true;

            ((Action)Action).RunSync();

            Assert.IsTrue(executed);
        }

        [TestMethod]
        public void RunSync_Func_ExecutesFunctionSynchronously()
        {
            string Func() => "TestResult";

            var result = ((Func<string>)Func).RunSync();

            Assert.AreEqual("TestResult", result);
        }

        [TestMethod]
        public async Task RunAsync_Func_RunsAsyncFunction()
        {
            async Task TestFunc()
            {
                await Task.Delay(50);
            }

            await ((Func<Task>)TestFunc).RunAsync();

            Assert.IsTrue(true); // If no exceptions occurred
        }

        [TestMethod]
        public async Task RunAsync_FuncWithResult_RunsAsyncFunctionAndReturnsResult()
        {
            async Task<int> TestFunc()
            {
                await Task.Delay(50);
                return 42;
            }

            var result = await ((Func<Task<int>>)TestFunc).RunAsync();

            Assert.AreEqual(42, result);
        }

        [TestMethod]
        public void FireAndForget_ValidFunction_ExecutesWithoutError()
        {
            async Task TestFunc() => await Task.Delay(50);

            AsyncUtils.FireAndForget(TestFunc);

            Assert.IsTrue(true); // Should pass unless an exception occurs
        }

        [TestMethod]
        public void FireAndForget_ErrorHandler_HandlesError()
        {
            Exception? capturedException = null;

            async Task TestFunc() => throw new InvalidOperationException("Simulated failure");

            void ErrorHandler(Exception ex) => capturedException = ex;

            AsyncUtils.FireAndForget(TestFunc, ErrorHandler);

            // Let the background task complete
            Thread.Sleep(100);

            Assert.IsNotNull(capturedException);
            Assert.IsInstanceOfType(capturedException, typeof(InvalidOperationException));
        }

        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task WithTimeout_TaskExceedsTimeout_ThrowsTimeoutException()
        {
            async Task<int> DelayTask()
            {
                await Task.Delay(500);
                return 42;
            }

            await DelayTask().WithTimeout(TimeSpan.FromMilliseconds(100));
        }

        [TestMethod]
        public async Task WithTimeout_TaskCompletesWithinTimeout_ReturnsValue()
        {
            async Task<int> DelayTask()
            {
                await Task.Delay(50);
                return 42;
            }

            var result = await DelayTask().WithTimeout(TimeSpan.FromMilliseconds(100));

            Assert.AreEqual(42, result);
        }

        [TestMethod]
        public async Task RetryAsync_OperationSucceeds_ReturnsResult()
        {
            var attemptCount = 0;

            async Task<int> Operation()
            {
                attemptCount++;
                return 42;
            }

            var result = await AsyncUtils.RetryAsync(Operation);

            Assert.AreEqual(42, result);
            Assert.AreEqual(1, attemptCount); // Only one attempt since it succeeds immediately
        }

        [TestMethod]
        public async Task RetryAsync_OperationFailsAndSucceeds_RetriesAndReturns()
        {
            var attemptCount = 0;

            async Task<int> Operation()
            {
                attemptCount++;
                if (attemptCount < 3)
                {
                    throw new Exception("Simulated Failure");
                }

                return 42;
            }

            var result = await AsyncUtils.RetryAsync(Operation, maxRetries: 5);

            Assert.AreEqual(42, result);
            Assert.AreEqual(3, attemptCount);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task RetryAsync_OperationAlwaysFails_ThrowsExceptionAfterRetries()
        {
            var attemptCount = 0;

            async Task<int> Operation()
            {
                attemptCount++;
                throw new Exception("Simulated Failure");
            }

            await AsyncUtils.RetryAsync(Operation, maxRetries: 3);
        }
    }
}