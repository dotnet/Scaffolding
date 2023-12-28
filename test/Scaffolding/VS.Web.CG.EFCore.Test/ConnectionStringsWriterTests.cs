// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
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

            //test SqlServer
            testObj.AddConnectionString("MyDbContext2", "MyDbContext-SqlServerDb", DbProvider.SqlServer);
            //test SqlServer
            testObj.AddConnectionString("MyDbContext3", "MyDbContext-SqliteDb", DbProvider.SQLite);
            //test SqlServer
            testObj.AddConnectionString("MyDbContext4", "MyDbContext-CosmosDb", DbProvider.CosmosDb);
            //test SqlServer
            testObj.AddConnectionString("MyDbContext5", "MyDbContext-PostgresDb", DbProvider.Postgres);
            //Assert
            string expected = @"{
  ""ConnectionStrings"": {
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
        [InlineData(DbProvider.SqlServer, "{}",
                    @"{
  ""ConnectionStrings"": {
    ""MyDbContext"": ""Server=(localdb)\\mssqllocaldb;Database=MyDbContext-NewGuid;Trusted_Connection=True;MultipleActiveResultSets=true""
  }
}")]
        // File with no node for connection name
        [InlineData(DbProvider.SqlServer, @"{
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
        [InlineData(DbProvider.SqlServer, @"{
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
        [InlineData(DbProvider.SQLite, "{}",
                    @"{
  ""ConnectionStrings"": {
    ""MyDbContext"": ""Data Source=MyDbContext-NewGuid.db""
  }
}")]
        // File with no node for connection name
        [InlineData(DbProvider.SQLite, @"{
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
        [InlineData(DbProvider.SQLite, @"{
  ""ConnectionStrings"": {
    ""MyDbContext"": ""SomeExistingValue""
  }
}",
                     @"{
  ""ConnectionStrings"": {
    ""MyDbContext"": ""SomeExistingValue""
  }
}")]
        public void AddConnectionString_Modifies_App_Settings_File_As_Required(DbProvider dbProvider, string previousContent, string newContent)
        {
            //Arrange
            var fs = new MockFileSystem();
            var appSettingsPath = Path.Combine(AppBase, "appsettings.json");
            fs.WriteAllText(appSettingsPath, previousContent);
            var testObj = GetTestObject(fs);

            //Act
            testObj.AddConnectionString("MyDbContext", "MyDbContext-NewGuid", dbProvider);

            //Assert
            Assert.Equal(newContent, fs.ReadAllText(appSettingsPath), ignoreCase: false, ignoreLineEndingDifferences: true);
        }
    }
}
