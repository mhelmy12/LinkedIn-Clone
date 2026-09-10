using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserService.Models;

public class Connection
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Key]
    public long Id { get; set; }

    public string RequesterId { get; set; }

    public string TargetId { get; set; }

    public ConnectionStatus Status { get; set; }

    public bool IsDeleted { get; set; } = false;

}
