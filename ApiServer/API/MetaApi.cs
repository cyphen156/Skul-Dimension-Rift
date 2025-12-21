using ApiServer.Ops;

namespace ApiServer.API
{
    public static class MetaApi
    {
        public static async Task<IResult> GetMeta(string id, string schema, MetaIndex metaIndex)
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

            string json = await FileTextProvider.ReadAllTextAsync(metaAbsPath).ConfigureAwait(false);

            if (string.IsNullOrEmpty(json) == true)
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

            return Results.Text(json, "application/json; charset=utf-8");
        }
    }
}