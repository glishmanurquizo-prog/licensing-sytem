using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LicensingClient
{
    public partial class Form1 : Form
    {
        private readonly HttpClient _http;
        private readonly string _apiBase = "http://localhost:5000";
        private int _localFails = 0;
        private System.Windows.Forms.Timer _pollTimer;
        private string _currentLicenseKey = "";
        private string _hwid = "";

        public Form1()
        {
            InitializeComponent();
            Init();

            this.Text = "MiApp - Activación";
            this.Width = 420;
            this.Height = 180;

            var lbl = new Label() { Left = 12, Top = 15, Width = 380, Text = "Ingresa tu clave de licencia:" };
            var txtKey = new TextBox() { Name = "txtKey", Left = 12, Top = 40, Width = 300 };
            var btnActivate = new Button() { Name = "btnActivate", Left = 320, Top = 38, Width = 80, Text = "Activar" };

            btnActivate.Click += async (s, e) => await OnActivateClicked(txtKey.Text.Trim());

            this.Controls.Add(lbl);
            this.Controls.Add(txtKey);
            this.Controls.Add(btnActivate);

            _http = new HttpClient();

            _hwid = GetHwidHash();

            _pollTimer = new System.Windows.Forms.Timer();
            _pollTimer.Interval = 60000;
            _pollTimer.Tick += async (_, __) => await PollValidateAsync();
        }

        private async void Init()
        {
#if !DEBUG
    AntiTamper.Check();
    AntiDebug.Check();
#endif

            await StartServerAsync();
        }

        private async Task StartServerAsync()
        {
            try
            {
                // 1. Verificar si ya está corriendo
                if (Process.GetProcessesByName("LicensingSystem").Length == 0)
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = @"C:\LicensingServer\LicensingSystem.exe",
                        WorkingDirectory = @"C:\LicensingServer",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    Process.Start(psi);
                }

                // 2. Esperar a que el servidor responda
                using (HttpClient client = new HttpClient())
                {
                    bool connected = false;

                    for (int i = 0; i < 10; i++) // intenta por ~5 segundos
                    {
                        try
                        {
                            var response = await client.GetAsync("http://localhost:5000");
                            if (response.IsSuccessStatusCode)
                            {
                                connected = true;
                                break;
                            }
                        }
                        catch { }

                        await Task.Delay(500);
                    }

                    if (!connected)
                    {
                        MessageBox.Show("No se pudo conectar al servidor.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error iniciando servidor: " + ex.Message);
            }
        }

        

        private async Task OnActivateClicked(string licenseKey)
        {
            if (string.IsNullOrEmpty(licenseKey))
            {
                MessageBox.Show("Ingresa la clave.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var payload = new { LicenseKey = licenseKey, HwidHash = _hwid };

            try
            {
                var res = await _http.PostAsJsonAsync($"{_apiBase}/api/activate", payload);

                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadFromJsonAsync<JsonElement>();

                    int activationId = json.GetProperty("activationId").GetInt32();
                    string signature = json.GetProperty("signature").GetString() ?? "";

                    string payloadToVerify = $"OK|{activationId}";
                    string expectedSignature = SecurityHelper.GenerateSignature(payloadToVerify);

                    if (signature != expectedSignature)
                    {
                        MessageBox.Show("Respuesta del servidor inválida.", "Error de seguridad");
                        Application.Exit();
                        return;
                    }

                    SaveState(licenseKey);
                    _currentLicenseKey = licenseKey;

                    MessageBox.Show("Activado correctamente.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    _pollTimer.Start();
                }
                else
                {
                    _localFails++;

                    MessageBox.Show("No autorizado.", "Licencia inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    if (_localFails >= 2)
                        Application.Exit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error de red o servidor: " + ex.Message);
            }
        }

        private async Task PollValidateAsync()
        {
            try
            {
                var st = LoadState();

                if (string.IsNullOrEmpty(st))
                    return;

                var payload = new { LicenseKey = st, HwidHash = _hwid };

                var res = await _http.PostAsJsonAsync($"{_apiBase}/api/verify", payload);

                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadFromJsonAsync<JsonElement>();

                    bool valid = json.GetProperty("valid").GetBoolean();

                    if (!valid)
                    {
                        MessageBox.Show("Licencia revocada. La aplicación se cerrará.", "Licencia inválida");
                        Application.Exit();
                    }
                }
                else
                {
                    _localFails++;

                    if (_localFails >= 3)
                        Application.Exit();
                }
            }
            catch
            {
                // ignoramos cortes de red momentáneos
            }
        }

        private void SaveState(string licenseKey)
        {
            try
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MiApp");

                Directory.CreateDirectory(folder);

                var path = Path.Combine(folder, "state.bin");

                var bytes = Encoding.UTF8.GetBytes(licenseKey);

                var enc = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);

                File.WriteAllBytes(path, enc);
            }
            catch { }
        }

        private string? LoadState()
        {
            try
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MiApp", "state.bin");

                if (!File.Exists(path))
                    return null;

                var enc = File.ReadAllBytes(path);

                var bytes = ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser);

                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return null;
            }
        }

        private string GetHwidHash()
        {
            try
            {
                string machineGuid = "";

                using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                    .OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
                {
                    machineGuid = key?.GetValue("MachineGuid")?.ToString() ?? "";
                }

                var raw = $"{machineGuid}|{Environment.MachineName}";

                using var sha = SHA256.Create();

                var sum = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));

                return Convert.ToHexString(sum).ToLowerInvariant();
            }
            catch
            {
                using var sha = SHA256.Create();

                var sum = sha.ComputeHash(Encoding.UTF8.GetBytes(Environment.MachineName));

                return Convert.ToHexString(sum).ToLowerInvariant();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}