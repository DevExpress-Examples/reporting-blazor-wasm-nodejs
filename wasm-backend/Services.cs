using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Data;
using DevExpress.DataAccess.Json;
using DevExpress.DataAccess.Web;
using DevExpress.XtraReports.UI;
using DevExpress.XtraReports.Web.Extensions;

public partial class ReportStorage : ReportStorageWebExtension {
    public override bool CanSetData(string url) { return true; }
    public override bool IsValidUrl(string url) { return true; }
    public override async Task<byte[]> GetDataAsync(string url) {
        return (await GetDataAsyncCore(url)).GetPropertyAsByteArray("report");
    }
    public override async Task<Dictionary<string, string>> GetUrlsAsync() {
        Dictionary<string, string> result = new Dictionary<string, string>();
        JSObject urlsData = await GetUrlsAsyncCore();
        string[] files = urlsData.GetPropertyAsString("~files").Split("|");
        foreach(var file in files) {
            result[file] = urlsData.GetPropertyAsString(file);
        }
        return result;
    }
    public override async Task SetDataAsync(XtraReport report, string url) {
        using var ms = new MemoryStream();
        report.SaveLayoutToXml(ms);
        await SetDataAsyncCore(ms.ToArray(), url);
    }
    public override async Task<string> SetNewDataAsync(XtraReport report, string defaultUrl) {
        using var ms = new MemoryStream();
        report.SaveLayoutToXml(ms);
        await SetNewDataAsyncCore(ms.ToArray(), defaultUrl);

        return defaultUrl;
    }

    [JSImport("reportStorage.getUrls", "main.mjs")]
    private static partial Task<JSObject> GetUrlsAsyncCore();
    [JSImport("reportStorage.getData", "main.mjs")]
    private static partial Task<JSObject> GetDataAsyncCore(string url);
    [JSImport("reportStorage.setData", "main.mjs")]
    private static partial Task<JSObject> SetDataAsyncCore(byte[] data, string url);
    [JSImport("reportStorage.setNewData", "main.mjs")]
    private static partial Task<JSObject> SetNewDataAsyncCore(byte[] data, string url);
}
public class CustomJsonDataConnectionProviderFactory : IJsonDataConnectionProviderFactory {
    readonly IJsonDataConnectionProviderService jsonDataConnectionProviderService;

    public CustomJsonDataConnectionProviderFactory(IJsonDataConnectionProviderService jsonDataConnectionProviderService) {
        this.jsonDataConnectionProviderService = jsonDataConnectionProviderService;
    }
    public IJsonDataConnectionProviderService Create() {
        return jsonDataConnectionProviderService;
    }

}
public partial class WebDocumentViewerJsonDataConnectionProvider : IJsonDataConnectionProviderService {
    protected JSObject connections;

    public WebDocumentViewerJsonDataConnectionProvider() {
        connections = GetJsonDataConnections();
    }
    public JsonDataConnection GetJsonDataConnection(string name) {
        if(!connections.HasProperty(name))
            throw new InvalidOperationException();

        var connection = connections.GetPropertyAsString(name);
        return CreateJsonDataConnectionFromString(name, connection);
    }

    protected static JsonDataConnection CreateJsonDataConnectionFromString(string connectionName, string connectionString) {
        var connection = new JsonDataConnection(connectionString) { StoreConnectionNameOnly = true, Name = connectionName };
        if(connection.GetJsonSource() is UriJsonSource uriJsonSource) {
            return new JsonDataConnection(new SimplifiedUriJsonSource(uriJsonSource.Uri)) { StoreConnectionNameOnly = true, Name = connectionName };
        }
        return connection;
    }

    [JSImport("dataConnectionStorage.getJsonDataConnections", "main.mjs")]
    internal static partial JSObject GetJsonDataConnections();
}
public class CustomDataSourceWizardJsonDataConnectionStorage : WebDocumentViewerJsonDataConnectionProvider, IDataSourceWizardJsonConnectionStorage {

    public CustomDataSourceWizardJsonDataConnectionStorage() : base() {
    }
    public bool CanSaveConnection => false;
    public bool ContainsConnection(string connectionName) {
        return connections.HasProperty(connectionName);
    }

    public IEnumerable<JsonDataConnection> GetConnections() {
        return connections
            .GetPropertyAsString("~keys")
            .Split("|")
            .Select(name => CreateJsonDataConnectionFromString(name, connections.GetPropertyAsString(name)));
    }
    public void SaveConnection(string connectionName, JsonDataConnection connection, bool saveCredentials) { }

}
public partial class SimplifiedUriJsonSource : UriJsonSource {
    public SimplifiedUriJsonSource(Uri uri) : base(uri) { }
    public override async Task<string> GetJsonStringAsync(IEnumerable<IParameter> sourceParameters, CancellationToken cancellationToken) {
        return await GetJsonData(Uri.ToString());
    }
    [JSImport("dataSource.getJsonData", "main.mjs")]
    internal static partial Task<string> GetJsonData(string url);
}
