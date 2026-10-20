namespace Spim.Domain.Exceptions;

/// <summary>Raised when a business rule is violated.</summary>
public class DomainException(string message) : Exception(message);
