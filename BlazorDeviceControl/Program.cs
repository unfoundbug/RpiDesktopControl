using BlazorDeviceControl.SystemServices;
using log4net;
using ServiceExtensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddLog4Net("log4net.config");

var log = LogManager.GetLogger("Startup");

builder.Services.AddSingleton<DPSService, DPSService>();
log.InfoDetail("DPS Service constructed");

log.InfoDetail("Services constructed");

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.WebHost.UseUrls(new[] { "http://*:5000" });
var app = builder.Build();
log.InfoDetail("Builder Ran");


app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

var dpsService = (DPSService)app.Services.GetService(typeof(DPSService));
if (dpsService != null)
{
    dpsService.Start();
    dpsService.Device.Enable_Output = false;
}

log.InfoDetail("App about to run");

app.Run();

log.InfoDetail("App ran");
