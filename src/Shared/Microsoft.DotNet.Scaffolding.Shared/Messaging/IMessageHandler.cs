using Microsoft.DotNet.Scaffolding.Shared.Messaging;

namespace Microsoft.DotNet.Scaffolding.Shared.Messaging
{
    public interface IMessageHandler
    {
        bool HandleMessage(IMessageSender sender, Message message);
    }
}