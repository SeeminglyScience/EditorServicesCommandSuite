using System.Management.Automation.Language;
using System.Threading.Tasks;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using Xunit;

namespace EditorServicesCommandSuite.Tests
{
    public class ChangeStringEnclosureTests
    {
        [Theory]
        [InlineData("\"testing\"", (int)StringEnclosureType.Expandable)]
        [InlineData("'testing'", (int)StringEnclosureType.Literal)]
        [InlineData("@\"\ntesting\n\"@", (int)StringEnclosureType.ExpandableHereString)]
        [InlineData("@'\ntes{{c}}ting\n'@", (int)StringEnclosureType.LiteralHereString)]
        public async void CanChangeToBareWord(string value, int currentType)
        {
            Assert.Matches(
                @"\r?\n?testing\r?\n?",
                await GetRefactoredTextAsync(
                    value,
                    (StringEnclosureType)currentType,
                    StringEnclosureType.BareWord));
        }

        [Theory]
        [InlineData("\"testing\"", (int)StringEnclosureType.Expandable)]
        [InlineData("@\"\ntesting\n\"@", (int)StringEnclosureType.ExpandableHereString)]
        [InlineData("@'\ntesting\n'@", (int)StringEnclosureType.LiteralHereString)]
        public async void CanChangeToLiteral(string value, int currentType)
        {
            Assert.Matches(
                @"'\r?\n?testing\r?\n?'",
                await GetRefactoredTextAsync(
                    value,
                    (StringEnclosureType)currentType,
                    StringEnclosureType.Literal));
        }

        [Theory]
        [InlineData("\"testing\"", (int)StringEnclosureType.Expandable)]
        [InlineData("'testing'", (int)StringEnclosureType.Literal)]
        [InlineData("@\"\ntesting\n\"@", (int)StringEnclosureType.LiteralHereString)]
        public async void CanChangeToLiteralHereString(string value, int currentType)
        {
            Assert.Matches(
                @"@'\r?\ntesting\r?\n'@",
                await GetRefactoredTextAsync(
                    value,
                    (StringEnclosureType)currentType,
                    StringEnclosureType.LiteralHereString));
        }

        [Theory]
        [InlineData("\"testing\"", (int)StringEnclosureType.Expandable)]
        [InlineData("'testing'", (int)StringEnclosureType.Literal)]
        [InlineData("@'\ntesting\n'@", (int)StringEnclosureType.LiteralHereString)]
        public async void CanChangeToExpandableHereString(string value, int currentType)
        {
            Assert.Matches(
                @"@""\r?\ntesting\r?\n""@",
                await GetRefactoredTextAsync(
                    value,
                    (StringEnclosureType)currentType,
                    StringEnclosureType.ExpandableHereString));
        }

        private async Task<string> GetRefactoredTextAsync(
            string testString,
            StringEnclosureType current,
            StringEnclosureType selected)
        {
            return await MockContext.GetRefactoredTextAsync(
                testString,
                context => ChangeStringEnclosureRefactor.GetEdits(
                    context.RootAst,
                    (StringToken)context.Token.Value,
                    current,
                    selected));
        }
    }
}
