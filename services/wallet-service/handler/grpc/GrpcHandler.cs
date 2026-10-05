namespace PolesTaxi.WalletService.Handler.Grpc;

public sealed class GrpcHandler
{
    private readonly Service.IService _service;

    public GrpcHandler(Service.IService service)
    {
        _service = service;
    }
}
