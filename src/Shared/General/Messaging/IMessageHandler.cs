using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Utils.Messaging
{
    public interface IMessageHandler
    {
        bool HandleMessage(IMessageSender sender, Message message);
    }
}