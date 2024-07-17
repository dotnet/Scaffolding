using System.Collections.Generic;
using Xunit;

namespace Microsoft.DotNet.Scaffolding.ComponentModel.Tests
{
    public class CommandInfoTests
    {
        private static List<CommandInfo> CommandInfos;
        private static Parameter ValidParameter;
        private static Parameter PartialParameter;
        private static Parameter PartialParameter2;
        private static CommandInfo ValidCommandInfo;
        private static CommandInfo PartialCommandInfo;
        private static CommandInfo PartialCommandInfo2;

        public CommandInfoTests()
        {
            ValidParameter = new()
            {
                Name = "parameter",
                DisplayName = "Param Display Name",
                Required = true,
                Description = "Parameter description",
                Type = CliTypes.String
            };

            PartialParameter = new()
            {
                Name = string.Empty,
                DisplayName = "Param Display Name 2",
                Required = true,
                Type = CliTypes.Int
            };

            PartialParameter2 = new()
            {
                Name = "parameter2",
                DisplayName = "Param Display Name 3",
                Required = true,
                Type = CliTypes.Int,
                Description = null,
                PickerType = InteractivePickerType.None
            };

            ValidCommandInfo = new()
            {
                Name = "command1",
                DisplayName = "Command 1",
                DisplayCategory = "General",
                Description = "Description 1",
                Parameters = [ValidParameter]
            };

            PartialCommandInfo = new()
            {
                Name = "command2",
                DisplayName = "Command 2",
                DisplayCategory = "General",
                Parameters =
                [
                    ValidParameter, PartialParameter
                ]
            };

            PartialCommandInfo2 = new()
            {
                Name = "command3",
                DisplayName = "Command 3",
                DisplayCategory = "General",
                Description = null,
                Parameters =
                [
                    ValidParameter, PartialParameter, PartialParameter2
                ]
            };

            CommandInfos =
            [
                ValidCommandInfo, PartialCommandInfo, PartialCommandInfo2
            ];
        }
    
        [Fact]
        public void SerialzationAndDeserializationTests()
        {
            var serializedJson = System.Text.Json.JsonSerializer.Serialize(CommandInfos);
            Assert.False(string.IsNullOrEmpty(serializedJson));
            Assert.True(serializedJson.Contains("\"Name\":\"command1\"", System.StringComparison.OrdinalIgnoreCase));
            Assert.True(serializedJson.Contains("\"Name\":\"command2\"", System.StringComparison.OrdinalIgnoreCase));
            Assert.True(serializedJson.Contains("\"Name\":\"command3\"", System.StringComparison.OrdinalIgnoreCase));

            var deserializedJson = System.Text.Json.JsonSerializer.Deserialize<List<CommandInfo>>(serializedJson);
            Assert.NotNull(deserializedJson);
            Assert.True(deserializedJson.Count == 3);

            var validCommandInfoDeserialized = deserializedJson[0];
            var partialCommandInfoDeserialized = deserializedJson[1];
            var partialCommandInfo2Deserialized = deserializedJson[2];
            Assert.NotNull(validCommandInfoDeserialized);
            Assert.NotNull(partialCommandInfoDeserialized);
            Assert.NotNull(partialCommandInfo2Deserialized);

            Assert.Contains(validCommandInfoDeserialized.Parameters, x => x.Name.Equals("parameter"));
            Assert.Contains(partialCommandInfoDeserialized.Parameters, x => x.Name.Equals(PartialParameter.Name));
            Assert.Contains(partialCommandInfo2Deserialized.Parameters, x => x.Description is null);
            Assert.Contains(partialCommandInfo2Deserialized.Parameters, x => x.PickerType is InteractivePickerType.None);
        }
    }
}
