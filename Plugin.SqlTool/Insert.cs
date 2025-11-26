using FastMember;
using Microsoft.Data.SqlClient;

namespace Plugin.SqlTool
{
    public class Insert
    {
        public static void BulkInsert<T>(SqlConnection conn, string tableName, List<T> data)
        {
            if (data == null || data.Count == 0)
                return;

            using var bulkCopy = new SqlBulkCopy(conn)
            {
                DestinationTableName = tableName,
                BulkCopyTimeout = 0, // no timeout for large inserts
                BatchSize = 5000     // adjust as needed
            };

            // Get all public properties of T
            var props = typeof(T).GetProperties().Where(p => p.CanRead).ToArray();

            // Map each property to a SQL column with the same name
            foreach (var prop in props)
            {
                bulkCopy.ColumnMappings.Add(prop.Name, prop.Name);
            }

            using var reader = ObjectReader.Create(data, props.Select(p => p.Name).ToArray());
            bulkCopy.WriteToServer(reader);
        }
    }
}
