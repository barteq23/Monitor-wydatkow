using System;
using System.Windows.Forms;

namespace BudgetAnalyzerApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm()); // Uruchomienie głównego formularza
        }
    }
}
