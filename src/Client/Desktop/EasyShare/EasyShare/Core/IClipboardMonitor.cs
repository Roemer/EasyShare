using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyShare.Core
{
    public interface IClipboardMonitor
    {
        event Action ClipboardUpdated;
        void Start();
        void Stop();
    }
}
