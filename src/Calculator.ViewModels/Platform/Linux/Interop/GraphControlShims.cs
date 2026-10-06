// GraphControl (WinRT graphing surface) shims for the net10.0 Linux build.
// The graphing UI itself is Phase 4 of the migration; these types exist so the
// graphing ViewModels compile and their non-graphing logic stays testable.

using System;

namespace GraphControl
{
    public enum ErrorType
    {
        Syntax = 0,
        Evaluation = 1,
        Other = 2,
    }

    public enum EvaluationErrorCode
    {
        InvalidExpression = 0,
        DivideByZero = 1,
        EquationHasNoSolution = 2,
        EquationTooComplexToSolve = 3,
        EquationTooComplexToSolveSymbolic = 4,
        EquationTooComplexToPlot = 5,
        InequalityTooComplexToSolve = 6,
        InequalityHasNoSolution = 7,
        RequireDegreesMode = 8,
        RequireRadiansMode = 9,
        Factorial = 10,
        Factorial2 = 11,
        FactorialCannotPerformOnLargeNumber = 12,
        Factorial2CannotPerformOnLargeNumber = 13,
        FactorialInvalidArgument = 14,
        Factorial2InvalidArgument = 15,
        ModuloCannotPerformOnFloat = 16,
        MutuallyExclusiveConditions = 17,
        OutOfDomain = 18,
        TooComplexToSolve = 19,
        Overflow = 20,
        GE_TooComplexToSolve = 21,
        GE_NotSupported = 22,
        GE = 23,
    }

    public enum SyntaxErrorCode
    {
        UnexpectedToken = 0,
        InvalidToken = 1,
        UnexpectedEndOfExpression = 2,
        EmptyExpression = 3,
        InvalidEquationSyntax = 4,
        InvalidEquationFormat = 5,
        TooManyEquals = 6,
        EqualWithoutEquation = 7,
        EqualWithoutGraphVariable = 8,
        ParenthesisMismatch = 9,
        UnmatchedParenthesis = 10,
        TooManyDecimalPoints = 11,
        DecimalPointWithoutDigits = 12,
        InvalidNumberDigit = 13,
        InvalidNumberBase = 14,
        CannotUseComplexInfinityInReal = 15,
        CannotUseIInInequalitySolving = 16,
        CannotUseIInReal = 17,
        CannotUseIndexVarInLimPoint = 18,
        CannotUseIndexVarInOpLimits = 19,
        BracketMismatch = 20,
        UnmatchedBracket = 21,
        ExpectParenthesisAfterFunctionName = 22,
        ExpectingScalarOperands = 23,
        ExpectingLogicalOperands = 24,
        IncorrectNumParameter = 25,
        InvalidVariableNameFormat = 26,
        InvalidVariableSpecification = 27,
    }

    public sealed class Variable
    {
        public Variable()
        {
        }

        public Variable(int initialValue)
        {
            Value = initialValue;
        }

        public double Min { get; set; }
        public double Max { get; set; }
        public double Step { get; set; }
        public double Value { get; set; }
    }

    public sealed class Equation
    {
        public string Expression { get; set; } = string.Empty;
        public Windows.UI.Color LineColor { get; set; }
        public bool IsLineEnabled { get; set; }
    }

    public sealed class Grapher
    {
        public int TrigUnitMode { get; set; }
        public double XAxisMin { get; set; }
        public double XAxisMax { get; set; }
        public double YAxisMin { get; set; }
        public double YAxisMax { get; set; }

        public void SetDisplayRanges(double xMin, double xMax, double yMin, double yMax)
        {
        }

        public void GetDisplayRanges(out double xMin, out double xMax, out double yMin, out double yMax)
        {
            xMin = 0;
            xMax = 10;
            yMin = 0;
            yMax = 10;
        }

        public void ResetGrid()
        {
        }
    }

    public sealed class KeyGraphFeaturesInfo
    {
        public string XIntercept { get; set; } = string.Empty;
        public string YIntercept { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Range { get; set; } = string.Empty;
        public string PeriodicityExpression { get; set; } = string.Empty;
        public string Minima { get; set; } = string.Empty;
        public string Maxima { get; set; } = string.Empty;
        public string InflectionPoints { get; set; } = string.Empty;
        public string VerticalAsymptotes { get; set; } = string.Empty;
        public string HorizontalAsymptotes { get; set; } = string.Empty;
        public string ObliqueAsymptotes { get; set; } = string.Empty;
        public int AnalysisError { get; set; }
        public int TooComplexFeatures { get; set; }
        public int Parity { get; set; }
        public int PeriodicityDirection { get; set; }
        public System.Collections.Generic.Dictionary<string, string> Monotonicity { get; set; } =
            new System.Collections.Generic.Dictionary<string, string>();
    }
}
