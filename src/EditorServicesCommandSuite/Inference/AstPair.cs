// This file is based on a file from https://github.com/PowerShell/PowerShell, and
// edited to use public API's where possible. While not generated, the comment below
// is included to exclude it from StyleCop analysis.
// <auto-generated/>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Inference
{
    /// <summary>
    /// The types for AstParameterArgumentPair
    /// </summary>
    internal enum AstParameterArgumentType
    {
        AstPair = 0,
        Switch = 1,
        Fake = 2,
        AstArray = 3,
        PipeObject = 4
    }

    /// <summary>
    /// The base class for parameter argument pair
    /// </summary>
    internal abstract class AstParameterArgumentPair
    {
        /// <summary>
        /// The parameter Ast
        /// </summary>
        public CommandParameterAst Parameter { get; protected set; }

        /// <summary>
        /// The argument type
        /// </summary>
        public AstParameterArgumentType ParameterArgumentType { get; protected set; }

        /// <summary>
        /// Indicate if the parameter is specified
        /// </summary>
        public bool ParameterSpecified { get; protected set; } = false;

        /// <summary>
        /// Indicate if the parameter is specified
        /// </summary>
        public bool ArgumentSpecified { get; protected set; } = false;

        /// <summary>
        /// The parameter name
        /// </summary>
        public string ParameterName { get; protected set; }

        /// <summary>
        /// The parameter text
        /// </summary>
        public string ParameterText { get; protected set; }

        /// <summary>
        /// The argument type
        /// </summary>
        public Type ArgumentType { get; protected set; }

        public static AstParameterArgumentPair Get(ParameterBindingResult bindingResult)
        {
            if (bindingResult == null) throw new ArgumentNullException(nameof(bindingResult));
            if (bindingResult.Parameter == null)
                throw new ArgumentException("The binding result specified does not contain parameter metadata.", nameof(bindingResult));

            if (bindingResult.Value == null)
            {
                if (bindingResult.Parameter.SwitchParameter)
                {
                    return new SwitchPair(
                        new CommandParameterAst(
                            Empty.Extent.Get(),
                            bindingResult.Parameter.Name,
                            null,
                            Empty.Extent.Get()));

                }

                return new FakePair(
                    new CommandParameterAst(
                        Empty.Extent.Get(),
                        bindingResult.Parameter.Name,
                        null,
                        Empty.Extent.Get()));
            }

            var elements = (bindingResult.Value.Parent as CommandAst)?.CommandElements;
            var parameterAst = elements[elements.IndexOf(bindingResult.Value) - 1] as CommandParameterAst;
            if (parameterAst == null || string.Equals(parameterAst.ParameterName, bindingResult.Parameter.Name, StringComparison.OrdinalIgnoreCase))
            {
                return new AstPair(null, bindingResult.Value);
            }

            return parameterAst.Argument == null
                ? new AstPair(parameterAst, bindingResult.Value)
                : new AstPair(parameterAst);
        }
    }

    /// <summary>
    /// Represent a parameter argument pair. The argument is a pipeline input object
    /// </summary>
    internal sealed class PipeObjectPair : AstParameterArgumentPair
    {
        internal PipeObjectPair(string parameterName, Type pipeObjType)
        {
            if (parameterName == null) throw new ArgumentNullException(nameof(parameterName));

            Parameter = null;
            ParameterArgumentType = AstParameterArgumentType.PipeObject;
            ParameterSpecified = true;
            ArgumentSpecified = true;
            ParameterName = parameterName;
            ParameterText = parameterName;
            ArgumentType = pipeObjType;
        }
    }

    /// <summary>
    /// Represent a parameter argument pair. The argument is an array of ExpressionAst (remaining
    /// arguments)
    /// </summary>
    internal sealed class AstArrayPair : AstParameterArgumentPair
    {
        internal AstArrayPair(string parameterName, ICollection<ExpressionAst> arguments)
        {
            if (parameterName == null) throw new ArgumentNullException(nameof(parameterName));
            if (arguments == null) throw new ArgumentNullException(nameof(parameterName));
            if (arguments.Count == 0) throw new ArgumentException("Arguments must not be empty", nameof(arguments));

            Parameter = null;
            ParameterArgumentType = AstParameterArgumentType.AstArray;
            ParameterSpecified = true;
            ArgumentSpecified = true;
            ParameterName = parameterName;
            ParameterText = parameterName;
            ArgumentType = typeof(Array);

            Argument = arguments.ToArray();
        }

        /// <summary>
        /// Get the argument
        /// </summary>
        public ExpressionAst[] Argument { get; } = null;
    }

    /// <summary>
    /// Represent a parameter argument pair. The argument is a fake object.
    /// </summary>
    internal sealed class FakePair : AstParameterArgumentPair
    {
        internal FakePair(CommandParameterAst parameterAst)
        {
            if (parameterAst == null) throw new ArgumentNullException(nameof(parameterAst));

            Parameter = parameterAst;
            ParameterArgumentType = AstParameterArgumentType.Fake;
            ParameterSpecified = true;
            ArgumentSpecified = true;
            ParameterName = parameterAst.ParameterName;
            ParameterText = parameterAst.ParameterName;
            ArgumentType = typeof(object);
        }
    }

    /// <summary>
    /// Represent a parameter argument pair. The parameter is a switch parameter.
    /// </summary>
    internal sealed class SwitchPair : AstParameterArgumentPair
    {
        internal SwitchPair(CommandParameterAst parameterAst)
        {
            if (parameterAst == null) throw new ArgumentNullException(nameof(parameterAst));

            Parameter = parameterAst;
            ParameterArgumentType = AstParameterArgumentType.Switch;
            ParameterSpecified = true;
            ArgumentSpecified = true;
            ParameterName = parameterAst.ParameterName;
            ParameterText = parameterAst.ParameterName;
            ArgumentType = typeof(bool);
        }

        /// <summary>
        /// Get the argument
        /// </summary>
        public bool Argument
        {
            get { return true; }
        }
    }

    /// <summary>
    /// Represent a parameter argument pair. It could be a pure argument (no parameter, only argument available);
    /// it could be a CommandParameterAst that contains its argument; it also could be a CommandParameterAst with
    /// another CommandParameterAst as the argument.
    /// </summary>
    internal sealed class AstPair : AstParameterArgumentPair
    {
        internal AstPair(CommandParameterAst parameterAst)
        {
            if (parameterAst == null) throw new ArgumentNullException(nameof(parameterAst));
            if (parameterAst.Argument == null) throw new ArgumentException("Argument property must not be null.", nameof(parameterAst));

            Parameter = parameterAst;
            ParameterArgumentType = AstParameterArgumentType.AstPair;
            ParameterSpecified = true;
            ArgumentSpecified = true;
            ParameterName = parameterAst.ParameterName;
            ParameterText = "-" + ParameterName + ":";
            ArgumentType = parameterAst.Argument.StaticType;

            ParameterContainsArgument = true;
            Argument = parameterAst.Argument;
        }

        internal AstPair(CommandParameterAst parameterAst, ExpressionAst argumentAst)
        {
            if (parameterAst != null && parameterAst.Argument != null)
                throw new ArgumentException(@"Argument must be null or have a null ""Argument"" property", nameof(parameterAst));

            if (parameterAst == null && argumentAst == null) throw new ArgumentNullException(nameof(argumentAst));

            Parameter = parameterAst;
            ParameterArgumentType = AstParameterArgumentType.AstPair;
            ParameterSpecified = parameterAst != null;
            ArgumentSpecified = argumentAst != null;
            ParameterName = parameterAst != null ? parameterAst.ParameterName : null;
            ParameterText = parameterAst != null ? parameterAst.ParameterName : null;
            ArgumentType = argumentAst != null ? argumentAst.StaticType : null;

            ParameterContainsArgument = false;
            Argument = argumentAst;
        }

        internal AstPair(CommandParameterAst parameterAst, CommandElementAst argumentAst)
        {
            if (parameterAst != null && parameterAst.Argument != null)
                throw new ArgumentException(@"Argument must be null or have a null ""Argument"" property", nameof(parameterAst));

            if (parameterAst == null && argumentAst == null) throw new ArgumentNullException(nameof(argumentAst));

            Parameter = parameterAst;
            ParameterArgumentType = AstParameterArgumentType.AstPair;
            ParameterSpecified = true;
            ArgumentSpecified = true;
            ParameterName = parameterAst.ParameterName;
            ParameterText = parameterAst.ParameterName;
            ArgumentType = typeof(string);

            ParameterContainsArgument = false;
            Argument = argumentAst;
            ArgumentIsCommandParameterAst = true;
        }

        /// <summary>
        /// Indicate if the argument is contained in the CommandParameterAst
        /// </summary>
        public bool ParameterContainsArgument { get; } = false;

        /// <summary>
        /// Indicate if the argument is of type CommandParameterAst
        /// </summary>
        public bool ArgumentIsCommandParameterAst { get; } = false;

        /// <summary>
        /// Get the argument
        /// </summary>
        public CommandElementAst Argument { get; } = null;
    }
}
