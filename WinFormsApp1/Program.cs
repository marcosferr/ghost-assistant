using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenCaptureBlocker
{
    public partial class MainForm : Form
    {
        // Windows API imports
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        // Constants
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint WDA_NONE = 0x00000000;
        private const uint WDA_MONITOR = 0x00000001;
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint VK_B = 0x42; // 'B' key
        private const int HOTKEY_ID = 1;

        private Thread monitorThread;
        private bool isRunning = true;
        private Form responseForm;
        private Label responseLabel;
        private Panel contentPanel;
        private ProgressBar loadingBar;
        private HttpClient httpClient;

        private const string OpenAI_API_Key = "your-api-key"; // Replace with your actual API key
        private const string OpenAI_API_Endpoint = "https://api.openai.com/v1/chat/completions";

        public MainForm()
        {
            InitializeComponent();

            // Hide main form
            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(1, 1);
            this.Opacity = 0;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(-10000, -10000);

            // Setup response form
            responseForm = new Form
            {
                Text = "Screen Analysis",
                Size = new Size(800, 600),
                FormBorderStyle = FormBorderStyle.FixedSingle,
                StartPosition = FormStartPosition.CenterScreen,
                ShowInTaskbar = true,
                Opacity = 0.95, // Increased opacity for better readability
                BackColor = Color.FromArgb(40, 44, 52),
                ForeColor = Color.White,
                TopMost = true
            };

            // Content panel
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(40, 44, 52)
            };
            responseForm.Controls.Add(contentPanel);

            // Loading bar
            loadingBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Dock = DockStyle.Top,
                Height = 4,
                Visible = false,
                ForeColor = Color.FromArgb(138, 43, 226)
            };
            contentPanel.Controls.Add(loadingBar);

            // Response label
            responseLabel = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.TopLeft,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.White,
                Padding = new Padding(0, 10, 0, 0),
                Text = "Press Ctrl+Alt+B to analyze screen"
            };
            contentPanel.Controls.Add(responseLabel);

            // Setup HTTP client
            SetupHttpClient();

            // Event handlers
            this.Load += MainForm_Load;
            responseForm.Shown += (s, e) =>
            {
                SetWindowDisplayAffinity(responseForm.Handle, WDA_EXCLUDEFROMCAPTURE);
                SetWindowPos(responseForm.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            };
            this.FormClosing += (s, e) =>
            {
                isRunning = false;
                monitorThread?.Join(1000);
                UnregisterHotKey(this.Handle, HOTKEY_ID);
                httpClient?.Dispose();
                if (!responseForm.IsDisposed) responseForm.Dispose();
            };
            this.KeyPreview = true;
        }

        private void SetupHttpClient()
        {
            try
            {
                httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OpenAI_API_Key);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.Timeout = TimeSpan.FromSeconds(30); // Add timeout to prevent hanging
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize HTTP client: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x0312 && m.WParam.ToInt32() == HOTKEY_ID)
            {
                ToggleResponseForm();
            }
        }

        private void ToggleResponseForm()
        {
            if (responseForm.Visible)
            {
                responseForm.Hide();
            }
            else
            {
                CaptureScreenAndAnalyze();
            }
        }

        private async void CaptureScreenAndAnalyze()
        {
            try
            {
                // Make sure the response form is properly set up
                ShowResponseForm("Analyzing screen...", true);

                // Hide the response form temporarily for capturing
                bool wasVisible = responseForm.Visible;
                if (wasVisible)
                {
                    SetWindowDisplayAffinity(responseForm.Handle, WDA_NONE);
                    responseForm.Hide();
                    await Task.Delay(100); // Give time for window to hide
                }

                // Capture the screen
                Bitmap screenshot = null;
                try
                {
                    screenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
                    using (Graphics g = Graphics.FromImage(screenshot))
                    {
                        g.CopyFromScreen(0, 0, 0, 0, screenshot.Size);
                    }

                    // Show the response form again
                    ShowResponseForm("Processing image...", true);

                    // Convert and analyze
                    string base64Image = BitmapToBase64(screenshot);
                    string analysis = await AnalyzeImageWithOpenAI(base64Image);

                    // Show results
                    ShowResponseForm(analysis, false);
                }
                catch (Exception ex)
                {
                    ShowResponseForm($"Screenshot error: {ex.Message}", false);
                }
                finally
                {
                    screenshot?.Dispose();
                }
            }
            catch (Exception ex)
            {
                ShowResponseForm($"Error: {ex.Message}", false);
            }
        }

        private void ShowResponseForm(string message, bool isLoading)
        {
            if (responseForm.InvokeRequired)
            {
                responseForm.Invoke(new Action(() => ShowResponseForm(message, isLoading)));
                return;
            }

            responseLabel.Text = message;
            loadingBar.Visible = isLoading;

            if (!responseForm.Visible)
            {
                responseForm.Show();
            }

            SetWindowDisplayAffinity(responseForm.Handle, WDA_EXCLUDEFROMCAPTURE);
            SetWindowPos(responseForm.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }

        private string BitmapToBase64(Bitmap bitmap)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                byte[] imageBytes = ms.ToArray();
                return Convert.ToBase64String(imageBytes);
            }
        }

        private async Task<string> AnalyzeImageWithOpenAI(string base64Image)
        {
            try
            {
                if (string.IsNullOrEmpty(OpenAI_API_Key) || OpenAI_API_Key == "YOUR_API_KEY")
                {
                    return "Please set your OpenAI API key in the code before using this feature.";
                }

                var requestData = new
                {
                    model = "gpt-4o-mini",
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new { type = "text", text = "Provide a solution for this leetcode" },
                                new { type = "image_url", image_url = new { url = $"data:image/png;base64,{base64Image}" } }
                            }
                        }
                    },
                    max_tokens = 500
                };

                string jsonContent = JsonSerializer.Serialize(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(OpenAI_API_Endpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                    {
                        JsonElement root = doc.RootElement;
                        if (root.TryGetProperty("choices", out JsonElement choices) && choices.GetArrayLength() > 0)
                        {
                            var contentText = choices[0]
                                .GetProperty("message")
                                .GetProperty("content")
                                .GetString();
                            return contentText ?? "No analysis returned";
                        }
                        return "Couldn't parse API response";
                    }
                }

                // Get more detailed error information
                string errorDetails = await response.Content.ReadAsStringAsync();
                return $"API Error: {response.StatusCode}\n{errorDetails}";
            }
            catch (Exception ex)
            {
                return $"Analysis error: {ex.Message}";
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            ShowWindow(this.Handle, SW_HIDE);
            if (!RegisterHotKey(this.Handle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_B))
            {
                MessageBox.Show("Failed to register hotkey (Ctrl+Alt+B). The application may not work correctly.",
                    "Hotkey Registration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            monitorThread = new Thread(MonitorScreenCapture);
            monitorThread.IsBackground = true;
            monitorThread.Start();
        }

        private void MonitorScreenCapture()
        {
            string[] captureTools = { "SnippingTool", "ScreenClippingHost", "SnipIt", "Snip & Sketch" };

            while (isRunning)
            {
                try
                {
                    IntPtr hwnd = GetForegroundWindow();
                    StringBuilder windowTitle = new StringBuilder(256);
                    GetWindowText(hwnd, windowTitle, windowTitle.Capacity);

                    if (responseForm.Visible)
                    {
                        foreach (string tool in captureTools)
                        {
                            if (windowTitle.ToString().Contains(tool, StringComparison.OrdinalIgnoreCase) ||
                                FindWindow(null, tool) != IntPtr.Zero)
                            {
                                responseForm.Invoke((Action)(() =>
                                {
                                    SetWindowDisplayAffinity(responseForm.Handle, WDA_EXCLUDEFROMCAPTURE);
                                }));
                                break;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Silently handle exceptions in monitoring thread
                }

                Thread.Sleep(200);
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(1, 1);
            this.Name = "MainForm";
            this.Text = "HiddenService";
            this.ResumeLayout(false);
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}