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

        //    // TEMPORARY bypass login for development
        //    Session.CurrentUserId = Guid.Parse("01ab3917-1991-4945-9a42-cec0921f3a2a");
        //    Session.CurrentUsername = "dwayne";
        //    Session.CurrentInitials = "DK";
        //    Session.CurrentFullName = "Dwayne Keast";
        //    Session.IsAdmin = true;

        //    Application.Run(new FrmViewDispatch());
        //}


        //Use this one once the app is finished so that the login form works.
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
