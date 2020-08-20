using System.ComponentModel;
using System.Reflection;

namespace Downloader
{
    public enum AdmissionType
    {
        [Description("БВИ")] WithoutExams,
        [Description("конкурс")] Rating,
        [Description("контракт")] Contract,
        [Description("льготный")] Preferences,
        [Description("целевой")] Targeted
    }

    public class Application
    {
        public Application(string name, bool withAgreement, AdmissionType admissionType, Mark math, Mark inf, Mark rus,
            int additionalScore, int totalScore)
        {
            Name = name;
            WithAgreement = withAgreement;
            AdmissionType = admissionType;
            Math = math;
            Inf = inf;
            Rus = rus;
            AdditionalScore = additionalScore;
            TotalScore = totalScore;
        }

        public string Name { get; set; }
        public bool WithAgreement { get; set; }
        public bool WithoutExams => AdmissionType == AdmissionType.WithoutExams;
        public Mark Math { get; set; }
        public Mark Inf { get; set; }
        public Mark Rus { get; set; }
        public int AdditionalScore { get; set; }
        public int TotalScore { get; set; }
        public AdmissionType AdmissionType { get; set; }

        public static Application Bvi(string name, bool withAgreement)
        {
            return new Application(name, withAgreement, AdmissionType.WithoutExams, Mark.NA, Mark.NA, Mark.NA, 0, 310);
        }

        public string[] GetCsvValues()
        {
            return new[]
            {
                Name,
                AdmissionTypeToString(AdmissionType),
                WithAgreement ? "согласие" : "",
                WithoutExams ? "БВИ" : "",
                Math.Score == 0 ? "" : Math.Score.ToString(),
                Math.Type,
                Inf.Score == 0 ? "" : Inf.Score.ToString(),
                Inf.Type,
                Rus.Score == 0 ? "" : Rus.Score.ToString(),
                Rus.Type,
                AdditionalScore.ToString(),
                TotalScore.ToString()
            };
        }

        private string AdmissionTypeToString(AdmissionType admissionType)
        {
            return typeof(AdmissionType).GetField(admissionType.ToString())!
                        .GetCustomAttribute<DescriptionAttribute>()!.Description;
        }

        public static string[] GetCsvHeaders()
        {
            return new[]
            {
                "name", "contract", "agree", "bvi", "math", "mathType", "inf", "infType", "rus", "rusType",
                "additional",
                "total"
            };
        }
    }
}