using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VKParser
{
    class LinksCollector
    {
        private Queue<Tuple<string, string[]>> _linksQueue = new Queue<Tuple<string, string[]>>();
        private object _linksQueueLocker = new object();

        private bool _parsingEnded = true;
        private object _parsingEndedLocker = new object();

        private Task _linksCollectorTask = null;
        private object _linksCollectorTaskLocker = new object();

        private bool IsParserEnded()
        {
            lock (_parsingEndedLocker)
            {
                return _parsingEnded;
            }
        }

        public int QueueCount
        {
            get
            {
                lock (_linksQueueLocker)
                {
                    if (_linksQueue != null)
                        return _linksQueue.Count;
                }
                return 0;
            }
        }

        public void AddLinksToQueue(string postId, string[] postLinks)
        {
            lock (_linksQueueLocker)
            {
                _linksQueue.Enqueue(Tuple.Create(postId, postLinks));
            }
        }

        private void WriteToJSON(string postId, string[] postLinks)
        {
            string postFileDir = Path.GetDirectoryName(Application.ExecutablePath) + @"\posts";
            string postFilePath = postFileDir + @"\links.json";

            if (!Directory.Exists(postFileDir))
                Directory.CreateDirectory(postFileDir);

            JObject postsLinksObject = new JObject();

            if (File.Exists(postFilePath))
            {
                try
                {
                    using (FileStream fs = new FileStream(postFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
                    using (StreamReader reader = new StreamReader(fs))
                    {
                        string jsonText = reader.ReadToEnd();
                        postsLinksObject = JObject.Parse(jsonText);
                    }
                }
                catch (Exception)
                {
                    File.Delete(postFilePath);
                }
            }

            JArray postLinksArray = new JArray();
            foreach (string postLink in postLinks)
            {
                postLinksArray.Add(postLink);
            }
            postsLinksObject[postId] = postLinksArray;

            using (FileStream fs = new FileStream(postFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            using (StreamWriter writer = new StreamWriter(fs))
            {
                writer.Write(postsLinksObject.ToString());
            }
        }

        private void CreateTask()
        {
            lock (_parsingEndedLocker)
            {
                _parsingEnded = false;
            }

            lock (_linksCollectorTaskLocker)
            {
                _linksCollectorTask = new Task(() =>
                {
                    while (!IsParserEnded() || (QueueCount > 0))
                    {
                        while (QueueCount > 0)
                        {
                            Tuple<string, string[]> postLinksTuple;

                            lock (_linksQueueLocker)
                            {
                                postLinksTuple = _linksQueue.Dequeue();
                            }

                            WriteToJSON(postLinksTuple.Item1, postLinksTuple.Item2);
                        }

                        Thread.Sleep(100);
                    }
                });
            }
        }

        private void StartTask()
        {
            lock (_linksCollectorTaskLocker)
            {
                _linksCollectorTask?.Start();
            }
        }

        private void WaitTask()
        {
            lock (_linksCollectorTaskLocker)
            {
                _linksCollectorTask?.Wait();
            }
        }

        public void Start()
        {
            CreateTask();
            StartTask();
        }

        public void Stop()
        {
            lock (_parsingEndedLocker)
            {
                _parsingEnded = true;
            }

            WaitTask();
        }
    }
}
