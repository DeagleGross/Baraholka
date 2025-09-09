// Original interface without DIM
public interface IProcessor
{
    string Process(string input);

    // NEW: Default Interface Method added
    string Process(ReadOnlySpan<char> input)
    {
        // Default implementation that converts span to string
        return Process(input.ToString());
    }
}

// User's implementation
public class MyProcessor : IProcessor
{
    public string Process(string input)
    {
        return $"Processed: {input}";
    }

    // User added their own method with the same signature
    // that they plan to add to the interface later
    public string Process(ReadOnlySpan<char> input)
    {
        return $"Span Processed: {input}";
    }
}

class Program
{
    static void Main()
    {
        var processor = new MyProcessor();

        // BREAKING CHANGE: This now calls the DIM instead of user's method!
        IProcessor iProcessor = processor;
        string result = iProcessor.Process("test".AsSpan());
        Console.WriteLine(result); // Output: "Processed: test" (DIM called user's string method)

        // Direct call still works as before
        string directResult = processor.Process("test".AsSpan());
        Console.WriteLine(directResult); // Output: "Span Processed: test"
    }
}