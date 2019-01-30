using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace VKParser
{
    class ParserHelper
    {
        public static bool DownloadImage(string url, string fileName)
        {
            try
            {
                string postsFilesDir = Path.GetDirectoryName(Application.ExecutablePath) + @"\posts";
                string postsImagesDir = postsFilesDir + @"\images";
                string postImagePath = postsImagesDir + @"\" + fileName;

                if (!Directory.Exists(postsFilesDir))
                    Directory.CreateDirectory(postsFilesDir);

                if (!Directory.Exists(postsImagesDir))
                    Directory.CreateDirectory(postsImagesDir);

                WebClient webClient = new WebClient();
                webClient.DownloadFileAsync(new Uri(url), postImagePath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static IWebElement[] FindElements(ISearchContext context, string selector)
        {
            try
            {
                return context.FindElements(By.CssSelector(selector)).ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static IWebElement FindElement(ISearchContext context, string selector)
        {
            try
            {
                return context.FindElement(By.CssSelector(selector));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string GetElementText(ISearchContext context, string selector)
        {
            try
            {
                IWebElement element = context.FindElement(By.CssSelector(selector));

                if (element.Text.Length < 1)
                    return null;

                return element.Text;
            }
            catch (Exception)
            {
                // MessageBox.Show("ParserHelper::GetElementText: " + e.Message, e.GetType().FullName);
                return null;
            }
        }

        public static void WaitForPageLoad(ChromeDriver driver)
        {
            try
            {
                while (true)
                {
                    string readyState = driver.ExecuteScript("return document.readyState;").ToString();
                    if (readyState == "complete")
                        break;

                    Thread.Sleep(50);
                }
            }
            catch (Exception)
            {
                // MessageBox.Show("ParserHelper::WaitForPageLoad: " + e.Message, e.GetType().FullName);
            }
        }

        public static void WaitForUrl(ChromeDriver driver, string url)
        {
            try
            {
                while (true)
                {
                    if (driver.Url == url)
                        break;

                    Thread.Sleep(50);
                }
            }
            catch (Exception)
            {
                // MessageBox.Show("ParserHelper::WaitForUrl: " + e.Message, e.GetType().FullName);
            }
        }
    }
}
