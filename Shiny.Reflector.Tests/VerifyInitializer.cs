using System.Runtime.CompilerServices;

namespace Shiny.Reflector.Tests;


public class VerifyInitializer
{
    [ModuleInitializer]
    public static void Init() =>
        VerifySourceGenerators.Initialize();
}