using Microsoft.AspNetCore.Http;

namespace ApiServer.API
{
    public static class HealthApi
    {
        public static IResult GetHealth()
        {
            return Results.Ok("OK");
        }
    }
}
