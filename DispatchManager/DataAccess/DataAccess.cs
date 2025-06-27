using DispatchManager.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;

namespace DispatchManager.DataAccess
{
    public static class DispatchData
    {
        private static readonly string connStr =
            ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;
     }
}
