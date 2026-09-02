using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace UserService.Models;

public class User
{

    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Key]
    public long Id { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? ProfilePictureUrl { get; set; }

    public string? CoverPictureUrl { get; set; }

    public string? About { get; set; }

    public string? Location { get; set; }

    public string? JobTitle { get; set; }


    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public string? Headline { get; set; }

    public string Role { get; set; }




}
