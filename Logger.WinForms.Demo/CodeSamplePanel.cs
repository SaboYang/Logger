using System.Drawing;
using System.Windows.Forms;

namespace Logger.WinForms.Demo
{
    internal sealed class CodeSamplePanel : GroupBox
    {
        private readonly TextBox _codeTextBox;

        public CodeSamplePanel()
        {
            Text = "代码示例";
            Dock = DockStyle.Top;
            Height = 210;
            Padding = new Padding(10);
            Margin = new Padding(0, 0, 0, 10);

            _codeTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point)
            };

            Controls.Add(_codeTextBox);
        }

        public string CodeText
        {
            get { return _codeTextBox.Text; }
            set
            {
                _codeTextBox.Text = value ?? string.Empty;
                _codeTextBox.SelectionStart = 0;
                _codeTextBox.SelectionLength = 0;
            }
        }
    }
}
