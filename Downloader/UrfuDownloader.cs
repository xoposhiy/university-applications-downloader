using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;

namespace Downloader
{
    class UrfuDownloader
    {
        public Dictionary<string, HtmlDocument> cache = new Dictionary<string, HtmlDocument>();

        public Application[] DownloadApplications(int department, string programName)
        {
            return DownloadApplications(department, programName, 18).Where(s => s.Name.EndsWith(")") && IsFirstWave(s)).Concat(
                DownloadApplications(department, programName, 20).Where(s => s.Name.EndsWith(")") && !IsFirstWave(s)).Concat(
                    DownloadApplications(department, programName, 22)))
                .OrderByDescending(a => a.TotalScore)
                .ToArray();
        }

        private bool IsFirstWave(Application application)
        {
            return application.AdmissionType == AdmissionType.Preferences
                   || application.AdmissionType == AdmissionType.Targeted
                   || application.AdmissionType == AdmissionType.WithoutExams;
        }

        private IEnumerable<Application> DownloadApplications(int department, string programName, int presetId)
        {
            var htmlDoc = cache.TryGetValue(department + "-" + presetId, out var doc)
                ? doc
                : GetApplicationListsDocument(presetId, department, 1);
            cache[department + "-" + presetId] = htmlDoc;
            var students = ExtractApplications(htmlDoc, programName, "по общему конкурсу", AdmissionType.Rating);
            var studentsK = ExtractApplications(htmlDoc, programName, "по договорам об оказании платных образовательных услуг",
                AdmissionType.Contract);
            var studentsL = ExtractApplications(htmlDoc, programName, "в пределах квоты приема лиц, имеющих особое право",
                AdmissionType.Preferences);
            var studentsG =
                ExtractApplications(htmlDoc, programName, "в пределах квоты целевого приема", AdmissionType.Targeted);
            return students.Concat(studentsK).Concat(studentsL).Concat(studentsG);
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

        //TODO: 18 тоже меняется после нулевой волны. Стал 20. Видимо после 1 волны ещё раз поменяется на 21. А после - на 23
        private static HtmlDocument GetApplicationListsDocument(int presetId, int department, int ftype)
        {
            var baseUrl = "https://urfu.ru/";
            var departmentJsonUrl = $"{baseUrl}api/ratings/departmental/{presetId}/{department}/{ftype}";
            var res = departmentJsonUrl.GetJson<UrfuDepartmentResponse>();
            var htmlWeb = new HtmlWeb
            {
                AutoDetectEncoding = false,
                OverrideEncoding = Encoding.UTF8
            };
            var doc = htmlWeb.Load($"{baseUrl}{res.url}");
            Console.WriteLine($"Retrieved department {department}  preset {presetId}");
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