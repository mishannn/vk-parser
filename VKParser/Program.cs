using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VKParser
{
    static class Program
    {
        private static MainForm _mainForm = null;
        private static object _mainFormLocker = new object();

        private static FeedForm _feedForm = null;
        private static object _feedFormLocker = new object();

        public static MainForm MainForm
        {
            get
            {
                lock (_mainFormLocker)
                {
                    return _mainForm;
                }
            }
        }

        public static FeedForm FeedForm
        {
            get
            {
                lock (_feedFormLocker)
                {
                    return _feedForm;
                }
            }
        }

        private static void CreateMainForm()
        {
            lock (_mainFormLocker)
            {
                _mainForm = new MainForm();
            }
        }

        private static void CreateFeedForm()
        {
            lock (_feedFormLocker)
            {
                _feedForm = new FeedForm();
                _feedForm.Show();
            }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            CreateMainForm();
            CreateFeedForm();
            Application.Run(MainForm);
        }
    }
}
