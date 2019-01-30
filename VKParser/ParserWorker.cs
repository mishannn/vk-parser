using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VKParser
{
    class ParserWorker : BackgroundWorker
    {
        private bool _parsingEnded = true;
        private object _parsingEndedLocker = new object();

        private TextCollector _textCollector = new TextCollector();
        private object _textCollectorLocker = new object();

        private LinksCollector _linksCollector = new LinksCollector();
        private object _linksCollectorLocker = new object();

        private ImagesCollector _imagesCollector = new ImagesCollector();
        private object _imagesCollectorLocker = new object();

        public TextCollector ParserTextCollector
        {
            get
            {
                lock (_textCollectorLocker)
                {
                    return _textCollector;
                }
            }
        }

        public LinksCollector ParserLinksCollector
        {
            get
            {
                lock (_linksCollectorLocker)
                {
                    return _linksCollector;
                }
            }
        }

        public ImagesCollector ParserImagesCollector
        {
            get
            {
                lock (_imagesCollectorLocker)
                {
                    return _imagesCollector;
                }
            }
        }

        public bool ParsingEnded
        {
            get
            {
                lock (_parsingEndedLocker)
                {
                    return _parsingEnded;
                }
            }
            private set
            {
                lock (_parsingEndedLocker)
                {
                    _parsingEnded = value;
                }
            }
        }

        public ParserWorker()
        {
            WorkerReportsProgress = true;
            WorkerSupportsCancellation = true;
        }

        private void HandlePost(ChromeDriver chrome, IWebElement post)
        {
            try
            {
                string postId = post.GetAttribute("id");

                string postTitle = ParserHelper.GetElementText(post, ".post_author");
                string postText = ParserHelper.GetElementText(post, ".wall_post_text");

                // ПАРСИНГ ТЕКСТА
                if (postTitle != null && postText != null)
                {
                    Program.MainForm.Log($@"Записываем текст новости с ID ""{postId}""");
                    ParserTextCollector.AddTextToQueue(postId, postTitle, postText);
                }
                else
                {
                    Program.MainForm.Log($@"Новость с ID ""{postId}"" не имеет текста");
                }

                // ПАРСИНГ ССЫЛОК
                IWebElement wallPostTextElement = ParserHelper.FindElement(post, ".wall_post_text");
                List<string> postLinks = new List<string>();

                if (wallPostTextElement != null)
                {
                    IWebElement[] linksElements = ParserHelper.FindElements(wallPostTextElement, "a");

                    foreach (IWebElement linkElement in linksElements)
                    {
                        string link = linkElement.GetAttribute("href");
                        if (link == null)
                            continue;

                        if (link.Length < 1)
                            continue;

                        postLinks.Add(link);
                    }

                    if (postLinks.Count > 0)
                    {
                        Program.MainForm.Log($@"Записываем ссылки новости с ID ""{postId}""");
                        ParserLinksCollector.AddLinksToQueue(postId, postLinks.ToArray());
                    }
                    else
                    {
                        Program.MainForm.Log($@"Новость с ID ""{postId}"" не имеет ссылок");
                    }
                }

                // ПАРСИНГ ИЗОБРАЖЕНИЙ
                IWebElement thumbElement = ParserHelper.FindElement(post, ".page_post_sized_thumbs");
                List<string> postImages = new List<string>();

                if (thumbElement != null)
                {
                    IWebElement[] imagesElements = ParserHelper.FindElements(thumbElement, ".page_post_thumb_wrap");

                    foreach (IWebElement imageElement in imagesElements)
                    {
                        string style = imageElement.GetAttribute("style");
                        Regex regex = new Regex("background-image: url\\(\"(.+/(.+?.jpg))\"\\);");
                        Match match = regex.Match(style);
                        if (match.Success)
                        {
                            if (ParserHelper.DownloadImage(match.Groups[1].Value, match.Groups[2].Value))
                                postImages.Add(match.Groups[2].Value);
                        }
                    }

                    if (postImages.Count > 0)
                    {
                        Program.MainForm.Log($@"Записываем изображения новости с ID ""{postId}""");
                        ParserImagesCollector.AddImagesToQueue(postId, postImages.ToArray());
                    }
                    else
                    {
                        Program.MainForm.Log($@"Новость с ID ""{postId}"" не имеет изображений");
                    }
                }

                Program.MainForm.Log($@"Добавляем новость с ID ""{postId}"" в очередь на вывод");
                Program.FeedForm.AddPostToQueue(postId, postTitle, postText, postLinks.ToArray(), postImages.ToArray());
            }
            catch (Exception exception)
            {
                MessageBox.Show("ParserWorker::HandlePost: " + exception.Message + "\n" + exception.StackTrace, exception.GetType().FullName);
            }
        }

        private void ParseNews(ChromeDriver chrome)
        {
            try
            {
                ReportProgress(10);
                chrome.Manage().Window.Maximize();

                Program.MainForm.Log("Открываем главную страницу ВК...");
                chrome.Navigate().GoToUrl("https://vk.com");

                ReportProgress(30);
                Program.MainForm.Log("Вводим данные логина...");

                IWebElement query;
                query = ParserHelper.FindElement(chrome, "#index_email");
                query.SendKeys(LoginData.Username);
                query = ParserHelper.FindElement(chrome, "#index_pass");
                query.SendKeys(LoginData.Password);
                query.Submit();

                ReportProgress(50);
                Program.MainForm.Log("Ждем открытия страницы новостей...");
                ParserHelper.WaitForUrl(chrome, "https://vk.com/feed");

                ReportProgress(70);
                Program.MainForm.Log("Ищем посты...");
                IWebElement[] postsElements = ParserHelper.FindElements(chrome, ".post");
                Program.MainForm.Log($"Найдено {postsElements.Length} новостей!");
                foreach (IWebElement post in postsElements)
                {
                    HandlePost(chrome, post);
                }

                Program.MainForm.Log("Все новости обработаны!");
                ReportProgress(100);
            }
            catch (Exception exception)
            {
                MessageBox.Show("ParserWorker::ParseNews: " + exception.Message + "\n" + exception.StackTrace, exception.GetType().FullName);
            }
        }

        protected override void OnDoWork(DoWorkEventArgs e)
        {
            try
            {
                ParsingEnded = false;

                ReportProgress(1);

                ChromeOptions chromeOptions = new ChromeOptions();
                chromeOptions.AddArgument("headless");
                Program.MainForm.Log("Установлены опции браузера");

                ChromeDriverService chromeDriverService = ChromeDriverService.CreateDefaultService();
                chromeDriverService.HideCommandPromptWindow = true;
                Program.MainForm.Log("Настроен сервис драйвера браузера");

                ParserTextCollector.Start();
                Program.MainForm.Log("Запущен сборщик текста");

                ParserLinksCollector.Start();
                Program.MainForm.Log("Запущен сборщик ссылок");

                ParserImagesCollector.Start();
                Program.MainForm.Log("Запущен сборщик изображений");

                Program.MainForm.Log("Запускаем браузер");
                using (ChromeDriver chrome = new ChromeDriver(chromeDriverService, chromeOptions))
                {
                    Program.MainForm.Log("Начинаем парсинг новостей!");
                    ParseNews(chrome);
                }

                Program.MainForm.Log("Останавливаем сборщик изображений");
                ParserImagesCollector.Stop();

                Program.MainForm.Log("Останавливаем сборщик ссылок");
                ParserLinksCollector.Stop();

                Program.MainForm.Log("Останавливаем сборщик текста");
                ParserTextCollector.Stop();

                Program.MainForm.Log("Работа завершена!");
                ParsingEnded = true;
            }
            catch (Exception exception)
            {
                MessageBox.Show("ParserWorker::OnDoWork: " + exception.Message + "\n" + exception.StackTrace, exception.GetType().FullName);
            }
        }
    }
}
