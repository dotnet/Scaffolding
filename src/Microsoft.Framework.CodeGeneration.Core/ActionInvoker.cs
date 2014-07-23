// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime.Common.CommandLine;

namespace Microsoft.Framework.CodeGeneration
{
    public class ActionInvoker
    {
        private List<ParameterDescriptor> _parameters;

        public ActionInvoker([NotNull]ActionDescriptor descriptor)
        {
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
                c.HelpOption("-h|-?|--help");
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

            // ToDo: Exceptions from GenerateCode are not really caught here
            // when the GenerateCode method is async.
            command.Invoke = () =>
            {
                object modelInstance;
                try
                {
                    modelInstance = Activator.CreateInstance(ActionDescriptor.ActionModel);
                }
                catch (Exception ex)
                {
                    throw new Exception("There was an error attempting to create an instace of model for GenerateCode method: " + ex.Message);
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
                        ((Task)result).ContinueWith(t =>
                        {
                            if (t.IsFaulted)
                            {
                                throw t.Exception;
                            }
                        }).Wait(); //Review: Is this bad? Command line mode does not allow async delegates - are there better ways?
                    }
                }
                catch (Exception ex)
                {
                    // We are ignoring if there are multiple exceptions with an AggregateException but
                    // mostly that's ok in our scenarios as our current implementations are not using multiple exceptions.
                    while (ex is TargetInvocationException || ex is AggregateException)
                    {
                        ex = ex.InnerException;
                    }
                    
                    throw new Exception("There was an error running the GenerateCode method: " + ex.Message);
                }

                return 0;
            };
        }
    }
}