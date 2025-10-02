using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;
using static SuperPatch.Core.Server.Infrastructure.Context.DataUtils;
using static SuperPatch.Core.Server.Infrastructure.Context.DataUtils.StoreProcedure;

namespace SuperPatch.Core.Server.Infrastructure.Context;

public static class ExecuteUtils
{
  public static async Task<int> ExecuteAsync(this DataContext dataContext, StoreProcedure sp)
  {
    if (dataContext.Database.CurrentTransaction?.GetDbTransaction() is not SqlTransaction transaction)
      throw new NotSupportedException("Richiede transazione");

    using var command = sp.GetCommand(transaction.Connection);
    command.Transaction = transaction;
    await command.ExecuteNonQueryAsync();

    if (command.Parameters.Contains(ReturnValueParameter.ReturnValueParamName))
      return (int)command.Parameters[ReturnValueParameter.ReturnValueParamName].Value;
    else
      return 0;
  }
}