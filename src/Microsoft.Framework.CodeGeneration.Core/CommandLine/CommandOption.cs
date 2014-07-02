// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Framework.CodeGeneration
{
    /// <summary>
    /// Specifies command line options.
    /// </summary>
    public class CommandOption
    {
        public CommandOption([NotNull]string longName, CommandOptionType optionType)
            :this(longName, optionType, null, "")
        {
        }

        public CommandOption([NotNull]string longName, CommandOptionType optionType,
            string shortName, string description)
        {
            LongName = longName;
            Description = description;
            ShortName = shortName;
            OptionType = optionType;
        }

        public string ShortName { get; private set; }
        public CommandOptionType OptionType { get; private set; }
        public string LongName { get; private set; }
        public string Description { get; private set; }

        //Utilitity function to convert to format expected by runtime command line utils.
        public string ToTemplate()
        {
            var template = "--" + LongName;
            if (string.IsNullOrEmpty(ShortName))
            {
                template += "|-" + ShortName;
            }
            return template;
        }
    }
}