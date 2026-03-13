using System;
using System.Windows.Forms;
using mRemoteNG.Resources.Language;

namespace mRemoteNG.UI.Controls
{
    public class MrngSearchBox : MrngTextBox
    {
        private readonly PictureBox _pbClear = new();
        private readonly ToolTip _btClearToolTip = new();

        public MrngSearchBox()
        {
            TextChanged += NGSearchBox_TextChanged;
            AddClearButton();
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            _btClearToolTip.SetToolTip(_pbClear, Language.ClearSearchString);
        }

        private void AddClearButton()
        {
            _pbClear.Image = Properties.Resources.Close_16x;
            _pbClear.Width = 20;
            _pbClear.Dock = DockStyle.Right;
            _pbClear.Cursor = Cursors.Default;
            _pbClear.Click += PbClear_Click;
            _pbClear.Visible = false;
            Controls.Add(_pbClear);
        }

        private void PbClear_Click(object sender, EventArgs e) => Text = string.Empty;

        private void NGSearchBox_TextChanged(object sender, EventArgs e)
        {
            _pbClear.Visible = TextLength > 0;
        }
    }
}
