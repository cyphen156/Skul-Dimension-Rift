using ApiServer.Ops;

namespace ApiServer.API
{
    public static class MetaApi
    {
        public static IResult GetMeta(string id, string schema, MetaIndex metaIndex)
        {
            if (string.IsNullOrWhiteSpace(id) == true)
            {
                return Results.BadRequest(
                    new
                    {
                        error = "missing id",
                        id = string.Empty,
                        schema = schema ?? string.Empty
                    }
                );
            }

            if (string.IsNullOrWhiteSpace(schema) == true)
            {
                return Results.BadRequest(
                    new
                    {
                        error = "missing schema",
                        id = id ?? string.Empty,
                        schema = string.Empty
                    }
                );
            }

            string i = id.Trim();
            string s = schema.Trim();

            if (metaIndex.TryGetMetaPath(s, i, out string metaAbsPath) == false)
            {
                return Results.NotFound(
                    new
                    {
                        error = "meta not found",
                        id = i,
                        schema = s
                    }
                );
            }

            return Results.File(metaAbsPath, "application/json; charset=utf-8");
        }
    }
}