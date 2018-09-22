using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using EditorServicesCommandSuite.Language;
using Xunit;

namespace EditorServicesCommandSuite.Tests
{
    public class CommandSplatTests
    {
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
                    .Line("$splat = @{")
                    .Line("    Recurse = $true")
                    .Line("}")
                    .Text("Get-ChildItem @splat"),
                await GetRefactoredTextAsync("Get-ChildItem -Recurse"));
        }

        [Fact]
        public async void HandlesQuoting()
        {
            Assert.Equal(
                TestBuilder.Create()
                    .Line("$splat = @{")
                    .Line("    Path = './myPath'")
                    .Line("}")
                    .Text("Get-ChildItem @splat"),
                await GetRefactoredTextAsync("Get-ChildItem -Path ./myPath"));
        }

        [Fact]
        public async void HandlesQuotingWithExpressions()
        {
            Assert.Equal(
                TestBuilder.Create()
                    .Line("$splat = @{")
                    .Line("    Path = \"./myPath$c\"")
                    .Line("}")
                    .Text("Get-ChildItem @splat"),
                await GetRefactoredTextAsync("Get-ChildItem -Path ./myPath$c"));
        }

        [Fact]
        public async void AllParameters_HandlesParameterSet_OneParamSet()
        {
            Assert.Equal(
                TestBuilder.Create()
                    .Line("$splat = @{")
                    .Line("    Attachments = $stringArrayAttachments")
                    .Line("    Bcc = $stringArrayBcc")
                    .Line("    Body = $stringBody")
                    .Line("    BodyAsHtml = $switchParameterBodyAsHtml")
                    .Line("    Encoding = $encodingEncoding")
                    .Line("    Cc = $stringArrayCc")
                    .Line("    DeliveryNotificationOption = $deliveryNotificationOptionsDeliveryNotificationOption")
                    .Line("    From = $mandatoryStringFrom")
                    .Line("    SmtpServer = $stringSmtpServer")
                    .Line("    Priority = $mailPriorityPriority")
                    .Line("    Subject = $mandatoryStringSubject")
                    .Line("    To = $mandatoryStringArrayTo")
                    .Line("    Credential = $pSCredentialCredential")
                    .Line("    UseSsl = $switchParameterUseSsl")
                    .Line("    Port = $int32Port")
                    .Line("}")
                    .Text("Send-MailMessage @splat"),
                await GetRefactoredTextAsync(
                    "Send-MailMessage",
                    allParameters: true));
        }

        [Fact]
        public async void AllParameters_HandlesParameterSet_OneParamSet_OneParamGiven()
        {
            Assert.Equal(
                TestBuilder.Create()
                    .Line("$splat = @{")
                    .Line("    From = 'someone@someplace.com'")
                    .Line("    Attachments = $stringArrayAttachments")
                    .Line("    Bcc = $stringArrayBcc")
                    .Line("    Body = $stringBody")
                    .Line("    BodyAsHtml = $switchParameterBodyAsHtml")
                    .Line("    Encoding = $encodingEncoding")
                    .Line("    Cc = $stringArrayCc")
                    .Line("    DeliveryNotificationOption = $deliveryNotificationOptionsDeliveryNotificationOption")
                    .Line("    SmtpServer = $stringSmtpServer")
                    .Line("    Priority = $mailPriorityPriority")
                    .Line("    Subject = $mandatoryStringSubject")
                    .Line("    To = $mandatoryStringArrayTo")
                    .Line("    Credential = $pSCredentialCredential")
                    .Line("    UseSsl = $switchParameterUseSsl")
                    .Line("    Port = $int32Port")
                    .Line("}")
                    .Text("Send-MailMessage @splat"),
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
                    .Line("$splat = @{")
                    .Line("    Path = $mandatoryStringArrayPath")
                    .Line("    Algorithm = $stringAlgorithm")
                    .Line("}")
                    .Text("Get-FileHash @splat"),
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
                    .Line("$splat = @{")
                    .Line("    Name = $true")
                    .Line("    Path = $stringArrayPath")
                    .Line("    Filter = $stringFilter")
                    .Line("    Include = $stringArrayInclude")
                    .Line("    Exclude = $stringArrayExclude")
                    .Line("    Recurse = $switchParameterRecurse")
                    .Line("    Depth = $uInt32Depth")
                    .Line("    Force = $switchParameterForce")
                    .Line("}")
                    .Text("Get-ChildItem @splat"),
                await GetRefactoredTextAsync(
                    "Get-ChildItem -Name",
                    allParameters: true));
        }

        [Fact]
        public async void AllParameters_HandlesParameterSet_MultipleParamSets_ParamFromDefaultSetGiven()
        {
            // Multiple parameter set cmdlet with parameter from default set,
            // should result in splat of default parameter set.
            Assert.Equal(
                TestBuilder.Create()
                    .Line("$splat = @{")
                    .Line("    Path = 'c:\\test\\test'")
                    .Line("    Value = $objectValue")
                    .Line("    Force = $switchParameterForce")
                    .Line("    Credential = $pSCredentialCredential")
                    .Line("}")
                    .Text("mkdir @splat"),
                await GetRefactoredTextAsync(
                    "mkdir -Path 'c:\\test\\test'",
                    allParameters: true));
        }

        [Fact]
        public async void AllParameters_HandlesParameterSet_MultipleParamSets_NonDefaultParamSet()
        {
            // Multiple parameter set cmdlet, with a parameter from one of the non-default parameter
            // sets. Should result in a splat of that set.
            Assert.Equal(
                TestBuilder.Create()
                    .Line("$splat = @{")
                    .Line("    Name = 'somename'")
                    .Line("    Path = $stringArrayPath")
                    .Line("    Value = $objectValue")
                    .Line("    Force = $switchParameterForce")
                    .Line("    Credential = $pSCredentialCredential")
                    .Line("}")
                    .Text("mkdir @splat"),
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
                    .Line("$splat = @{")
                    .Line("    InputObject = $pSObjectInputObject")
                    .Line("    Expression = $mandatoryScriptBlockExpression")
                    .Line("}")
                    .Text("Measure-Command @splat -ThisIsAnInvalidParameter"),
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
                    .Line("$splat = @{")
                    .Line()
                    .Line("}")
                    .Text("Get-Host @splat -ThisIsAnInvalidParameter"),
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
                    .Line("$splat = @{")
                    .Line("    From = $mandatoryStringFrom")
                    .Line("    Subject = $mandatoryStringSubject")
                    .Line("    To = $mandatoryStringArrayTo")
                    .Line("}")
                    .Text("Send-MailMessage @splat"),
                await GetRefactoredTextAsync(
                    "Send-MailMessage",
                    mandatoryParameters: true));
        }

        [Fact]
        public async void MandatoryParameters_HandlesParameterSet_OneParamSet_OneParamGiven()
        {
            Assert.Equal(
                TestBuilder.Create()
                    .Line("$splat = @{")
                    .Line("    From = 'someone@someplace.com'")
                    .Line("    Subject = $mandatoryStringSubject")
                    .Line("    To = $mandatoryStringArrayTo")
                    .Line("}")
                    .Text("Send-MailMessage @splat"),
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
                    .Line("$splat = @{")
                    .Line("    Path = $mandatoryStringArrayPath")
                    .Line("}")
                    .Text("Get-FileHash @splat"),
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
                    .Line("$splat = @{")
                    .Line("    Name = $true")
                    .Line("}")
                    .Text("Get-ChildItem @splat"),
                await GetRefactoredTextAsync(
                    "Get-ChildItem -Name",
                    mandatoryParameters: true));
        }

        [Fact]
        public async void MandatoryParameters_HandlesParameterSet_MultipleParamSets_ParamFromDefaultSetGiven()
        {
            // Multiple parameter set cmdlet with a parameter from default set. Should result
            // in splat of the default parameter set.
            Assert.Equal(
                TestBuilder.Create()
                    .Line("$splat = @{")
                    .Line("    Path = 'c:\\test\\test'")
                    .Line("}")
                    .Text("mkdir @splat"),
                await GetRefactoredTextAsync(
                    "mkdir -Path 'c:\\test\\test'",
                    mandatoryParameters: true));
        }

        [Fact]
        public async void MandatoryParameters_HandlesParameterSet_MultipleParamSets_NonDefaultParamSet()
        {
            // Multiple parameter set cmdlet, given a parameter from one of the non-default
            // parameter sets. Should result in a splat of that set.
            Assert.Equal(
                TestBuilder.Create()
                    .Line("$splat = @{")
                    .Line("    Name = 'somename'")
                    .Line("}")
                    .Text("mkdir @splat"),
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
                    .Line("$splat = @{")
                    .Line("    Expression = $mandatoryScriptBlockExpression")
                    .Line("}")
                    .Text("Measure-Command @splat -ThisIsAnInvalidParameter"),
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
                    .Line("$splat = @{")
                    .Line("    From = $from")
                    .Line("    Subject = $subject")
                    .Line("    To = $to")
                    .Line("}")
                    .Text("Send-MailMessage @splat"),
                await GetRefactoredTextAsync(
                    "Send-MailMessage",
                    mandatoryParameters: true,
                    noHints: true));
        }

        private async Task<string> GetRefactoredTextAsync(
            string testString,
            string variableName = "splat",
            bool newLineAfterHashtable = false,
            bool allParameters = false,
            bool mandatoryParameters = false,
            bool noHints = false,
            CancellationToken cancellationToken = default)
        {
            AdditionalParameterTypes includedTypes =
                allParameters ? AdditionalParameterTypes.All
                    : mandatoryParameters ? AdditionalParameterTypes.Mandatory
                    : AdditionalParameterTypes.None;

            return await MockContext.GetRefactoredTextAsync(
                testString,
                context => CommandSplatRefactor.GetEdits(
                    variableName,
                    context.Ast.FindParent<CommandAst>(),
                    includedTypes,
                    newLineAfterHashtable,
                    noHints,
                    context.PipelineThread,
                    context.CancellationToken),
                withRunspace: true,
                cancellationToken);
        }
    }
}
