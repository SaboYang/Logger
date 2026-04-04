using System.Drawing;
using System.Windows.Forms;

namespace Logger.WinForms.Demo
{
    internal sealed class CodeSamplePanel : GroupBox
    {
        private readonly TextBox _codeTextBox;
        private readonly Button _copyButton;

        public CodeSamplePanel()
        {
            Text = "代码示例";
            Dock = DockStyle.Top;
            Height = 210;
            Padding = new Padding(10);
            Margin = new Padding(0, 0, 0, 10);

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            FlowLayoutPanel buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8),
                WrapContents = false
            };

            _copyButton = new Button
            {
                AutoSize = true,
                Height = 30,
                Padding = new Padding(10, 0, 10, 0),
                Text = "复制代码",
                UseVisualStyleBackColor = true
            };
            _copyButton.Click += CopyButton_Click;
            buttonPanel.Controls.Add(_copyButton);

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

            layout.Controls.Add(buttonPanel, 0, 0);
            layout.Controls.Add(_codeTextBox, 0, 1);
            Controls.Add(layout);
        }

        public string CodeText
        {
            get { return _codeTextBox.Text; }
            set
            {
                _codeTextBox.Text = value ?? string.Empty;
                _codeTextBox.SelectionStart = 0;
                _codeTextBox.SelectionLength = 0;
                _copyButton.Enabled = _codeTextBox.TextLength > 0;
                _copyButton.Text = "复制代码";
            }
        }

        private void CopyButton_Click(object sender, System.EventArgs e)
        {
            if (string.IsNullOrEmpty(_codeTextBox.Text))
            {
                return;
            }

            Clipboard.SetText(_codeTextBox.Text);
            _copyButton.Text = "已复制";
        }
    }
}
