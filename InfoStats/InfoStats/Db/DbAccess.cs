using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Dapper;
using System.Configuration;

namespace InfoStats.Db
{
    /// <summary>
    /// Class resposible for accessing the database
    /// </summary>
    public class DbAccess
    {
        /// <summary>
        /// Inserts a list of values to the database
        /// </summary>
        /// <param name="newRecords"></param>
        public void Insert(IEnumerable<BibtexRecord> newRecords)
        {
            // sanity check
            if (newRecords == null)
                throw new ArgumentNullException();

            // performing insertion
            using(SqlConnection connection = OpenConnection())
            {
                foreach(BibtexRecord currentRecord in newRecords)
                {
                    connection.Execute(Queries.InsertQuery, currentRecord);
                }
            }
        }
        /// <summary>
        /// Gets all records from the database
        /// </summary>
        /// <returns></returns>
        public List<BibtexRecord> GetAllRecords()
        {
            // performing insertion
            using (SqlConnection connection = OpenConnection())
            {
                return connection.Query<BibtexRecord>(Queries.SelectAllQuery).ToList();
            }
        }

        /// <summary>
        /// Opens the database connection
        /// </summary>
        /// <returns></returns>
        private SqlConnection OpenConnection()
        {
            SqlConnection connection = new SqlConnection(GetConnectionString());
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Gets the connection string to connect to the database
        /// </summary>
        /// <returns></returns>
        private string GetConnectionString()
        {
            return ConfigurationManager.AppSettings["ConnectionString"];
        }
    }
}
