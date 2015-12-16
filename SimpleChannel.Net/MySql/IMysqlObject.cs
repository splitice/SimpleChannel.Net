using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace SimpleChannel.Net.MySql
{
    public interface IMysqlObject
    {
        void SetValue(String key, object value);
        MySqlCommand GetInsertQuery();
    }
}
