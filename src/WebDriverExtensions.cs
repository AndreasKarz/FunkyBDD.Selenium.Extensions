using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;

namespace FunkyBDD.Selenium.Extensions
{
    public static class WebDriverExtensions
    {
        /// <summary>
        /// Get current ScrollYPosition
        /// </summary>
        /// <param name="driver">IWebDriver to check ScrollPosition</param>
        /// <returns></returns>
        public static int GetScrollPosition(this IWebDriver driver)
        {
            var scriptExecutor = (IJavaScriptExecutor)driver;
            return Convert.ToInt32(scriptExecutor.ExecuteScript("return window.pageYOffset;"));
        }

        /// <summary>
        ///     Compares 2 images and returns true if the images are identical else false.
        ///     Also it's possible to create and safe a Heathmap of the differences.
        /// </summary>
        /// <param name="driver">Selenium Webdriver to extend with</param>
        /// <param name="bmp1">First image for comparsion</param>
        /// <param name="bmp2">First image for comparsion</param>
        /// <param name="heathMap">Boolean whether a Heathmap should be created. Default is true.</param>
        /// <param name="heathMapFileName">File name for the Heathmap image without extension. Default is "heathmap"</param>
        /// <param name="accuracy">Accuracy in promille / 100, default 999</param>
        /// <param name="r">R of RGB, Default 255</param>
        /// <param name="g">G of RGB, Default 0</param>
        /// <param name="b">B of RGB, Default 0</param>
        /// <returns>true if the images are identical else false</returns>
        public static bool ImagesEquals(this IWebDriver driver, Bitmap bmp1, Bitmap bmp2, bool heathMap = true, string heathMapFileName = "./heathmap.jpg", int accuracy = 1000, int r = 255, int g = 0, int b = 0)
        {
            var heathColorSpot = Color.FromArgb(2, r, g, b);
            var heathColorDraw = Color.FromArgb(128, r, g, b);

            var heatmap = new Bitmap(bmp2);
            var lastX = -21;
            var lastY = -21;
            var pen = new Pen(heathColorDraw, 3);
            var numberOfHits = 0;
            var numberOfPixels = bmp1.Width * bmp1.Height;
            var numberOfSpots = 0;
            var numberOfNotWhitePixels = 0;

            if (!bmp1.Size.Equals(bmp2.Size))
            {
                numberOfSpots = 1;
                numberOfHits = numberOfPixels;
            }
            else
            {
                for (var x = 0; x < bmp1.Width; ++x)
                {
                    for (var y = 0; y < bmp1.Height; ++y)
                    {
                        numberOfNotWhitePixels = (bmp1.GetPixel(x, y).Name != "fffefefe") ? numberOfNotWhitePixels + 1 : numberOfNotWhitePixels;
                        if (bmp1.GetPixel(x, y) != bmp2.GetPixel(x, y))
                        {
                            using (var graph = Graphics.FromImage(heatmap))
                            {
                                graph.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                                using (Brush brush = new SolidBrush(heathColorDraw))
                                {
                                    if (lastX < (x - 20) || lastY < (y - 20))
                                    {
                                        graph.DrawEllipse(pen, x - 10, y - 10, 20, 20);
                                        lastX = x;
                                        lastY = y;
                                        numberOfSpots++;
                                    }
                                }
                                using (Brush brush = new SolidBrush(heathColorSpot))
                                {
                                    graph.FillEllipse(brush, x - 5, y - 5, 10, 10);
                                }
                                using (Brush brush = new SolidBrush(heathColorSpot))
                                {
                                    graph.FillEllipse(brush, x - 3, y - 3, 6, 6);
                                }
                            }
                            numberOfHits++;
                        }
                    }
                }
            }

            var numberOfMaxFails = Math.Abs((numberOfNotWhitePixels - ((numberOfNotWhitePixels / 1000) * accuracy)) / 100);

            if (numberOfHits > numberOfMaxFails)
            {
                if (heathMap)
                {
                    var propItem = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
                    var propValue = $"A total of {numberOfHits} different pixels found";

                    propItem.Id = 40092;
                    propItem.Type = 1;
                    propItem.Len = propValue.Length + 1;
                    propItem.Value = Encoding.Unicode.GetBytes(propValue);
                    heatmap.SetPropertyItem(propItem);

                    propValue = (numberOfHits * 100 / (double)numberOfPixels).ToString("0.###") + $"% deviation";
                    propItem.Id = 40091;
                    propItem.Type = 1;
                    propItem.Len = propValue.Length + 1;
                    propItem.Value = Encoding.Unicode.GetBytes(propValue);
                    heatmap.SetPropertyItem(propItem);

                    propValue = $"{numberOfSpots} spots found";
                    propItem.Id = 40095;
                    propItem.Type = 1;
                    propItem.Len = propValue.Length + 1;
                    propItem.Value = Encoding.Unicode.GetBytes(propValue);
                    heatmap.SetPropertyItem(propItem);
                    heatmap.Save(heathMapFileName, ImageFormat.Jpeg);

                }
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Returns a partial screenshot of the selected element
        /// </summary>
        /// <param name="driver">This IWebDriver to extend</param>
        /// <param name="by">The selector. If "null", the whole screen will be taken</param>
        /// <param name="onBeforeScreenShot">Called immediately before taking the screenshot</param>
        /// <param name="onAfterScreenshot">Called immediately after taking the screenshot</param>
        public static Bitmap GetElementScreenshot(this IWebDriver driver, By by, Action onBeforeScreenShot = null, Action onAfterScreenshot = null)
        {
            if (by == null)
            {
                // No element has been selected. We just return the normalized image.
                return GetNormalizedScreenshot(driver);
            }

            onBeforeScreenShot?.Invoke();

            IWebElement element = driver.TryFindElement(by);

            #region Element location with Safari Hack

            int x;
            int y;
            try
            {
                x = element.Location.X;
            }
            catch (Exception)
            {
                x = ((RemoteWebElement)element).LocationOnScreenOnceScrolledIntoView.X;
            }

            try
            {
                y = element.Location.Y;
            }
            catch (Exception)
            {
                y = ((RemoteWebElement)element).LocationOnScreenOnceScrolledIntoView.Y;
            }

            #endregion

            var executor = (IJavaScriptExecutor)driver;

            executor.ExecuteScript($"window.scroll(0, {y}); ");

            var yScrollPos = Convert.ToInt32(executor.ExecuteScript(
                "var doc = document.documentElement; return (window.pageYOffset || doc.scrollTop)  - (doc.clientTop || 0);"));



            Bitmap img = GetNormalizedScreenshot(driver);

            onAfterScreenshot?.Invoke();

            var startX = x;
            var startY = y - yScrollPos;
            var width = element.Size.Width;
            var height = element.Size.Height;

            if (height > img.Height)
            {
                height = img.Height;
            }

            return img.Clone(new Rectangle(startX, startY, width, height), img.PixelFormat);
        }


        /// <summary>
        ///     Returns a normalized screenshot
        ///        - Resize image if the device pixel ratio is above 1
        ///        - Returns only the viewport of the browser. (E.g. in iPhone app and nav bar is removed)
        /// </summary>
        /// <param name="driver">This IWebDriver to extend</param>
        public static Bitmap GetNormalizedScreenshot(IWebDriver driver)
        {
            ICapabilities capabilities = ((RemoteWebDriver)driver).Capabilities;

            var yOffsetValue = (string)capabilities.GetCapability("sl_yOffset");
            var yOffset = Convert.ToInt32(yOffsetValue);

            Screenshot sc = ((ITakesScreenshot)driver).GetScreenshot();
            var img = Image.FromStream(new MemoryStream(sc.AsByteArray)) as Bitmap;

            var executor = (IJavaScriptExecutor)driver;
            var devicePixelRatio = Convert.ToDouble(executor.ExecuteScript("return window.devicePixelRatio || 1"));

            // Resize image if devicePixelRatio is not 1
            if (Math.Abs(devicePixelRatio - 1) > 0.0000001)
            {
                var newWidth = Convert.ToInt32(Math.Round(img.Width / devicePixelRatio));
                var newHeight = Convert.ToInt32(Math.Round(img.Height / devicePixelRatio));

                img = new Bitmap(img, newWidth, newHeight);
            }

            var height = Convert.ToInt32(executor.ExecuteScript("return window.innerHeight;"));

            if (height > img.Height)
            {
                height = img.Height;
            }

            Bitmap retImg = img.Clone(new Rectangle(0, yOffset, img.Width, height), img.PixelFormat);

            return retImg;
        }


        /// <summary>
        ///     Navigates to the relative path based on the current URL.
        /// </summary>
        /// <param name="driver">Selenium Webdriver to extend with</param>
        /// <param name="path">path relative to URL</param>
        public static void NavigateToPath(this IWebDriver driver, string path)
        {
            var baseUrl = new Uri(driver.Url).GetLeftPart(UriPartial.Authority);
            var newUrl = baseUrl + path;
            driver.Navigate().GoToUrl(newUrl);
        }

        /// <summary>
        ///     Set the key/value pair into the local storage.
        /// </summary>
        /// <param name="driver">Selenium Webdriver to extend with</param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void LocalStorageSetItem(this IWebDriver driver, string key, string value)
        {
            var scriptExecutor = (IJavaScriptExecutor)driver;
            scriptExecutor.ExecuteScript($"window.localStorage.setItem('{key}','{value}');");
        }

        /// <summary>
        ///     Get the value from the key element from the local storage
        /// </summary>
        /// <param name="driver">Selenium Webdriver to extend with</param>
        /// <param name="key"></param>
        /// <returns>(string) Value</returns>
        public static string LocalStorageGetItem(this IWebDriver driver, string key)
        {
            var scriptExecutor = (IJavaScriptExecutor)driver;
            return (string)scriptExecutor.ExecuteScript($"return window.localStorage.getItem('{key}');");
        }

        /// <summary>
        ///     Clear the local storage
        /// </summary>
        public static void LocalStorageClear(this IWebDriver driver)
        {
            try
            {
                var scriptExecutor = (IJavaScriptExecutor)driver;
                scriptExecutor.ExecuteScript("window.localStorage.clear();");
            }
            catch (Exception)
            {
                // some browsers block the access to the local storage clear methode
            }
        }

        /// <summary>
        ///     Count the elements in the local storage
        /// </summary>
        /// <returns>(int) Count of elements</returns>
        public static int LocalStorageLength(this IWebDriver driver)
        {
            var scriptExecutor = (IJavaScriptExecutor)driver;
            return (int)scriptExecutor.ExecuteScript("return window.localStorage.length;");
        }

        /// <summary>
        /// Waits until the element is clickable
        /// </summary>
        public static void WaitUntilElementIsClickable(this IWebDriver driver, IWebElement element, int timeout)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeout));
            try
            {
                wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(element));
            }
            catch
            {
                // ignored
            }
        }

        public static void WaitUntilElementIsClickable(this IWebDriver driver, IWebElement element)
        {
            WaitUntilElementIsClickable(driver, element, 5);
        }

        /// <summary>
        ///     Get the element matching the current criteria
        /// </summary>
        /// <param name="driver">Selenium Webdriver to extend with</param>
        /// <param name="by">Selenium By selector</param>
        /// <returns>First matching IWebElement or Null</returns>
        public static IWebElement FindElementFirstOrDefault(this IWebDriver driver, By by)
        {
            return FindElementFirstOrDefault(driver, by, 5);
        }

        /// <seealso cref="FindElementFirstOrDefault"/>
        /// <param name="explicitWait">Timeout in seconds</param>
        public static IWebElement FindElementFirstOrDefault(this IWebDriver driver, By by, int explicitWait)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(explicitWait));

            try
            {
                return wait.Until(
                    d => {
                        try
                        {
                            return driver.FindElement(by);
                        }
                        catch (Exception)
                        {
                            return null;
                        }
                    }
                );
            }
            catch (Exception)
            {
                return null;
            }

        }

        /// <summary>
        ///     Set the browser size
        /// </summary>
        /// <param name="driver">Selenium Webdriver to extend with</param>
        /// <param name="width">Width in pixels. Default 750</param>
        /// <param name="height">Height in pixels. Default 750</param>
        public static void SetMobileSize(this IWebDriver driver, int width = 640, int height = 1024)
        {
            driver.Manage().Window.Size = new Size(width, height);
        }

        /// <summary>
        ///     Make a screenshot and safe it into the folder "SeleniumResults"
        /// </summary>
        /// <param name="driver">Selenium Webdriver to extend with</param>
        /// <param name="fileName"></param>
        public static void MakeScreenshot(this IWebDriver driver, string fileName)
        {
            Thread.Sleep(1000);
            var artifactDirectory = Directory.GetCurrentDirectory();
            artifactDirectory = Path.GetFullPath(Path.Combine(artifactDirectory, $"TestResults"));

            if (!Directory.Exists(artifactDirectory))
            {
                Directory.CreateDirectory(artifactDirectory);
            }

            var screenshotFilePath = Path.Combine(artifactDirectory, fileName);

            try
            {
                Screenshot screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                screenshot.SaveAsFile(screenshotFilePath, ScreenshotImageFormat.Png);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

        /// <summary>
        ///     Execute a JavaScript into the actual browser
        /// </summary>
        /// <param name="driver">Selenium Webdriver to extend with</param>
        /// <param name="script"></param>
        public static void ExecuteScript(this IWebDriver driver, string script)
        {
            var scriptExecutor = (IJavaScriptExecutor)driver;
            scriptExecutor.ExecuteScript(script);
        }

        /// <summary>
        ///    Set the Selenium flag in the SL Namespace
        /// </summary>
        /// <param name="driver"></param>
        public static void SetSeleniumFlag(this IWebDriver driver)
        {
            var scriptExecutor = (IJavaScriptExecutor)driver;
            scriptExecutor.ExecuteScript("SL.selenium=true;");
        }
    }
}
