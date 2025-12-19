using ApiServer.API;
using ApiServer.Ops;

namespace ApiServer
{
    public static class Program
    {
        static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            string remoteDatasRoot = Path.Combine(
                AppContext.BaseDirectory,
                "CDN",
                "RemoteDatas"
            );

            string metaRoot = Path.Combine(
                remoteDatasRoot,
                "Meta"
            );

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
