using CefSharp;
using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VKParser
{
    public partial class FeedForm : Form
    {
        private Queue<Tuple<string, string, string, string[], string[]>> _postsQueue = new Queue<Tuple<string, string, string, string[], string[]>>();
        private object _postsQueueLocker = new object();

        private ChromiumWebBrowser _browser;
        private object _browserLocker = new object();

        private bool _listingEnded = true;
        private object _listingEndedLocker = new object();

        private Task _postsListingTask = null;
        private object _postsListingTaskLocker = new object();

        public int QueueCount
        {
            get
            {
                lock (_postsQueueLocker)
                {
                    if (_postsQueue != null)
                        return _postsQueue.Count;
                }
                return 0;
            }
        }

        public ChromiumWebBrowser Browser
        {
            get
            {
                lock (_browserLocker)
                {
                    return _browser;
                }
            }
            set
            {
                lock (_browserLocker)
                {
                    _browser = value;
                }
            }
        }

        private bool IsListingEnded()
        {
            lock (_listingEndedLocker)
            {
                return _listingEnded;
            }
        }

        public void AddPostToQueue(string postId, string postTitle, string postText, string[] postLinks, string[] postImages)
        {
            lock (_postsQueueLocker)
            {
                _postsQueue.Enqueue(Tuple.Create(postId, postTitle, postText, postLinks, postImages));
            }
        }

        private void InitializeChromium()
        {
            CefSettings settings = new CefSettings();
            settings.Locale = "ru-RU";
            Cef.Initialize(settings);

            string htmlPath = Path.GetDirectoryName(Application.ExecutablePath) + @"\FeedForm\index.html";
            Browser = new ChromiumWebBrowser(htmlPath);

            Controls.Add(Browser);
            Browser.Dock = DockStyle.Fill;
            Browser.MenuHandler = new FeedFormMenuHandler();
        }

        private void CreateTask()
        {
            lock (_listingEndedLocker)
            {
                _listingEnded = false;
            }

            lock (_postsListingTaskLocker)
            {
                _postsListingTask = new Task(() =>
                {
                    while (!IsListingEnded() || (QueueCount > 0))
                    {
                        while (QueueCount > 0)
                        {
                            Tuple<string, string, string, string[], string[]> postTuple;

                            lock (_postsQueueLocker)
                            {
                                postTuple = _postsQueue.Dequeue();
                            }

                            // MessageBox.Show(postTuple.Item1);
                            // Browser.EvaluateScriptAsync("asddas").Result;

                            string jsPath = Path.GetDirectoryName(Application.ExecutablePath) + @"\FeedForm\add-post.js";
                            using (StreamReader jsReader = new StreamReader(jsPath))
                            {
                                string jsCode = jsReader.ReadToEnd();

                                jsCode = jsCode.Replace("%POST_TITLE%", postTuple.Item2);
                                jsCode = jsCode.Replace("%POST_TEXT%", postTuple.Item3);

                                Task jsTask = Browser.EvaluateScriptAsync(jsCode);
                                jsTask.Wait();
                            }
                        }

                        Thread.Sleep(100);
                    }
                });
            }
        }

        private void StartTask()
        {
            lock (_postsListingTaskLocker)
            {
                _postsListingTask?.Start();
            }
        }

        private void WaitTask()
        {
            lock (_postsListingTaskLocker)
            {
                _postsListingTask?.Wait();
            }
        }

        public FeedForm()
        {
            Opacity = 0;
            InitializeComponent();
            InitializeChromium();
            CreateTask();
            StartTask();
        }

        private void FeedForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                lock (_listingEndedLocker)
                {
                    _listingEnded = true;
                }

                WaitTask();
                Cef.Shutdown();
            }
        }

        private void FeedForm_Shown(object sender, EventArgs e)
        {
            Hide();
            Opacity = 1;
        }
    }
}
