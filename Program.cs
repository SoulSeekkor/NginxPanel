using NginxPanel.Services;
using Radzen;
using System.Net;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

// Initialize application configuration
// NOTE: This creates files including self-signed.pfx!
NginxPanel.AppConfig.Init();

// Verify port is set and PFX exists
if (NginxPanel.AppConfig.Port <= 0)
{
	// Fail on port not set
	throw new Exception($"Must specify '{nameof(NginxPanel.AppConfig.Port)}' in the configuration file!");
}
else if (!File.Exists(NginxPanel.AppConfig.PFXPath))
{
	// Fail on non-existent PFX
	throw new Exception($"Specified PFX file at '{NginxPanel.AppConfig.PFXPath}' does not exist! Clear this value in the config to create a new self-signed one.");
}

builder.WebHost.ConfigureKestrel((context, serverOptions) =>
{
	serverOptions.Listen(IPAddress.Any, NginxPanel.AppConfig.Port, listenOptions =>
	{
		listenOptions.UseHttps(new X509Certificate2(NginxPanel.AppConfig.PFXPath, NginxPanel.AppConfig.PFXPassword));
	});
});

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddRadzenComponents();

// Add custom services.
builder.Services.AddSingleton<CLI>();
builder.Services.AddSingleton<Nginx>();
builder.Services.AddSingleton<ACME>();

builder.Services.AddScoped<DUO>();
builder.Services.AddScoped<AuthState>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	//app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();