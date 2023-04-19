// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.VisualStudio.Web.CodeGeneration.Core;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public class ActionInvoker
    {
        public ActionInvoker(ActionDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            ActionDescriptor = descriptor;
        }

        public ActionDescriptor ActionDescriptor
        {
            get;
            private set;
        }

        public void Execute(string[] args)
        {
            var app = new CommandLineApplication();

            app.Command(ActionDescriptor.Generator.Name, c =>
            {
                c.HelpOption("--help|-h|-?");
                BuildCommandLine(c);
            });

            app.Execute(args);
        }

        internal void BuildCommandLine(CommandLineApplication command)
        {
            foreach (var param in ActionDescriptor.Parameters)
            {
                param.AddCommandLineParameterTo(command);
            }

            command.Invoke = () =>
            {
                object modelInstance;
                try
                {
                    modelInstance = Activator.CreateInstance(ActionDescriptor.ActionModel);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(string.Format(MessageStrings.ModelCreationFailed, ex.Message));
                }

                foreach (var param in ActionDescriptor.Parameters)
                {
                    param.Property.SetValue(modelInstance, param.Value);
                }

                var codeGeneratorInstance = ActionDescriptor.Generator.CodeGeneratorInstance;

                try
                {
                    var result = ActionDescriptor.ActionMethod.Invoke(codeGeneratorInstance, new[] { modelInstance });

                    if (result is Task)
                    {
                        ((Task)result).Wait();
                    }
                }
                catch (Exception ex)
                {
                    while (ex is TargetInvocationException)
                    {
                        ex = ex.InnerException;
                    }

                    if (ex is AggregateException)
                    {
                        ex = ex.GetBaseException();
                    }

                    throw new InvalidOperationException(ex.Message, ex);
                }

                return 0;
            };
        }
    }
}
