using NginxPanel.Services;
using Radzen;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

string certName = "self-signed.pfx";
string certPass = "wXp3uOnHbLQSD3IrhGkY5MAXZsPDgiuC";
#if DEBUG
string certPath = "/mnt/d/Repositories/Visual Studio Projects/NginxPanel";
#else
string certPath = "/etc/nginxpanel";
#endif

X509Certificate2? cert = null;

if (!File.Exists(Path.Combine(certPath, certName)))
{
	SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
	sanBuilder.AddIpAddress(IPAddress.Loopback);
	sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
	sanBuilder.AddDnsName("localhost");
	sanBuilder.AddDnsName(Environment.MachineName);

	X500DistinguishedName distinguishedName = new X500DistinguishedName($"CN=NginxPanel");

	using (RSA rsa = RSA.Create(2048))
	{
		var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

		request.CertificateExtensions.Add(
			new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));

		request.CertificateExtensions.Add(
		   new X509EnhancedKeyUsageExtension(
			   new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));

		request.CertificateExtensions.Add(sanBuilder.Build());

		var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddDays(3650)));

		byte[] certData = certificate.Export(X509ContentType.Pfx, certPass);
		cert = new X509Certificate2(certData, certPass, X509KeyStorageFlags.MachineKeySet);

		if (!Directory.Exists(certPath))
			Directory.CreateDirectory(certPath);

		File.WriteAllBytes(Path.Combine(certPath, certName), certData);        
	}
}
else
{
	cert = new X509Certificate2(Path.Combine(certPath, certName), certPass);
}

builder.WebHost.ConfigureKestrel((context, serverOptions) =>
{
	serverOptions.Listen(IPAddress.Any, 5000, listenOptions =>
	{
		listenOptions.UseHttps(cert!);
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