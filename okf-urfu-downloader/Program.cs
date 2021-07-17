using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Google.Apis.Sheets.v4;
using HtmlAgilityPack;
using lib.db;

namespace okf_urfu_downloader
{
    public class Program{
        
        private static readonly string spreadSheetId = "12HdT-UBx0fw__lcwSxt98W3AfkcGRxEoO2lp0lXYSPs";
        private static readonly string sheetName = "ФИИТ okf";

        public static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                var exeName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
                Console.WriteLine("Более безопасный и быстрый вариант:");
                Console.WriteLine("  " + exeName + " <JSESSIONID_COOKIE_VALUE>");
                Console.WriteLine("  JSESSIONID_COOKIE_VALUE нужно скопировать из кук в браузере. И повторять это каждый раз, когда старая сессия протухает");
                Console.WriteLine();
                Console.WriteLine("Этот способ будет проходить аутентификацию каждый раз, что не очень эффективно. И будет принимать пароль, что не очень безопасно");
                Console.WriteLine("  " + exeName + " <login> <password>");
                return;
            }
            
            var client = args.Length == 1
                ? new RatingClient(args[0])
                : new RatingClient(args[0], args[1]);

            var sleep = TimeSpan.FromMinutes(1);
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
                try
                {
                    var budget = await client.Get(
                        "https://okf.urfu.ru/fx/uni/ru.naumen.uni.published_jsp?cc=uncasso2k3g080000m350gp742lv52qs&unit=undiin18ggl5g0000iud4ege0ubra5rc&uuid=fakeobUNI_EntrantCoreRoot&activeComponent=EntrantRating");
                    //var contract = await client.Get(
                    //    "https://okf.urfu.ru/fx/uni/ru.naumen.uni.published_jsp?cc=uncasso2k3g080000m350gp6lpfth30c&unit=undiin18ggl5g0000iud4ege0ubra5rc&uuid=fakeobUNI_EntrantCoreRoot&activeComponent=EntrantRating");
                    //var doc = new HtmlDocument();
                    //budget.Load("doc.html");
                    //htmlDocument.Save("doc.html");
                    var applications = GetStudents(budget, "Фундаментальная информатика").ToList();
                    Console.WriteLine("Количество заявлений: " + applications.Count);
                    Console.WriteLine("Количество согласий: " + applications.Count(a => a.Agreement == "согласие"));
                    SaveStudents(applications);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                nextTime = DateTime.Now + sleep;
                Console.WriteLine($"Next run: {nextTime}");
            }
        }

        private static void SaveStudents(List<StudentApplication> applications)
        {
            var sheet = new GSheetClient().GetSpreadsheet(spreadSheetId).GetSheetByName(sheetName);
            var studentLines = sheet.ReadRange("A2:F");
            PatchStudents(studentLines, applications);
            sheet.Edit().WriteRange((1, 0), studentLines).Execute();
        }

        private static void PatchStudents(List<List<string>> data, List<StudentApplication> freshApplications)
        {
            var apps = freshApplications.ToDictionary(a => a.Fio);
            foreach (var line in data)
            {
                while(line.Count < 7)
                    line.Add("");
                if (line.Count == 0 || string.IsNullOrWhiteSpace(line[0])) continue;
                if (apps.TryGetValue(line[0], out var app))
                {
                    line[1] = app.Concurs;
                    line[2] = app.BonusScore;
                    line[3] = app.TotalScore;
                    line[4] = app.Docs;
                    line[5] = app.Agreement;
                    line[6] = DateTime.Now.ToString("s");
                    apps.Remove(line[0]);
                }
                else
                {
                    line[6] = "пропал";
                }
            }

            foreach (var app in apps.Values)
            {
                data.Add(new List<string>
                {
                    app.Fio,
                    app.Concurs,
                    app.BonusScore,
                    app.TotalScore,
                    app.Docs,
                    app.Agreement,
                    DateTime.Now.ToString("s")
                });
            }
        }


        private static IEnumerable<StudentApplication> GetStudents(HtmlDocument htmlDocument, string programName)
        {
            var tables = htmlDocument.DocumentNode.Descendants("table")
                .Where(t => t.HasClass("supp") && t.Descendants("td").Any(td => td.InnerText.Contains(programName)));
            foreach (var table in tables)
            {
                var formNode = GetNextTable(table);
                var studentsTable = GetNextTable(formNode);
                var studentLines = studentsTable.Descendants("tr").Skip(1)
                    .Select(tr => tr.Descendants("td").Select(td => td.InnerText)
                        .Append(HasAgreement(tr)?"согласие" : "").ToArray()).ToArray();
                foreach (var studentLine in studentLines)
                {
                    yield return Parse(studentLine);
                }
            }
        }

        private static bool HasAgreement(HtmlNode tr)
        {
            var fioCell = tr.Descendants("td").ElementAt(1);
            return fioCell.GetAttributes("style").Any(a => a != null && a.Value != "");
        }

        private static StudentApplication Parse(string[] fields)
        {
            return new(fields[1], fields[2], fields[3], fields[4], fields[5], fields[6]);
        }


        private static HtmlNode GetNextTable(HtmlNode node)
        {
            node = node.NextSibling;
            while (!node.Name.Equals("table", StringComparison.OrdinalIgnoreCase))
                node = node.NextSibling;
            return node;
        }
    }

    public record StudentApplication(string Fio, string Concurs, string BonusScore, string TotalScore, string Docs, string Agreement);
}