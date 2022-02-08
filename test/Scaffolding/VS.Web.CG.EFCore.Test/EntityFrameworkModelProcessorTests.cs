using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test
{
    
    public class EntityFrameworkModelProcessorTests
    {
        [InlineData("Server= myServerAddress; Database=myDataBase ; uid   = myUsername; Pwd=myPassword;", " 'myUsername'")]
        [InlineData("Server  =  myServerAddress ; Database=myDataBase;  User Id=myUsername ; Password=myPassword;", " 'myUsername'")]
        [InlineData("Server=myServerAddress;Database=myDataBase;uid=myUsername;Pwd=myPassword;", " 'myUsername'")]
        [InlineData("Server=myServerAddress;Database=myDataBase;uid=myUsername;", " 'myUsername'")]
        [InlineData("uid=myUsername;Server=myServerAddress;Database=myDataBase;Pwd=myPassword;", " 'myUsername'")]
        [InlineData("uid=myUsername;Pwd=myPassword;", " 'myUsername'")]
        [InlineData("uid=myUsername;", " 'myUsername'")]
        [InlineData("Server=myServerAddress;Database=myDataBase;UserId=myUsername;Password=myPassword;", " 'myUsername'")]
        [InlineData("Access denied for user 'admin'@'localhost' (using password: YES)", "")]
        [Theory]
        public void GetUsernameTest(string connectionString, string usernameValue)
        {
            Assert.Equal(usernameValue, EntityFrameworkModelProcessor.GetUsername(connectionString));
        }
    }
}
