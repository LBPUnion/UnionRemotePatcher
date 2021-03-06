using Eto.Drawing;
using Eto.Forms;
using System;
using System.Diagnostics;

namespace UnionRemotePatcher
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            Console.WriteLine("Welcome to UnionRemotePatcher");
        }

        public RemotePatch RemotePatcher = new RemotePatch();

        private TextBox ps3LocalIP;
        private TextBox lbpGameID;
        private TextBox serverUrl;
        private TextBox ftpUser;
        private TextBox ftpPass;

        public Dialog CreateOkDialog(string title, string errorMessage)
        {
            DynamicLayout layout = new();
            Button button;

            layout.Spacing = new Size(5, 5);
            layout.MinimumSize = new Size(350, 100);

            layout.BeginHorizontal();
            layout.Add(new Label
            {
                Text = errorMessage,
            });

            layout.BeginHorizontal();
            layout.BeginVertical();
            layout.Add(null);
            layout.Add(button = new Button
            {
                Text = "OK",
            });

            layout.EndVertical();
            layout.EndHorizontal();
            layout.EndHorizontal();

            Dialog dialog = new()
            {
                Content = layout,
                Padding = new Padding(10, 10, 10, 10),
                Title = title,
                
            };

            button.Click += delegate {
                dialog.Close();
            };

            return dialog;
        }

        public Control CreatePatchButton(int tabIndex = 0)
        {
            Button control = new()
            {
                Text = "Patch!",
                TabIndex = tabIndex,
                Width = 200,
            };

            control.Click += delegate {
                if (string.IsNullOrEmpty(this.ps3LocalIP.Text))
                {
                    this.CreateOkDialog("Error", "No PS3 IP address specified!").ShowModal();
                    return;
                }

                if (string.IsNullOrEmpty(this.lbpGameID.Text))
                {
                    this.CreateOkDialog("Error", "No game ID specified!").ShowModal();
                    return;
                }

                if (string.IsNullOrEmpty(this.serverUrl.Text))
                {
                    this.CreateOkDialog("Error", "No server URL specified!").ShowModal();
                    return;
                }
                
                if (!Uri.TryCreate(this.serverUrl.Text, UriKind.Absolute, out _))
                {
                    this.CreateOkDialog("Error", "Server URL is invalid! Please enter a valid URL.").ShowModal();
                    return;
                }

                try
                {
                    if (this.lbpGameID.Text.ToUpper().StartsWith('B'))
                    {
                        RemotePatcher.DiscEBOOTRemotePatch(this.ps3LocalIP.Text, this.lbpGameID.Text, this.serverUrl.Text, this.ftpUser.Text, this.ftpPass.Text);
                    }
                    else
                    {
                        RemotePatcher.PSNEBOOTRemotePatch(this.ps3LocalIP.Text, this.lbpGameID.Text, this.serverUrl.Text, this.ftpUser.Text, this.ftpPass.Text);
                    }
                }
                catch (Exception e)
                {
                    this.CreateOkDialog("Error occurred while patching", "An error occured while patching:\n" + e).ShowModal();
                    return;
                }

                this.CreateOkDialog("Success!", $"The Server URL for {this.lbpGameID.Text} on the PS3 at {this.ps3LocalIP.Text} has been patched to {this.serverUrl.Text}").ShowModal();
            };

            return control;
        }
        
        public Control CreateRevertEBOOTButton(int tabIndex = 0)
        {
            Button control = new()
            {
                Text = "Revert EBOOT",
                TabIndex = tabIndex,
                Width = 200,
            };

            control.Click += delegate {
                if (string.IsNullOrEmpty(this.ps3LocalIP.Text))
                {
                    this.CreateOkDialog("Form Error", "No PS3 IP address specified!").ShowModal();
                    return;
                }

                if (string.IsNullOrEmpty(this.lbpGameID.Text))
                {
                    this.CreateOkDialog("Form Error", "No game ID specified!").ShowModal();
                    return;
                }
                
                try
                {
                    RemotePatcher.RevertEBOOT(ps3LocalIP.Text, lbpGameID.Text, serverUrl.Text, ftpUser.Text, ftpPass.Text);
                }
                catch (Exception e)
                {
                    this.CreateOkDialog("Error occurred while reverting EBOOT", "An error occured while patching:\n" + e).ShowModal();
                    return;
                }

                this.CreateOkDialog("Success!", $"UnionRemotePatcher reverted your the EBOOT for {lbpGameID.Text} to stock. You're ready to patch your EBOOT again.").ShowModal();
            };

            return control;
        }

        public Control CreateHelpButton(int tabIndex = 0)
        {
            Button control = new()
            {
                Text = "Help",
                TabIndex = tabIndex,
            };

            control.Click += delegate {
                Process process = new();

                process.StartInfo.UseShellExecute = true;
                process.StartInfo.FileName = "https://www.lbpunion.com";
                process.Start();
            };

            return control;
        }
        void InitializeComponent()
        {
            Title = "UnionRemotePatcher - LBP EBOOT Patcher";
            MinimumSize = new Size(450, 200);
            Resizable = false;
            Padding = 10;

            Content = new TableLayout
            {
                Spacing = new Size(5, 5),
                Padding = new Padding(10, 10, 10, 10),
                Rows = {
                    new TableRow(
                        new TableCell(new Label { Text = "PS3 Local IP: ", VerticalAlignment = VerticalAlignment.Center }),
                        new TableCell(this.ps3LocalIP = new TextBox { TabIndex = 0 })
                    ),
                    new TableRow(
                        new TableCell(new Label { Text = "Game ID: ", VerticalAlignment = VerticalAlignment.Center }),
                        new TableCell(this.lbpGameID = new TextBox { TabIndex = 1 })
                    ),
                    new TableRow(
                        new TableCell(new Label { Text = "Server URL: ", VerticalAlignment = VerticalAlignment.Center }),
                        new TableCell(this.serverUrl = new TextBox { TabIndex = 2 })
                    ),
                    new TableRow(
                        new TableCell(new Label { Text = "FTP Username: ", VerticalAlignment = VerticalAlignment.Center }),
                        new TableCell(this.ftpUser = new TextBox { TabIndex = 3, Text = "anonymous" })
                    ),
                    new TableRow(
                        new TableCell(new Label { Text = "FTP Password: ", VerticalAlignment = VerticalAlignment.Center }),
                        new TableCell(this.ftpPass = new TextBox { TabIndex = 4 })
                    ),
                    new TableRow(
                        new TableCell(this.CreateHelpButton(5)),
                        new TableRow(
                            new TableCell(this.CreatePatchButton(6)),
                            new TableCell(this.CreateRevertEBOOTButton(7))
                        )
                    ),
                },
            };
        }
    }
}
