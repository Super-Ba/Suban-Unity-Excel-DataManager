#define READ_SHEET

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Suban.DataManager
{
    public class ExcelConverter
    {
        
        private static readonly string GameDataPath = $"{Directory.GetCurrentDirectory()}/_GameData";
        private static readonly string JsonDataPath = $"{Directory.GetCurrentDirectory()}/Assets/Data/GameData";

        
        [MenuItem("GameData/Convert Excel Sheets", false, 3)]
        public static void ConvertExcelToJson()
        {
            try
            {
                var path = new DirectoryInfo(GameDataPath);
                if (!path.Exists)
                {
                    path.Create();
                    return;
                }
                
                path = new DirectoryInfo(JsonDataPath);
                if (!path.Exists)
                {
                    path.Create();
                }
                
                var stopWatch = new Stopwatch();
                
                var files = Directory.GetFiles(GameDataPath, "*.xls*");
                foreach (var file in files)
                {
                    var filename = file.Split('\\', '.', '/');
                    var name = filename[^2];

                    if (name.StartsWith("~$"))
                    {
                        continue;
                    }

                    stopWatch.Start();
                    Debug.Log($"Converting {name}...");

                    
                    if (!File.Exists(file)) continue;
                    using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        var xssWorkbook = new XSSFWorkbook(stream);

#if READ_SHEET
                        
                        for (int i = 0; i < xssWorkbook.NumberOfSheets; i++)
                        {
                            var sheet = xssWorkbook.GetSheetAt(i);
                            if (sheet.SheetName[0] == '#')
                            {
                                continue;
                            }
#else
                        {
                            var sheet = xssWorkbook.GetSheetAt(0);
#endif
                            
                            var data = ExcelDataToJson(sheet);
                            
                            if (data != null)
                            {
                                
#if READ_SHEET
                                var sw = new StreamWriter($"{JsonDataPath}/{sheet.SheetName}.json", false, Encoding.UTF8);
#else
                                var sw = new StreamWriter($"{JsonDataPath}/{name}.json", false, Encoding.UTF8);
#endif
                                sw.WriteLine(data);
                                sw.Close();
                            }
                        }
                        
                    }
                    
                    stopWatch.Stop();
                    Debug.Log($"{name} Finished (time : {stopWatch.ElapsedMilliseconds}ms)");
                    stopWatch.Reset();
                }
                
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            AssetDatabase.Refresh();
        }

        
        private static string ExcelDataToJson(ISheet sheet)
        {
            var result = new StringBuilder();
            
            var columns = new List<string>();
            
                // 1 번째 행은 어떤 데이터가 있는지 확인하는 용도
                result.Append("[\n\t[\n");

                var headerRow = sheet.GetRow(1);
                for (var cellNum = 0; cellNum < headerRow.LastCellNum; cellNum++)
                {
                    var cell = headerRow.GetCell(cellNum);
                    cell?.SetCellType(CellType.String);

                    var name = cell?.ToString().Trim();

                    // #으로 시작하는 column 걸러내기
                    if (string.IsNullOrWhiteSpace(name) || name[0] == '#')
                    {
                        name = null;
                    }

                    columns.Add(name);

                    if (name != null)
                    {
                        result.Append($"\t\t\"{name}\",\n");
                    }
                }

                result.Append("\t],\n\t[\n");

                // 나머지 행들을 돌아가며 데이터 가공

                for (var rowNum = 2; rowNum <= sheet.LastRowNum; rowNum++)
                {
                    var datas = new Dictionary<string, string>();
                    var row = sheet.GetRow(rowNum);

                    if (row == null)
                    {
                        continue;
                    }

                    // #으로 시작하는 row 걸러내기
                    var firstCell = GetCellString(row.GetCell(0));
                    if (!string.IsNullOrWhiteSpace(firstCell) && firstCell[0] == '#')
                    {
                        continue;
                    }
                    
                    for (var cellNum = 0; cellNum < columns.Count; cellNum++)
                    {
                        if (string.IsNullOrEmpty(columns[cellNum]))
                        {
                            continue;
                        }
                        
                        string data = GetCellString(row.GetCell(cellNum));
                        
                        if (!string.IsNullOrWhiteSpace(data))
                        {
                            datas.Add(columns[cellNum], data);
                        }

                    }

                    if (datas.Count != 0)
                    {
                        result.Append("\t\t{\n");
                        foreach (var data in datas)
                        {
                            result.Append($"\t\t\t\"{data.Key}\":\"{data.Value}\",\n");
                        }
                        result.Append("\t\t},\n");
                    }
                }

                result.Append("\t]\n]");

                return result.ToString();
        }
        

        private static string GetCellString(ICell cell)
        {
            if (cell == null || cell.CellType == CellType.Blank)
            {
                return null;
            }
            
            if (cell.CellType == CellType.Formula) 
            {
                switch (cell.CachedFormulaResultType) 
                {
                    case CellType.Boolean:
                        return cell.BooleanCellValue.ToString();
                    case CellType.Numeric:
                        return cell.NumericCellValue.ToString();
                    case CellType.String:
                        return cell.StringCellValue;
                }
            }
            
            cell.SetCellType(CellType.String);
            return cell?.ToString();
        }
    }
}