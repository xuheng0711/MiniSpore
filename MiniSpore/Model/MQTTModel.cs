using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSpore.Model
{
    public class MQTTModel
    {
        public string Address { get; set; }
        public string Port { get; set; }
        public string ClientID { get; set; }
        public string Account { get; set; }
        public string Password { get; set; }
    }
}
