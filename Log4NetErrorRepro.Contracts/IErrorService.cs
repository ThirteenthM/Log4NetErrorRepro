namespace Log4NetErrorRepro.Contracts
{
    public interface IErrorService
    {
        string Ping();

        RemoteResponse Execute(Scenario scenario);
    }
}
