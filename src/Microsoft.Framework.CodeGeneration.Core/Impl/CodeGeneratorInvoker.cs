// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


namespace Microsoft.Framework.CodeGeneration
{
    //Is this class really necessary?
    public class CodeGeneratorInvoker
    {
        public CodeGeneratorInvoker([NotNull]ICodeGeneratorDescriptor descriptor)
        {
            CodeGeneratorDescriptor = descriptor;
        }

        public ICodeGeneratorDescriptor CodeGeneratorDescriptor
        {
            get;
            private set;
        }

        public void Execute(string[] args)
        {
            var actionInvoker = new ActionInvoker(CodeGeneratorDescriptor.CodeGeneratorAction);
            actionInvoker.Execute(args);
        }
    }
}