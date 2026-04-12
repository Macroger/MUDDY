using Xunit;

namespace Muddy.Tests
{
    public class CommandParserTests
    {
        [Fact]
        public void Parse_MoveCommand_ReturnsCorrectParts()
        {
            var parser = new CommandParser();

            var cmd = parser.Parse("move north");

            Assert.Equal("move", cmd.Verb);
            Assert.Equal("north", cmd.Arg);
        }
    }

    public class Command
    {
        public string Verb { get; set; }
        public string Arg { get; set; }
    }

    public class CommandParser
    {
        public Command Parse(string input)
        {
            var parts = input.Split(' ');
            return new Command
            {
                Verb = parts[0],
                Arg = parts.Length > 1 ? parts[1] : ""
            };
        }
    }
}