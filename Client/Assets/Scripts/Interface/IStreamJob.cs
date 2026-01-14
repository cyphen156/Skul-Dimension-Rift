using System.IO;
using System.Threading;

namespace Assets.Scripts.Interface
{
    public interface IStreamJob<T>
    {
        bool Execute(Stream stream, CancellationToken ct, out T result);
    }
}
