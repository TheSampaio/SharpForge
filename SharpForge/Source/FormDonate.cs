using System.Diagnostics;

namespace SharpForge.Source;

/// <summary>
/// Provides currency-specific links for supporting Sharp Forge development.
/// </summary>
public sealed partial class FormDonate : Form
{
    private const string EuroDonationUrl = "https://www.paypal.com/donate/?business=77G38P6S7QYWJ&no_recurring=0&item_name=Your+support+helps+me+continue+creating+great+work+for+you.+Every+donation+makes+a+difference%2C+thank+you%21&currency_code=EUR";
    private const string DollarDonationUrl = "https://www.paypal.com/donate/?business=77G38P6S7QYWJ&no_recurring=0&item_name=Your+support+helps+me+continue+creating+great+work+for+you.+Every+donation+makes+a+difference%2C+thank+you%21&currency_code=USD";
    private const string RealDonationUrl = "https://www.paypal.com/donate/?business=77G38P6S7QYWJ&no_recurring=0&item_name=Seu+apoio+me+ajuda+a+continuar+criando+um+%C3%B3timo+trabalho+para+voc%C3%AA.+Cada+doa%C3%A7%C3%A3o+faz+a+diferen%C3%A7a%2C+obrigado%21&currency_code=BRL";

    /// <summary>
    /// Initializes the donation window.
    /// </summary>
    public FormDonate()
    {
        InitializeComponent();
        KeyPreview = true;
    }

    /// <summary>
    /// Opens a donation link in the user's default browser.
    /// </summary>
    /// <param name="url">The HTTPS donation URL.</param>
    private void OpenLink(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            MessageBox.Show(
                this,
                $"The donation page could not be opened.{Environment.NewLine}{Environment.NewLine}{exception.Message}",
                "Sharp Forge",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Opens the Euro donation page.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void Btn_MoneyEuro_Click(object? sender, EventArgs e)
    {
        OpenLink(EuroDonationUrl);
    }

    /// <summary>
    /// Opens the US Dollar donation page.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void Btn_MoneyDollar_Click(object? sender, EventArgs e)
    {
        OpenLink(DollarDonationUrl);
    }

    /// <summary>
    /// Opens the Brazilian Real donation page.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void Btn_MoneyReal_Click(object? sender, EventArgs e)
    {
        OpenLink(RealDonationUrl);
    }

    /// <summary>
    /// Closes the dialog when the user presses Escape.
    /// </summary>
    /// <param name="sender">The form that raised the event.</param>
    /// <param name="e">The key event data.</param>
    private void FormDonate_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Escape)
        {
            return;
        }

        e.SuppressKeyPress = true;
        Close();
    }
}
