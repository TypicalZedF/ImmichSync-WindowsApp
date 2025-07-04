using System.Windows.Forms;

namespace ImmichSyncApp
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private NotifyIcon notifyIcon1;
        private ContextMenuStrip trayMenu;
        private Button btnAddFolder;
        private Button btnRemoveFolder;
        private ListBox lstFolders;
        private FolderBrowserDialog folderBrowserDialog1;
        private Label lblLocalServerUrl;
        private ComboBox cmbLocalServerUrl;
        private Label lblRemoteServerUrl;
        private ComboBox cmbRemoteServerUrl;
        private Button btnSaveServerUrls;
        private Button btnSetCurrentNetwork;
        private Label lblLocalDesc;
        private Label lblRemoteDesc;
        private Label lblNetworkDesc;
        private Label lblCurrentNetwork;
        private Label lblApiKey;
        private TextBox txtApiKey;
        private Button btnTestConnection;
        private Label lblApiKeyDesc;
        private Label lblServerSettingsStatus;
        private GroupBox grpFolders;
        private GroupBox grpServer;
        private GroupBox grpApiKey;
        private GroupBox grpQueue;
        private DataGridView dgvQueue;
        private Button btnResetSyncedFiles;
        private CheckBox chkCreateAlbumsForSyncedFolders;
        private CheckBox chkStartMinimizedAtBoot;
        private CheckBox chkCloseButtonMinimizes;
        private Button btnPauseResumeQueue;
        private Button btnCancelTask;
        private Button btnClearFailedTasks;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private Panel panelQueueButtons;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            notifyIcon1 = new NotifyIcon(components);
            trayMenu = new ContextMenuStrip(components);
            btnAddFolder = new Button();
            btnRemoveFolder = new Button();
            lstFolders = new ListBox();
            folderBrowserDialog1 = new FolderBrowserDialog();
            lblLocalServerUrl = new Label();
            cmbLocalServerUrl = new ComboBox();
            lblRemoteServerUrl = new Label();
            cmbRemoteServerUrl = new ComboBox();
            btnSaveServerUrls = new Button();
            btnSetCurrentNetwork = new Button();
            lblLocalDesc = new Label();
            lblRemoteDesc = new Label();
            lblNetworkDesc = new Label();
            lblCurrentNetwork = new Label();
            lblApiKey = new Label();
            txtApiKey = new TextBox();
            btnTestConnection = new Button();
            lblApiKeyDesc = new Label();
            lblServerSettingsStatus = new Label();
            grpFolders = new GroupBox();
            btnResetSyncedFiles = new Button();
            chkCreateAlbumsForSyncedFolders = new CheckBox();
            chkStartMinimizedAtBoot = new CheckBox();
            chkCloseButtonMinimizes = new CheckBox();
            grpServer = new GroupBox();
            grpApiKey = new GroupBox();
            grpQueue = new GroupBox();
            dgvQueue = new DataGridView();
            dataGridViewTextBoxColumn1 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn3 = new DataGridViewTextBoxColumn();
            panelQueueButtons = new Panel();
            btnPauseResumeQueue = new Button();
            btnCancelTask = new Button();
            btnClearFailedTasks = new Button();
            grpFolders.SuspendLayout();
            grpServer.SuspendLayout();
            grpApiKey.SuspendLayout();
            grpQueue.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvQueue).BeginInit();
            panelQueueButtons.SuspendLayout();
            SuspendLayout();
            // 
            // notifyIcon1
            // 
            notifyIcon1.ContextMenuStrip = trayMenu;
            notifyIcon1.Icon = (Icon)resources.GetObject("notifyIcon1.Icon");
            notifyIcon1.Text = "Immich Sync";
            notifyIcon1.Visible = true;
            // 
            // trayMenu
            // 
            trayMenu.Name = "trayMenu";
            trayMenu.Size = new Size(61, 4);
            // 
            // btnAddFolder
            // 
            btnAddFolder.Location = new Point(15, 25);
            btnAddFolder.Name = "btnAddFolder";
            btnAddFolder.Size = new Size(120, 30);
            btnAddFolder.TabIndex = 1;
            btnAddFolder.Text = "Add Folder";
            btnAddFolder.Click += btnAddFolder_Click;
            // 
            // btnRemoveFolder
            // 
            btnRemoveFolder.Location = new Point(141, 25);
            btnRemoveFolder.Name = "btnRemoveFolder";
            btnRemoveFolder.Size = new Size(120, 30);
            btnRemoveFolder.TabIndex = 20;
            btnRemoveFolder.Text = "Remove Folder";
            btnRemoveFolder.Click += btnRemoveFolder_Click;
            // 
            // lstFolders
            // 
            lstFolders.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lstFolders.IntegralHeight = false;
            lstFolders.ItemHeight = 15;
            lstFolders.Location = new Point(15, 141);
            lstFolders.Name = "lstFolders";
            lstFolders.Size = new Size(410, 84);
            lstFolders.TabIndex = 3;
            lstFolders.SelectedIndexChanged += lstFolders_SelectedIndexChanged_1;
            // 
            // lblLocalServerUrl
            // 
            lblLocalServerUrl.Location = new Point(15, 25);
            lblLocalServerUrl.Name = "lblLocalServerUrl";
            lblLocalServerUrl.Size = new Size(120, 23);
            lblLocalServerUrl.TabIndex = 3;
            lblLocalServerUrl.Text = "Local Immich URL:";
            // 
            // cmbLocalServerUrl
            // 
            cmbLocalServerUrl.Items.AddRange(new object[] { "http://192.168.1.100:2283", "http://localhost:2283", "http://192.168.0.10:2283" });
            cmbLocalServerUrl.Location = new Point(140, 25);
            cmbLocalServerUrl.Name = "cmbLocalServerUrl";
            cmbLocalServerUrl.Size = new Size(180, 23);
            cmbLocalServerUrl.TabIndex = 4;
            // 
            // lblRemoteServerUrl
            // 
            lblRemoteServerUrl.Location = new Point(15, 120);
            lblRemoteServerUrl.Name = "lblRemoteServerUrl";
            lblRemoteServerUrl.Size = new Size(120, 23);
            lblRemoteServerUrl.TabIndex = 6;
            lblRemoteServerUrl.Text = "Remote Immich URL:";
            // 
            // cmbRemoteServerUrl
            // 
            cmbRemoteServerUrl.Items.AddRange(new object[] { "https://yourdomain.com", "https://immich.example.com" });
            cmbRemoteServerUrl.Location = new Point(140, 120);
            cmbRemoteServerUrl.Name = "cmbRemoteServerUrl";
            cmbRemoteServerUrl.Size = new Size(180, 23);
            cmbRemoteServerUrl.TabIndex = 7;
            // 
            // btnSaveServerUrls
            // 
            btnSaveServerUrls.Location = new Point(16, 199);
            btnSaveServerUrls.Name = "btnSaveServerUrls";
            btnSaveServerUrls.Size = new Size(480, 25);
            btnSaveServerUrls.TabIndex = 9;
            btnSaveServerUrls.Text = "Save URLs and API Key";
            btnSaveServerUrls.Click += btnSaveServerUrls_Click;
            // 
            // btnSetCurrentNetwork
            // 
            btnSetCurrentNetwork.Location = new Point(330, 25);
            btnSetCurrentNetwork.Name = "btnSetCurrentNetwork";
            btnSetCurrentNetwork.Size = new Size(160, 23);
            btnSetCurrentNetwork.TabIndex = 10;
            btnSetCurrentNetwork.Text = "Set Local Network";
            btnSetCurrentNetwork.Click += btnSetCurrentNetwork_Click;
            // 
            // lblLocalDesc
            // 
            lblLocalDesc.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            lblLocalDesc.Location = new Point(15, 50);
            lblLocalDesc.Name = "lblLocalDesc";
            lblLocalDesc.Size = new Size(430, 17);
            lblLocalDesc.TabIndex = 5;
            lblLocalDesc.Text = "Used when connected to the specified local network (see below).";
            // 
            // lblRemoteDesc
            // 
            lblRemoteDesc.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            lblRemoteDesc.Location = new Point(15, 145);
            lblRemoteDesc.Name = "lblRemoteDesc";
            lblRemoteDesc.Size = new Size(430, 17);
            lblRemoteDesc.TabIndex = 8;
            lblRemoteDesc.Text = "Used when not connected to the specified local network.";
            // 
            // lblNetworkDesc
            // 
            lblNetworkDesc.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            lblNetworkDesc.Location = new Point(15, 93);
            lblNetworkDesc.Name = "lblNetworkDesc";
            lblNetworkDesc.Size = new Size(490, 17);
            lblNetworkDesc.TabIndex = 11;
            lblNetworkDesc.Text = "Click 'Set Local Network to Current' to associate the local URL with your current network.";
            // 
            // lblCurrentNetwork
            // 
            lblCurrentNetwork.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblCurrentNetwork.Location = new Point(15, 70);
            lblCurrentNetwork.Name = "lblCurrentNetwork";
            lblCurrentNetwork.Size = new Size(490, 23);
            lblCurrentNetwork.TabIndex = 12;
            lblCurrentNetwork.Text = "Current local network: (not set)";
            // 
            // lblApiKey
            // 
            lblApiKey.Location = new Point(15, 25);
            lblApiKey.Name = "lblApiKey";
            lblApiKey.Size = new Size(120, 23);
            lblApiKey.TabIndex = 13;
            lblApiKey.Text = "Immich API Key:";
            // 
            // txtApiKey
            // 
            txtApiKey.Location = new Point(140, 25);
            txtApiKey.Name = "txtApiKey";
            txtApiKey.Size = new Size(180, 23);
            txtApiKey.TabIndex = 14;
            txtApiKey.UseSystemPasswordChar = true;
            // 
            // btnTestConnection
            // 
            btnTestConnection.Location = new Point(340, 25);
            btnTestConnection.Name = "btnTestConnection";
            btnTestConnection.Size = new Size(120, 23);
            btnTestConnection.TabIndex = 16;
            btnTestConnection.Text = "Test Connection";
            btnTestConnection.Click += btnTestConnection_Click;
            // 
            // lblApiKeyDesc
            // 
            lblApiKeyDesc.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            lblApiKeyDesc.Location = new Point(15, 55);
            lblApiKeyDesc.Name = "lblApiKeyDesc";
            lblApiKeyDesc.Size = new Size(480, 200);
            lblApiKeyDesc.TabIndex = 15;
            lblApiKeyDesc.Text = resources.GetString("lblApiKeyDesc.Text");
            lblApiKeyDesc.Click += lblApiKeyDesc_Click;
            // 
            // lblServerSettingsStatus
            // 
            lblServerSettingsStatus.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            lblServerSettingsStatus.ForeColor = Color.Gray;
            lblServerSettingsStatus.Location = new Point(16, 176);
            lblServerSettingsStatus.Name = "lblServerSettingsStatus";
            lblServerSettingsStatus.Size = new Size(480, 20);
            lblServerSettingsStatus.TabIndex = 13;
            // 
            // grpFolders
            // 
            grpFolders.Controls.Add(btnAddFolder);
            grpFolders.Controls.Add(btnRemoveFolder);
            grpFolders.Controls.Add(lstFolders);
            grpFolders.Controls.Add(btnResetSyncedFiles);
            grpFolders.Controls.Add(chkCreateAlbumsForSyncedFolders);
            grpFolders.Controls.Add(chkStartMinimizedAtBoot);
            grpFolders.Controls.Add(chkCloseButtonMinimizes);
            grpFolders.Location = new Point(10, 10);
            grpFolders.Name = "grpFolders";
            grpFolders.Size = new Size(440, 244);
            grpFolders.TabIndex = 1;
            grpFolders.TabStop = false;
            grpFolders.Text = "Sync Folders";
            // 
            // btnResetSyncedFiles
            // 
            btnResetSyncedFiles.Location = new Point(267, 25);
            btnResetSyncedFiles.Name = "btnResetSyncedFiles";
            btnResetSyncedFiles.Size = new Size(120, 30);
            btnResetSyncedFiles.TabIndex = 17;
            btnResetSyncedFiles.Text = "Resync";
            btnResetSyncedFiles.Click += btnResetSyncedFiles_Click;
            // 
            // chkCreateAlbumsForSyncedFolders
            // 
            chkCreateAlbumsForSyncedFolders.Location = new Point(15, 60);
            chkCreateAlbumsForSyncedFolders.Name = "chkCreateAlbumsForSyncedFolders";
            chkCreateAlbumsForSyncedFolders.Size = new Size(292, 25);
            chkCreateAlbumsForSyncedFolders.TabIndex = 19;
            chkCreateAlbumsForSyncedFolders.Text = "Create Albums For Synced Folders";
            chkCreateAlbumsForSyncedFolders.CheckedChanged += chkCreateAlbumsForSyncedFolders_CheckedChanged;
            // 
            // chkStartMinimizedAtBoot
            // 
            chkStartMinimizedAtBoot.Location = new Point(15, 85);
            chkStartMinimizedAtBoot.Name = "chkStartMinimizedAtBoot";
            chkStartMinimizedAtBoot.Size = new Size(292, 25);
            chkStartMinimizedAtBoot.TabIndex = 21;
            chkStartMinimizedAtBoot.Text = "Start application minimized at system boot";
            // 
            // chkCloseButtonMinimizes
            // 
            chkCloseButtonMinimizes.Location = new Point(15, 110);
            chkCloseButtonMinimizes.Name = "chkCloseButtonMinimizes";
            chkCloseButtonMinimizes.Size = new Size(292, 25);
            chkCloseButtonMinimizes.TabIndex = 22;
            chkCloseButtonMinimizes.Text = "Close button should minimize the window";
            // 
            // grpServer
            // 
            grpServer.Controls.Add(lblLocalServerUrl);
            grpServer.Controls.Add(cmbLocalServerUrl);
            grpServer.Controls.Add(btnSetCurrentNetwork);
            grpServer.Controls.Add(lblLocalDesc);
            grpServer.Controls.Add(lblServerSettingsStatus);
            grpServer.Controls.Add(lblCurrentNetwork);
            grpServer.Controls.Add(lblNetworkDesc);
            grpServer.Controls.Add(lblRemoteServerUrl);
            grpServer.Controls.Add(cmbRemoteServerUrl);
            grpServer.Controls.Add(lblRemoteDesc);
            grpServer.Controls.Add(btnSaveServerUrls);
            grpServer.Location = new Point(460, 10);
            grpServer.Name = "grpServer";
            grpServer.Size = new Size(510, 230);
            grpServer.TabIndex = 2;
            grpServer.TabStop = false;
            grpServer.Text = "Server Settings";
            // 
            // grpApiKey
            // 
            grpApiKey.Controls.Add(lblApiKey);
            grpApiKey.Controls.Add(txtApiKey);
            grpApiKey.Controls.Add(btnTestConnection);
            grpApiKey.Controls.Add(lblApiKeyDesc);
            grpApiKey.Location = new Point(460, 250);
            grpApiKey.Name = "grpApiKey";
            grpApiKey.Size = new Size(510, 299);
            grpApiKey.TabIndex = 3;
            grpApiKey.TabStop = false;
            grpApiKey.Text = "API Key";
            // 
            // grpQueue
            // 
            grpQueue.Controls.Add(dgvQueue);
            grpQueue.Controls.Add(panelQueueButtons);
            grpQueue.Location = new Point(10, 260);
            grpQueue.Name = "grpQueue";
            grpQueue.Size = new Size(440, 289);
            grpQueue.TabIndex = 4;
            grpQueue.TabStop = false;
            grpQueue.Text = "Sync Queue";
            // 
            // dgvQueue
            // 
            dgvQueue.AllowUserToAddRows = false;
            dgvQueue.AllowUserToDeleteRows = false;
            dgvQueue.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvQueue.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvQueue.Columns.AddRange(new DataGridViewColumn[] { dataGridViewTextBoxColumn1, dataGridViewTextBoxColumn2, dataGridViewTextBoxColumn3 });
            dgvQueue.Dock = DockStyle.Fill;
            dgvQueue.Location = new Point(3, 19);
            dgvQueue.Name = "dgvQueue";
            dgvQueue.ReadOnly = true;
            dgvQueue.RowHeadersVisible = false;
            dgvQueue.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvQueue.Size = new Size(434, 223);
            dgvQueue.TabIndex = 0;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.HeaderText = "File Path";
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.HeaderText = "Status";
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.HeaderText = "Timestamp";
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.ReadOnly = true;
            // 
            // panelQueueButtons
            // 
            panelQueueButtons.Controls.Add(btnPauseResumeQueue);
            panelQueueButtons.Controls.Add(btnCancelTask);
            panelQueueButtons.Controls.Add(btnClearFailedTasks);
            panelQueueButtons.Dock = DockStyle.Bottom;
            panelQueueButtons.Location = new Point(3, 242);
            panelQueueButtons.Name = "panelQueueButtons";
            panelQueueButtons.Size = new Size(434, 44);
            panelQueueButtons.TabIndex = 10;
            panelQueueButtons.Paint += panelQueueButtons_Paint;
            // 
            // btnPauseResumeQueue
            // 
            btnPauseResumeQueue.Location = new Point(15, 7);
            btnPauseResumeQueue.Name = "btnPauseResumeQueue";
            btnPauseResumeQueue.Size = new Size(120, 30);
            btnPauseResumeQueue.TabIndex = 1;
            btnPauseResumeQueue.Text = "Pause Queue";
            btnPauseResumeQueue.Click += btnPauseResumeQueue_Click;
            // 
            // btnCancelTask
            // 
            btnCancelTask.Location = new Point(150, 7);
            btnCancelTask.Name = "btnCancelTask";
            btnCancelTask.Size = new Size(120, 30);
            btnCancelTask.TabIndex = 2;
            btnCancelTask.Text = "Cancel Task";
            btnCancelTask.Click += btnCancelTask_Click;
            // 
            // btnClearFailedTasks
            // 
            btnClearFailedTasks.Location = new Point(285, 7);
            btnClearFailedTasks.Name = "btnClearFailedTasks";
            btnClearFailedTasks.Size = new Size(120, 30);
            btnClearFailedTasks.TabIndex = 3;
            btnClearFailedTasks.Text = "Clear Failed";
            btnClearFailedTasks.Click += btnClearFailedTasks_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(984, 561);
            Controls.Add(grpFolders);
            Controls.Add(grpServer);
            Controls.Add(grpApiKey);
            Controls.Add(grpQueue);
            Name = "Form1";
            Text = "Immich Sync";
            grpFolders.ResumeLayout(false);
            grpServer.ResumeLayout(false);
            grpApiKey.ResumeLayout(false);
            grpApiKey.PerformLayout();
            grpQueue.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvQueue).EndInit();
            panelQueueButtons.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
    }
}
