using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Assets.Scripts.Utility
{
    public static class Sha256FileTask
    {
        private sealed class State
        {
            public string FilePath;
            public int ChunkBytes;
            public CancellationToken Ct;
            public IProgress<float> Progress;
        }

        public static Task<string> ComputeFileHexAsync(
            string filePath,
            int chunkBytes,
            CancellationToken ct,
            IProgress<float> progress
        )
        {
            if (chunkBytes <= 0)
            {
                chunkBytes = 256 * 1024;
            }

            State state = new State();
            state.FilePath = filePath;
            state.ChunkBytes = chunkBytes;
            state.Ct = ct;
            state.Progress = progress;

            return Task.Factory.StartNew(
                ComputeFileHexWorker,
                state,
                ct,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default
            );
        }

        public static Task<string> ComputeFileHexAsync(
            string filePath,
            int chunkBytes,
            CancellationToken ct
        )
        {
            return ComputeFileHexAsync(filePath, chunkBytes, ct, null);
        }

        private static string ComputeFileHexWorker(object obj)
        {
            State s = obj as State;
            if (s == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(s.FilePath))
            {
                return string.Empty;
            }

            FileStream fs = null;
            SHA256 sha = null;

            long totalBytes = 0;
            long processedBytes = 0;

            bool reachedEof = false;

            byte[] finalHash = null;

            try
            {
                FileInfo fi = new FileInfo(s.FilePath);
                totalBytes = fi.Length;

                fs = new FileStream(s.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
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
                        break;
                    }

                    int readBytes = fs.Read(buffer, 0, buffer.Length);
                    if (readBytes <= 0)
                    {
                        reachedEof = true;
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

                finalHash = new byte[hash.Length];
                Buffer.BlockCopy(hash, 0, finalHash, 0, hash.Length);

                if (reachedEof)
                {
                    if (s.Progress != null)
                    {
                        s.Progress.Report(1.0f);
                    }
                }
            }
            catch
            {
                if (sha != null)
                {
                    try
                    {
                        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                        byte[] hash = sha.Hash;
                        if (hash != null && hash.Length == 32)
                        {
                            finalHash = new byte[hash.Length];
                            Buffer.BlockCopy(hash, 0, finalHash, 0, hash.Length);
                        }
                    }
                    catch
                    {
                    }
                }
            }
            finally
            {
                if (sha != null)
                {
                    sha.Dispose();
                }

                if (fs != null)
                {
                    fs.Dispose();
                }
            }

            return ToLowerHex(finalHash);
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
