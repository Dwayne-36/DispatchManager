using DispatchManager.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DispatchManager
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        //static void Main()
        //{
        //    Application.EnableVisualStyles();
        //    Application.SetCompatibleTextRenderingDefault(false);
        //    Application.Run(new FrmViewDispatch());
        //}
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Show login form first
            using (FrmLogin loginForm = new FrmLogin())
            {
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    Application.Run(new FrmViewDispatch()); // Only run main form if login succeeds
                }
                else
                {
                    Application.Exit(); // Quit if login fails or cancelled
                }
            }
        }
    }
}
