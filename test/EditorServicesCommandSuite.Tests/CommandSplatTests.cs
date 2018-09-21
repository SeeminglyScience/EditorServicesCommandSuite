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
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Recurse = $true\n")
                .Append("}\n")
                .Append("Get-ChildItem @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync("Get-ChildItem -Recurse"));
        }

        [Fact]
        public async void HandlesQuoting()
        {
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Path = './myPath'\n")
                .Append("}\n")
                .Append("Get-ChildItem @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync("Get-ChildItem -Path ./myPath"));
        }

        [Fact]
        public async void HandlesQuotingWithExpressions()
        {
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Path = \"./myPath$c\"\n")
                .Append("}\n")
                .Append("Get-ChildItem @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync("Get-ChildItem -Path ./myPath$c"));
        }

        #region AllParameters

        [Fact]
        public async void AllParameters_HandlesParameterSet_OneParamSet()
        {
            // Single parameteterset-CmdLet.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Attachments = $stringArrayAttachments\n")
                .Append("    Bcc = $stringArrayBcc\n")
                .Append("    Body = $stringBody\n")
                .Append("    BodyAsHtml = $switchParameterBodyAsHtml\n")
                .Append("    Encoding = $encodingEncoding\n")
                .Append("    Cc = $stringArrayCc\n")
                .Append("    DeliveryNotificationOption = $deliveryNotificationOptionsDeliveryNotificationOption\n")
                .Append("    From = $mandatoryStringFrom\n")
                .Append("    SmtpServer = $stringSmtpServer\n")
                .Append("    Priority = $mailPriorityPriority\n")
                .Append("    Subject = $mandatoryStringSubject\n")
                .Append("    To = $mandatoryStringArrayTo\n")
                .Append("    Credential = $pSCredentialCredential\n")
                .Append("    UseSsl = $switchParameterUseSsl\n")
                .Append("    Port = $int32Port\n")
                .Append("}\n")
                .Append("Send-MailMessage @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "Send-MailMessage",
                    allParameters: true));
        }

        [Fact]
        public async void AllParameters_HandlesParameterSet_OneParamSet_OneParamGiven()
        {
            // Single parameterset-Cmdlet, one parameter filled in.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Attachments = $stringArrayAttachments\n")
                .Append("    Bcc = $stringArrayBcc\n")
                .Append("    Body = $stringBody\n")
                .Append("    BodyAsHtml = $switchParameterBodyAsHtml\n")
                .Append("    Encoding = $encodingEncoding\n")
                .Append("    Cc = $stringArrayCc\n")
                .Append("    DeliveryNotificationOption = $deliveryNotificationOptionsDeliveryNotificationOption\n")
                .Append("    From = 'someone@someplace.com'\n")
                .Append("    SmtpServer = $stringSmtpServer\n")
                .Append("    Priority = $mailPriorityPriority\n")
                .Append("    Subject = $mandatoryStringSubject\n")
                .Append("    To = $mandatoryStringArrayTo\n")
                .Append("    Credential = $pSCredentialCredential\n")
                .Append("    UseSsl = $switchParameterUseSsl\n")
                .Append("    Port = $int32Port\n")
                .Append("}\n")
                .Append("Send-MailMessage @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "Send-MailMessage -From 'someone@someplace.com'",
                    allParameters: true));
        }

        [Fact]
        public async void AllParameters_HandlesParameterSet_MultipleParamSets()
        {
            // Multi-parameterset-Cmdlet, no parameters given. Should result in default set.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Path = $mandatoryStringArrayPath\n")
                .Append("    Algorithm = $stringAlgorithm\n")
                .Append("}\n")
                .Append("Get-FileHash @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "Get-FileHash",
                    allParameters: true));
        }

        [Fact]
        public async void AllParameters_HandlesParameterSet_MultipleParamSets_NonDeterminantParamGiven()
        {
            // Multi-parameterset-Cmdlet, with a parameter from the '__AllParameterSets' set, should result in default parameterset.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Path = $stringArrayPath\n")
                .Append("    Filter = $stringFilter\n")
                .Append("    Include = $stringArrayInclude\n")
                .Append("    Exclude = $stringArrayExclude\n")
                .Append("    Recurse = $switchParameterRecurse\n")
                .Append("    Depth = $uInt32Depth\n")
                .Append("    Force = $switchParameterForce\n")
                .Append("    Name = $true\n")
                .Append("}\n")
                .Append("Get-ChildItem @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "Get-ChildItem -Name",
                    allParameters: true));
        }

        [Fact]
        public async void AllParameters_HandlesParameterSet_MultipleParamSets_ParamFromDefaultSetGiven()
        {
            // Multi-paramset-Cmdlet with parameter from default paramset filled in, should result in splat of default parameterset.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Path = 'c:\\test\\test'\n")
                .Append("    Value = $objectValue\n")
                .Append("    Force = $switchParameterForce\n")
                .Append("    Credential = $pSCredentialCredential\n")
                .Append("}\n")
                .Append("mkdir @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "mkdir -Path 'c:\\test\\test'",
                    allParameters: true));
        }

        [Fact]
        public async void AllParameters_HandlesParameterSet_MultipleParamSets_NonDefaultParamSet()
        {
            // Multi-paramset-Cmdlet, given a parameter from one of the non-default parametersets should result in a splat of that set.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Path = $stringArrayPath\n")
                .Append("    Name = 'somename'\n")
                .Append("    Value = $objectValue\n")
                .Append("    Force = $switchParameterForce\n")
                .Append("    Credential = $pSCredentialCredential\n")
                .Append("}\n")
                .Append("mkdir @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "mkdir -Name 'somename'",
                    allParameters: true));
        }


        [Fact]
        public async void AllParameters_HandlesParameterSet_GivenUnresolvableParameter()
        {
            // Single-paramset-Cmdlet, given an incorrect parameter, should result in a splat with all params, and the incorrect param should remain on the same line as the cmdlet.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    InputObject = $pSObjectInputObject\n")
                .Append("    Expression = $mandatoryScriptBlockExpression\n")
                .Append("}\n")
                .Append("Measure-Command @splat -ThisIsAnInvalidParameter");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "Measure-Command -ThisIsAnInvalidParameter 'somevalue'",
                    allParameters: true));

            // A warning should also be displayed. This is not tested here.
        }

        [Fact]
        public async void AllParameters_HandlesNoParameters_GivenUnresolvableParameter()
        {
            // Test single parameterset CmdLet, with no parameters other than the Common parameters, with an invalid parameter.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("\n")
                .Append("}\n")
                .Append("Get-Host @splat -ThisIsAnInvalidParameter");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "Get-Host -ThisIsAnInvalidParameter 'somevalue'",
                    allParameters: true));
        }

        [Fact]
        public async void AllParameters_HandlesParameterSet_AmbiguousParameterSet()
        {
            // Multi-parameterset-Cmdlet, given parameters from two seperate sets, should result in an ambigous parameterset exception.
            await Assert.ThrowsAsync<PSInvalidOperationException>(
                () =>
                    GetRefactoredTextAsync(
                        "Get-FileHash -LiteralPath 'c:\\test\\test' -Path 'c:\\test\\test'",
                        allParameters: true));
        }

        #endregion // AllParameters

        #region MandatoryParameters

        [Fact]
        public async void MandatoryParameters_HandlesParameterSet_OneParamSet()
        {
            // Single parameteterset-CmdLet.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    From = $mandatoryStringFrom\n")
                .Append("    Subject = $mandatoryStringSubject\n")
                .Append("    To = $mandatoryStringArrayTo\n")
                .Append("}\n")
                .Append("Send-MailMessage @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "Send-MailMessage",
                    mandatoryParameters: true));
        }

        [Fact]
        public async void MandatoryParameters_HandlesParameterSet_OneParamSet_OneParamGiven()
        {
            // Single parameterset-Cmdlet, one parameter filled in.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    From = 'someone@someplace.com'\n")
                .Append("    Subject = $mandatoryStringSubject\n")
                .Append("    To = $mandatoryStringArrayTo\n")
                .Append("}\n")
                .Append("Send-MailMessage @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "Send-MailMessage -From 'someone@someplace.com'",
                    mandatoryParameters: true));
        }

        [Fact]
        public async void MandatoryParameters_HandlesParameterSet_MultipleParamSets()
        {
            // Multi-parameterset-Cmdlet, no parameters given. Should result in default set.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Path = $mandatoryStringArrayPath\n")
                .Append("}\n")
                .Append("Get-FileHash @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "Get-FileHash",
                    mandatoryParameters: true));
        }

        [Fact]
        public async void MandatoryParameters_HandlesParameterSet_MultipleParamSets_NonDeterminantParamGiven()
        {
            // Multi-parameterset-Cmdlet, with a parameter from the '__AllParameterSets' set, should result in default parameterset.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Name = $true\n")
                .Append("}\n")
                .Append("Get-ChildItem @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "Get-ChildItem -Name",
                    mandatoryParameters: true));
        }

        [Fact]
        public async void MandatoryParameters_HandlesParameterSet_MultipleParamSets_ParamFromDefaultSetGiven()
        {
            // Multi-paramset-Cmdlet with parameter from default paramset filled in, should result in splat of default parameterset.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Path = 'c:\\test\\test'\n")
                .Append("}\n")
                .Append("mkdir @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "mkdir -Path 'c:\\test\\test'",
                    mandatoryParameters: true));
        }

        [Fact]
        public async void MandatoryParameters_HandlesParameterSet_MultipleParamSets_NonDefaultParamSet()
        {
            // Multi-paramset-Cmdlet, given a parameter from one of the non-default parametersets should result in a splat of that set.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Name = 'somename'\n")
                .Append("}\n")
                .Append("mkdir @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "mkdir -Name 'somename'",
                    mandatoryParameters: true));
        }


        [Fact]
        public async void MandatoryParameters_HandlesParameterSet_GivenUnresolvableParameter()
        {
            // Single-paramset-Cmdlet, given an incorrect parameter, should result in a splat with all params, and the incorrect param should remain on the same line as the cmdlet.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Expression = $mandatoryScriptBlockExpression\n")
                .Append("}\n")
                .Append("Measure-Command @splat -ThisIsAnInvalidParameter");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "Measure-Command -ThisIsAnInvalidParameter 'somevalue'",
                    mandatoryParameters: true));

            /*
             A warning should also be displayed. This is harder to test.
             TODO: find out how this works: http://www.blackwasp.co.uk/MoqTimes.aspx
             Mock<IRefactorUI> ui;
            */
        }

        #endregion //MandatoryParameters

        #region NoHints

        [Fact]
        public async void NoHints_HandlesParameterSet_OneParamSet()
        {
            // Single parameteterset-CmdLet.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    From = $from\n")
                .Append("    Subject = $subject\n")
                .Append("    To = $to\n")
                .Append("}\n")
                .Append("Send-MailMessage @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "Send-MailMessage",
                    mandatoryParameters: true,
                    noHints: true));
        }

        #endregion //NoHints

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
