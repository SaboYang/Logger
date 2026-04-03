using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Logger.WinForms.Demo
{
    [DesignerCategory("Code")]
    internal sealed class StressSummaryPanel : GroupBox
    {
        private readonly Label _scenarioValueLabel;
        private readonly Label _statusValueLabel;
        private readonly Label _logCountValueLabel;
        private readonly Label _durationValueLabel;
        private readonly Label _throughputValueLabel;
        private readonly Label _timestampValueLabel;
        private readonly Label _detailsValueLabel;

        public StressSummaryPanel()
        {
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Dock = DockStyle.Top;
            Margin = new Padding(0, 0, 0, 10);
            Padding = new Padding(12, 10, 12, 12);
            Text = "压测结果摘要";

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 4,
                RowCount = 4
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            _scenarioValueLabel = CreateValueLabel();
            _statusValueLabel = CreateValueLabel();
            _logCountValueLabel = CreateValueLabel();
            _durationValueLabel = CreateValueLabel();
            _throughputValueLabel = CreateValueLabel();
            _timestampValueLabel = CreateValueLabel();
            _detailsValueLabel = CreateValueLabel();
            _detailsValueLabel.AutoEllipsis = false;
            _detailsValueLabel.MaximumSize = new Size(880, 0);

            AddField(layout, 0, "压测场景", _scenarioValueLabel);
            AddField(layout, 2, "状态", _statusValueLabel);
            AddField(layout, 4, "日志条数", _logCountValueLabel);
            AddField(layout, 6, "耗时", _durationValueLabel);
            AddField(layout, 8, "吞吐量", _throughputValueLabel);
            AddField(layout, 10, "更新时间", _timestampValueLabel);

            Label detailsTitleLabel = CreateTitleLabel("说明");
            detailsTitleLabel.Margin = new Padding(0, 6, 12, 0);
            _detailsValueLabel.Margin = new Padding(0, 6, 0, 0);
            layout.Controls.Add(detailsTitleLabel, 0, 3);
            layout.Controls.Add(_detailsValueLabel, 1, 3);
            layout.SetColumnSpan(_detailsValueLabel, 3);

            Controls.Add(layout);
        }

        public void UpdateSummary(StressTestSummary summary)
        {
            if (summary == null)
            {
                return;
            }

            _scenarioValueLabel.Text = summary.Scenario;
            _statusValueLabel.Text = GetStatusText(summary.State);
            _statusValueLabel.ForeColor = GetStatusColor(summary.State);
            _logCountValueLabel.Text = summary.LogCount > 0 ? string.Format("{0:N0} 条", summary.LogCount) : "-";
            _durationValueLabel.Text = summary.DurationMs > 0 ? string.Format("{0:N0} ms", summary.DurationMs) : "-";
            _throughputValueLabel.Text = summary.State == StressTestState.Succeeded
                ? string.Format("{0:N0} 条/秒", summary.Throughput)
                : "-";
            _timestampValueLabel.Text = summary.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            _detailsValueLabel.Text = string.IsNullOrWhiteSpace(summary.Details) ? "-" : summary.Details;
        }

        private void AddField(TableLayoutPanel layout, int columnIndex, string title, Label valueLabel)
        {
            int rowIndex = columnIndex / 4;
            int actualColumn = columnIndex % 4;

            Label titleLabel = CreateTitleLabel(title);
            layout.Controls.Add(titleLabel, actualColumn, rowIndex);
            layout.Controls.Add(valueLabel, actualColumn + 1, rowIndex);
        }

        private static Label CreateTitleLabel(string text)
        {
            return new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 6, 12, 0),
                ForeColor = Color.FromArgb(75, 85, 99),
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                Text = text
            };
        }

        private static Label CreateValueLabel()
        {
            return new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 6, 24, 0),
                ForeColor = Color.FromArgb(31, 41, 55),
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point)
            };
        }

        private static Color GetStatusColor(StressTestState state)
        {
            switch (state)
            {
                case StressTestState.Running:
                    return Color.FromArgb(29, 78, 216);
                case StressTestState.Succeeded:
                    return Color.FromArgb(22, 163, 74);
                case StressTestState.Failed:
                    return Color.FromArgb(220, 38, 38);
                default:
                    return Color.FromArgb(107, 114, 128);
            }
        }

        private static string GetStatusText(StressTestState state)
        {
            switch (state)
            {
                case StressTestState.Running:
                    return "进行中";
                case StressTestState.Succeeded:
                    return "成功";
                case StressTestState.Failed:
                    return "失败";
                default:
                    return "未执行";
            }
        }
    }
}
