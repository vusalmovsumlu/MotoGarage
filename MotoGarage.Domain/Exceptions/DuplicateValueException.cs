namespace MotoGarage.Domain.Exceptions;

public class DuplicateValueException : ConflictException
{
    public string IndexName { get; }

    public DuplicateValueException(string indexName, Exception innerException)
        : base("Bu məlumat artıq mövcuddur.", innerException)
    {
        IndexName = indexName;
    }
}
