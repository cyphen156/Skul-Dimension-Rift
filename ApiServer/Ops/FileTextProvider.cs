using System.Text;

namespace ApiServer.Ops
{
    public static class FileTextProvider
    {
        public static async Task<string> ReadAllTextAsync(string absolutePath)
        {
            if (File.Exists(absolutePath) == false)
            {
                return string.Empty;
            }

            using (FileStream stream = new FileStream(
                absolutePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan
            ))
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                string text = await reader.ReadToEndAsync().ConfigureAwait(false);
                return text;
            }
        }
    }
}