using System.Globalization;

namespace MyPlants.Services;

/// <summary>
/// Service de logging pour faciliter le débogage en développement et production.
/// </summary>
public class LoggingService
{
    public static void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        Console.WriteLine($"[{timestamp}] {message}");
    }

    public static void LogError(string message, Exception? ex = null)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        Console.Error.WriteLine($"[{timestamp}] ❌ {message}");
        if (ex != null)
        {
            Console.Error.WriteLine($"[{timestamp}]    Exception: {ex.GetType().Name}");
            Console.Error.WriteLine($"[{timestamp}]    Message: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.Error.WriteLine($"[{timestamp}]    Inner: {ex.InnerException.Message}");
            }
        }
    }

    public static void LogWarning(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        Console.WriteLine($"[{timestamp}] ⚠️  {message}");
    }

    public static void LogInfo(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        Console.WriteLine($"[{timestamp}] ℹ️  {message}");
    }
}
