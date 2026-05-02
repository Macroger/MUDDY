// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
namespace Muddy.Tests
{
    [TestClass]
    public class CommandParserLogicTests
    {
        [TestMethod]
        public void Parse_MoveCommand_ReturnsCorrectParts()
        {
            var parser = new CommandParser();
            var cmd = parser.Parse("move north");
            Assert.AreEqual("move", cmd.Verb);
            Assert.AreEqual("north", cmd.Arg);
        }
    }

    public class Command
    {
        public string Verb { get; set; } = string.Empty;
        public string Arg { get; set; } = string.Empty;
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
