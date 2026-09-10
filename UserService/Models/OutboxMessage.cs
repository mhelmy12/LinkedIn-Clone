using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserService.Models;

public class OutboxMessage
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Key]
    public string Id { get; set; }
    public string EventType { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public DateTime OccurredOn { get; set; }
    public string AggregateType { get; set; }
    public string AggregateId { get; set; }

}
