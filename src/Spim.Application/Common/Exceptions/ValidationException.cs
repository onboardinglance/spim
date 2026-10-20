using FluentValidation.Results;

namespace Spim.Application.Common.Exceptions;

public sealed class ValidationException(IEnumerable<ValidationFailure> failures)
    : Exception("One or more validation failures have occurred.")
{
    public IDictionary<string, string[]> Errors { get; } = failures
        .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
        .ToDictionary(g => g.Key, g => g.ToArray());
}
