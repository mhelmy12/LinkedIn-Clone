using System;
using IdGen;

namespace UserService.Services.UserIdGenerator;

public class UserIdSnowflakeGenerator : IUserIdGenerator
{
    private readonly IIdGenerator<long> generator;

    public UserIdSnowflakeGenerator(IIdGenerator<long> Generator)
    {
        generator = Generator;
    }
    public string Generate()
    {
        var id = generator.CreateId();
        return id.ToString();
    }
}
