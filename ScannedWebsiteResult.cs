using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCrawler
{
    public class ScannedWebsiteResult
    {
        public string Website { get; set; } = "";
        public bool Google { get; set; } = false;
        public string ScanStarted { get; set; } = "";
        public string ScanCompleted { get; set; } = "";
    }
}
