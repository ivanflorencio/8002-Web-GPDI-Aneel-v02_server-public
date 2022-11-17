using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace PeD.Core
{

    public class EmailSettings
    {
        public string Mail { get; set; }
        public string DisplayName { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
    }

}