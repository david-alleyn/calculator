// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;

namespace CalculatorApp.ViewModel.Common
{
    /// <summary>
    /// Serializes expression commands (operand, unary, binary, parentheses).
    /// This is the C# port of the C++ SerializeCommandVisitor; it uses a managed
    /// binary writer so it runs on both Windows and Linux.
    /// </summary>
    public class ExpressionCommandSerializer
    {
        private readonly BinaryWriter _writer;

        public ExpressionCommandSerializer(BinaryWriter writer)
        {
            _writer = writer;
        }

        public void SerializeOperand(bool isNegative, bool isDecimalPresent, bool isSciFmt, IList<int> commands)
        {
            _writer.Write(isNegative);
            _writer.Write(isDecimalPresent);
            _writer.Write(isSciFmt);

            _writer.Write((uint)commands.Count);
            foreach (int cmd in commands)
            {
                _writer.Write(cmd);
            }
        }

        public void SerializeUnary(IList<int> commands)
        {
            _writer.Write((uint)commands.Count);
            foreach (int cmd in commands)
            {
                _writer.Write(cmd);
            }
        }

        public void SerializeBinary(int command)
        {
            _writer.Write(command);
        }

        public void SerializeParentheses(int command)
        {
            _writer.Write(command);
        }
    }
}
