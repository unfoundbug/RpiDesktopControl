using BlazorDeviceControl.SystemServices;
using log4net;
using ServiceExtensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddLog4Net("log4net.config");

var log = LogManager.GetLogger("Startup");

builder.Services.AddSingleton<DPSService, DPSService>();
builder.Services.AddSingleton<FZ35Service, FZ35Service>();

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

log.InfoDetail("App about to run");

var dps = (DPSService)app.Services.GetService(typeof(DPSService));
dps?.Start();

var fz = (FZ35Service)app.Services.GetService(typeof(FZ35Service));
fz?.Start();

app.Run();

log.InfoDetail("App ran");
