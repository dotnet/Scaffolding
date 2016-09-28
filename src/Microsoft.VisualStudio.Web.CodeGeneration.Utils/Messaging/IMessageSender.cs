using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Utils.Messaging
{
    public interface IMessageSender
    {
        bool Send(Message message);
    }
}
