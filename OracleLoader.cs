using Oracle.ManagedDataAccess.Client;
using OracleLoader.Exceptions;
using OracleLoader.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace OracleLoader
{
    public class OracleLoader
    {
        #region fields
        List<OracleLoaderColumnInfo> columns = new List<OracleLoaderColumnInfo>();
        List<OracleLoaderRecord> records = new List<OracleLoaderRecord>();
        #endregion

        #region public properties
        public int BufferSize { get; set; } = 100;
        public OracleConnection Connection { get; set; }
        public string TableName { get; set; }
        public bool DisableConstraints { get; set; } = true;
        #endregion

        #region private properties
        OracleLoaderRecord CurrentRow
        {
            get
            {
                return records.Last();
            }
        }
        #endregion

        #region public
        public void Open()
        {
            if (string.IsNullOrEmpty(TableName))
            {
                throw new TableNameNotSetException();
            }

            CreateColumns();

            if (DisableConstraints)
            {
                DoDisableConstraints();
            }
        }

        public void SetValue(string columnName, object value)
        {
            var column = columns.Where(c => c.Name == columnName).FirstOrDefault();
            if (column == null)
            {
                throw new ColumnNotFoundException { ColumnName = columnName };
            }

            if (value == null && !column.AllowDBNull)
            {
                throw new NullValueNotAllowedException { ColumnNames = new List<string> { column.Name } };
            }

            if (!TypeChecker.CanConvert(value, column.Type))
            {
                throw new TypeIncompatibleException
                {
                    ExpectedType = column.Type,
                    GivenType = value.GetType()
                };
            }

            CurrentRow.Add(column.Name, value);
        }

        public void NextRow()
        {
            if (records.Any())
            {
                var nullInfo =
                    columns.Where(c => !c.AllowDBNull)
                    .Select(c => new { Name = c.Name, Value = CurrentRow.GetSafeValue(c.Name) })
                    .Where(c => c.Value == null)
                    .ToList();
                if (nullInfo.Any())
                {
                    throw new NullValueNotAllowedException {
                        ColumnNames = nullInfo.Select(i => i.Name).ToList()
                    };
                }
            }

            if (records.Count >= BufferSize)
            {
                Flush();
            }

            records.Add(new OracleLoaderRecord());
        }

        public void Close()
        {
            Flush();

            if (DisableConstraints)
            {
                DoEnableConstraints();
            }
        }
        #endregion

        #region impl
        void DoDisableConstraints()
        {
            var cmd = Connection.CreateCommand();
            cmd.CommandText = string.Format(@"BEGIN
  FOR c IN
  (SELECT c.owner, c.table_name, c.constraint_name, c.constraint_type
   FROM user_constraints c, user_constraints r
   WHERE c.constraint_type = 'R'
   AND r.owner = c.r_owner
   AND c.r_constraint_name = r.constraint_name
   AND r.table_name = '{0}'
   AND c.status = 'ENABLED'
   UNION ALL
   SELECT c.owner, c.table_name, c.constraint_name, c.constraint_type
   FROM user_constraints c
   WHERE c.constraint_type = 'P'
   AND c.table_name = '{0}')
  LOOP
    DBMS_UTILITY.EXEC_DDL_STATEMENT('ALTER TABLE ""' || c.owner || '"".""' || c.table_name || '"" DISABLE CONSTRAINT ""' || c.constraint_name || '""');
  END LOOP;
            END; ", TableName);
            cmd.ExecuteNonQuery();
        }

        void DoEnableConstraints()
        {
            var cmd = Connection.CreateCommand();
            cmd.CommandText = string.Format(@"BEGIN
  FOR c IN
  (SELECT c.owner, c.table_name, c.constraint_name, c.constraint_type
   FROM user_constraints c, user_constraints r
   WHERE c.constraint_type = 'R'
   AND r.owner = c.r_owner
   AND c.r_constraint_name = r.constraint_name
   AND r.table_name = '{0}'
   AND c.status = 'DISABLED'
   UNION ALL
   SELECT c.owner, c.table_name, c.constraint_name, c.constraint_type
   FROM user_constraints c
   WHERE c.constraint_type = 'P'
   AND c.table_name = '{0}')
  LOOP
    DBMS_UTILITY.EXEC_DDL_STATEMENT('ALTER TABLE ""' || c.owner || '"".""' || c.table_name || '"" ENABLE CONSTRAINT ""' || c.constraint_name || '""');
  END LOOP;
            END; ", TableName);
            cmd.ExecuteNonQuery();
        }

        void CreateColumns()
        {
            columns = new List<OracleLoaderColumnInfo>();
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = TableName;
                cmd.CommandType = System.Data.CommandType.TableDirect;
                using (var reader = cmd.ExecuteReader(System.Data.CommandBehavior.SchemaOnly))
                {
                    using (var table = reader.GetSchemaTable())
                    {
                        foreach (DataRow item in table.Rows)
                        {
                            // skip read only columns
                            if (!(bool)item["IsReadOnly"])
                            {
                                var test = item["ProviderType"];
                                columns.Add(new OracleLoaderColumnInfo
                                {
                                    Name = (string)item["COLUMNNAME"],
                                    Type = (Type)item["DataType"],
                                    AllowDBNull = (bool)item["AllowDBNull"],
                                    ProviderType = (OracleDbType)item["ProviderType"],
                                    Size = (int)item["ColumnSize"]
                                });
                            }
                        }
                    }

                    reader.Close();
                }
            }

            NextRow();
        }

        void Flush()
        {
            records = records.Where(r => r.Any()).ToList();
            if (records.Any())
            {
                var cmd = Connection.CreateCommand();
                cmd.CommandText = string.Format(
                    @"insert into {0} ({1}) values ({2})",
                    TableName,
                    string.Join(", ", columns.Select(c => c.Name)),
                    string.Join(", ", Enumerable.Range(0, columns.Count).Select(c => string.Format(":{0}", c))));
                cmd.ArrayBindCount = records.Count;
                foreach (var column in columns)
                {
                    var p = new OracleParameter();
                    p.OracleDbType = column.ProviderType;
                    p.Size = column.Size;
                    p.Value = records.Select(r => r.GetSafeValue(column.Name)).ToArray();
                    cmd.Parameters.Add(p);
                }

                cmd.ExecuteNonQuery();
            }

            records.Clear();
        }
        #endregion
    }
}
