using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DispatchManager
{
    internal class Session
    {
        public static Guid? CurrentUserId { get; set; }
        public static string CurrentInitials { get; set; }
        public static string FullName { get; set; }
    }
}
