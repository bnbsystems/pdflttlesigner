using System;
using System.Globalization;

namespace PdfLittleSigner
{
    public static class DDateTimeParser
    {
        public static DateTime ToDateTimeFromDString(this string dstr)
        {
            if (dstr == null) throw new ArgumentNullException();
            string[] formats = new[] {
                "yyyyMMddHHmmss",
                "yyyyMMddHHmmsszzz",
                "'D:'yyyyMMddHHmmss",
                "'D:'yyyyMMddHHmmsszzz",
            };

            dstr = dstr.Replace("'", "");
            if (dstr.Contains('-') || dstr.Contains('+'))
            {
                if (DateTime.TryParseExact(dstr, formats, null, DateTimeStyles.AssumeUniversal, out DateTime dateTime))
                {
                    return dateTime;
                }
            }
            else
            {
                if (DateTime.TryParseExact(dstr, formats, null, DateTimeStyles.AssumeLocal, out DateTime dateTime))
                {
                    return dateTime;
                }
            }

            throw new FormatException();
        }
    }
}
