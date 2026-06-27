using System.IO;
using System.Runtime.CompilerServices;

namespace backend.Tests.TestInfrastructure
{
    internal static class TestOutputSetup
    {
        // Reduce noise from Console.WriteLine calls in app code during test execution.
        [ModuleInitializer]
        internal static void Initialize()
        {
            Console.SetOut(TextWriter.Null);
            Console.SetError(TextWriter.Null);
        }
    }
}
