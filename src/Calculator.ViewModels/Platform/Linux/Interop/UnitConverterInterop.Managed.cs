// Managed replacement for the CalcManager.Interop unit converter surface on
// Linux. The conversion engine is ported from src/CalcManager/UnitConverter.cpp
// (the data itself is already supplied by the C# UnitConverterDataLoader).

using System;
using System.Collections.Generic;
using System.Linq;

namespace CalcManager.Interop
{
    public enum UnitConverterCommand
    {
        Zero = 0,
        One = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Decimal = 10,
        Negate = 11,
        Backspace = 12,
        Clear = 13,
        Reset = 14,
        None = 15,
    }

    public struct UnitWrapper
    {
        public int Id;
        public string Name;
        public string AccessibleName;
        public string Abbreviation;
        public bool IsConversionSource;
        public bool IsConversionTarget;
        public bool IsWhimsical;

        public UnitWrapper(int id, string name, string accessibleName, string abbreviation, bool isConversionSource, bool isConversionTarget, bool isWhimsical)
        {
            Id = id;
            Name = name;
            AccessibleName = accessibleName;
            Abbreviation = abbreviation;
            IsConversionSource = isConversionSource;
            IsConversionTarget = isConversionTarget;
            IsWhimsical = isWhimsical;
        }
    }

    public struct CategoryWrapper
    {
        public int Id;
        public string Name;
        public bool SupportsNegative;

        public CategoryWrapper(int id, string name, bool supportsNegative)
        {
            Id = id;
            Name = name;
            SupportsNegative = supportsNegative;
        }
    }

    public struct ConversionDataWrapper
    {
        public double Ratio;
        public double Offset;
        public bool OffsetFirst;

        public ConversionDataWrapper(double ratio, double offset, bool offsetFirst)
        {
            Ratio = ratio;
            Offset = offset;
            OffsetFirst = offsetFirst;
        }
    }

    public struct UnitConversionEntry
    {
        public UnitWrapper Unit;
        public double Ratio;
        public double Offset;
        public bool OffsetFirst;

        public UnitConversionEntry(UnitWrapper unit, double ratio, double offset, bool offsetFirst)
        {
            Unit = unit;
            Ratio = ratio;
            Offset = offset;
            OffsetFirst = offsetFirst;
        }
    }

    public struct SuggestedValueWrapper
    {
        public string Value;
        public UnitWrapper Unit;

        public SuggestedValueWrapper(string value, UnitWrapper unit)
        {
            Value = value;
            Unit = unit;
        }
    }

    public sealed class CategorySelectionResult
    {
        public CategorySelectionResult()
        {
        }

        public CategorySelectionResult(UnitWrapper[] units, UnitWrapper fromUnit, UnitWrapper toUnit)
        {
            Units = units;
            FromUnit = fromUnit;
            ToUnit = toUnit;
        }

        public UnitWrapper[] Units { get; }
        public UnitWrapper FromUnit { get; }
        public UnitWrapper ToUnit { get; }
    }

    public class UnitConverterVMCallbackBase
    {
        protected UnitConverterVMCallbackBase()
        {
        }

        protected virtual void DisplayCallback(string fromValue, string toValue)
        {
        }

        protected virtual void SuggestedValueCallback(SuggestedValueWrapper[] suggestedValues)
        {
        }

        protected virtual void MaxDigitsReached()
        {
        }

        internal void RaiseDisplayCallback(string fromValue, string toValue) => DisplayCallback(fromValue, toValue);
        internal void RaiseSuggestedValueCallback(SuggestedValueWrapper[] suggestedValues) => SuggestedValueCallback(suggestedValues);
        internal void RaiseMaxDigitsReached() => MaxDigitsReached();
    }

    public class ViewModelCurrencyCallbackBase
    {
        protected ViewModelCurrencyCallbackBase()
        {
        }

        protected virtual void CurrencyDataLoadFinished(bool didLoad)
        {
        }

        protected virtual void CurrencySymbolsCallback(string fromSymbol, string toSymbol)
        {
        }

        protected virtual void CurrencyRatiosCallback(string ratioEquality, string accRatioEquality)
        {
        }

        protected virtual void CurrencyTimestampCallback(string timestamp, bool isWeekOldData)
        {
        }

        protected virtual void NetworkBehaviorChanged(int newBehavior)
        {
        }

        internal void RaiseCurrencyDataLoadFinished(bool didLoad) => CurrencyDataLoadFinished(didLoad);
        internal void RaiseCurrencySymbolsCallback(string fromSymbol, string toSymbol) => CurrencySymbolsCallback(fromSymbol, toSymbol);
        internal void RaiseCurrencyRatiosCallback(string ratioEquality, string accRatioEquality) => CurrencyRatiosCallback(ratioEquality, accRatioEquality);
        internal void RaiseCurrencyTimestampCallback(string timestamp, bool isWeekOldData) => CurrencyTimestampCallback(timestamp, isWeekOldData);
        internal void RaiseNetworkBehaviorChanged(int newBehavior) => NetworkBehaviorChanged(newBehavior);
    }

    public abstract class ConverterDataLoaderBase
    {
        protected ConverterDataLoaderBase()
        {
        }

        protected abstract void LoadData();
        protected abstract CategoryWrapper[] GetOrderedCategories();
        protected abstract UnitWrapper[] GetOrderedUnits(CategoryWrapper category);
        protected abstract UnitConversionEntry[] LoadOrderedRatios(UnitWrapper unit);
        protected abstract bool SupportsCategory(CategoryWrapper target);

        // Internal facades so the managed UnitConverterWrapper can drive the
        // loader (WinRT resolves the same calls through the COM vtable).
        internal void InvokeLoadData() => LoadData();
        internal CategoryWrapper[] InvokeGetOrderedCategories() => GetOrderedCategories();
        internal UnitWrapper[] InvokeGetOrderedUnits(CategoryWrapper category) => GetOrderedUnits(category);
        internal UnitConversionEntry[] InvokeLoadOrderedRatios(UnitWrapper unit) => LoadOrderedRatios(unit);
        internal bool InvokeSupportsCategory(CategoryWrapper target) => SupportsCategory(target);
    }

    public sealed class UnitConverterWrapper
    {
        private const int MaximumDigitsAllowed = 15;
        private const int OptimalDigitsAllowed = 7;
        private const double OptimalDecimalAllowed = 1e-6;
        private const double MinimumDecimalAllowed = 1e-14;
        private const char LeftEscapeChar = '{';
        private const char RightEscapeChar = '}';

        private static readonly Dictionary<char, string> QuoteConversions = new Dictionary<char, string>
        {
            ['|'] = "{p}",
            ['['] = "{lc}",
            [']'] = "{rc}",
            [':'] = "{co}",
            [','] = "{cm}",
            [';'] = "{sc}",
            [LeftEscapeChar] = "{lb}",
            [RightEscapeChar] = "{rb}",
        };

        private static readonly Dictionary<string, char> UnquoteConversions = new Dictionary<string, char>
        {
            ["{p}"] = '|',
            ["{lc}"] = '[',
            ["{rc}"] = ']',
            ["{co}"] = ':',
            ["{cm}"] = ',',
            ["{sc}"] = ';',
            ["{lb}"] = LeftEscapeChar,
            ["{rb}"] = RightEscapeChar,
        };

        private static readonly UnitWrapper EmptyUnit = default;

        private readonly ConverterDataLoaderBase _dataLoader;
        private readonly ConverterDataLoaderBase _currencyDataLoader;

        private CategoryWrapper _currentCategory;
        private UnitWrapper _fromType;
        private UnitWrapper _toType;
        private readonly List<CategoryWrapper> _categories = new List<CategoryWrapper>();
        private readonly Dictionary<int, UnitWrapper[]> _categoryToUnits = new Dictionary<int, UnitWrapper[]>();
        private readonly Dictionary<int, UnitConversionEntry[]> _ratioMap = new Dictionary<int, UnitConversionEntry[]>();

        private string _currentDisplay = "0";
        private string _returnDisplay = "0";
        private bool _currentHasDecimal;
        private bool _returnHasDecimal;
        private bool _switchedActive;

        private UnitConverterVMCallbackBase _vmCallback;
        private ViewModelCurrencyCallbackBase _vmCurrencyCallback;

        public UnitConverterWrapper(ConverterDataLoaderBase dataLoader)
            : this(dataLoader, null)
        {
        }

        public UnitConverterWrapper(ConverterDataLoaderBase dataLoader, ConverterDataLoaderBase currencyDataLoader)
        {
            _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
            _currencyDataLoader = currencyDataLoader;
            ClearValues();
            ResetCategoriesAndRatios();
        }

        public void Initialize()
        {
            _dataLoader.InvokeLoadData();
        }

        private bool CheckLoad()
        {
            if (_categories.Count == 0)
            {
                ResetCategoriesAndRatios();
            }
            return _categories.Count != 0;
        }

        public CategoryWrapper[] GetCategories()
        {
            CheckLoad();
            return _categories.ToArray();
        }

        public CategorySelectionResult SetCurrentCategory(CategoryWrapper category)
        {
            if (_currencyDataLoader != null && _currencyDataLoader.InvokeSupportsCategory(category))
            {
                _currencyDataLoader.InvokeLoadData();
            }

            UnitWrapper[] newUnitList = Array.Empty<UnitWrapper>();
            if (CheckLoad())
            {
                if (_currentCategory.Id != category.Id)
                {
                    _currentCategory = category;
                    if (!_currentCategory.SupportsNegative && _currentDisplay.StartsWith("-", StringComparison.Ordinal))
                    {
                        _currentDisplay = _currentDisplay.Substring(1);
                    }
                }

                _categoryToUnits.TryGetValue(category.Id, out newUnitList);
            }

            InitializeSelectedUnits();
            return new CategorySelectionResult(
                newUnitList ?? Array.Empty<UnitWrapper>(),
                _fromType,
                _toType);
        }

        public CategoryWrapper GetCurrentCategory()
        {
            return _currentCategory;
        }

        public void SetCurrentUnitTypes(UnitWrapper fromType, UnitWrapper toType)
        {
            if (!CheckLoad())
            {
                return;
            }

            if (!UnitsEqual(_fromType, fromType))
            {
                _switchedActive = true;
            }

            _fromType = fromType;
            _toType = toType;
            Calculate();

            UpdateCurrencySymbols();
        }

        public void SwitchActive(string newValue)
        {
            if (!CheckLoad())
            {
                return;
            }

            if (newValue != null && newValue.Length > 0)
            {
                _returnDisplay = newValue;
            }

            string temp = _currentDisplay;
            _currentDisplay = _returnDisplay;
            _returnDisplay = temp;

            bool tempDecimal = _currentHasDecimal;
            _currentHasDecimal = _returnHasDecimal;
            _returnHasDecimal = tempDecimal;

            _switchedActive = !_switchedActive;
        }

        public bool IsSwitchedActive => _switchedActive;

        private static bool UnitsEqual(UnitWrapper a, UnitWrapper b)
        {
            return a.Id == b.Id
                && a.Name == b.Name
                && a.Abbreviation == b.Abbreviation
                && a.IsWhimsical == b.IsWhimsical;
        }

        public string SaveUserPreferences()
        {
            return Quote(_currentCategory.Id + ";"
                + QuoteUnit(_fromType) + ";"
                + QuoteUnit(_toType));
        }

        public void RestoreUserPreferences(string userPreferences)
        {
            if (string.IsNullOrEmpty(userPreferences))
            {
                return;
            }

            string[] tokens = userPreferences.Split(';');
            if (tokens.Length != 3)
            {
                return;
            }

            int categoryId;
            if (!int.TryParse(Unquote(tokens[0]), out categoryId))
            {
                return;
            }

            UnitWrapper fromUnit;
            UnitWrapper toUnit;
            if (!TryUnquoteUnit(tokens[1], out fromUnit) || !TryUnquoteUnit(tokens[2], out toUnit))
            {
                return;
            }

            foreach (CategoryWrapper category in _categories)
            {
                if (category.Id == categoryId)
                {
                    if (!_categoryToUnits.TryGetValue(categoryId, out UnitWrapper[] units))
                    {
                        return;
                    }
                    foreach (UnitWrapper unit in units)
                    {
                        if (UnitsEqual(unit, fromUnit))
                        {
                            _fromType = fromUnit;
                        }
                        if (UnitsEqual(unit, toUnit))
                        {
                            _toType = toUnit;
                        }
                    }
                    _currentCategory = category;
                }
            }
        }

        private static string Quote(string text)
        {
            string result = string.Empty;
            foreach (char c in text)
            {
                result += QuoteConversions.TryGetValue(c, out string quoted) ? quoted : c.ToString();
            }
            return result;
        }

        private static string Unquote(string text)
        {
            string result = string.Empty;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == LeftEscapeChar)
                {
                    foreach (var pair in UnquoteConversions)
                    {
                        if (text.Substring(i).StartsWith(pair.Key, StringComparison.Ordinal))
                        {
                            result += pair.Value;
                            i += pair.Key.Length - 1;
                            break;
                        }
                    }
                }
                else
                {
                    result += text[i];
                }
            }
            return result;
        }

        private static string QuoteUnit(UnitWrapper unit)
        {
            return unit.Id + "," + unit.Name + "," + unit.Abbreviation;
        }

        private static bool TryUnquoteUnit(string token, out UnitWrapper unit)
        {
            unit = default;
            string[] parts = token.Split(',');
            if (parts.Length != 3)
            {
                return false;
            }
            int id;
            if (!int.TryParse(parts[0], out id))
            {
                return false;
            }
            unit = new UnitWrapper(id, Unquote(parts[1]), Unquote(parts[1]), Unquote(parts[2]), false, false, false);
            return true;
        }

        public void SendCommand(UnitConverterCommand command)
        {
            if (!CheckLoad())
            {
                return;
            }

            bool clearFront = false;
            bool clearBack = false;
            if (command != UnitConverterCommand.Negate && _switchedActive)
            {
                ClearValues();
                _switchedActive = false;
                clearFront = true;
                clearBack = false;
            }
            else
            {
                clearFront = _currentDisplay == "0";
                clearBack =
                    (_currentHasDecimal && _currentDisplay.Length - 1 >= MaximumDigitsAllowed)
                    || (!_currentHasDecimal && _currentDisplay.Length >= MaximumDigitsAllowed);
            }

            switch (command)
            {
                case UnitConverterCommand.Zero:
                case UnitConverterCommand.One:
                case UnitConverterCommand.Two:
                case UnitConverterCommand.Three:
                case UnitConverterCommand.Four:
                case UnitConverterCommand.Five:
                case UnitConverterCommand.Six:
                case UnitConverterCommand.Seven:
                case UnitConverterCommand.Eight:
                case UnitConverterCommand.Nine:
                    _currentDisplay += (char)('0' + (int)command);
                    break;

                case UnitConverterCommand.Decimal:
                    clearFront = false;
                    clearBack = false;
                    if (!_currentHasDecimal)
                    {
                        _currentDisplay += '.';
                        _currentHasDecimal = true;
                    }
                    break;

                case UnitConverterCommand.Backspace:
                    clearFront = false;
                    clearBack = false;
                    if ((_currentDisplay[0] != '-' && _currentDisplay.Length > 1) || _currentDisplay.Length > 2)
                    {
                        if (_currentDisplay[_currentDisplay.Length - 1] == '.')
                        {
                            _currentHasDecimal = false;
                        }
                        _currentDisplay = _currentDisplay.Substring(0, _currentDisplay.Length - 1);
                    }
                    else
                    {
                        _currentDisplay = "0";
                        _currentHasDecimal = false;
                    }
                    break;

                case UnitConverterCommand.Negate:
                    clearFront = false;
                    clearBack = false;
                    if (_currentCategory.SupportsNegative)
                    {
                        _currentDisplay = _currentDisplay[0] == '-'
                            ? _currentDisplay.Substring(1)
                            : "-" + _currentDisplay;
                    }
                    break;

                case UnitConverterCommand.Clear:
                    clearFront = false;
                    clearBack = false;
                    ClearValues();
                    break;

                case UnitConverterCommand.Reset:
                    clearFront = false;
                    clearBack = false;
                    ClearValues();
                    ResetCategoriesAndRatios();
                    break;
            }

            if (clearFront)
            {
                _currentDisplay = _currentDisplay.Substring(1);
            }
            if (clearBack)
            {
                _currentDisplay = _currentDisplay.Substring(0, _currentDisplay.Length - 1);
                _vmCallback?.RaiseMaxDigitsReached();
            }

            Calculate();
        }

        public void SetViewModelCallback(UnitConverterVMCallbackBase callback)
        {
            _vmCallback = callback;
            if (CheckLoad())
            {
                UpdateViewModel();
            }
        }

        public void SetViewModelCurrencyCallback(ViewModelCurrencyCallbackBase callback)
        {
            _vmCurrencyCallback = callback;
        }

        public void Calculate()
        {
            if (_fromType.Id == EmptyUnit.Id || _toType.Id == EmptyUnit.Id)
            {
                _returnDisplay = _currentDisplay;
                _returnHasDecimal = _currentHasDecimal;
                TrimTrailingZeros(ref _returnDisplay);
                UpdateViewModel();
                return;
            }

            ConversionDataWrapper conversionData = GetConversionData(_fromType, _toType);
            if (conversionData.Ratio == 1.0 && conversionData.Offset == 0.0)
            {
                _returnDisplay = _currentDisplay;
                _returnHasDecimal = _currentHasDecimal;
                TrimTrailingZeros(ref _returnDisplay);
            }
            else
            {
                double currentValue;
                if (!double.TryParse(_currentDisplay, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out currentValue))
                {
                    currentValue = 0.0;
                }

                double returnValue = Convert(currentValue, conversionData);

                bool isCurrencyConverter = _currencyDataLoader != null && _currencyDataLoader.InvokeSupportsCategory(_currentCategory);
                if (isCurrencyConverter)
                {
                    _returnDisplay = RoundSignificantDigits(returnValue, MaximumDigitsAllowed);
                    TrimTrailingZeros(ref _returnDisplay);
                }
                else
                {
                    uint numPreDecimal = GetNumberDigitsWholeNumberPart(returnValue);
                    if (numPreDecimal > MaximumDigitsAllowed || (returnValue != 0 && Math.Abs(returnValue) < MinimumDecimalAllowed))
                    {
                        _returnDisplay = ToScientificNumber(returnValue);
                    }
                    else
                    {
                        uint currentNumberSignificantDigits = GetNumberDigits(_currentDisplay);
                        uint precision;
                        if (Math.Abs(returnValue) < OptimalDecimalAllowed)
                        {
                            precision = (uint)MaximumDigitsAllowed;
                        }
                        else
                        {
                            uint numberDigits = (uint)Math.Max(OptimalDigitsAllowed, (int)Math.Min(MaximumDigitsAllowed, currentNumberSignificantDigits));
                            precision = numberDigits > numPreDecimal ? numberDigits - numPreDecimal : 0;
                        }

                        _returnDisplay = RoundSignificantDigits(returnValue, precision);
                        TrimTrailingZeros(ref _returnDisplay);
                    }
                    _returnHasDecimal = _returnDisplay.Contains(".", StringComparison.Ordinal);
                }
            }
            UpdateViewModel();
        }

        private ConversionDataWrapper GetConversionData(UnitWrapper fromType, UnitWrapper toType)
        {
            if (!_ratioMap.TryGetValue(fromType.Id, out UnitConversionEntry[] entries))
            {
                return default;
            }
            foreach (UnitConversionEntry entry in entries)
            {
                if (entry.Unit.Id == toType.Id)
                {
                    return new ConversionDataWrapper(entry.Ratio, entry.Offset, entry.OffsetFirst);
                }
            }
            return default;
        }

        public void ResetCategoriesAndRatios()
        {
            _switchedActive = false;
            _categories.Clear();
            _categoryToUnits.Clear();
            _ratioMap.Clear();

            foreach (CategoryWrapper category in _dataLoader.InvokeGetOrderedCategories())
            {
                _categories.Add(category);
            }
            if (_categories.Count == 0)
            {
                return;
            }

            _currentCategory = _categories[0];
            bool readyCategoryFound = false;

            foreach (CategoryWrapper category in _categories)
            {
                ConverterDataLoaderBase activeDataLoader = GetDataLoaderForCategory(category);
                if (activeDataLoader == null)
                {
                    continue;
                }

                UnitWrapper[] units = activeDataLoader.InvokeGetOrderedUnits(category);
                _categoryToUnits[category.Id] = units;

                if (units.Length > 0)
                {
                    foreach (UnitWrapper unit in units)
                    {
                        _ratioMap[unit.Id] = activeDataLoader.InvokeLoadOrderedRatios(unit);
                    }

                    if (!readyCategoryFound)
                    {
                        _currentCategory = category;
                        readyCategoryFound = true;
                    }
                }
            }

            InitializeSelectedUnits();
        }

        private ConverterDataLoaderBase GetDataLoaderForCategory(CategoryWrapper category)
        {
            if (_currencyDataLoader != null && _currencyDataLoader.InvokeSupportsCategory(category))
            {
                return _currencyDataLoader;
            }
            return _dataLoader;
        }

        private void InitializeSelectedUnits()
        {
            if (_categoryToUnits.Count == 0)
            {
                return;
            }

            if (!_categoryToUnits.TryGetValue(_currentCategory.Id, out UnitWrapper[] currentUnits))
            {
                return;
            }
            if (currentUnits.Length == 0)
            {
                _fromType = EmptyUnit;
                _toType = EmptyUnit;
                return;
            }

            bool isFromUnitValid = _fromType.Id != EmptyUnit.Id && ContainsUnit(currentUnits, _fromType);
            bool isToUnitValid = _toType.Id != EmptyUnit.Id && ContainsUnit(currentUnits, _toType);

            if (isFromUnitValid && isToUnitValid)
            {
                return;
            }

            bool conversionSourceSet = false;
            bool conversionTargetSet = false;
            foreach (UnitWrapper current in currentUnits)
            {
                if (!conversionSourceSet && current.IsConversionSource && !isFromUnitValid)
                {
                    _fromType = current;
                    conversionSourceSet = true;
                }

                if (!conversionTargetSet && current.IsConversionTarget && !isToUnitValid)
                {
                    _toType = current;
                    conversionTargetSet = true;
                }

                if (conversionSourceSet && conversionTargetSet)
                {
                    return;
                }
            }

            _fromType = EmptyUnit;
            _toType = EmptyUnit;
        }

        private static bool ContainsUnit(UnitWrapper[] units, UnitWrapper unit)
        {
            foreach (UnitWrapper candidate in units)
            {
                if (UnitsEqual(candidate, unit))
                {
                    return true;
                }
            }
            return false;
        }

        private void ClearValues()
        {
            _currentHasDecimal = false;
            _returnHasDecimal = false;
            _currentDisplay = "0";
        }

        private static double Convert(double value, ConversionDataWrapper conversionData)
        {
            if (conversionData.OffsetFirst)
            {
                return (value + conversionData.Offset) * conversionData.Ratio;
            }
            return (value * conversionData.Ratio) + conversionData.Offset;
        }

        private static void TrimTrailingZeros(ref string number)
        {
            if (number.IndexOf('.') < 0)
            {
                return;
            }

            number = number.TrimEnd('0');
            if (number.EndsWith(".", StringComparison.Ordinal))
            {
                number = number.Substring(0, number.Length - 1);
            }
        }

        private static uint GetNumberDigits(string value)
        {
            TrimTrailingZeros(ref value);
            uint result = (uint)value.Length;
            if (value.IndexOf('.') >= 0)
            {
                result--;
            }
            if (value.IndexOf('-') >= 0)
            {
                result--;
            }
            return result;
        }

        private static uint GetNumberDigitsWholeNumberPart(double value)
        {
            return value == 0 ? 1u : (uint)(1 + Math.Max(0.0, Math.Log10(Math.Abs(value))));
        }

        private static string RoundSignificantDigits(double number, uint digits)
        {
            return number.ToString("F" + digits, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string ToScientificNumber(double number)
        {
            return number.ToString("E", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void UpdateCurrencySymbols()
        {
            _vmCurrencyCallback?.RaiseCurrencySymbolsCallback(string.Empty, string.Empty);
        }

        private void UpdateViewModel()
        {
            _vmCallback?.RaiseDisplayCallback(_currentDisplay, _returnDisplay);
            _vmCallback?.RaiseSuggestedValueCallback(CalculateSuggested());
        }

        private SuggestedValueWrapper[] CalculateSuggested()
        {
            if (_currencyDataLoader != null && _currencyDataLoader.InvokeSupportsCategory(_currentCategory))
            {
                return Array.Empty<SuggestedValueWrapper>();
            }

            var regularResults = new List<(string Value, UnitWrapper Unit)>();
            var whimsicalResults = new List<(double Magnitude, string Value, UnitWrapper Unit)>();

            double currentValue;
            if (!double.TryParse(_currentDisplay, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out currentValue))
            {
                currentValue = 0.0;
            }

            _categoryToUnits.TryGetValue(_currentCategory.Id, out UnitWrapper[] units);
            foreach (UnitWrapper unit in units ?? Array.Empty<UnitWrapper>())
            {
                if (UnitsEqual(unit, _fromType) || UnitsEqual(unit, _toType))
                {
                    continue;
                }

                double convertedValue = Convert(currentValue, GetConversionData(_fromType, unit));
                string rounded;
                if (Math.Abs(convertedValue) < 100)
                {
                    rounded = RoundSignificantDigits(convertedValue, 2);
                }
                else if (Math.Abs(convertedValue) < 1000)
                {
                    rounded = RoundSignificantDigits(convertedValue, 1);
                }
                else
                {
                    rounded = RoundSignificantDigits(convertedValue, 0);
                }

                double parsedRounded;
                if (!double.TryParse(rounded, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsedRounded))
                {
                    continue;
                }

                if (parsedRounded != 0.0 || _currentCategory.SupportsNegative)
                {
                    TrimTrailingZeros(ref rounded);
                    if (unit.IsWhimsical)
                    {
                        // The native converter ranks suggested values by their
                        // magnitude order (log10 of the converted value).
                        whimsicalResults.Add((Math.Log10(Math.Abs(convertedValue) > 0 ? Math.Abs(convertedValue) : 1.0), rounded, unit));
                    }
                    else
                    {
                        regularResults.Add((rounded, unit));
                    }
                }
            }

            var results = new List<(string Value, UnitWrapper Unit)>(regularResults);
            if (whimsicalResults.Count > 0)
            {
                // The "best" whimsical value is the one closest to the input
                // magnitude (matching the native converter's choice).
                var best = whimsicalResults
                    .OrderBy(entry => Math.Abs(entry.Magnitude))
                    .ThenByDescending(entry => entry.Magnitude)
                    .First();
                results.Add((best.Value, best.Unit));
            }

            var suggested = new SuggestedValueWrapper[results.Count];
            for (int i = 0; i < results.Count; i++)
            {
                suggested[i] = new SuggestedValueWrapper(results[i].Value, results[i].Unit);
            }
            return suggested;
        }
    }
}
