namespace CelesteDeployer.Services;
public sealed class DeployException : Exception
{
    public DeployException(string userMessage) : base(userMessage) { }
    public DeployException(string userMessage, Exception inner) : base(userMessage, inner) { }
}
