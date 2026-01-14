using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Assets.Scripts.Utility
{
    public static class Sha256StreamTask
    {
        public static Task<string> ComputeHexAsync(
            System.IO.Stream stream,
            int chunkBytes,
            CancellationToken ct,
            IProgress<float> progress
        )
        {
            if (stream == null)
            {
                return Task.FromResult(string.Empty);
            }

            if (chunkBytes <= 0)
            {
                chunkBytes = 256 * 1024;
            }

            object[] state = new object[4];
            state[0] = stream;
            state[1] = chunkBytes;
            state[2] = ct;
            state[3] = progress;

            return Task.Factory.StartNew(
                ComputeWorker,
                state,
                ct,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default
            );
        }

        public static Task<string> ComputeHexAsync(
            System.IO.Stream stream,
            int chunkBytes,
            CancellationToken ct
        )
        {
            return ComputeHexAsync(stream, chunkBytes, ct, null);
        }

        private static string ComputeWorker(object obj)
        {
            object[] s = obj as object[];
            if (s == null || s.Length != 4)
            {
                return string.Empty;
            }

            System.IO.Stream stream = s[0] as System.IO.Stream;
            if (stream == null)
            {
                return string.Empty;
            }

            int chunkBytes = (int)s[1];
            CancellationToken ct = (CancellationToken)s[2];
            IProgress<float> progress = s[3] as IProgress<float>;

            long totalBytes = 0;
            long processedBytes = 0;

            try
            {
                totalBytes = stream.Length;
            }
            catch
            {
                totalBytes = 0;
            }

            SHA256 sha = null;

            try
            {
                sha = SHA256.Create();

                byte[] buffer = new byte[chunkBytes];

                if (progress != null)
                {
                    progress.Report(0.0f);
                }

                while (true)
                {
                    if (ct.IsCancellationRequested)
                    {
                        return string.Empty;
                    }

                    int readBytes = stream.Read(buffer, 0, buffer.Length);
                    if (readBytes <= 0)
                    {
                        break;
                    }

                    sha.TransformBlock(buffer, 0, readBytes, null, 0);
                    processedBytes += readBytes;

                    if (progress != null && totalBytes > 0)
                    {
                        float p = (float)processedBytes / (float)totalBytes;
                        if (p > 1.0f)
                        {
                            p = 1.0f;
                        }
                        progress.Report(p);
                    }
                }

                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                byte[] hash = sha.Hash;
                if (hash == null || hash.Length != 32)
                {
                    return string.Empty;
                }

                if (progress != null)
                {
                    progress.Report(1.0f);
                }

                return ToLowerHex(hash);
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
                if (sha != null)
                {
                    sha.Dispose();
                }
            }
        }

        private static string ToLowerHex(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString("x2"));
            }

            return sb.ToString();
        }
    }
}
