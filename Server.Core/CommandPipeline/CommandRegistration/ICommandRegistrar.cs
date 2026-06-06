namespace Server.Core.CommandPipeline.CommandRegistration

{
    public interface ICommandRegistrar
    {
        void Register(ICommandRouter router);
    }
}

