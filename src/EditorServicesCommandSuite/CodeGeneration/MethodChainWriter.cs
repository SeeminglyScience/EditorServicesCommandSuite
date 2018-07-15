using System;
using System.Collections.Generic;
using static EditorServicesCommandSuite.Internal.Symbols;

namespace EditorServicesCommandSuite.CodeGeneration
{
    internal class MethodChainWriter
    {
        private readonly PowerShellScriptWriter _writer;

        private bool _shouldWriteArgsOnNewLine;

        private bool _isMethodFinishPending;

        private bool _isFirstArgument;

        private bool _wasMethodNameNewLineSkipped;

        internal MethodChainWriter(PowerShellScriptWriter writer)
        {
            _writer = writer;
        }

        internal MethodChainWriter Method(string name, bool skipMethodNameNewLine = false)
        {
            MaybeFinishMethod();
            if (!skipMethodNameNewLine)
            {
                _writer.PushIndent();
                _writer.WriteLine();
            }

            _writer.Write(name);
            _writer.Write(ParenOpen);
            _wasMethodNameNewLineSkipped = skipMethodNameNewLine;
            _isMethodFinishPending = true;
            _shouldWriteArgsOnNewLine = false;
            _isFirstArgument = true;
            return this;
        }

        internal MethodChainWriter ArgumentsOnNewLines()
        {
            _shouldWriteArgsOnNewLine = true;
            _writer.PushIndent();
            return this;
        }

        internal MethodChainWriter Argument(Action<PowerShellScriptWriter> action, string parameterName = null)
        {
            if (!_isFirstArgument)
            {
                _writer.Write(Comma);
                if (!_shouldWriteArgsOnNewLine)
                {
                    _writer.Write(Space);
                }
            }

            if (_shouldWriteArgsOnNewLine)
            {
                _writer.WriteLine();
            }

            _isFirstArgument = false;
            if (!string.IsNullOrEmpty(parameterName))
            {
                _writer.WriteExplicitMethodParameterName(parameterName);
            }

            action(_writer);
            return this;
        }

        internal MethodChainWriter Arguments<TItem>(
            IEnumerable<TItem> arguments,
            Action<PowerShellScriptWriter, TItem> action)
        {
            foreach (TItem item in arguments)
            {
                if (!_isFirstArgument)
                {
                    _writer.Write(Comma);
                    if (!_shouldWriteArgsOnNewLine)
                    {
                        _writer.Write(Space);
                    }
                }

                if (_shouldWriteArgsOnNewLine)
                {
                    _writer.WriteLine();
                }

                _isFirstArgument = false;
                action(_writer, item);
            }

            return this;
        }

        internal void Complete()
        {
            MaybeFinishMethod(isForComplete: true);
        }

        private void MaybeFinishMethod(bool isForComplete = false)
        {
            if (!_isMethodFinishPending)
            {
                return;
            }

            _writer.Write(ParenClose);
            if (!_wasMethodNameNewLineSkipped)
            {
                _writer.PopIndent();
            }

            if (isForComplete)
            {
                return;
            }

            _writer.Write(Dot);
            if (_shouldWriteArgsOnNewLine)
            {
                _shouldWriteArgsOnNewLine = false;
                _writer.PopIndent();
            }
        }
    }
}
