using Microsoft.AspNetCore.Builder;

namespace AssuranceApi.Test.Config;

public class EnvironmentTest
{
    [Fact]
    public void IsNotDevModeByDefault()
    {
        var _builder = WebApplication.CreateBuilder();

        var isDev = AssuranceApi.Config.Environment.IsDevMode(_builder);

        Assert.False(isDev);
    }
}
