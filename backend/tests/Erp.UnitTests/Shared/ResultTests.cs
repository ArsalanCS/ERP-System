using Erp.Shared.Errors;
using Erp.Shared.Results;
using Xunit;

namespace Erp.UnitTests.Shared;

public class ResultTests
{
    [Fact]
    public void Success_has_no_error()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_carries_the_error()
    {
        var error = Error.NotFound("User not found");

        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Generic_success_exposes_the_value()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Accessing_value_of_a_failure_throws()
    {
        Result<int> result = Error.Validation("bad input");

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Value_implicitly_converts_to_success()
    {
        Result<string> result = "ok";

        Assert.True(result.IsSuccess);
        Assert.Equal("ok", result.Value);
    }

    [Fact]
    public void Error_implicitly_converts_to_failure()
    {
        Result<string> result = Error.Conflict("duplicate");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Conflict, result.Error!.Code);
        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }

    [Fact]
    public void Validation_error_maps_to_validation_type_and_code()
    {
        var details = new[] { new ErrorDetail("email", "Email already registered") };

        var error = Error.Validation("Validation failed", details);

        Assert.Equal(ErrorCodes.Validation, error.Code);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("email", error.Details![0].Field);
    }
}
