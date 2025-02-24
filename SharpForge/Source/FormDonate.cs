using System.Diagnostics;

namespace SharpForge.Source
{
    public partial class FormDonate : Form
    {
        private readonly string _DonationEuro = "https://www.paypal.com/donate/?business=77G38P6S7QYWJ&no_recurring=0&item_name=Your+support+helps+me+continue+creating+great+work+for+you.+Every+donation+makes+a+difference%2C+thank+you%21&currency_code=EUR";
        private readonly string _DonationDollar = "https://www.paypal.com/donate/?business=77G38P6S7QYWJ&no_recurring=0&item_name=Your+support+helps+me+continue+creating+great+work+for+you.+Every+donation+makes+a+difference%2C+thank+you%21&currency_code=USD";
        private readonly string _DonationReal = "https://www.paypal.com/donate/?business=77G38P6S7QYWJ&no_recurring=0&item_name=Seu+apoio+me+ajuda+a+continuar+criando+um+%C3%B3timo+trabalho+para+voc%C3%AA.+Cada+doa%C3%A7%C3%A3o+faz+a+diferen%C3%A7a%2C+obrigado%21&currency_code=BRL";

        public FormDonate()
        {
            InitializeComponent();
            SetupComponent();
        }

        private void SetupComponent()
        {
            KeyPreview = true;
        }

        private static void OpenLink(string url)
        {
            ProcessStartInfo processStartInfo = new()
            {
                FileName = url,
                UseShellExecute = true
            };

            Process.Start(processStartInfo);
        }

        private void Btn_MoneyEuro_Click(object sender, EventArgs e)
        {
            OpenLink(_DonationEuro);
        }

        private void Btn_MoneyDollar_Click(object sender, EventArgs e)
        {
            OpenLink(_DonationDollar);
        }

        private void Btn_MoneyReal_Click(object sender, EventArgs e)
        {
            OpenLink(_DonationReal);
        }

        private void FormDonate_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Escape)
                Close();
        }
    }
}
