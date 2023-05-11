using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSpore.Model
{
    public class MQTTClientInfo
    {
        public ClientInfo result { get; set; }
        public int code { get; set; }
        public string msg { get; set; }
    }

    public class ClientInfo
    {
        public string clientId { get; set; }
        public string passwords { get; set; }
        public string userName { get; set; }
    }

}
