using NginxPanel.Services;
using Radzen;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel((context, serverOptions) =>
{
	serverOptions.Listen(IPAddress.Any, 5000);
	//serverOptions.Listen(IPAddress.Loopback, 5001, listenOptions =>
	//{
	//	listenOptions.UseHttps("testCert.pfx", "testPassword");
	//});
});

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddRadzenComponents();

// Add custom services.
builder.Services.AddSingleton<CLI>();
builder.Services.AddSingleton<Nginx>();
builder.Services.AddSingleton<ACME>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();