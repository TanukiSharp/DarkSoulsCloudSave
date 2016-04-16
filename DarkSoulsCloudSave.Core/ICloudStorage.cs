using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkSoulsCloudSave.Core
{
    public interface ICloudStorage : IDisposable
    {
        Task Initialize();
        Task<string[]> ListFiles();
        Task<Stream> Download(string fullFilename);
        Task Upload(string fullFilename, Stream stream);
    }
}
