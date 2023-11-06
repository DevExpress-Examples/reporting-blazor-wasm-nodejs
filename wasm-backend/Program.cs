using System;
using System.Collections.Specialized;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using DevExpress.Blazor.Reporting;
using DevExpress.DataAccess.Json;
using DevExpress.DataAccess.Web;
using DevExpress.XtraReports.Web.Extensions;
using DevExpress.XtraReports.Web.Native.ClientControls;
using DevExpress.XtraReports.Web.Native.ClientControls.Services;
using DevExpress.XtraReports.Web.QueryBuilder.Native.Services;
using DevExpress.XtraReports.Web.ReportDesigner.Native.Services;
using DevExpress.XtraReports.Web.ReportDesigner.Services;
using DevExpress.XtraReports.Web.WebDocumentViewer.Native.Services;
using Microsoft.Extensions.DependencyInjection;

return 0;

public partial class WasmAdapter {
    static IServiceProvider serviceProvider;

    public static IServiceProvider BuildServices() {
        IServiceCollection services = new ServiceCollection();
        services.AddDevExpressBlazorReportingWebAssembly(configure => configure.UseDevelopmentMode());
        services.AddScoped<ReportStorageWebExtension, ReportStorage>();
        services.AddScoped<IJsonDataConnectionProviderFactory, CustomJsonDataConnectionProviderFactory>();
        services.AddScoped<IJsonDataConnectionProviderService, WebDocumentViewerJsonDataConnectionProvider>();
        services.AddScoped<IDataSourceWizardJsonConnectionStorage, CustomDataSourceWizardJsonDataConnectionStorage>();
        return services.BuildServiceProvider();
    }
    [JSExport]
    protected static void Init() {
        serviceProvider = BuildServices();
    }
    [JSExport]
    protected static async Task<string> GetReportDesignerModel() {
        var builder = serviceProvider.GetService<IReportDesignerModelBuilder>();

        return await builder
            .AllowMDI(true)
            .DesignerUri("/DXXRD")
            .ViewerUri("/DXXRDV")
            .Report("report1.repx")
            .BuildJsonModelAsync();
    }
    [JSExport]
    protected static async Task ProcessViewerRequestAsync(JSObject request, JSObject response) {
        IWebDocumentViewerRequestManagerAsync requestManager = serviceProvider.GetService<IWebDocumentViewerRequestManagerAsync>();
        await ProcessRequestCoreAsync(requestManager, request, response);
    }
    [JSExport]
    protected static async Task ProcessDesignerRequestAsync(JSObject request, JSObject response) {
        IReportDesignerRequestManagerAsync requestManager = serviceProvider.GetService<IReportDesignerRequestManagerAsync>();
        await ProcessRequestCoreAsync(requestManager, request, response);
    }
    [JSExport]
    protected static async Task ProcessQueryBuilderRequestAsync(JSObject request, JSObject response) {
        IQueryBuilderRequestManagerAsync requestManager = serviceProvider.GetService<IQueryBuilderRequestManagerAsync>();
        await ProcessRequestCoreAsync(requestManager, request, response);
    }

    protected static async Task ProcessRequestCoreAsync(IRequestManagerAsync requestManager, JSObject request, JSObject response) {
        NameValueCollection query = null;
        if(request != null) {
            string[] keys = request.GetPropertyAsString("keys").Split(',');
            query = new NameValueCollection(keys.Length);
            foreach(var key in keys) {
                query.Add(key, request.GetPropertyAsString(key));
            }
        }

        var result = await requestManager.ProcessRequestAsync(query);

        if(result is IJsonContentResult json) {
            response.SetProperty("data", json.Stringify());
        } else if(result is BinaryHttpActionResult fileResult) {
            response.SetProperty("data", fileResult.Bytes);
            response.SetProperty("fileName", fileResult.FileName);
            response.SetProperty("contentType", fileResult.ContentType);
        }
    }
}
