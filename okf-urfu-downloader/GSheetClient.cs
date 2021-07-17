using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;

namespace lib.db
{
    public class GSheetClient
    {
        public GSheetClient()
        {
            SheetsService = new SheetsService(
                new BaseClientService.Initializer
                {
                    HttpClientInitializer = GoogleCredential.FromFile("googleapi-credentials.json")
                        .CreateScoped(SheetsService.Scope.Spreadsheets),
                    ApplicationName = "okf-urfu-downloader"
                });
        }

        public GSpreadsheet GetSpreadsheet(string spreadsheetId) =>
            new(spreadsheetId, SheetsService);

        private SheetsService SheetsService { get; }
    }
}
