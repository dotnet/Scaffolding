// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity
{
    /// <summary>
    /// Identity helper with static methods to help IdentityGenerator and BlazorIdentityGenerator with scaffoding.
    /// </summary>
    internal static class IdentityHelper
    {
        internal static bool IsTypeDerivedFromIdentityUser(Type type)
        {
            var parentType = type.BaseType;
            while (parentType != null && parentType != typeof(object))
            {
                if (parentType.FullName == "Microsoft.AspNetCore.Identity.IdentityUser"
                    && parentType.Assembly.GetName().Name == "Microsoft.Extensions.Identity.Stores")
                {
                    return true;
                }

                parentType = parentType.BaseType;
            }

            return false;
        }

        internal static string GetNamespaceFromTypeName(string dbContext)
        {
            if (dbContext.LastIndexOf('.') == -1)
            {
                return null;
            }

            return dbContext.Substring(0, dbContext.LastIndexOf('.'));
        }

        internal static string GetClassNameFromTypeName(string dbContext)
        {
            var lastIndexOfDot = dbContext.LastIndexOf('.');
            if (lastIndexOfDot == -1)
            {
                return dbContext;
            }

            return dbContext.Substring(lastIndexOfDot + 1);
        }

        internal static void ValidateExistingDbContext(Type existingDbContext, string userclassName)
        {
            var errorStrings = new List<string>();

            // Validate that the dbContext inherits from IdentityDbContext.
            bool foundValidParentDbContextClass = IsTypeDerivedFromIdentityDbContext(existingDbContext);

            if (!foundValidParentDbContextClass)
            {
                errorStrings.Add(
                    string.Format(MessageStrings.DbContextNeedsToInheritFromIdentityContextMessage,
                        existingDbContext.Name,
                        "Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext"));
            }

            // Validate that the `--userClass` parameter is not passed.
            if (!string.IsNullOrEmpty(userclassName))
            {
                errorStrings.Add(MessageStrings.UserClassAndDbContextCannotBeSpecifiedTogether);
            }

            if (errorStrings.Count != 0)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, errorStrings));
            }
        }

        internal static Type FindUserTypeFromDbContext(Type existingDbContext)
        {
            var usersProperty = existingDbContext.GetProperties()
                .FirstOrDefault(p => p.Name == "Users");

            if (usersProperty == null ||
                !usersProperty.PropertyType.IsGenericType ||
                usersProperty.PropertyType.GetGenericArguments().Count() != 1)
            {
                // The IdentityDbContext has DbSet<UserType> Users property.
                // The only case this would happen is if the user hides the inherited property.
                throw new InvalidOperationException(
                    string.Format(MessageStrings.UserClassCouldNotBeDetermined,
                        existingDbContext.Name));
            }

            return usersProperty.PropertyType.GetGenericArguments().First();
        }

        internal static string GetDefaultDbContextName(string projectName)
        {
            var defaultDbContextName = $"{projectName}IdentityDbContext";

            if (!SyntaxFacts.IsValidIdentifier(defaultDbContextName))
            {
                defaultDbContextName = "IdentityDataContext";
            }

            return defaultDbContextName;
        }

        internal static bool IsTypeDerivedFromIdentityDbContext(Type type)
        {
            var parentType = type.BaseType;
            while (parentType != null && parentType != typeof(object))
            {
                // There are multiple variations of IdentityDbContext classes.
                // So have to use StartsWith instead of comparing names.
                // 1. IdentityDbContext
                // 2. IdentityDbContext <TUser, TRole, TKey>
                // 3. IdentityDbContext <TUser, TRole, string>
                // 4. IdentityDbContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken> etc.
                if (parentType.Name.StartsWith("IdentityDbContext")
                    && parentType.Namespace == "Microsoft.AspNetCore.Identity.EntityFrameworkCore"
                    && parentType.Assembly.GetName().Name == "Microsoft.AspNetCore.Identity.EntityFrameworkCore")
                {
                    return true;
                }

                parentType = parentType.BaseType;
            }

            return false;
        }

        internal static void ValidateExistingUserType(Type existingUser)
        {
            var errorStrings = new List<string>();

            // Validate that the user type inherits from IdentityUser
            bool foundValidParentDbContextClass = IsTypeDerivedFromIdentityUser(existingUser);

            if (!foundValidParentDbContextClass)
            {
                errorStrings.Add(
                    string.Format(MessageStrings.DbContextNeedsToInheritFromIdentityContextMessage,
                        existingUser.Name,
                        "Microsoft.AspNetCore.Identity.IdentityUser"));
            }

            if (errorStrings.Count != 0)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, errorStrings));
            }
        }

        internal static readonly char[] SemicolonSeparator = new char[] { ';' };
    }
}
