using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Assets.Scripts.Utility
{
    public static class Sha256StreamTask
    {
        private sealed class State
        {
            public Stream Stream;
            public int ChunkBytes;
            public CancellationToken Ct;
            public IProgress<float> Progress;
        }

        public static Task<string> ComputeHexAsync(
            Stream stream,
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

            State state = new State();
            state.Stream = stream;
            state.ChunkBytes = chunkBytes;
            state.Ct = ct;
            state.Progress = progress;

            return Task.Factory.StartNew(
                ComputeWorker,
                state,
                ct,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default
            );
        }

        public static Task<string> ComputeHexAsync(
            Stream stream,
            int chunkBytes,
            CancellationToken ct
        )
        {
            return ComputeHexAsync(stream, chunkBytes, ct, null);
        }

        private static string ComputeWorker(object obj)
        {
            State s = obj as State;
            if (s == null)
            {
                return string.Empty;
            }

            Stream stream = s.Stream;
            if (stream == null)
            {
                return string.Empty;
            }

            if (s.Ct.IsCancellationRequested)
            {
                return string.Empty;
            }

            if (stream.CanSeek)
            {
                try
                {
                    stream.Position = 0;
                }
                catch
                {
                }
            }

            long totalBytes = 0;
            long processedBytes = 0;

            try
            {
                if (stream.CanSeek)
                {
                    totalBytes = stream.Length;
                }
            }
            catch
            {
                totalBytes = 0;
            }

            SHA256 sha = null;

            try
            {
                sha = SHA256.Create();

                byte[] buffer = new byte[s.ChunkBytes];

                if (s.Progress != null)
                {
                    s.Progress.Report(0.0f);
                }

                while (true)
                {
                    if (s.Ct.IsCancellationRequested)
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

                    if (s.Progress != null && totalBytes > 0)
                    {
                        float p = (float)processedBytes / (float)totalBytes;
                        if (p > 1.0f)
                        {
                            p = 1.0f;
                        }
                        s.Progress.Report(p);
                    }
                }

                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                byte[] hash = sha.Hash;
                if (hash == null || hash.Length != 32)
                {
                    return string.Empty;
                }

                if (s.Progress != null)
                {
                    s.Progress.Report(1.0f);
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
