using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveMusicLite.Commands
{
    public class MediaCommandExecutedEventArgs : EventArgs
    {
        public object Parameter { get; set; }
    }
}
