using System.Collections.Generic;
using System.Threading.Tasks;
using HardDev.AsTask;
using HardDev.AsTask.Awaiters;
using HardDev.AsTask.Context;
using HardDev.AsTask.TaskHelpers;
using static System.Console;

namespace AsTaskExample
{
    public static class QAsTaskConsole
    {
        private static bool _isShutdown;

        public static void Main() => QAsyncContext.Run(MainAsync, "MainContext");
        public static void Shutdown() => _isShutdown = true;

        private static async Task MainAsync()
        {
            WriteLine("Initialize AsTask and capture the context of the main thread.");
            QAsTask.Initialize();

            WriteLine("Assign an exception handler.");
            QAsTask.SetExceptionHandler(task =>
            {
                Error.WriteLine(task.Exception != null
                    ? $"[ExceptionHandler] {task.Exception.GetBaseException().Message}"
                    : $"[ExceptionHandler] Unhandled exception in task {task}");
            });

            WriteLine($"Print to the console information about the current thread: {QAsTask.WhereAmI()}");

            WriteLine("Switch to the background thread.");
            await QAsTask.ToBackgroundThread();

            WriteLine($"Now we get information about the context of the background thread: {QAsTask.WhereAmI()}");

            WriteLine("Back switch to the main thread.");
            await QAsTask.ToMainThread();

            WriteLine("We call the asynchronous methods, which performs the heavy work...");
            var tasks = new List<Task<long>>();
            for (var i = 1; i <= 10; i++)
            {
                tasks.Add(FindPrimeNumberAsync(20000 * i));
            }

            WriteLine("Asynchronously waiting for the execution of tasks.");
            tasks.WhenAll().ContinueWith(async task =>
            {
                await QAsTask.ToMainThread();

                WriteLine("This is the result of our calculations:");

                foreach (var val in task.Result)
                    WriteLine(val);

                Shutdown();
            }).ExceptionHandlerWR();

            if (QAsTask.IsMainThread())
                WriteLine("hmm, we're still in the main thread!:)");

            WriteLine("The life cycle of a console application runs on the main thread.");
            while (!_isShutdown)
            {
                WriteLine("We are doing something...");
                await 1000; // Each iteration waits asynchronously for 1000 ms
            }

            WriteLine("Shutdown...");
            for (var i = 5; i > 0; i--)
            {
                WriteLine($"Shutdown through {i}s");
                await 1000;
            }
        }

        private static async Task<long> FindPrimeNumberAsync(int n)
        {
            // Here we switch to a normal thread pool to do the heavy work...
            await QAsTask.ToNormalThreadPool();

            var count = 0;
            long a = 2;
            while (count < n)
            {
                long b = 2;
                var prime = 1;
                while (b * b <= a)
                {
                    if (a % b == 0)
                    {
                        prime = 0;
                        break;
                    }

                    b++;
                }

                if (prime > 0)
                {
                    count++;
                }

                a++;
            }

            return --a;
        }
    }
}