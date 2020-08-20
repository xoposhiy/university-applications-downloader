using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;

namespace Downloader
{
    class UrfuDownloader
    {
        public Dictionary<int, HtmlDocument> cache = new Dictionary<int, HtmlDocument>();

        public Application[] DownloadApplications(int department, string programName)
        {
            var htmlDoc = cache.TryGetValue(department, out var doc) ? doc : GetApplicationListsDocument(department, 1);
            cache[department] = htmlDoc;
            var students = ExtractApplications(htmlDoc, programName, "по общему конкурсу", AdmissionType.Rating);
            var studentsK = ExtractApplications(htmlDoc, programName, "по договорам об оказании платных образовательных услуг",
                AdmissionType.Contract);
            var studentsL = ExtractApplications(htmlDoc, programName, "в пределах квоты приема лиц, имеющих особое право",
                AdmissionType.Preferences);
            var studentsG = ExtractApplications(htmlDoc, programName, "в пределах квоты целевого приема", AdmissionType.Targeted);
            var allStudents = students.Concat(studentsK).Concat(studentsL).Concat(studentsG).ToArray();
            return allStudents;
        }

        private static Application[] ExtractApplications(HtmlDocument doc, string programSubstring, string konkursSubstring, AdmissionType admissionType)
        {
            var tables = doc.DocumentNode.Descendants("table").Where(t => t.Descendants("td").Any(td => td.InnerText.Contains(programSubstring)));
            foreach (var table in tables)
            {
                var formNode = table.NextSibling.NextSibling;
                if (!formNode.InnerText.Contains(konkursSubstring)) continue;
                var studentsTable = formNode.NextSibling;
                var studentLines = studentsTable.Descendants("tr").Skip(2).Select(tr => tr.Descendants("td").Select(td => td.InnerText).ToArray()).ToArray();
                return studentLines.Select(line => ParseApplication(line, admissionType)).ToArray();
            }
            return new Application[0];
        }

        private static HtmlDocument GetApplicationListsDocument(int department, int ftype)
        {
            var baseUrl = "https://urfu.ru/";
            var departmentJsonUrl = $"{baseUrl}api/ratings/departmental/18/{department}/{ftype}";
            var res = departmentJsonUrl.GetJson<UrfuDepartmentResponse>();
            var htmlWeb = new HtmlWeb
            {
                AutoDetectEncoding = false,
                OverrideEncoding = Encoding.UTF8
            };
            var doc = htmlWeb.Load($"{baseUrl}{res.url}");
            Console.WriteLine($"Retrieved department {department}");
            return doc;
        }

        private static Application ParseApplication(string[] values, AdmissionType admissionType)
        {
            try
            {
                var contract = values.Length == 7;
                var i = 0;
                var name = values[i++];
                i++; // regNumber
                var withAgreement = !contract && values[i++].Contains("Да");
                if (values[i].Contains("Без вступительных испытаний"))
                {
                    return Application.Bvi(name, withAgreement);
                }

                var math = Mark.Parse(values[i++]);
                var inf = Mark.Parse(values[i++]);
                var rus = Mark.Parse(values[i++]);
                var additionalScore = int.TryParse(values[i++], out var v) ? v : 0;
                var totalScore = int.Parse(values[i++]);
                return new Application(name, withAgreement, admissionType, math, inf, rus, additionalScore, totalScore);
            }
            catch (Exception e)
            {
                throw new FormatException(string.Join(";", values), e);
            }
        }
    }

    public class UrfuDepartmentResponse
    {
        public string tstamp { get; set; }
        public string url { get; set; }
    }

}