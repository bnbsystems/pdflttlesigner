using FluentAssertions;
using PdfLittleSigner;
using System;
using System.Threading.Tasks;
using Xunit;

namespace PdfLittleSignerSpecification
{
    public class DDateTimeParserTests
    {

        [Theory]
        [InlineData("20230527014059", "2023-05-27 01:40:59")]
        [InlineData("D:20230527014059", "2023-05-27 01:40:59")]
        [InlineData("20230527014059+02'00'", "2023-05-27 01:40:59+02:00")]
        [InlineData("D:20230527014059+02'00'", "2023-05-27 01:40:59+02:00")]
        [InlineData("D:20230527014059+00'00'", "2023-05-27 01:40:59+00:00")]
        [InlineData("D:20230527014059+0200", "2023-05-27 01:40:59+02:00")]
        [InlineData("D:20230527014059+0000", "2023-05-27 01:40:59+00:00")]
        [InlineData("20230527014059+00'00'", "2023-05-27 01:40:59+00:00")]
        [InlineData("D:20230527014059-0300", "2023-05-27 01:40:59-03:00")]
        [InlineData("20230527014059-03'00'", "2023-05-27 01:40:59-03:00")]
        [InlineData("D:20230527211515+02'00'", "2023-05-27 21:15:15+02:00")]
        [InlineData("20230527211515+02'00'", "2023-05-27 21:15:15+02:00")]
        [InlineData("20230527211515", "2023-05-27 21:15:15")]
        [InlineData("D:20230527211515-04'00'", "2023-05-27 21:15:15-04:00")]
        [InlineData("20230527211515-04'00'", "2023-05-27 21:15:15-04:00")]
        [InlineData("D:20230527211515+05'30'", "2023-05-27 21:15:15+05:30")]
        [InlineData("D:20010101010101+01'00'", "2001-01-01 01:01:01+01:00")]
        [InlineData("D:20991231235959+00'00'", "2099-12-31 23:59:59+00:00")]
        public void It_should_return_correct_date(string dstr, string expectedDateInStdFormat)
        {
            DateTime expected = DateTime.Parse(expectedDateInStdFormat);
            var res = DDateTimeParser.ToDateTimeFromDString(dstr);
            res.Should().BeCloseTo(expected, TimeSpan.FromSeconds(1));
        }


        [Fact]
        public void It_should_throw_argument_exception()
        {
            Action act = () => DDateTimeParser.ToDateTimeFromDString(null);
            act.Should().Throw<ArgumentException>();
        }


        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("D")]
        [InlineData("D:")]
        [InlineData("20230527")] // only date
        [InlineData("d:20230527014059+00'00'")] // lower d
        [InlineData("D:20231327014059+00'00'")] // 13th month
        [InlineData("20230527014060")] // 60 second
        [InlineData("D: 20230527014059")] // spaces 
        [InlineData("2023 05 27 01 40 59 +02'00'")] // spaces 
        [InlineData("D:20230527014059 +02'00'")] // spaces 
        public void It_should_throw_format_exception(string dstr)
        {
            Action act = () => DDateTimeParser.ToDateTimeFromDString(dstr);
            act.Should().Throw<FormatException>();
        }
    }
}
