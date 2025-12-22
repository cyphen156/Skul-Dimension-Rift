using ApiServer.API;
using ApiServer.Ops;

namespace ApiServer
{
    public static class Program
    {
        static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            string cdnRoot = Path.Combine(AppContext.BaseDirectory, "CDN");
            string verifyRoot = Path.Combine(cdnRoot, "Verify");
            string metaRoot = Path.Combine(verifyRoot, "Meta");

            MetaIndex metaIndex = new MetaIndex(metaRoot);
            metaIndex.Build();
            builder.Services.AddSingleton(metaIndex);

            WebApplication app = builder.Build();

            app.MapGet("/health", HealthApi.GetHealth);
            app.MapGet("/api/meta", MetaApi.GetMeta);

            app.Run("http://127.0.0.1:5001");
        }
    }
}
