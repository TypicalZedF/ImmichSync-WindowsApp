using System;
using System.Windows.Forms;

namespace ImmichSyncApp
{
    public partial class DebugWindow : Form
    {
        public DebugWindow(string debugText)
        {
            InitializeComponent();
            txtDebug.Text = debugText;
        }
    }
}
