using System.Collections.Generic;

namespace SuperPatchUtils.Commands.Utils
{
  public static class ExcelExporter
  {
    public static void ExportToExcel<T>(string FileName, string SheetName, List<T> data)
    {
      using var pck = new OfficeOpenXml.ExcelPackage();
      var wsData = pck.Workbook.Worksheets.Add(SheetName);
      var range = wsData.Cells["A1"].LoadFromCollection(data, true, OfficeOpenXml.Table.TableStyles.None);
      pck.SaveAs(FileName);
    }
  }
}
