using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Data;
using DevExpress.DataAccess.Json;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;

return 0;

public partial class ReportExporter {
    [JSExport]
    internal static async Task ExportToPdfAsync(JSObject exportModel, JSObject result) {
        using var report = new XtraReport();
        ((IServiceContainer)report).AddService(typeof(IJsonSourceCustomizationService), new JsonSourceCustomizationService());

        using var reportStream = new MemoryStream(exportModel.GetPropertyAsByteArray("reportXml"));
        report.LoadLayoutFromXml(reportStream, true);

        PdfExportOptions pdfOptions = report.ExportOptions.Pdf;
        if(exportModel.HasProperty("exportOptions")) {
            SimplifiedFillExportOptions(pdfOptions, exportModel.GetPropertyAsJSObject("exportOptions"));
        }

        using var resultStream = new MemoryStream();
        await report.ExportToPdfAsync(resultStream, pdfOptions);
        result.SetProperty("pdf", resultStream.ToArray());
        resultStream.Close();
    }

    static void SimplifiedFillExportOptions(object exportOptions, JSObject jsExportOptions) {
        PropertyInfo[] propInfos = exportOptions.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach(PropertyInfo pi in propInfos) {
            if(!jsExportOptions.HasProperty(pi.Name))
                continue;

            if(pi.PropertyType == typeof(bool)) {
                pi.SetValue(exportOptions, jsExportOptions.GetPropertyAsBoolean(pi.Name));

            } else if(pi.PropertyType == typeof(string)) {
                pi.SetValue(exportOptions, jsExportOptions.GetPropertyAsString(pi.Name));

            } else if(pi.PropertyType.IsEnum) {
                string val = jsExportOptions.GetPropertyAsString(pi.Name);
                if(Enum.IsDefined(pi.PropertyType, val)) {
                    pi.SetValue(exportOptions, Enum.Parse(pi.PropertyType, val));
                }

            } else if(pi.PropertyType.IsClass) {
                SimplifiedFillExportOptions(pi.GetValue(exportOptions), jsExportOptions.GetPropertyAsJSObject(pi.Name));
            }
        }
    }
}
public class JsonSourceCustomizationService : IJsonSourceCustomizationService {
    public JsonSourceBase CustomizeJsonSource(JsonDataSource jsonDataSource) {
        return jsonDataSource.JsonSource is UriJsonSource uriJsonSource ? new SimplifiedUriJsonSource(uriJsonSource.Uri) : jsonDataSource.JsonSource;
    }
}
public partial class SimplifiedUriJsonSource : UriJsonSource {
    public SimplifiedUriJsonSource(Uri uri) : base(uri) { }
    public override Task<string> GetJsonStringAsync(IEnumerable<IParameter> sourceParameters, CancellationToken cancellationToken) {
        return GetJsonData(Uri.ToString());
    }
    [JSImport("getJsonData", "main.mjs")]
    internal static partial Task<string> GetJsonData(string url);
}
