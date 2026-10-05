namespace PolesTaxi.GatewayService.Handler.Http;

public sealed class HttpHandler
{
    private readonly Service.IService _service;

    public HttpHandler(Service.IService service)
    {
        _service = service;
    }
}
