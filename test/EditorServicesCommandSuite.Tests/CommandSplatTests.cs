using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Text;
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
                .Append("    Recurse = $true  #   [SwitchParameter]\n")
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
                .Append("    Path = './myPath'  #   [String[]]\n")
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
                .Append("    Path = \"./myPath$c\"  #   [String[]]\n")
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
                .Append("    Attachments                =   #   [String[]]\n")
                .Append("    Bcc                        =   #   [String[]]\n")
                .Append("    Body                       =   #   [String]\n")
                .Append("    BodyAsHtml                 =   #   [SwitchParameter]\n")
                .Append("    Encoding                   =   #   [Encoding]\n")
                .Append("    Cc                         =   #   [String[]]\n")
                .Append("    DeliveryNotificationOption =   #   [DeliveryNotificationOptions]\n")
                .Append("    From                       =   # * [String]\n")
                .Append("    SmtpServer                 =   #   [String]\n")
                .Append("    Priority                   =   #   [MailPriority]\n")
                .Append("    Subject                    =   # * [String]\n")
                .Append("    To                         =   # * [String[]]\n")
                .Append("    Credential                 =   #   [PSCredential]\n")
                .Append("    UseSsl                     =   #   [SwitchParameter]\n")
                .Append("    Port                       =   #   [Int32]\n")
                .Append("}\n")
                .Append("Send-MailMessage @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "Send-MailMessage",
                    "splat",
                    false,
                    true,
                    false,
                    false));
        }

        [Fact]
        public async void AllParameters_HandlesParameterSet_OneParamSet_OneParamGiven()
        {
            // Single parameterset-Cmdlet, one parameter filled in.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Attachments                =   #   [String[]]\n")
                .Append("    Bcc                        =   #   [String[]]\n")
                .Append("    Body                       =   #   [String]\n")
                .Append("    BodyAsHtml                 =   #   [SwitchParameter]\n")
                .Append("    Encoding                   =   #   [Encoding]\n")
                .Append("    Cc                         =   #   [String[]]\n")
                .Append("    DeliveryNotificationOption =   #   [DeliveryNotificationOptions]\n")
                .Append("    From                       = 'someone@someplace.com'  # * [String]\n")
                .Append("    SmtpServer                 =   #   [String]\n")
                .Append("    Priority                   =   #   [MailPriority]\n")
                .Append("    Subject                    =   # * [String]\n")
                .Append("    To                         =   # * [String[]]\n")
                .Append("    Credential                 =   #   [PSCredential]\n")
                .Append("    UseSsl                     =   #   [SwitchParameter]\n")
                .Append("    Port                       =   #   [Int32]\n")
                .Append("}\n")
                .Append("Send-MailMessage @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "Send-MailMessage -From 'someone@someplace.com'",
                    "splat",
                    false,
                    true,
                    false,
                    false));
        }

        [Fact]
        public async void AllParameters_HandlesParameterSet_MultipleParamSets()
        {
            // Multi-parameterset-Cmdlet, no parameters given. Should result in default set.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Path      =   # * [String[]]\n")
                .Append("    Algorithm =   #   [String]\n")
                .Append("}\n")
                .Append("Get-FileHash @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "Get-FileHash",
                    "splat",
                    false,
                    true,
                    false,
                    false));
        }

        [Fact]
        public async void AllParameters_HandlesParameterSet_MultipleParamSets_NonDeterminantParamGiven()
        {
            // Multi-parameterset-Cmdlet, with a parameter from the '__AllParameterSets' set, should result in default parameterset.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Path    =   #   [String[]]\n")
                .Append("    Filter  =   #   [String]\n")
                .Append("    Include =   #   [String[]]\n")
                .Append("    Exclude =   #   [String[]]\n")
                .Append("    Recurse =   #   [SwitchParameter]\n")
                .Append("    Depth   =   #   [UInt32]\n")
                .Append("    Force   =   #   [SwitchParameter]\n")
                .Append("    Name    = $true  #   [SwitchParameter]\n")
                .Append("}\n")
                .Append("Get-ChildItem @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "Get-ChildItem -Name",
                    "splat",
                    false,
                    true,
                    false,
                    false));
        }

        [Fact]
        public async void AllParameters_HandlesParameterSet_MultipleParamSets_ParamFromDefaultSetGiven()
        {
            // Multi-paramset-Cmdlet with parameter from default paramset filled in, should result in splat of default parameterset.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Path       = 'c:\\test\\test'  # * [String[]]\n")
                .Append("    Value      =   #   [Object]\n")
                .Append("    Force      =   #   [SwitchParameter]\n")
                .Append("    Credential =   #   [PSCredential]\n")
                .Append("}\n")
                .Append("mkdir @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "mkdir -Path 'c:\\test\\test'",
                    "splat",
                    false,
                    true,
                    false,
                    false));
        }

        [Fact]
        public async void AllParameters_HandlesParameterSet_MultipleParamSets_NonDefaultParamSet()
        {
            // Multi-paramset-Cmdlet, given a parameter from one of the non-default parametersets should result in a splat of that set.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Path       =   #   [String[]]\n")
                .Append("    Name       = 'somename'  # * [String]\n")
                .Append("    Value      =   #   [Object]\n")
                .Append("    Force      =   #   [SwitchParameter]\n")
                .Append("    Credential =   #   [PSCredential]\n")
                .Append("}\n")
                .Append("mkdir @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "mkdir -Name 'somename'",
                    "splat",
                    false,
                    true,
                    false,
                    false));
        }


        [Fact]
        public async void AllParameters_HandlesParameterSet_GivenUnresolvableParameter()
        {
            // Single-paramset-Cmdlet, given an incorrect parameter, should result in a splat with all params, and the incorrect param should remain on the same line as the cmdlet.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    InputObject =   #   [PSObject]\n")
                .Append("    Expression  =   # * [ScriptBlock]\n")
                .Append("}\n")
                .Append("Measure-Command @splat -ThisIsAnInvalidParameter");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "Measure-Command -ThisIsAnInvalidParameter 'somevalue'",
                    "splat",
                    false,
                    true,
                    false,
                    false));

            /*
             A warning should also be displayed. This is harder to test.
             TODO: find out how this works: http://www.blackwasp.co.uk/MoqTimes.aspx
             Mock<IRefactorUI> ui;
            */
        }

        [Fact]
        public async void AllParameters_HandlesNoParameters_GivenUnresolvableParameter()
        {
            // Test single parameterset CmdLet, with no parameters other than the Common parameters, with an invalid parameter. Should do ... what?

            Assert.Equal(
                "Get-Host -ThisIsAnInvalidParameter",
                await GetRefactoredTextAsync(
                    "Get-Host -ThisIsAnInvalidParameter 'somevalue'",
                    "splat",
                    false,
                    true,
                    false,
                    false));
        }

        [Fact]
        public async void AllParameters_HandlesParameterSet_AmbiguousParameterSet()
        {
            // Multi-parameterset-Cmdlet, given parameters from two seperate sets, should result in an ambigous parameterset exception.
            await Assert.ThrowsAsync<ParameterBindingException>(
                () =>
                    GetRefactoredTextAsync(
                        "Get-FileHash -LiteralPath 'c:\\test\\test' -Path 'c:\\test\\test'",
                        "splat",
                        false,
                        true,
                        false,
                        false));
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
                .Append("    From    =   # * [String]\n")
                .Append("    Subject =   # * [String]\n")
                .Append("    To      =   # * [String[]]\n")
                .Append("}\n")
                .Append("Send-MailMessage @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "Send-MailMessage",
                    "splat",
                    false,
                    false,
                    true,
                    false));
        }

        [Fact]
        public async void MandatoryParameters_HandlesParameterSet_OneParamSet_OneParamGiven()
        {
            // Single parameterset-Cmdlet, one parameter filled in.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    From    = 'someone@someplace.com'  # * [String]\n")
                .Append("    Subject =   # * [String]\n")
                .Append("    To      =   # * [String[]]\n")
                .Append("}\n")
                .Append("Send-MailMessage @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "Send-MailMessage -From 'someone@someplace.com'",
                    "splat",
                    false,
                    false,
                    true,
                    false));
        }

        [Fact]
        public async void MandatoryParameters_HandlesParameterSet_MultipleParamSets()
        {
            // Multi-parameterset-Cmdlet, no parameters given. Should result in default set.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Path =   # * [String[]]\n")
                .Append("}\n")
                .Append("Get-FileHash @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "Get-FileHash",
                    "splat",
                    false,
                    false,
                    true,
                    false));
        }

        [Fact]
        public async void MandatoryParameters_HandlesParameterSet_MultipleParamSets_NonDeterminantParamGiven()
        {
            // Multi-parameterset-Cmdlet, with a parameter from the '__AllParameterSets' set, should result in default parameterset.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Name = $true  #   [SwitchParameter]\n")
                .Append("}\n")
                .Append("Get-ChildItem @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "Get-ChildItem -Name",
                    "splat",
                    false,
                    false,
                    true,
                    false));
        }

        [Fact]
        public async void MandatoryParameters_HandlesParameterSet_MultipleParamSets_ParamFromDefaultSetGiven()
        {
            // Multi-paramset-Cmdlet with parameter from default paramset filled in, should result in splat of default parameterset.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Path = 'c:\\test\\test'  # * [String[]]\n")
                .Append("}\n")
                .Append("mkdir @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "mkdir -Path 'c:\\test\\test'",
                    "splat",
                    false,
                    false,
                    true,
                    false));
        }

        [Fact]
        public async void MandatoryParameters_HandlesParameterSet_MultipleParamSets_NonDefaultParamSet()
        {
            // Multi-paramset-Cmdlet, given a parameter from one of the non-default parametersets should result in a splat of that set.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Name = 'somename'  # * [String]\n")
                .Append("}\n")
                .Append("mkdir @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "mkdir -Name 'somename'",
                    "splat",
                    false,
                    false,
                    true,
                    false));
        }


        [Fact]
        public async void MandatoryParameters_HandlesParameterSet_GivenUnresolvableParameter()
        {
            // Single-paramset-Cmdlet, given an incorrect parameter, should result in a splat with all params, and the incorrect param should remain on the same line as the cmdlet.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    Expression =   # * [ScriptBlock]\n")
                .Append("}\n")
                .Append("Measure-Command @splat -ThisIsAnInvalidParameter");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "Measure-Command -ThisIsAnInvalidParameter 'somevalue'",
                    "splat",
                    false,
                    false,
                    true,
                    false));

            /*
             A warning should also be displayed. This is harder to test.
             TODO: find out how this works: http://www.blackwasp.co.uk/MoqTimes.aspx
             Mock<IRefactorUI> ui;
            */
        }

        #endregion //MandatoryParameters

        #region NoHints

        public async void NoHints_HandlesParameterSet_OneParamSet()
        {
            // Single parameteterset-CmdLet.
            var sb = new StringBuilder();
            sb
                .Append("$splat = @{\n")
                .Append("    From                       = \n")
                .Append("    Subject                    = \n")
                .Append("    To                         = \n")
                .Append("}\n")
                .Append("Send-MailMessage @splat");

            sb.Replace("\n    ","\n\t");

            Assert.Equal(
                sb.ToString(),
                await GetRefactoredTextAsync(
                    "Send-MailMessage",
                    "splat",
                    false,
                    true,
                    false,
                    true));
        }

        #endregion //NoHints

        private async Task<string> GetRefactoredTextAsync(
            string testString,
            string variableName = "splat",
            bool newLineAfterHashtable = false,
            bool allParameters = false,
            bool mandatoryParameters = false,
            bool noHints = false)
        {
            using (var runspace = RunspaceFactory.CreateRunspace())
            {
                runspace.Open();
                // StaticParameterBinder.BindCommand() will only try to bind a CommandAst to a real command if there is a DefaultRunspace available.
                var oldRunspace = Runspace.DefaultRunspace;
                Runspace.DefaultRunspace = runspace;
                try
        {
                    EngineIntrinsics executionContext =
                        (EngineIntrinsics)runspace
                            .SessionStateProxy
                            .GetVariable("ExecutionContext");

            return await MockContext.GetRefactoredTextAsync(
                testString,
                context => CommandSplatRefactor.GetEdits(
                    variableName,
                    context.Ast.FindParent<CommandAst>(),
                    newLineAfterHashtable,
                            allParameters,
                            mandatoryParameters,
                            noHints,
                            executionContext));
                }
                finally
                {
                    Runspace.DefaultRunspace = oldRunspace;
                }
            }
        }
    }
}
