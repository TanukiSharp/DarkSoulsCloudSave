using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkSoulsCloudSave.DataModels.Configuration.V1
{
    public class Configuration : ConfigurationBase<Configuration>
    {
        public bool AutoRestore { get; set; }
        public bool AutoStore { get; set; }
        public string Token { get; set; }
    }
}
