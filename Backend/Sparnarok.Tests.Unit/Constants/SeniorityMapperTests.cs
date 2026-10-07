using Sparnarok.Core.Constants;
using Xunit;

namespace Sparnarok.Tests.Unit.Constants;

public class SeniorityMapperTests
{
    [Theory]
    [InlineData(1, "Junior / Neófito")]
    [InlineData(4, "Junior / Neófito")]
    [InlineData(5, "Semi-Senior / Adepto")]
    [InlineData(9, "Semi-Senior / Adepto")]
    [InlineData(10, "Senior / Veterano")]
    [InlineData(14, "Senior / Veterano")]
    [InlineData(15, "Staff / Maestro")]
    [InlineData(19, "Staff / Maestro")]
    [InlineData(20, "Principal / Leyenda")]
    [InlineData(100, "Principal / Leyenda")]
    public void GetTitleForLevel_ReturnsCorrectTitle(int level, string expectedTitle)
    {
        var title = SeniorityMapper.GetTitleForLevel(level);
        Assert.Equal(expectedTitle, title);
    }
}
