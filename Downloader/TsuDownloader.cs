using System;
using System.Linq;
using HtmlAgilityPack;

namespace Downloader
{
    class TsuDownloader
    {
        public static Application[] GetTsuStudents()
        {
            var doc = GetTsuEnrolleeListsDocument();
            var tables = doc.DocumentNode.Descendants("table").Where(t => t.Id == "sites");
            foreach (var table in tables)
            {
                var studentLines = table.Descendants("tbody").SelectMany(body => body.Descendants("tr"));
                return studentLines.Select(ParseTsuStudent).ToArray();
            }
            return new Application[0];
        }

        private static HtmlDocument GetTsuEnrolleeListsDocument()
        {
            var url = "http://abiturient.tsu.ru/rating?fp=0&dt=0&p=09.03.04_%D0%9E_%D0%91_00000373&l=1&f=8&d=09.03.04&b=%D0%91%D1%8E%D0%B4%D0%B6%D0%B5%D1%82&ef=1";
            //var url = "http://abiturient.tsu.ru/rating?p=09.03.04_%D0%9E_00000373&l=1&f=8&d=09.03.04&b=%D0%91%D1%8E%D0%B4%D0%B6%D0%B5%D1%82&ef=1";
            var htmlWeb = new HtmlWeb();
            var doc = htmlWeb.Load(url);
            Console.WriteLine($"Retrieved TSU");
            return doc;
        }

        private static Application ParseTsuStudent(HtmlNode tr)
        {
            var cells = tr.Descendants("td").Select(td => td.InnerText.Trim()).ToArray();
            try
            {
                //209;Подковырин Иван Валерьевич;На общих основаниях;Нет;Очная;Данные ожидаются
                var code = cells[1];
                var name = cells[2];
                var status = cells[3];
                var admissionType = ParseTsuAdmissionType(status);
                var agreement = cells[4].Contains("Да");
                if (admissionType == AdmissionType.WithoutExams) return Application.Bvi(name, agreement, agreement);
                if (cells[6] == "Данные ожидаются")
                    return new Application(name, agreement, agreement, admissionType, Mark.NA, Mark.NA, Mark.NA, Mark.NA, 0, 0);
                var inf = Mark.Parse(cells[6]);
                var math = Mark.Parse(cells[7]);
                var rus = Mark.Parse(cells[8]);
                var additionalScore = int.TryParse(cells[9], out var v) ? v : 0;
                var totalScore = int.Parse(cells[10]);
                return new Application(name, agreement,agreement, admissionType, math, inf, Mark.NA, rus, additionalScore, totalScore);
            }
            catch (Exception e)
            {
                throw new FormatException(string.Join(";", cells), e);
            }
        }

        private static AdmissionType ParseTsuAdmissionType(string status)
        {
            if (status == "Имеющие особое право") return AdmissionType.Preferences;
            if (status == "Целевое обучение") return AdmissionType.Targeted;
            if (status == "Без вступительных испытаний") return AdmissionType.WithoutExams;
            if (status == "На общих основаниях") return AdmissionType.Rating;
            if (status == "Платно") return AdmissionType.Contract;
            throw new Exception(status);
        }

    }
}