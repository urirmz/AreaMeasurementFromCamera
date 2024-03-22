using System;
using System.Windows.Forms;

namespace MedicionCamara
{
    internal static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Form mainForm = new Form1();
            mainForm.Text = "Medición con cámara iRayple A5201MU150";
            Application.Run(mainForm);            
        }
    }
}
