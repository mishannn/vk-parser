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
    class ImagesCollector
    {
        private Queue<Tuple<string, string[]>> _imagesQueue = new Queue<Tuple<string, string[]>>();
        private object _imagesQueueLocker = new object();

        private bool _parsingEnded = true;
        private object _parsingEndedLocker = new object();

        private Task _imagesCollectorTask = null;
        private object _imagesCollectorTaskLocker = new object();

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
                lock (_imagesQueueLocker)
                {
                    if (_imagesQueue != null)
                        return _imagesQueue.Count;
                }
                return 0;
            }
        }

        public void AddImagesToQueue(string postId, string[] postImages)
        {
            lock (_imagesQueueLocker)
            {
                _imagesQueue.Enqueue(Tuple.Create(postId, postImages));
            }
        }

        private void WriteToJSON(string postId, string[] postImages)
        {
            string postsFilesDir = Path.GetDirectoryName(Application.ExecutablePath) + @"\posts";
            string postsFilePath = postsFilesDir + @"\images.json";

            if (!Directory.Exists(postsFilesDir))
                Directory.CreateDirectory(postsFilesDir);

            JObject postsImagesObject = new JObject();

            if (File.Exists(postsFilePath))
            {
                try
                {
                    using (FileStream fs = new FileStream(postsFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
                    using (StreamReader reader = new StreamReader(fs))
                    {
                        string jsonText = reader.ReadToEnd();
                        postsImagesObject = JObject.Parse(jsonText);
                    }
                }
                catch (Exception)
                {
                    File.Delete(postsFilePath);
                }
            }

            JArray postImagesArray = new JArray();
            foreach (string postImage in postImages)
            {
                postImagesArray.Add(postImage);
            }
            postsImagesObject[postId] = postImagesArray;

            using (FileStream fs = new FileStream(postsFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            using (StreamWriter writer = new StreamWriter(fs))
            {
                writer.Write(postsImagesObject.ToString());
            }
        }

        private void CreateTask()
        {
            lock (_parsingEndedLocker)
            {
                _parsingEnded = false;
            }

            lock (_imagesCollectorTaskLocker)
            {
                _imagesCollectorTask = new Task(() =>
                {
                    while (!IsParserEnded() || (QueueCount > 0))
                    {
                        while (QueueCount > 0)
                        {
                            Tuple<string, string[]> postLinksTuple;

                            lock (_imagesQueueLocker)
                            {
                                postLinksTuple = _imagesQueue.Dequeue();
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
            lock (_imagesCollectorTaskLocker)
            {
                _imagesCollectorTask?.Start();
            }
        }

        private void WaitTask()
        {
            lock (_imagesCollectorTaskLocker)
            {
                _imagesCollectorTask?.Wait();
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
