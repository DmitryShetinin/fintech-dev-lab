using System.ComponentModel.DataAnnotations.Schema;

namespace Infrastructure.Outbox;

[Table("OutboxMessages")]
public class OutboxMessage
{
  public Guid Id { get; set; }
  public Guid PlantId { get; set; }
  public string EventType { get; set; } = default!;
  public DateTime CreatedAt { get; set; }
}
