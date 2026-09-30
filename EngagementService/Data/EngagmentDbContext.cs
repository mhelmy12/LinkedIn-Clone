using System;
using Microsoft.EntityFrameworkCore;

namespace EngagementService.Data;

public class EngagmentDbContext : DbContext
{

    public EngagmentDbContext(DbContextOptions<EngagmentDbContext> options) : base(options)
    {
    }
    

}
