using FluentAssertions;
using Awaken.Contracts.Common;

namespace Awaken.UnitTests.Contracts;

public class ApiResponseTests
{
    [Fact]
    public void PreservesDataAndCorrelationId()
    {
        var response = new ApiResponse<string>("ok", "corr-123");

        response.Data.Should().Be("ok");
        response.CorrelationId.Should().Be("corr-123");
    }
}
