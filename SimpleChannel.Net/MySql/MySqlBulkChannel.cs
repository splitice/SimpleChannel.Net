using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using SimpleChannel.Net.MySql;

namespace SimpleChannel.Net.MySQL
{
    public class MySqlBulkChannel<T>: IChannel<T> where T: IMysqlObject, new()
    {
        private string _sqlQuery;
        private readonly string _ackQuery = null;
        private MySqlConnection _connection;
        private MySqlDataReader _reader;

        public MySqlBulkChannel(MySqlConnection connection, String sqlQuery)
        {
            _connection = connection;
            _sqlQuery = sqlQuery;
        }


        public void Ack()
        {
            if (_ackQuery != null)
            {
                //TODO: implement?
            }
        }

        public bool Offer(T toPut, int ms)
        {
            Put(toPut);
            return true;
        }

        public void Put(T item)
        {
            var query = item.GetInsertQuery();
            query.Connection = _connection;
            query.ExecuteNonQuery();
        }

        public bool Poll(out T val, int timeout)
        {
            if (_reader == null)
            {
                MySqlCommand command = new MySqlCommand(_sqlQuery, _connection);
                _reader = command.ExecuteReader();
            }

            if (!_reader.HasRows)
            {
                val = default(T);
                return false;
            }

            _reader.Read();
            T v = new T();
            for (int i = 0; i < _reader.FieldCount; i++)
            {
                var field = _reader.GetName(i);
                var value = _reader[i];
                v.SetValue(field, value);
            }
            val = v;

            return true;
        }

        public T Take()
        {
            T ret;
            if (!Poll(out ret, 0))
            {
                return default(T);
                //                throw new Exception();
            }
            return ret;
        }
    }
}
