using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace NginxPanel
{
    public class AppConfig
    {
#if DEBUG
        private const string _basePath = "/mnt/d/Repositories/Visual Studio Projects/NginxPanel/config";
#else
        private const string _basePath = "/etc/nginxpanel";
#endif
        public const string AppConfigPath = _basePath + "/app.conf";

        public static int Port { get; set; } = 5000;
        public static string PFXPath { get; set; } = Path.Combine(_basePath, "self-signed.pfx");
        public static string PFXPassword { get; set; } = GeneratePFXPassword();

        // Basic auth related settings
        public static string Username { get; set; } = string.Empty;
        public static string Password { get; set; } = string.Empty;

        // DUO related settings
        public static bool DUOEnabled { get; set; } = false;
        public static string DUOIntegrationKey { get; set; } = string.Empty;
        public static string DUOSecretKey { get; set; } = string.Empty;
        public static string DUOAPIHostname { get; set; } = string.Empty;

        public static void Init()
        {
            // Check if config already exists
            if (File.Exists(AppConfigPath))
            {
                // Load config
                ReadConfig();
            }
            else
            {
                // Create a new config file with defaults
                SaveConfig();
            }
        }

        public static void SaveConfig()
        {
            if (!Directory.Exists(_basePath))
                Directory.CreateDirectory(_basePath);

            StringBuilder config = new StringBuilder();

            config.AppendLine($"Port='{Port}'");
            config.AppendLine($"PFXPath='{PFXPath}'");
            config.AppendLine($"PFXPassword='{PFXPassword}'");

            // Basic auth related settings
            config.AppendLine($"Username='{Username}'");
            config.AppendLine($"Password='{Password}'");

            // DUO related settings
            config.AppendLine($"DUOEnabled='{DUOEnabled}'");
            config.AppendLine($"DUOIntegrationKey='{DUOIntegrationKey}'");
            config.AppendLine($"DUOSecretKey='{DUOSecretKey}'");
            config.AppendLine($"DUOAPIHostname='{DUOAPIHostname}'");

            // Output config file (overwrites)
            File.WriteAllText(AppConfigPath, config.ToString());

            // Make sure certificate exists, if not then generate a new self-signed one
            if (!File.Exists(PFXPath))
                GenerateSelfSignedCert();
        }

        public static void ReadConfig()
        {
            string[] lines = File.ReadAllLines(AppConfigPath);
            string[] split;

            foreach (string line in lines)
            {
                split = line.Split('=');
                split[1] = split[1].Trim('\'');

                switch (split[0].ToLower())
                {
                    case "port":
                        Port = int.Parse(split[1]); break;
                    case "pfxpath":
                        PFXPath = split[1]; break;
                    case "pfxpassword":
                        PFXPassword = split[1]; break;
                    case "username":
                        Username = split[1]; break;
                    case "password":
                        Password = split[1]; break;
                    case "duoenabled":
                        DUOEnabled = (split[1] == "1" || split[1] == "true"); break;
                    case "duointegrationkey":
                        DUOIntegrationKey = split[1]; break;
                    case "duosecretkey":
                        DUOSecretKey = split[1]; break;
                    case "duoapihostname":
                        DUOAPIHostname = split[1]; break;
                }
            }

            // If PFX path is blank, generate a new self-signed one
            // by updating and saving the default PFX path value
            if (String.IsNullOrWhiteSpace(PFXPath))
            {
                PFXPath = Path.Combine(_basePath, "self-signed.pfx");
                SaveConfig();
            }
        }

        private static string GeneratePFXPassword()
        {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder pass = new StringBuilder();
            Random rnd = new Random();
            for (int c = 0; c < 20; c++)
            {
                pass.Append(valid[rnd.Next(valid.Length)]);
                Thread.Sleep(50);
            }
            return pass.ToString();
        }

        private static void GenerateSelfSignedCert()
        {
            FileInfo pfx = new FileInfo(PFXPath);

            SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddIpAddress(IPAddress.Loopback);
            sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
            sanBuilder.AddDnsName("localhost");
            sanBuilder.AddDnsName(Environment.MachineName);

            X500DistinguishedName distinguishedName = new X500DistinguishedName($"CN=NginxPanel");

            using (RSA rsa = RSA.Create(2048))
            {
                CertificateRequest request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));

                request.CertificateExtensions.Add(
                   new X509EnhancedKeyUsageExtension(
                       new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));

                request.CertificateExtensions.Add(sanBuilder.Build());

                X509Certificate2 certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddDays(3650)));
                byte[] certData = certificate.Export(X509ContentType.Pfx, PFXPassword);

                if (!Directory.Exists(pfx.DirectoryName))
                    Directory.CreateDirectory(pfx.DirectoryName!);

                File.WriteAllBytes(pfx.FullName, certData);
            }
        }
    }
}