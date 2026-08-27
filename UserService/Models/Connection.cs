using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserService.Models;

public class Connection
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Key]
    public long Id { get; set; }

    public long RequesterId { get; set; }

    public long TargetId { get; set; }

    public ConnectionStatus Status { get; set; }

}
