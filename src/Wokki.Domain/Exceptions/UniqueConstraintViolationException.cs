namespace Wokki.Domain.Exceptions;

public sealed class UniqueConstraintViolationException(
    string? tableName,
    string? constraintName,
    Exception innerException) : Exception("A unique constraint was violated.", innerException)
{
    public string? TableName { get; } = tableName;
    public string? ConstraintName { get; } = constraintName;
}
