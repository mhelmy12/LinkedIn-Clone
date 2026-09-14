using System;
using Microsoft.EntityFrameworkCore;

namespace PostService.Data;

public class PostDbContext : DbContext
{
    public PostDbContext(DbContextOptions<PostDbContext> options) : base(options)
    {
    }


}
