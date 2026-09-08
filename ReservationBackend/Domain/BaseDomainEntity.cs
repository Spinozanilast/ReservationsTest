namespace ReservationBackend.Domain;

public abstract class BaseDomainEntity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public string? CreatedBy { get; private set; }
    public DateTime? LastModifiedAt { get; private set; }
    public string? LastModifiedBy { get; private set; }
    public bool IsDeleted { get; private set; } = false;

    public void UpdateModified(string user)
    {
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = user;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
    }
}