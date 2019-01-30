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
    class TextCollector
    {
        private Queue<Tuple<string, string, string>> _textQueue = new Queue<Tuple<string, string, string>>();
        private object _textQueueLocker = new object();

        private bool _parsingEnded = true;
        private object _parsingEndedLocker = new object();

        private Task _textCollectorTask = null;
        private object _textCollectorTaskLocker = new object();

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
                lock (_textQueueLocker)
                {
                    if (_textQueue != null)
                        return _textQueue.Count;
                }
                return 0;
            }
        }

        public void AddTextToQueue(string postId, string postTitle, string postText)
        {
            lock (_textQueueLocker)
            {
                _textQueue.Enqueue(Tuple.Create(postId, postTitle, postText));
            }
        }

        private void WriteToJSON(string postId, string postTitle, string postText)
        {
            string postFileDir = Path.GetDirectoryName(Application.ExecutablePath) + @"\posts";
            string postFilePath = postFileDir + @"\text.json";

            if (!Directory.Exists(postFileDir))
                Directory.CreateDirectory(postFileDir);

            JObject postsTextObject = new JObject();

            if (File.Exists(postFilePath))
            {
                try
                {
                    using (FileStream fs = new FileStream(postFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
                    using (StreamReader reader = new StreamReader(fs))
                    {
                        string jsonText = reader.ReadToEnd();
                        postsTextObject = JObject.Parse(jsonText);
                    }
                }
                catch (Exception)
                {
                    File.Delete(postFilePath);
                }
            }

            JObject postTextObject = new JObject();
            postTextObject["title"] = postTitle;
            postTextObject["text"] = postText;
            postsTextObject[postId] = postTextObject;

            using (FileStream fs = new FileStream(postFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            using (StreamWriter writer = new StreamWriter(fs))
            {
                writer.Write(postsTextObject.ToString());
            }
        }

        private void CreateTask()
        {
            lock (_parsingEndedLocker)
            {
                _parsingEnded = false;
            }

            lock (_textCollectorTaskLocker)
            {
                _textCollectorTask = new Task(() =>
                {
                    while (!IsParserEnded() || (QueueCount > 0))
                    {
                        while (QueueCount > 0)
                        {
                            Tuple<string, string, string> postTextsTuple;

                            lock (_textQueueLocker)
                            {
                                postTextsTuple = _textQueue.Dequeue();
                            }

                            WriteToJSON(postTextsTuple.Item1, postTextsTuple.Item2, postTextsTuple.Item3);
                        }

                        Thread.Sleep(100);
                    }
                });
            }
        }

        private void StartTask()
        {
            lock (_textCollectorTaskLocker)
            {
                _textCollectorTask?.Start();
            }
        }

        private void WaitTask()
        {
            lock (_textCollectorTaskLocker)
            {
                _textCollectorTask?.Wait();
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
