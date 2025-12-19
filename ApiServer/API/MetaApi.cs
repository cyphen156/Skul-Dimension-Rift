using ApiServer.Ops;

namespace ApiServer.API
{
    public static class MetaApi
    {
        public static async Task<IResult> GetMeta(string Key, MetaIndex metaIndex)
        {
            if (string.IsNullOrWhiteSpace(Key) == true)
            {
                return Results.BadRequest(
                    new
                    {
                        error = "missing key",
                        key = string.Empty
                    }
                );
            }

            string k = Key.Trim();

            if (metaIndex.TryGetMetaPath(k, out string metaAbsPath) == false)
            {
                return Results.NotFound(
                    new
                    {
                        error = "meta not found",
                        key = k
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
                        key = k
                    }
                );
            }

            return Results.Text(json, "application/json; charset=utf-8");
        }
    }
}