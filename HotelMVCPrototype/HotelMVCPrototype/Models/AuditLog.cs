public class AuditLog
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Who
    public string? UserId { get; set; }  
    public string? UserName { get; set; }      
    public string? Role { get; set; }           

    // What
    public string Action { get; set; } = null!; 
    public string EntityType { get; set; } = null!; 
    public int? EntityId { get; set; }

    // Extra info
    public string? Description { get; set; }     
    public string? DataJson { get; set; }       

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
