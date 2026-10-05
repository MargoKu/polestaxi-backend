namespace PolesTaxi.WalletService.App;

/// <summary>Composition root. Wiring and servers — later phases.</summary>
public static class Run
{
    public static Task<int> ExecuteAsync(string[] args)
    {
        _ = args;
        Console.WriteLine("PolesTaxi.WalletService skeleton (phase 1: no HTTP/gRPC yet)");
        return Task.FromResult(0);
    }
}
