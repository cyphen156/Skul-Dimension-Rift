using ApiServer.Ops;

namespace ApiServer.API
{
    public static class MetaApi
    {
        public static IResult GetMeta(string platform, string id, string schema, MetaIndex metaIndex)
        {
            if (string.IsNullOrWhiteSpace(platform))
            {
                return Results.BadRequest(
                    new
                    {
                        error = "missing platform",
                        platform = string.Empty,
                        id = id ?? string.Empty,
                        schema = schema ?? string.Empty
                    }
                );
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                return Results.BadRequest(
                    new
                    {
                        error = "missing id",
                        platform = platform ?? string.Empty,
                        id = string.Empty,
                        schema = schema ?? string.Empty
                    }
                );
            }

            if (string.IsNullOrWhiteSpace(schema))
            {
                return Results.BadRequest(
                    new
                    {
                        error = "missing schema",
                        platform = platform ?? string.Empty,
                        id = id ?? string.Empty,
                        schema = string.Empty
                    }
                );
            }

            string p = platform.Trim();
            string i = id.Trim();
            string s = schema.Trim();

            if (metaIndex.TryGetMetaPath(p, s, i, out string metaAbsPath) == false)
            {
                return Results.NotFound(
                    new
                    {
                        error = "meta not found",
                        platform = p,
                        id = i,
                        schema = s
                    }
                );
            }

            return Results.File(metaAbsPath, "application/json; charset=utf-8");
        }
    }
}
