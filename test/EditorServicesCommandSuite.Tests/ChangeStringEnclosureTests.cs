using System;
using System.Management.Automation.Language;
using System.Threading.Tasks;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using Xunit;
using static EditorServicesCommandSuite.CodeGeneration.Refactors.ChangeStringEnclosureRefactor;

namespace EditorServicesCommandSuite.Tests
{
    public class ChangeStringEnclosureTests
    {
        private readonly MockedRefactorService _refactorService = new MockedRefactorService(new ChangeStringEnclosureRefactor());

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
            static StringEnclosureInfo GetInfo(StringEnclosureType type) => type switch
            {
                StringEnclosureType.BareWord => StringEnclosureInfo.BareWord,
                StringEnclosureType.Expandable => StringEnclosureInfo.Expandable,
                StringEnclosureType.ExpandableHereString => StringEnclosureInfo.ExpandableHereString,
                StringEnclosureType.Literal => StringEnclosureInfo.Literal,
                StringEnclosureType.LiteralHereString => StringEnclosureInfo.LiteralHereString,
                _ => throw new ArgumentOutOfRangeException(nameof(type)),
            };

            return await _refactorService.GetRefactoredString(
                testString,
                context => ChangeStringEnclosureRefactor.ProcessCodeActionAsync(
                    context,
                    GetInfo(selected),
                    GetInfo(current),
                    (StringToken)context.Token.Value));
        }
    }
}
