internal sealed class UpdateProgressForm : Form
{
    private readonly Label _messageLabel;
    private readonly Label _detailLabel;
    private readonly ProgressBar _progressBar;

    public UpdateProgressForm(string title)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ControlBox = false;
        ShowInTaskbar = true;
        ClientSize = new Size(520, 150);

        _messageLabel = new Label
        {
            AutoSize = false,
            Location = new Point(18, 16),
            Size = new Size(484, 28),
            Font = new Font(FontFamily.GenericSansSerif, 10f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _detailLabel = new Label
        {
            AutoSize = false,
            Location = new Point(18, 52),
            Size = new Size(484, 40),
            TextAlign = ContentAlignment.TopLeft
        };

        _progressBar = new ProgressBar
        {
            Location = new Point(18, 104),
            Size = new Size(484, 24),
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 25
        };

        Controls.Add(_messageLabel);
        Controls.Add(_detailLabel);
        Controls.Add(_progressBar);
    }

    public void UpdateProgress(UpdateProgressInfo progress)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateProgress(progress));
            return;
        }

        _messageLabel.Text = progress.Message;
        _detailLabel.Text = progress.Detail ?? string.Empty;

        if (progress.Percent is int percent)
        {
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.MarqueeAnimationSpeed = 0;
            _progressBar.Value = Math.Clamp(percent, _progressBar.Minimum, _progressBar.Maximum);
        }
        else
        {
            _progressBar.Style = ProgressBarStyle.Marquee;
            _progressBar.MarqueeAnimationSpeed = 25;
        }
    }
}
