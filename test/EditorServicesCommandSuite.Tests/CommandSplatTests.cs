using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using EditorServicesCommandSuite.Language;
using Xunit;

namespace EditorServicesCommandSuite.Tests
{
    public class CommandSplatTests
    {
        private readonly MockedRefactorService _refactorService = new MockedRefactorService(new CommandSplatRefactor(null));

        [Fact]
        public async void DoesNothingWithNoParameters()
        {
            Assert.Equal("Get-ChildItem", await GetRefactoredTextAsync("Get-ChildItem"));
        }

        [Fact]
        public async void HandlesSwitchParameters()
        {
            Assert.Equal(
                TestBuilder.Create()
                    .Lines("$splat = @{")
                    .Lines("    Recurse = $true")
                    .Lines("}")
                    .Texts("Get-ChildItem @splat"),
                await GetRefactoredTextAsync("Get-ChildItem -Recurse"));
        }

        [Fact]
        public async void HandlesQuoting()
        {
            Assert.Equal(
                TestBuilder.Create()
                    .Lines("$splat = @{")
                    .Lines("    Path = './myPath'")
                    .Lines("}")
                    .Texts("Get-ChildItem @splat"),
                await GetRefactoredTextAsync("Get-ChildItem -Path ./myPath"));
        }

        [Fact]
        public async void HandlesQuotingWithExpressions()
        {
            Assert.Equal(
                TestBuilder.Create()
                    .Lines(@"$splat = @{")
                    .Lines(@"    Path = ""./myPath$c""")
                    .Lines(@"}")
                    .Texts(@"Get-ChildItem @splat"),
                await GetRefactoredTextAsync("Get-ChildItem -Path ./myPath$c"));
        }

        [Theory]
        [InlineData("$true", "$true")]
        [InlineData("0", "0")]
        [InlineData("Test", "'Test'")]
        [InlineData("Test, Test2", "'Test', 'Test2'")]
        [InlineData("Test$var", "\"Test$var\"")]
        [InlineData("$test.InvokeCommand", "$test.InvokeCommand")]
        [InlineData("$test.InvokeCommand()", "$test.InvokeCommand()")]
        public async void HandlesVariousCommandElementTypes(
            string commandElement,
            string hashtableValue)
        {
            Assert.Equal(
                TestBuilder.Create()
                    .Lines(@"$splat = @{")
                    .Lines($"    Path = {hashtableValue}")
                    .Lines(@"}")
                    .Texts("Get-ChildItem @splat"),
                await GetRefactoredTextAsync($"Get-ChildItem -Path {commandElement}"));
        }

        [Fact]
        public async void AllParameters_HandlesParameterSet_OneParamSet()
        {
            Assert.Equal(
                TestBuilder.Create()
                    .Lines("$splat = @{")
                    .Lines("    Attachments = $stringArrayAttachments")
                    .Lines("    Bcc = $stringArrayBcc")
                    .Lines("    Body = $stringBody")
                    .Lines("    BodyAsHtml = $switchParameterBodyAsHtml")
                    .Lines("    Encoding = $encodingEncoding")
                    .Lines("    Cc = $stringArrayCc")
                    .Lines("    DeliveryNotificationOption = $deliveryNotificationOptionsDeliveryNotificationOption")
                    .Lines("    From = $mandatoryStringFrom")
                    .Lines("    SmtpServer = $stringSmtpServer")
                    .Lines("    Priority = $mailPriorityPriority")
                    .Lines("    ReplyTo = $stringArrayReplyTo")
                    .Lines("    Subject = $stringSubject")
                    .Lines("    To = $mandatoryStringArrayTo")
                    .Lines("    Credential = $pSCredentialCredential")
                    .Lines("    UseSsl = $switchParameterUseSsl")
                    .Lines("    Port = $int32Port")
                    .Lines("}")
                    .Texts("Send-MailMessage @splat"),
                await GetRefactoredTextAsync(
                    "Send-MailMessage",
                    allParameters: true));
        }

        [Fact]
        public async void AllParameters_HandlesParameterSet_OneParamSet_OneParamGiven()
        {
            Assert.Equal(
                TestBuilder.Create()
                    .Lines("$splat = @{")
                    .Lines("    From = 'someone@someplace.com'")
                    .Lines("    Attachments = $stringArrayAttachments")
                    .Lines("    Bcc = $stringArrayBcc")
                    .Lines("    Body = $stringBody")
                    .Lines("    BodyAsHtml = $switchParameterBodyAsHtml")
                    .Lines("    Encoding = $encodingEncoding")
                    .Lines("    Cc = $stringArrayCc")
                    .Lines("    DeliveryNotificationOption = $deliveryNotificationOptionsDeliveryNotificationOption")
                    .Lines("    SmtpServer = $stringSmtpServer")
                    .Lines("    Priority = $mailPriorityPriority")
                    .Lines("    ReplyTo = $stringArrayReplyTo")
                    .Lines("    Subject = $stringSubject")
                    .Lines("    To = $mandatoryStringArrayTo")
                    .Lines("    Credential = $pSCredentialCredential")
                    .Lines("    UseSsl = $switchParameterUseSsl")
                    .Lines("    Port = $int32Port")
                    .Lines("}")
                    .Texts("Send-MailMessage @splat"),
                await GetRefactoredTextAsync(
                    "Send-MailMessage -From 'someone@someplace.com'",
                    allParameters: true));
        }

        [Fact]
        public async void AllParameters_HandlesParameterSet_MultipleParamSets()
        {
            // Should result in default set.
            Assert.Equal(
                TestBuilder.Create()
                    .Lines("$splat = @{")
                    .Lines("    Path = $mandatoryStringArrayPath")
                    .Lines("    Algorithm = $stringAlgorithm")
                    .Lines("}")
                    .Texts("Get-FileHash @splat"),
                await GetRefactoredTextAsync(
                    "Get-FileHash",
                    allParameters: true));
        }

        [Fact(Skip = "After pipeline changes Get-ChildItem picks up all dynamic parameters. Need to pick a different command to test.")]
        public async void AllParameters_HandlesParameterSet_MultipleParamSets_NonDeterminantParamGiven()
        {
            // Multiple parameter set cmdlet, with a parameter from the '__AllParameterSets' set.
            // Should result in default parameter set.
            Assert.Equal(
                TestBuilder.Create()
                    .Lines("$splat = @{")
                    .Lines("    Name = $true")
                    .Lines("    Path = $stringArrayPath")
                    .Lines("    Filter = $stringFilter")
                    .Lines("    Include = $stringArrayInclude")
                    .Lines("    Exclude = $stringArrayExclude")
                    .Lines("    Recurse = $switchParameterRecurse")
                    .Lines("    Depth = $uInt32Depth")
                    .Lines("    Force = $switchParameterForce")
                    .Lines("}")
                    .Texts("Get-ChildItem @splat"),
                await GetRefactoredTextAsync(
                    "Get-ChildItem -Name",
                    allParameters: true));
        }

        [Fact(Skip = "mkdir function is Windows only")]
        public async void AllParameters_HandlesParameterSet_MultipleParamSets_ParamFromDefaultSetGiven()
        {
            // Multiple parameter set cmdlet with parameter from default set,
            // should result in splat of default parameter set.
            Assert.Equal(
                TestBuilder.Create()
                    .Lines("$splat = @{")
                    .Lines("    Path = 'c:\\test\\test'")
                    .Lines("    Value = $objectValue")
                    .Lines("    Force = $switchParameterForce")
                    .Lines("    Credential = $pSCredentialCredential")
                    .Lines("}")
                    .Texts("mkdir @splat"),
                await GetRefactoredTextAsync(
                    "mkdir -Path 'c:\\test\\test'",
                    allParameters: true));
        }

        [Fact(Skip = "mkdir function is Windows only")]
        public async void AllParameters_HandlesParameterSet_MultipleParamSets_NonDefaultParamSet()
        {
            // Multiple parameter set cmdlet, with a parameter from one of the non-default parameter
            // sets. Should result in a splat of that set.
            Assert.Equal(
                TestBuilder.Create()
                    .Lines("$splat = @{")
                    .Lines("    Name = 'somename'")
                    .Lines("    Path = $stringArrayPath")
                    .Lines("    Value = $objectValue")
                    .Lines("    Force = $switchParameterForce")
                    .Lines("    Credential = $pSCredentialCredential")
                    .Lines("}")
                    .Texts("mkdir @splat"),
                await GetRefactoredTextAsync(
                    "mkdir -Name 'somename'",
                    allParameters: true));
        }

        [Fact]
        public async void AllParameters_HandlesParameterSet_GivenUnresolvableParameter()
        {
            // Single parameter set cmdlet with an incorrect parameter. Should result in a splat with
            // all parameters, and the incorrect parameter should remain on the same line as the cmdlet.
            Assert.Equal(
                TestBuilder.Create()
                    .Lines("$splat = @{")
                    .Lines("    InputObject = $pSObjectInputObject")
                    .Lines("    Expression = $mandatoryScriptBlockExpression")
                    .Lines("    ThisIsAnInvalidParameter = 'somevalue'")
                    .Lines("}")
                    .Texts("Measure-Command @splat"),
                await GetRefactoredTextAsync(
                    "Measure-Command -ThisIsAnInvalidParameter 'somevalue'",
                    allParameters: true));

            // A warning should also be displayed. This is not tested here.
        }

        [Fact]
        public async void AllParameters_HandlesNoParameters_GivenUnresolvableParameter()
        {
            // Test single parameter set cmdlet, with no parameters other than the common
            // parameters, with an invalid parameter.
            Assert.Equal(
                TestBuilder.Create()
                    .Lines("$splat = @{")
                    .Lines("    ThisIsAnInvalidParameter = 'somevalue'")
                    .Lines("}")
                    .Texts("Get-Host @splat"),
                await GetRefactoredTextAsync(
                    "Get-Host -ThisIsAnInvalidParameter 'somevalue'",
                    allParameters: true));
        }

        [Fact]
        public async void AllParameters_HandlesParameterSet_AmbiguousParameterSet()
        {
            // Multiple parameter set cmdlet, given parameters from two seperate sets, should result
            // in an ambigous parameterset exception.
            await Assert.ThrowsAsync<PSInvalidOperationException>(
                () =>
                    GetRefactoredTextAsync(
                        "Get-FileHash -LiteralPath 'c:\\test\\test' -Path 'c:\\test\\test'",
                        allParameters: true));
        }

        [Fact]
        public async void MandatoryParameters_HandlesParameterSet_OneParamSet()
        {
            Assert.Equal(
                TestBuilder.Create()
                    .Lines("$splat = @{")
                    .Lines("    From = $mandatoryStringFrom")
                    .Lines("    To = $mandatoryStringArrayTo")
                    .Lines("}")
                    .Texts("Send-MailMessage @splat"),
                await GetRefactoredTextAsync(
                    "Send-MailMessage",
                    mandatoryParameters: true));
        }

        [Fact]
        public async void MandatoryParameters_HandlesParameterSet_OneParamSet_OneParamGiven()
        {
            Assert.Equal(
                TestBuilder.Create()
                    .Lines("$splat = @{")
                    .Lines("    From = 'someone@someplace.com'")
                    .Lines("    To = $mandatoryStringArrayTo")
                    .Lines("}")
                    .Texts("Send-MailMessage @splat"),
                await GetRefactoredTextAsync(
                    "Send-MailMessage -From 'someone@someplace.com'",
                    mandatoryParameters: true));
        }

        [Fact]
        public async void MandatoryParameters_HandlesParameterSet_MultipleParamSets()
        {
            // Should result in default set.
            Assert.Equal(
                TestBuilder.Create()
                    .Lines("$splat = @{")
                    .Lines("    Path = $mandatoryStringArrayPath")
                    .Lines("}")
                    .Texts("Get-FileHash @splat"),
                await GetRefactoredTextAsync(
                    "Get-FileHash",
                    mandatoryParameters: true));
        }

        [Fact]
        public async void MandatoryParameters_HandlesParameterSet_MultipleParamSets_NonDeterminantParamGiven()
        {
            // Multiple parameter set cmdlet, with a parameter from the '__AllParameterSets' set.
            // Should result in default parameter set.
            Assert.Equal(
                TestBuilder.Create()
                    .Lines("$splat = @{")
                    .Lines("    Name = $true")
                    .Lines("}")
                    .Texts("Get-ChildItem @splat"),
                await GetRefactoredTextAsync(
                    "Get-ChildItem -Name",
                    mandatoryParameters: true));
        }

        [Fact(Skip = "mkdir function is Windows only")]
        public async void MandatoryParameters_HandlesParameterSet_MultipleParamSets_ParamFromDefaultSetGiven()
        {
            // Multiple parameter set cmdlet with a parameter from default set. Should result
            // in splat of the default parameter set.
            Assert.Equal(
                TestBuilder.Create()
                    .Lines("$splat = @{")
                    .Lines("    Path = 'c:\\test\\test'")
                    .Lines("}")
                    .Texts("mkdir @splat"),
                await GetRefactoredTextAsync(
                    "mkdir -Path 'c:\\test\\test'",
                    mandatoryParameters: true));
        }

        [Fact(Skip = "mkdir function is Windows only")]
        public async void MandatoryParameters_HandlesParameterSet_MultipleParamSets_NonDefaultParamSet()
        {
            // Multiple parameter set cmdlet, given a parameter from one of the non-default
            // parameter sets. Should result in a splat of that set.
            Assert.Equal(
                TestBuilder.Create()
                    .Lines("$splat = @{")
                    .Lines("    Name = 'somename'")
                    .Lines("}")
                    .Texts("mkdir @splat"),
                await GetRefactoredTextAsync(
                    "mkdir -Name 'somename'",
                    mandatoryParameters: true));
        }

        [Fact]
        public async void MandatoryParameters_HandlesParameterSet_GivenUnresolvableParameter()
        {
            // Single parameter set cmdlet with an incorrect parameter. Should result in a
            // splat with all params, and the incorrect parameter should remain on the same line
            // as the cmdlet.
            Assert.Equal(
                TestBuilder.Create()
                    .Lines("$splat = @{")
                    .Lines("    Expression = $mandatoryScriptBlockExpression")
                    .Lines("    ThisIsAnInvalidParameter = 'somevalue'")
                    .Lines("}")
                    .Texts("Measure-Command @splat"),
                await GetRefactoredTextAsync(
                    "Measure-Command -ThisIsAnInvalidParameter 'somevalue'",
                    mandatoryParameters: true));

            /*
             A warning should also be displayed. This is harder to test.
             TODO: find out how this works: http://www.blackwasp.co.uk/MoqTimes.aspx
             Mock<IRefactorUI> ui;
            */
        }

        [Fact]
        public async void NoHints_HandlesParameterSet_OneParamSet()
        {
            Assert.Equal(
                TestBuilder.Create()
                    .Lines("$splat = @{")
                    .Lines("    From = $from")
                    .Lines("    To = $to")
                    .Lines("}")
                    .Texts("Send-MailMessage @splat"),
                await GetRefactoredTextAsync(
                    "Send-MailMessage",
                    mandatoryParameters: true,
                    noHints: true));
        }

        private async Task<string> GetRefactoredTextAsync(
            string testString,
            string variableName = "splat",
            bool noNewLineAfterHashtable = true,
            bool allParameters = false,
            bool mandatoryParameters = false,
            bool noHints = false,
            CancellationToken cancellationToken = default)
        {
            AdditionalParameterTypes includedTypes =
                allParameters ? AdditionalParameterTypes.All
                    : mandatoryParameters ? AdditionalParameterTypes.Mandatory
                    : AdditionalParameterTypes.None;

            var config = new CommandSplatRefactorSettings()
            {
                AdditionalParameters = includedTypes,
                ExcludeHints = noHints,
                NoNewLineAfterHashtable = noNewLineAfterHashtable,
                VariableName = variableName,
            };

            return await _refactorService.GetRefactoredString(
                testString,
                context => CommandSplatRefactor.SplatCommandAsync(
                    context,
                    context.Ast.FindParent<CommandAst>(),
                    includedTypes,
                    null),
                cancellationToken: cancellationToken,
                configuration: config,
                requiresRunspace: true);
        }
    }
}
