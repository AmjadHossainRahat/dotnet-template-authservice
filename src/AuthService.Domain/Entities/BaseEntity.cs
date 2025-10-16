namespace AuthService.Domain.Entities
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
