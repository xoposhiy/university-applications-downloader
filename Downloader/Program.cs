using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

// ReSharper disable IdentifierTypo

namespace Downloader
{
    internal static class Program
    {
        private static readonly string spreadSheetId = "12HdT-UBx0fw__lcwSxt98W3AfkcGRxEoO2lp0lXYSPs";

        private static void Main(string[] args)
        {
            var sleep = TimeSpan.FromMinutes(int.Parse(args[1]));
            var nextTime = DateTime.Now;
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    Console.ReadKey();
                }
                else
                {
                    if (nextTime > DateTime.Now)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                }

                Console.WriteLine(DateTime.Now);
                var urfuDownloader = new UrfuDownloader();
                try
                {
                    Console.WriteLine("Где\tколво\tсогласие\tБВИ\tконтракт");
                    DownloadUrfu(urfuDownloader, "ФИИТ", 4,
                        "ИЕНиМ, 02.03.02 Фундаментальная информатика и информационные технологии (Разработка программных продуктов)");
                    DownloadUrfu(urfuDownloader, "КН МОАИС", 4,
                        "ИЕНиМ, 02.00.00 Компьютерные и информационные науки (Компьютерные и информационные науки (02.03.01, 02.03.03))");
                    DownloadUrfu(urfuDownloader, "КБ", 4, "ИЕНиМ, 10.05.01 Компьютерная безопасность");
                    DownloadUrfu(urfuDownloader, "ПрИнф", 7,
                        "ИРИТ-РТФ, 09.03.03 Прикладная информатика (Прикладная информатика)");
                    DownloadUrfu(urfuDownloader, "ПрИнж", 7,
                        "ИРИТ-РТФ, 09.03.04 Программная инженерия (Программная инженерия)");
                    DownloadUrfu(urfuDownloader, "ИВТ", 7,
                        "ИРИТ-РТФ, 09.03.01 Информатика и вычислительная техника (Информатика и вычислительная техника)");
                    DownloadTsu("ТГУ");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                if (!args.Contains("loop")) break;
                nextTime = DateTime.Now + sleep;
                Console.WriteLine($"Next run: {nextTime}");
            }
        }

        private static void DownloadTsu(string shortName)
        {
            var students = TsuDownloader.GetTsuStudents();
            SaveStudentsTo(spreadSheetId, shortName, students);
        }


        private static void DownloadUrfu(
            UrfuDownloader urfuDownloader, string shortName, int department,
            string programName)
        {
            var allStudents = urfuDownloader.DownloadApplications(department, programName);
            SaveStudentsTo(spreadSheetId, shortName, allStudents);
        }

        private static void SaveStudentsTo(string spreadSheetId, string sheetName, Application[] students)
        {
            Console.Write(
                $"{sheetName}\t{students.Length}\t{students.Count(s => s.WithAgreement)}\t{students.Count(s => s.WithoutExams)}");
            var service = CreateService();

            EnsureSheetExists(spreadSheetId, sheetName, service);

            var range = $"{sheetName}!A1:Z100000";
            var requestBody = new ClearValuesRequest();
            var deleteRequest = service.Spreadsheets.Values.Clear(requestBody, spreadSheetId, range);
            deleteRequest.Execute();

            var valueRange = new ValueRange();

            var values = students.Select(s => s.GetCsvValues())
                .Prepend(Application.GetCsvHeaders().Append(DateTime.Now.ToString()));
            valueRange.Values = values.Select(v => (IList<object>) v.Cast<object>().ToList()).ToList();

            var updateRequest = service.Spreadsheets.Values.Update(valueRange, spreadSheetId, $"{sheetName}!A1");
            updateRequest.ValueInputOption =
                SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            updateRequest.Execute();
            Console.WriteLine(" saved");
        }

        private static void EnsureSheetExists(string spreadSheetId, string sheetName, SheetsService service)
        {
            var request = service.Spreadsheets.Get(spreadSheetId);
            var response = request.Execute();
            if (response.Sheets.All(s => s.Properties.Title != sheetName))
            {
                var addSheetRequest = new AddSheetRequest
                {
                    Properties = new SheetProperties {Title = sheetName}
                };
                var batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest();
                batchUpdateSpreadsheetRequest.Requests = new List<Request>();
                batchUpdateSpreadsheetRequest.Requests.Add(new Request {AddSheet = addSheetRequest});
                var batchUpdateRequest = service.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest, spreadSheetId);
                batchUpdateRequest.Execute();
            }
        }

        private static SheetsService CreateService()
        {
            var credential = GoogleCredential.FromFile("googleapi-credentials.json")
                .CreateScoped(SheetsService.Scope.Spreadsheets);
            return new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "StudentsDownloader"
            });
        }

        public static TValue GetJson<TValue>(this string departmentJsonUrl)
        {
            using var client = new WebClient();
            var doc = client.DownloadString(departmentJsonUrl);
            return JsonSerializer.Deserialize<TValue>(doc);
        }
    }
}