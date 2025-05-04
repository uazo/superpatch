using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Reflection;
using System.Threading;

using DiffPatch.Data;
using SuperPatch.Core;
using SuperPatch.Core.Storages;
using OfficeOpenXml;

namespace SuperPatchUtils
{
  static class Program
  {
    static async Task<int> Main(string[] args)
    {
      ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

      var cmd = new RootCommand();
      foreach (var command in Commands.BromiteRepo.GetCommands())
        cmd.AddCommand(command);
      foreach (var command in Commands.LocalRepo.GetCommands())
        cmd.AddCommand(command);
      foreach (var command in Commands.ParseFlagList.GetCommands())
        cmd.AddCommand(command);
      foreach (var command in Commands.BraveRepo.GetCommands())
        cmd.AddCommand(command);
      foreach (var command in Commands.ConsolidateFlagList.GetCommands())
        cmd.AddCommand(command);
			foreach (var command in Commands.JsonToExcel.GetCommands())
				cmd.AddCommand(command);
			foreach (var command in Commands.FetchVariations.GetCommands())
				cmd.AddCommand(command);
			foreach (var command in Commands.RebaseFixup.GetCommands())
				cmd.AddCommand(command);

			return await cmd.InvokeAsync(args);
    }
  }
}
