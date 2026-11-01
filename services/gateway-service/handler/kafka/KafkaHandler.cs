namespace PolesTaxi.GatewayService.Handler.Kafka;

public sealed class KafkaHandler
{
    private readonly Service.IService _service;

    public KafkaHandler(Service.IService service)
    {
        _service = service;
    }
}
