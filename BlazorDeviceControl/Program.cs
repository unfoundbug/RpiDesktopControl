using BlazorDeviceControl.SystemServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<DPSService, DPSService>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.WebHost.UseUrls(new[] { "http://*:5000" });
var app = builder.Build();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
