// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test;
using Microsoft.VisualStudio.Web.CodeGeneration.Templating;
using Microsoft.VisualStudio.Web.CodeGeneration.Test.Sources;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public class ConnectionStringsWriterTests
    {
        private static readonly string AppBase = "AppBase";
        private ConnectionStringsWriter GetTestObject(IFileSystem fs)
        {
            var app = new Mock<IApplicationInfo>();
            app.Setup(a => a.ApplicationBasePath).Returns(AppBase);
            return new ConnectionStringsWriter(app.Object, fs);
        }

        [Fact]
        public void AddConnectionString_Creates_App_Settings_File()
        {
            //Arrange
            var fs = new MockFileSystem();
            var testObj = GetTestObject(fs);

            //Act, test obsolete AddConnectionString
            testObj.AddConnectionString("MyDbContext", "MyDbContext-NewGuid", false);
            //test SqlServer
            testObj.AddConnectionString("MyDbContext2", "MyDbContext-SqlServerDb", DbType.SqlServer);
            //test SqlServer
            testObj.AddConnectionString("MyDbContext3", "MyDbContext-SqliteDb", DbType.SQLite);
            //test SqlServer
            testObj.AddConnectionString("MyDbContext4", "MyDbContext-CosmosDb", DbType.CosmosDb);
            //test SqlServer
            testObj.AddConnectionString("MyDbContext5", "MyDbContext-PostgresDb", DbType.Postgres);
            //Assert
            string expected = @"{
  ""ConnectionStrings"": {
    ""MyDbContext"": ""Server=(localdb)\\mssqllocaldb;Database=MyDbContext-NewGuid;Trusted_Connection=True;MultipleActiveResultSets=true"",
    ""MyDbContext2"": ""Server=(localdb)\\mssqllocaldb;Database=MyDbContext-SqlServerDb;Trusted_Connection=True;MultipleActiveResultSets=true"",
    ""MyDbContext3"": ""Data Source=MyDbContext-SqliteDb.db"",
    ""MyDbContext4"": ""AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="",
    ""MyDbContext5"": ""server=localhost;username=postgres;database=MyDbContext-PostgresDb""
  }
}";

            var appSettingsPath = Path.Combine(AppBase, "appsettings.json"); 
            fs.FileExists(appSettingsPath);
            var appsettingsstring = fs.ReadAllText(appSettingsPath);
            Assert.Equal(expected, fs.ReadAllText(appSettingsPath), ignoreCase: false, ignoreLineEndingDifferences: true);
        }

        [Theory]
        // Local Db Tests
        // Empty appsettings.json
        [InlineData(false, "{}",
                    @"{
  ""ConnectionStrings"": {
    ""MyDbContext"": ""Server=(localdb)\\mssqllocaldb;Database=MyDbContext-NewGuid;Trusted_Connection=True;MultipleActiveResultSets=true""
  }
}")]
        // File with no node for connection name
        [InlineData(false, @"{
  ""ConnectionStrings"": {
  }
}",
                    @"{
  ""ConnectionStrings"": {
    ""MyDbContext"": ""Server=(localdb)\\mssqllocaldb;Database=MyDbContext-NewGuid;Trusted_Connection=True;MultipleActiveResultSets=true""
  }
}")]
        // File with node for connection name and also existing ConnectionString property
        // modification should be skipped in this case
        [InlineData(false, @"{
  ""ConnectionStrings"": {
    ""MyDbContext"": ""SomeExistingValue""
  }
}",
                     @"{
  ""ConnectionStrings"": {
    ""MyDbContext"": ""SomeExistingValue""
  }
}")]
        // SQLite tests
        [InlineData(true, "{}",
                    @"{
  ""ConnectionStrings"": {
    ""MyDbContext"": ""Data Source=MyDbContext-NewGuid.db""
  }
}")]
        // File with no node for connection name
        [InlineData(true, @"{
  ""ConnectionStrings"": {
  }
}",
                    @"{
  ""ConnectionStrings"": {
    ""MyDbContext"": ""Data Source=MyDbContext-NewGuid.db""
  }
}")]
        // File with node for connection name and also existing ConnectionString property
        // modification should be skipped in this case
        [InlineData(true, @"{
  ""ConnectionStrings"": {
    ""MyDbContext"": ""SomeExistingValue""
  }
}",
                     @"{
  ""ConnectionStrings"": {
    ""MyDbContext"": ""SomeExistingValue""
  }
}")]
        public void AddConnectionString_Modifies_App_Settings_File_As_Required(bool useSqLite, string previousContent, string newContent)
        {
            //Arrange
            var fs = new MockFileSystem();
            var appSettingsPath = Path.Combine(AppBase, "appsettings.json");
            fs.WriteAllText(appSettingsPath, previousContent);
            var testObj = GetTestObject(fs);

            //Act
            testObj.AddConnectionString("MyDbContext", "MyDbContext-NewGuid", useSqLite);

            //Assert
            Assert.Equal(newContent, fs.ReadAllText(appSettingsPath), ignoreCase: false, ignoreLineEndingDifferences: true);
        }
    }
}
