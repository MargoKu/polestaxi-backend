namespace PolesTaxi.AnalyticService.Service;

public interface IService
{
}

public sealed class Service : IService
{
    private readonly Repository.IRepository _repository;
    private readonly Gateway.IGateway _gateway;

    public Service(Repository.IRepository repository, Gateway.IGateway gateway)
    {
        _repository = repository;
        _gateway = gateway;
    }
}
