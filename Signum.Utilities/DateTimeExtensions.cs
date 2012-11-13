﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.Properties;
using System.Globalization;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Signum.Utilities
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Checks if the date is inside a C interval defined by the two given dates
        /// </summary>
        [MethodExpander(typeof(IsInIntervalExpander))]
        public static bool IsInInterval(this DateTime date, DateTime minDate, DateTime maxDate)
        {
            return minDate <= date && date < maxDate;
        }

        /// <summary>
        /// Checks if the date is inside a C interval defined by the two given dates
        /// </summary>
        [MethodExpander(typeof(IsInIntervalExpanderNull))]
        public static bool IsInInterval(this DateTime date, DateTime minDate, DateTime? maxDate)
        {
            return minDate <= date && (maxDate == null || date < maxDate);
        }

        /// <summary>
        /// Checks if the date is inside a C interval defined by the two given dates
        /// </summary>
        [MethodExpander(typeof(IsInIntervalExpanderNullNull))]
        public static bool IsInInterval(this DateTime date, DateTime? minDate, DateTime? maxDate)
        {
            return (minDate == null || minDate <= date) &&
                   (maxDate == null || date < maxDate);
        }

        static void AssertDateOnly(DateTime? date)
        {
            if (date == null)
                return;
            DateTime d = date.Value;
            if (d.Hour != 0 || d.Minute != 0 || d.Second != 0 || d.Millisecond != 0)
                throw new InvalidOperationException("The date has some hours, minutes, seconds or milliseconds");
        }

        /// <summary>
        /// Checks if the date is inside a date-only interval (compared by entires days) defined by the two given dates
        /// </summary>
        [MethodExpander(typeof(IsInIntervalExpander))]
        public static bool IsInDateInterval(this DateTime date, DateTime minDate, DateTime maxDate)
        {
            AssertDateOnly(date);
            AssertDateOnly(minDate);
            AssertDateOnly(maxDate);
            return minDate <= date && date <= maxDate;
        }

        /// <summary>
        /// Checks if the date is inside a date-only interval (compared by entires days) defined by the two given dates
        /// </summary>
        [MethodExpander(typeof(IsInIntervalExpanderNull))]
        public static bool IsInDateInterval(this DateTime date, DateTime minDate, DateTime? maxDate)
        {
            AssertDateOnly(date);
            AssertDateOnly(minDate);
            AssertDateOnly(maxDate);
            return (minDate == null || minDate <= date) &&
                   (maxDate == null || date < maxDate);
        }

        /// <summary>
        /// Checks if the date is inside a date-only interval (compared by entires days) defined by the two given dates
        /// </summary>
        [MethodExpander(typeof(IsInIntervalExpanderNullNull))]
        public static bool IsInDateInterval(this DateTime date, DateTime? minDate, DateTime? maxDate)
        {
            AssertDateOnly(date);
            AssertDateOnly(minDate);
            AssertDateOnly(maxDate);
            return (minDate == null || minDate <= date) &&
                   (maxDate == null || date < maxDate);
        }

        class IsInIntervalExpander : IMethodExpander
        {
            static readonly Expression<Func<DateTime, DateTime, DateTime, bool>> func = (date, minDate, maxDate) => minDate <= date && date < maxDate;

            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {
                return Expression.Invoke(func, arguments[0], arguments[1], arguments[2]);
            }
        }

        class IsInIntervalExpanderNull : IMethodExpander
        {
            Expression<Func<DateTime, DateTime, DateTime?, bool>> func = (date, minDate, maxDate) => minDate <= date && (maxDate == null || date < maxDate);

            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {
                return Expression.Invoke(func, arguments[0], arguments[1], arguments[2]);
            }
        }

        class IsInIntervalExpanderNullNull : IMethodExpander
        {
            Expression<Func<DateTime, DateTime?, DateTime?, bool>> func = (date, minDate, maxDate) =>
                (minDate == null || minDate <= date) &&
                (maxDate == null || date < maxDate);

            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {
                return Expression.Invoke(func, arguments[0], arguments[1], arguments[2]);
            }
        }

        public static int YearsTo(this DateTime min, DateTime max)
        {
            int result = max.Year - min.Year;
            if (max.Month < min.Month || (max.Month == min.Month & max.Day < min.Day))
                result--;

            return result;
        }

        public static int MonthsTo(this DateTime min, DateTime max)
        {
            int result = max.Month - min.Month + (max.Year - min.Year) * 12;
            if (max.Day < min.Day)
                result--;

            return result;
        }

        public static DateSpan DateSpanTo(this DateTime min, DateTime max)
        {
            return DateSpan.FromToDates(min, max);
        }

        public static DateTime Add(this DateTime date, DateSpan dateSpan)
        {
            return dateSpan.AddTo(date);
        }

        public static DateTime Min(DateTime a, DateTime b)
        {
            return a < b ? a : b;
        }

        public static DateTime Max(DateTime a, DateTime b)
        {
            return a > b ? a : b;
        }

        /// <param name="precision">Using Milliseconds does nothing, using Days use DateTime.Date</param>
        public static DateTime TrimTo(this DateTime dateTime, DateTimePrecision precision)
        {
            switch (precision)
            {
                case DateTimePrecision.Days: return dateTime.Date;
                case DateTimePrecision.Hours: return TrimToHours(dateTime);
                case DateTimePrecision.Minutes: return TrimToMinutes(dateTime);
                case DateTimePrecision.Seconds: return TrimToSeconds(dateTime);
                case DateTimePrecision.Milliseconds: return dateTime;
            }
            throw new ArgumentException("precission");
        }

        public static DateTime TrimToSeconds(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Kind);
        }

        public static DateTime TrimToMinutes(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0, dateTime.Kind);
        }

        public static DateTime TrimToHours(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0, dateTime.Kind);
        }

        public static DateTimePrecision GetPrecision(this DateTime dateTime)
        {
            if (dateTime.Millisecond != 0)
                return DateTimePrecision.Milliseconds;
            if (dateTime.Second != 0)
                return DateTimePrecision.Seconds;
            if (dateTime.Minute != 0)
                return DateTimePrecision.Minutes;
            if (dateTime.Hour != 0)
                return DateTimePrecision.Hours;

            return DateTimePrecision.Days;
        }

        public static TimeSpan TrimTo(this TimeSpan timeSpan, DateTimePrecision precision)
        {
            switch (precision)
            {
                case DateTimePrecision.Days: return timeSpan.TrimToDays();
                case DateTimePrecision.Hours: return TrimToHours(timeSpan);
                case DateTimePrecision.Minutes: return TrimToMinutes(timeSpan);
                case DateTimePrecision.Seconds: return TrimToSeconds(timeSpan);
                case DateTimePrecision.Milliseconds: return timeSpan;
            }
            throw new ArgumentException("precission");
        }

        public static TimeSpan TrimToSeconds(this TimeSpan dateTime)
        {
            return new TimeSpan(dateTime.Days, dateTime.Hours, dateTime.Minutes, dateTime.Seconds);
        }

        public static TimeSpan TrimToMinutes(this TimeSpan dateTime)
        {
            return new TimeSpan(dateTime.Days, dateTime.Hours, dateTime.Minutes, 0);
        }

        public static TimeSpan TrimToHours(this TimeSpan dateTime)
        {
            return new TimeSpan(dateTime.Days, dateTime.Hours, 0, 0);
        }

        public static TimeSpan TrimToDays(this TimeSpan dateTime)
        {
            return new TimeSpan(dateTime.Days, 0, 0, 0);
        }

        public static DateTimePrecision? GetPrecision(this TimeSpan timeSpan)
        {
            if (timeSpan.Milliseconds != 0)
                return DateTimePrecision.Milliseconds;
            if (timeSpan.Seconds != 0)
                return DateTimePrecision.Seconds;
            if (timeSpan.Minutes != 0)
                return DateTimePrecision.Minutes;
            if (timeSpan.Hours != 0)
                return DateTimePrecision.Hours;
            if (timeSpan.Days != 0)
                return DateTimePrecision.Days;
            
            return null;
        }

        public static string SmartDatePattern(this DateTime date)
        {
            DateTime currentdate = DateTime.Today;
            return SmartDatePattern(date, currentdate);
        }

        public static string SmartDatePattern(this DateTime date, DateTime currentdate)
        {
            int datediff = (date.Date - currentdate).Days;

            if (-7 <= datediff && datediff <= -2)
                return Resources.DateLast.Formato(CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(date.DayOfWeek).FirstUpper());

            if (datediff == -1)
                return Resources.Yesterday;

            if (datediff == 0)
                return Resources.Today;

            if (datediff == 1)
                return Resources.Tomorrow;

            if (2 <= datediff && datediff <= 7)
                return Resources.DateThis.Formato(CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(date.DayOfWeek).FirstUpper());

            if (date.Year == currentdate.Year)
            {
                string pattern = CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern;
                pattern = Regex.Replace(pattern, "('[^']*')?yyy?y?('[^']*')?", "");
                string dateString = date.ToString(pattern);
                return dateString.Trim().FirstUpper();
            }
            return date.ToLongDateString();
        }

        public static string ToAgoString(this DateTime dateTime)
        {
            DateTime now = dateTime.Kind == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now;

            TimeSpan ts = now.Subtract(dateTime);
            string resource = null;
            if (ts.TotalMilliseconds < 0)
                resource = Resources.In;
            else
                resource = Resources.Ago;

            int months = Math.Abs(ts.Days) / 30;
            if (months > 0)
                return resource.Formato((months == 1 ? Resources._0Month : Resources._0Months).Formato(Math.Abs(months))).ToLower();
            if (Math.Abs(ts.Days) > 0)
                return resource.Formato((ts.Days == 1 ? Resources._0Day : Resources._0Days).Formato(Math.Abs(ts.Days))).ToLower();
            if (Math.Abs(ts.Hours) > 0)
                return resource.Formato((ts.Hours == 1 ? Resources._0Hour : Resources._0Hours).Formato(Math.Abs(ts.Hours))).ToLower();
            if (Math.Abs(ts.Minutes) > 0)
                return resource.Formato((ts.Minutes == 1 ? Resources._0Minute : Resources._0Minutes).Formato(Math.Abs(ts.Minutes))).ToLower();

            return resource.Formato((ts.Seconds == 1 ? Resources._0Second : Resources._0Seconds).Formato(Math.Abs(ts.Seconds))).ToLower();
        }
        
        public static DateTime MonthStart(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, 1, 0, 0, 0, dateTime.Kind); 
        }

        public static string NiceToString(this TimeSpan timeSpan)
        {
            return timeSpan.NiceToString(DateTimePrecision.Milliseconds); 
        }

        public static string NiceToString(this TimeSpan timeSpan, DateTimePrecision precission)
        {
            StringBuilder sb = new StringBuilder();
            bool any = false;
            if (timeSpan.Days != 0 && precission >= DateTimePrecision.Days)
            {
                sb.AppendFormat("{0}d ", timeSpan.Days);
                any = true;
            }

            if ((any || timeSpan.Hours != 0) && precission >= DateTimePrecision.Hours)
            {
                sb.AppendFormat("{0,2}h ", timeSpan.Hours);
                any = true;
            }

            if ((any || timeSpan.Minutes != 0) && precission >= DateTimePrecision.Minutes)
            {
                sb.AppendFormat("{0,2}m ", timeSpan.Minutes);
                any = true;
            }

            if ((any || timeSpan.Seconds != 0) && precission >= DateTimePrecision.Seconds)
            {
                sb.AppendFormat("{0,2}s ", timeSpan.Seconds);
                any = true;
            }

            if ((any || timeSpan.Milliseconds != 0) && precission >= DateTimePrecision.Milliseconds)
            {
                sb.AppendFormat("{0,3}ms", timeSpan.Milliseconds);
            }

            return sb.ToString();
        }

        public static long JavascriptMilliseconds(this DateTime dateTime)
        {
            if (dateTime.Kind != DateTimeKind.Utc)
                throw new InvalidOperationException("dateTime should be UTC"); 

            return (long)new TimeSpan(dateTime.Ticks - new DateTime(1970, 1, 1).Ticks).TotalMilliseconds;
        } 
    }

    public enum DateTimePrecision
    {
        Days,
        Hours,
        Minutes,
        Seconds,
        Milliseconds,
    }

    public struct DateSpan
    {
        public static readonly DateSpan Zero = new DateSpan(0, 0, 0);

        public readonly int Years;
        public readonly int Months;
        public readonly int Days;

        public DateSpan(int years, int months, int days)
        {
            this.Years = years;
            this.Months = months;
            this.Days = days;
        }

        public static DateSpan FromToDates(DateTime min, DateTime max)
        {
            if (min > max) return FromToDates(max, min).Invert();

            int years = max.Year - min.Year;
            int months = max.Month - min.Month;


            if (max.Day < min.Day)
                months -= 1;

            if (months < 0)
            {
                years -= 1;
                months += 12;
            }

            int days = max.Subtract(min.AddYears(years).AddMonths(months)).Days;

            return new DateSpan(years, months, days);
        }

        public DateTime AddTo(DateTime date)
        {
            return date.AddDays(Days).AddMonths(Months).AddYears(Years);
        }

        public DateSpan Invert()
        {
            return new DateSpan(-Years, -Months, -Days);
        }

        public override string ToString()
        {
            string resultado= ", ".Combine(
                         Years == 0 ? null :
                         Years == 1 ? Resources._0Year.Formato(Years) :
                                     Resources._0Years.Formato(Years),
                         Months == 0 ? null :
                         Months == 1 ? Resources._0Month.Formato(Months) :
                                      Resources._0Months.Formato(Months),
                         Days == 0 ? null :
                         Days == 1 ? Resources._0Day.Formato(Days) :
                                    Resources._0Days.Formato(Days));

            if (string.IsNullOrEmpty(resultado))
                resultado = Resources._0Day.Formato(0);

            return resultado;

        }
    }
}