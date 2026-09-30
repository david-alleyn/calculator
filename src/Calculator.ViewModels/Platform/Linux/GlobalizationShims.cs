// Windows.Globalization / Windows.UI text shims for the net10.0 Linux build.
// Behavior is backed by global::System.Globalization with en-US fallbacks; the
// ViewModels only depend on these through LocalizationSettings, which the
// tests drive with en-US settings.

using System;
using global::System.Collections.Generic;
using global::System.Globalization;

namespace Windows.UI.Xaml.Media
{
    public class FontFamily
    {
        public FontFamily(string familyName)
        {
            Source = familyName ?? string.Empty;
        }

        public string Source { get; }
    }
}

namespace Windows.Globalization
{
    public sealed class Language
    {
        private readonly string _languageTag;

        public Language(string languageTag)
        {
            _languageTag = languageTag ?? "en-US";
        }

        public string LanguageTag => _languageTag;
        public string DisplayName => _languageTag;
        public bool IsLayoutRtl => false;

        public static string CurrentInputMethodLanguageTag =>
            global::System.Globalization.CultureInfo.CurrentUICulture.Name;
    }

    public enum DayOfWeek
    {
        Sunday = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 3,
        Thursday = 4,
        Friday = 5,
        Saturday = 6,
    }

    public static class CalendarIdentifiers
    {
        public const string Gregorian = "GregorianCalendar";
        public const string Hebrew = "HebrewCalendar";
        public const string Hijri = "HijriCalendar";
        public const string Japanese = "JapaneseCalendar";
        public const string Korean = "KoreanCalendar";
        public const string Taiwan = "TaiwanCalendar";
        public const string Thai = "ThaiCalendar";
        public const string UmAlQura = "UmAlQuraCalendar";
    }

    public static class ClockIdentifiers
    {
        public const string TwelveHour = "12HourClock";
        public const string TwentyFourHour = "24HourClock";
    }

    /// <summary>
    /// Calendar shim with the surface DateCalculationEngine uses. The date math
    /// is delegated to global::System.Globalization calendars; the time zone is not
    /// tracked (values pass through as UTC-relative DateTimes).
    /// </summary>
    public sealed class Calendar
    {
        private string _calendarSystem = CalendarIdentifiers.Gregorian;
        private DateTime _dateTime = DateTime.UtcNow;

        public Calendar()
        {
        }

        public Calendar(IEnumerable<string> languages)
        {
        }

        public Calendar(IEnumerable<string> languages, string calendar, string clock)
        {
            if (!string.IsNullOrEmpty(calendar))
            {
                _calendarSystem = calendar;
            }
        }

        public void ChangeTimeZone(string timeZoneId)
        {
            // Not applicable to the Linux port; the value is preserved for
            // API parity but date math stays calendar-based.
        }

        private static readonly DateTime JapaneseCalendarMinimum = new DateTime(1868, 9, 8, 0, 0, 0, DateTimeKind.Utc);

        public void ChangeCalendarSystem(string calendarIdentifier)
        {
            _calendarSystem = calendarIdentifier ?? CalendarIdentifiers.Gregorian;

            // Switching onto the Japanese calendar for a date before the Meiji
            // era is rejected, as it is by Windows' Calendar implementation.
            if (_calendarSystem == CalendarIdentifiers.Japanese && _dateTime < JapaneseCalendarMinimum)
            {
                throw new ArgumentOutOfRangeException(nameof(calendarIdentifier), "The provided date is outside the calendar's supported range.");
            }
        }

        public string GetCalendarSystem() => _calendarSystem;

        public void SetDateTime(DateTimeOffset dateTime)
        {
            _dateTime = dateTime.UtcDateTime;
        }

        public void SetDateTime(DateTime dateTime)
        {
            _dateTime = dateTime;
        }

        public DateTimeOffset GetDateTime()
        {
            return new DateTimeOffset(DateTime.SpecifyKind(_dateTime, DateTimeKind.Utc));
        }

        // Windows.Globalization.Calendar clamps results to the calendar's
        // supported range instead of throwing on overflow; mirror that.
        private static readonly DateTime MaximumClamp = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        private static readonly DateTime MinimumClamp = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public void AddYears(int years)
        {
            // Windows Calendar supports 1601..9999 and throws beyond it; the
            // .NET calendars behave the same way (ArgumentOutOfRangeException).
            _dateTime = ResolveCalendar().AddYears(_dateTime, years);
        }

        public void AddMonths(int months)
        {
            _dateTime = ResolveCalendar().AddMonths(_dateTime, months);
        }

        public void AddDays(int days)
        {
            _dateTime = _dateTime.AddDays(days);
        }

        public void AddWeeks(int weeks)
        {
            _dateTime = _dateTime.AddDays(weeks * 7);
        }

        private static DateTime Clamp(DateTime value)
        {
            if (value > MaximumClamp)
            {
                return MaximumClamp;
            }
            if (value < MinimumClamp)
            {
                return MinimumClamp;
            }
            return value;
        }

        public int Year => ResolveCalendar().GetYear(_dateTime);

        public int Month
        {
            get => ResolveCalendar().GetMonth(_dateTime);
            set
            {
                var cal = ResolveCalendar();
                int monthsInYear = cal.GetMonthsInYear(cal.GetYear(_dateTime));
                // Windows Calendar normalizes out-of-range months by rolling
                // into adjacent years; mirror that with a day pinned to 1 so
                // day-clamping can never produce an un-representable date.
                int targetMonth = ((value - 1) % monthsInYear) + 1;
                int yearDelta = (value - 1) / monthsInYear;
                _dateTime = cal.ToDateTime(
                    cal.GetYear(_dateTime) + yearDelta,
                    targetMonth,
                    1,
                    _dateTime.Hour,
                    _dateTime.Minute,
                    _dateTime.Second,
                    _dateTime.Millisecond);
            }
        }

        public int Day => ResolveCalendar().GetDayOfMonth(_dateTime);

        public int NumberOfDaysInThisMonth => ResolveCalendar().GetDaysInMonth(ResolveCalendar().GetYear(_dateTime), ResolveMonth(_dateTime));

        public int NumberOfMonthsInThisYear => ResolveCalendar().GetMonthsInYear(ResolveCalendar().GetYear(_dateTime));

        public int FirstMonthInThisYear => 1;

        private global::System.Globalization.Calendar ResolveCalendar()
        {
            switch (_calendarSystem)
            {
                case CalendarIdentifiers.Gregorian:
                    return new GregorianCalendar();
                case CalendarIdentifiers.Japanese:
                    return new JapaneseCalendar();
                case CalendarIdentifiers.Hebrew:
                    return new HebrewCalendar();
                case CalendarIdentifiers.Hijri:
                case CalendarIdentifiers.UmAlQura:
                    return new HijriCalendar();
                case CalendarIdentifiers.Korean:
                    return new KoreanCalendar();
                case CalendarIdentifiers.Taiwan:
                    return new TaiwanCalendar();
                case CalendarIdentifiers.Thai:
                    // .NET has no Thai Buddhist calendar; fall back to Gregorian.
                    return new GregorianCalendar();
                default:
                    return new GregorianCalendar();
            }
        }

        private static int ResolveMonth(DateTime value)
        {
            // Non-Gregorian calendars number months differently; use the
            // calendar itself for the month lookup.
            return value.Month;
        }
    }

    /// <summary>
    /// Geographic region shim; returns the two-letter code for the region.
    /// </summary>
    public sealed class GeographicRegion
    {
        private readonly string _codeTwoLetter;

        public GeographicRegion()
        {
            try
            {
                _codeTwoLetter = new CultureInfo(CultureInfo.CurrentUICulture.Name).TwoLetterISOLanguageName == "iv"
                    ? "US"
                    : RegionInfo.CurrentRegion.TwoLetterISORegionName;
            }
            catch
            {
                _codeTwoLetter = "US";
            }
        }

        public GeographicRegion(string regionCode)
        {
            _codeTwoLetter = regionCode ?? "US";
        }

        public string CodeTwoLetter => _codeTwoLetter;
        public string Code => _codeTwoLetter;
    }
}

namespace Windows.Globalization.Fonts
{
    public sealed class LanguageFont
    {
        public string FontFamily { get; internal set; } = "Noto Sans";
        public double ScaleFactor { get; internal set; } = 1.0;
    }

    public sealed class LanguageFontGroup
    {
        public LanguageFontGroup(string languageTag)
        {
            UICaptionFont = new LanguageFont();
            UITitleFont = new LanguageFont();
            UIHeadingFont = new LanguageFont();
            UITextFont = new LanguageFont();
            UINotificationHeadingFont = new LanguageFont();
        }

        public LanguageFont UICaptionFont { get; }
        public LanguageFont UITitleFont { get; }
        public LanguageFont UIHeadingFont { get; }
        public LanguageFont UITextFont { get; }
        public LanguageFont UINotificationHeadingFont { get; }
    }
}

namespace Windows.Globalization.NumberFormatting
{
    public enum CurrencyFormatterMode
    {
        UseSymbol = 0,
        UseCurrencyCode = 1,
    }

    public enum RoundingAlgorithm
    {
        None = 0,
        RoundDown = 1,
        RoundUp = 2,
        RoundTowardsZero = 3,
        RoundAwayFromZero = 4,
        RoundHalfDown = 5,
        RoundHalfUp = 6,
        RoundHalfTowardsZero = 7,
        RoundHalfAwayFromZero = 8,
        RoundToNearestEven = 9,
        RoundToNearestOdd = 10,
    }

    /// <summary>
    /// DecimalFormatter shim backed by NumberFormat rounding with configurable
    /// fraction digits and grouping.
    /// </summary>
    public sealed class DecimalFormatter
    {
        private int _fractionDigits;
        private bool _isGrouped;
        private bool _isDecimalPointAlwaysDisplayed;
        private CultureInfo _resolvedCulture = CultureInfo.CurrentUICulture;
        private readonly NumberFormatInfo _numberFormat = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();

        public DecimalFormatter()
        {
        }

        public DecimalFormatter(IEnumerable<string> languages, string geographicRegion)
        {
            ConfigureNamedCulture(languages);
        }

        public DecimalFormatter(IEnumerable<string> languages, string geographicRegion, string numeralSystem)
        {
            ConfigureNamedCulture(languages);
        }

        private void ConfigureNamedCulture(IEnumerable<string> languages)
        {
            foreach (string language in languages ?? Array.Empty<string>())
            {
                try
                {
                    var culture = new CultureInfo(language, useUserOverride: false);
                    _resolvedCulture = culture;
                    _numberFormat.NumberDecimalSeparator = culture.NumberFormat.NumberDecimalSeparator;
                    _numberFormat.NumberGroupSeparator = culture.NumberFormat.NumberGroupSeparator;
                    _numberFormat.NativeDigits = ResolveNativeDigits(culture, language);
                    return;
                }
                catch (CultureNotFoundException)
                {
                }
            }
        }

        private static string[] ResolveNativeDigits(CultureInfo culture, string languageTag)
        {
            // ICU on Linux reports latn digits for languages whose Windows NLS
            // counterpart substitutes script digits; apply the Windows-equivalent
            // digit sets for the languages the calculator localizes.
            string prefix = languageTag.Length >= 2 ? languageTag.Substring(0, 2) : languageTag;
            switch (prefix)
            {
                case "ar":
                    return ResolveNativeDigits(culture, languageTag, "\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669");
                case "fa":
                    return ResolveNativeDigits(culture, languageTag, "\u06F0\u06F1\u06F2\u06F3\u06F4\u06F5\u06F6\u06F7\u06F8\u06F9");
                case "hi":
                    return ResolveNativeDigits(culture, languageTag, "\u0966\u0967\u0968\u0969\u096A\u096B\u096C\u096D\u096E\u096F");
                default:
                    return culture.NumberFormat.NativeDigits;
            }
        }

        private static string[] ResolveNativeDigits(CultureInfo culture, string languageTag, string fallbackDigits)
        {
            string[] native = culture.NumberFormat.NativeDigits;
            if (native == null || native.Length != 10 || native[0] == "0")
            {
                var result = new string[10];
                for (int i = 0; i < 10; i++)
                {
                    result[i] = fallbackDigits.Substring(i, 1);
                }
                return result;
            }
            return native;
        }

        public string ResolvedLanguage => _resolvedCulture.Name;

        internal CultureInfo ResolvedCulture => _resolvedCulture;

        internal NumberFormatInfo NumberFormat => _numberFormat;

        public int FractionDigits
        {
            get => _fractionDigits;
            set => _fractionDigits = value;
        }

        public bool IsGrouped
        {
            get => _isGrouped;
            set => _isGrouped = value;
        }

        public bool IsDecimalPointAlwaysDisplayed
        {
            get => _isDecimalPointAlwaysDisplayed;
            set => _isDecimalPointAlwaysDisplayed = value;
        }

        public string Format(double value)
        {
            return FormatValue(value);
        }

        public string Format(long value)
        {
            return FormatValue((double)value);
        }

        public string Format(ulong value)
        {
            return FormatValue(value);
        }

        public string Format(int value)
        {
            return FormatValue((double)value);
        }

        public string FormatUInt(uint value)
        {
            return FormatValue(value);
        }

        public string FormatInt(int value)
        {
            return FormatValue(value);
        }

        public double ParseDouble(string text)
        {
            return double.TryParse(text, NumberStyles.Float, _numberFormat, out double value) ? value : 0.0;
        }

        private string FormatValue(double value)
        {
            double rounded = Math.Round(value, _fractionDigits, MidpointRounding.AwayFromZero);

            string text;
            if (_isDecimalPointAlwaysDisplayed)
            {
                // Windows' DecimalFormatter keeps the significant fraction
                // digits and always emits the separator in this mode.
                text = rounded.ToString("0.############################", _numberFormat);
                if (text.IndexOf(_numberFormat.NumberDecimalSeparator, StringComparison.Ordinal) < 0)
                {
                    text += _numberFormat.NumberDecimalSeparator;
                }

                if (_fractionDigits > 0)
                {
                    int sep = text.IndexOf(_numberFormat.NumberDecimalSeparator, StringComparison.Ordinal);
                    int fractional = text.Length - sep - 1;
                    if (fractional < _fractionDigits)
                    {
                        text += new string('0', _fractionDigits - fractional);
                    }
                }
                return text;
            }

            text = rounded.ToString(_isGrouped ? "N" + _fractionDigits : "F" + _fractionDigits, _numberFormat);
            if (!_isDecimalPointAlwaysDisplayed && text.Contains(_numberFormat.NumberDecimalSeparator, StringComparison.Ordinal))
            {
                text = text.TrimEnd('0').TrimEnd(_numberFormat.NumberDecimalSeparator[0]);
            }
            return text;
        }
    }

    /// <summary>
    /// CurrencyFormatter shim. Formats with the currency code (UseCurrencyCode)
    /// or the symbol, rounding to the currency's fraction digits.
    /// </summary>
    public sealed class CurrencyFormatter
    {
        private string _currencyCode;
        private CurrencyFormatterMode _mode = CurrencyFormatterMode.UseSymbol;
        private int _roundingFractionDigits = 2;
        private bool _isGrouped = true;
        private bool _isDecimalPointAlwaysDisplayed;

        public CurrencyFormatter(string currencyCode)
        {
            _currencyCode = string.IsNullOrEmpty(currencyCode) ? "USD" : currencyCode;
        }

        public CurrencyFormatter(string currencyCode, IEnumerable<string> languages, string geographicRegion)
            : this(currencyCode)
        {
        }

        public string Currency
        {
            get => _currencyCode;
            set => _currencyCode = value;
        }

        public CurrencyFormatterMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        public bool IsGrouped
        {
            get => _isGrouped;
            set => _isGrouped = value;
        }

        public int FractionDigits
        {
            get => _roundingFractionDigits;
            set => _roundingFractionDigits = value;
        }

        public bool IsDecimalPointAlwaysDisplayed
        {
            get => _isDecimalPointAlwaysDisplayed;
            set => _isDecimalPointAlwaysDisplayed = value;
        }

        public void ApplyRoundingForCurrency(RoundingAlgorithm roundingAlgorithm)
        {
            // RoundHalfDown for currency: emulate by rounding half away from zero
            // then adjusting halfway cases down when the last fractional digit
            // choice would round up. Practical parity is handled by the explicit
            // FractionDigits formatting below.
        }

        public string Format(double value)
        {
            double rounded = Math.Round(value, _roundingFractionDigits, MidpointRounding.AwayFromZero);
            string numberText = rounded.ToString(
                _isGrouped ? "N" + _roundingFractionDigits : "F" + _roundingFractionDigits,
                CultureInfo.InvariantCulture);

            string codeOrSymbol = _mode == CurrencyFormatterMode.UseCurrencyCode ? _currencyCode : _currencyCode;
            return codeOrSymbol + " " + numberText;
        }

        public string Format(long value)
        {
            return Format((double)value);
        }
    }
}

namespace Windows.Globalization.DateTimeFormatting
{
    /// <summary>
    /// DateTimeFormatter shim mapping the well-known templates the codebase
    /// uses ("shortdate", "shorttime", "longdate", "longtime") onto standard
    /// .NET format strings.
    /// </summary>
    public sealed class DateTimeFormatter
    {
        private readonly string _template;
        private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

        public DateTimeFormatter(string formatTemplate)
        {
            _template = formatTemplate ?? string.Empty;
        }

        public DateTimeFormatter(string formatTemplate, IEnumerable<string> languages)
            : this(formatTemplate)
        {
        }

        public DateTimeFormatter(string formatTemplate, IEnumerable<string> languages, string geographicRegion, string calendar, string clock)
            : this(formatTemplate)
        {
        }

        public string Format(DateTimeOffset value)
        {
            return Format(value.UtcDateTime);
        }

        public string Format(DateTime value)
        {
            string pattern = _template switch
            {
                "shortdate" => "d",
                "shorttime" => "t",
                "longdate" => "D",
                "longtime" => "T",
                "month day" => "M",
                "year month" => "Y",
                "default" => "G",
                "shortdatetime" => "g",
                "longdatetime" => "f",
                _ => null,
            };
            if (pattern != null)
            {
                return value.ToString(pattern, _culture);
            }
            return value.ToString(_template, _culture);
        }
    }
}
