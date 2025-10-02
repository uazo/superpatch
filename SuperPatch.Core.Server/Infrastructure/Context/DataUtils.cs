using System.Data;
using Microsoft.Data.SqlClient;

namespace SuperPatch.Core.Server.Infrastructure.Context
{
  public static partial class DataUtils
  {
    public class SqlQuery(string SqlText) : StoreProcedure(SqlText)
    {
      protected override CommandType GetCommandType() => CommandType.Text;
    }

    public class StoreProcedure(string Name)
    {
      internal class ColumnOutputParameter(DataColumn Column, string Name) : Parameter(Name, null!)
      {
        private readonly DataColumn _Column = Column;
        internal DataColumn Column { get { return _Column; } }
      }

      public class SimpleIntOutputParameter : SimpleOutputParameter
      {
        public override bool CanHaveValue => true;

        public SimpleIntOutputParameter(string Name) : base(Name, SqlDbType.Int, null!)
        {
          _assignDataMethod = parameter =>
          {
            if (parameter.Value == DBNull.Value)
              Value = 0;
            else
              Value = (int)parameter.Value;
          };
        }
      }

      public class SimpleOutputParameter : Parameter
      {
        internal SqlDbType _sqlType;
        internal string _Name;
        internal int _Size;
        internal bool _SizeSpecified = false;
        internal AssignDataDelegate _assignDataMethod;

        public delegate void AssignDataDelegate(SqlParameter parameter);
        public virtual bool CanHaveValue => false;

        public SimpleOutputParameter(string Name, SqlDbType sqlType, AssignDataDelegate assignDataMethod)
          : base(Name, null!)
        {
          _Name = Name;
          _sqlType = sqlType;
          _assignDataMethod = assignDataMethod;
        }

        public SimpleOutputParameter(string Name, SqlDbType sqlType, int Size, AssignDataDelegate assignDataMethod)
          : base(Name, null!)
        {
          _Name = Name;
          _sqlType = sqlType;
          _assignDataMethod = assignDataMethod;
          _Size = Size;
          _SizeSpecified = true;
        }

        internal override void ParseOutputValue(SqlParameter parameter)
        {
          _assignDataMethod?.Invoke(parameter);
        }

        internal override bool IsOutput
          => true;

        internal override SqlParameter ToSqlParameter()
          => new()
          {
            ParameterName = _Name,
            Direction = ParameterDirection.InputOutput,
            SqlDbType = _sqlType,
            Size = _Size,
            Value = DBNull.Value,
          };
      }

      public class ReturnValueParameter : Parameter
      {
        internal const string ReturnValueParamName = "@RETURNVALUE";

        public ReturnValueParameter() : base(ReturnValueParamName, null!)
        {
        }

        internal override SqlParameter ToSqlParameter()
         => new()
         {
           ParameterName = Name,
           SqlDbType = SqlDbType.Int,
           Direction = ParameterDirection.ReturnValue,
         };

        internal override bool AddTo => false;
      }

      public class Parameter(string Name, object Value)
      {
        private readonly string _Name = Name;

        public string Name { get { return _Name; } }
        public object Value { get; set; } = Value;

        internal virtual SqlParameter ToSqlParameter()
          => new()
          {
            ParameterName = _Name,
            Direction = ParameterDirection.Input,
            Value = Value ?? DBNull.Value,
          };

        internal virtual bool AddTo { get { return true; } }

        internal virtual void ParseOutputValue(SqlParameter parameter) { }
        internal virtual bool IsOutput { get { return false; } }
      }

      public StoreProcedure AddObjectAsParameters<T>(T obj)
      {
        var cached = DeepCopyExtensions.GetCacheFor<T>();
        var props = cached.GetProps();

        foreach (var prop in props)
        {
          if (string.IsNullOrEmpty(prop.ColumnName))
          {
            throw new NotSupportedException($"Manca il NOTMAPPED alla proprietà {prop.NameUpper}");
          }
          var value = prop.GetValue(obj) ?? DBNull.Value;
          AddParameter("@" + prop.ColumnName ?? prop.NameUpper, value);
        }

        return this;
      }

      private readonly string _Name = Name;
      public string Name { get { return _Name; } }

      private readonly List<Parameter> _Parameters = [];
      public List<Parameter> Parameters { get { return _Parameters; } }

      private Action<int> _getReturnValue = null!;
      private int _TimeOut = -1;

      public StoreProcedure AddParameter(string ParameterName, object Value)
      {
        _Parameters.Add(new Parameter(ParameterName, Value));
        return this;
      }

      public StoreProcedure SetTimeout(int TimeOut)
      {
        _TimeOut = TimeOut;
        return this;
      }

      public SimpleIntOutputParameter AddOutputParameterInt(string Name)
      {
        var p = new SimpleIntOutputParameter(Name);
        _Parameters.Add(p);
        return p;
      }

      public StoreProcedure AddOutputParameter(DataColumn dataColumn, string Name)
      {
        _Parameters.Add(new ColumnOutputParameter(dataColumn, Name));
        return this;
      }

      public StoreProcedure AddOutputParameter(string Name, SqlDbType sqlType,
        SimpleOutputParameter.AssignDataDelegate assignDataMethod)
      {
        _Parameters.Add(new SimpleOutputParameter(Name, sqlType, assignDataMethod));
        return this;
      }

      public StoreProcedure AddOutputParameter(string Name, SqlDbType sqlType, int Size,
        SimpleOutputParameter.AssignDataDelegate assignDataMethod)
      {
        _Parameters.Add(new SimpleOutputParameter(Name, sqlType, Size, assignDataMethod));
        return this;
      }

      public StoreProcedure AddReturnValue()
      {
        _Parameters.Add(new ReturnValueParameter());
        return this;
      }

      public StoreProcedure AddReturnValue(Action<int> getReturnValue)
      {
        _Parameters.Add(new Parameter(ReturnValueParameter.ReturnValueParamName, null!));
        _getReturnValue = getReturnValue;
        return this;
      }

      internal Action<int> GetReturnValueAction { get { return _getReturnValue; } }

      public interface IDestination
      {
        internal abstract void Parse(SqlDataReader reader, out bool nextResult);
      }

      private readonly List<IDestination> _Destinations = [];

      public StoreProcedure AddDestination(IDestination dest)
      {
        _Destinations.Add(dest);
        return this;
      }

      internal SqlCommand GetCommand(System.Data.Common.DbConnection cnn)
      {
        var cmd = (SqlCommand)cnn.CreateCommand();
        cmd.CommandText = _Name;
        cmd.CommandType = GetCommandType();

        if (_TimeOut != -1)
          cmd.CommandTimeout = _TimeOut;
        else
          cmd.CommandTimeout = 0;

        foreach (Parameter sp in _Parameters)
        {
          if (sp is ColumnOutputParameter columnParameter)
          {
            if (columnParameter.Column.DataType == typeof(string))
            {
              var sqlParam = new SqlParameter(
                columnParameter.Name, SqlDbType.VarChar)
              {
                Direction = ParameterDirection.Output,
                Size = columnParameter.Column.MaxLength
              };
              cmd.Parameters.Add(sqlParam);
            }
          }
          else if (sp is SimpleOutputParameter simpleOutputParameter)
          {
            var sqlParam = new SqlParameter(
              simpleOutputParameter.Name, simpleOutputParameter._sqlType);
            if (simpleOutputParameter._SizeSpecified)
              sqlParam.Size = simpleOutputParameter._Size;
            sqlParam.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(sqlParam);
          }
          else
          {
            if (sp.Name == ReturnValueParameter.ReturnValueParamName)
            {
              cmd.Parameters.Add(ReturnValueParameter.ReturnValueParamName, SqlDbType.Int);
              cmd.Parameters[ReturnValueParameter.ReturnValueParamName].Direction = ParameterDirection.ReturnValue;
            }
            else
            {
              cmd.Parameters.AddWithValue(sp.Name, sp.Value ?? DBNull.Value);
            }
          }
        }

        return cmd;
      }

      protected virtual CommandType GetCommandType() => CommandType.StoredProcedure;

      internal List<IDestination> GetDestinations()
      {
        return _Destinations;
      }
    }
  }
}
