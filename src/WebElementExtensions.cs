using System;
using System.Collections.ObjectModel;
using OpenQA.Selenium;
using OpenQA.Selenium.Internal;
using OpenQA.Selenium.Support.UI;

namespace FunkyBDD.Selenium.Extensions
{
    /// <summary>
    ///   Some helpers for the IWebElement
    /// </summary>
    public static class WebElementExtensions
    {
        private static IWebDriver WebDriver(IWebElement element)
        {
            return ((IWrapsDriver)element).WrappedDriver;
        }

        private static IJavaScriptExecutor JavaScriptExecutor(IWebElement element)
        {
            return (IJavaScriptExecutor)WebDriver(element);
        }

        /// <summary>
        ///   Scroll to the element, safer then with Selenium Action
        /// </summary>
        /// <param name="element"></param>
        public static void ScrollTo(this IWebElement element)
        {
            JavaScriptExecutor(element).ExecuteScript("arguments[0].scrollIntoView(true);", element);
        }

        public static void SetAttribute(this IWebElement element, string name, string value)
        {
            JavaScriptExecutor(element).ExecuteScript("arguments[0].setAttribute(arguments[1], arguments[2]);", element, name,
                value);
        }

        public static void RemoveAttribute(this IWebElement element, string name)
        {
            JavaScriptExecutor(element).ExecuteScript("arguments[0].removeAttribute(arguments[1]);", element, name);
        }

        /// <summary>
        ///   Get the property of the computed style of the element
        /// </summary>
        /// <param name="element">Self</param>
        /// <param name="property">The property of the pseudo element e.g. 'background-color'</param>
        /// <returns></returns>
        public static string GetComputedStyle(this IWebElement element, string property)
        {
            var result = JavaScriptExecutor(element).ExecuteScript(
                $"return window.getComputedStyle(arguments[0], '::before').getPropertyValue('{property}');", element);
            return result.ToString();
        }

        /// <summary>
        ///     Get the element inside this parent matching the current criteria
        /// </summary>
        /// <param name="component">Selenium IWebElement to extend with</param>
        /// <param name="by">Selenium By selector</param>
        /// <param name="explicitWait">Timeout in seconds</param>
        /// <returns>First matching IWebElement or Null</returns>
        public static IWebElement FindElementFirstOrDefault(this IWebElement element, By by, int explicitWait)
        {
            var wait = new DefaultWait<ISearchContext>(element)
            {
                Timeout = TimeSpan.FromSeconds(explicitWait)
            };

            IWebElement result;

            try
            {
                result = wait.Until(
                    d =>
                    {
                        try
                        {
                            return element.FindElement(by);
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
                result = null;
            }

            return result;
        }

        /// <summary>
        ///     Get the element inside this parent matching the current criteria
        /// </summary>
        /// <param name="component">Selenium IWebElement to extend with</param>
        /// <param name="by">Selenium By selector</param>
        /// <returns>First matching IWebElement or Null</returns>
        public static IWebElement FindElementFirstOrDefault(this IWebElement element, By by)
        {
            return FindElementFirstOrDefault(element, by, 2);
        }

        /// <summary>
        ///     Get the elements inside this parent matching the current criteria
        /// </summary>
        /// <param name="component">Selenium IWebElement to extend with</param>
        /// <param name="by">Selenium By selector</param>
        /// <param name="explicitWait">Timeout in seconds</param>
        /// <returns>First matching IWebElement or Null</returns>
        public static ReadOnlyCollection<IWebElement> FindElementsOrDefault(this IWebElement element, By by, int explicitWait)
        {
            IWebDriver driver = WebDriver(element);
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(explicitWait));
            ReadOnlyCollection<IWebElement> result = element.FindElements(by);

            try
            {
                result = wait.Until(
                    d =>
                    {
                        try
                        {
                            ReadOnlyCollection<IWebElement> tResult = element.FindElements(by);
                            if (tResult.Count == 0)
                            {
                                return null;
                            }
                            else
                            {
                                return tResult;
                            }
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
                result = null;
            }

            return result;
        }

        /// <summary>
        ///     Get the elements inside this parent matching the current criteria
        /// </summary>
        /// <param name="component">Selenium IWebElement to extend with</param>
        /// <param name="by">Selenium By selector</param>
        /// <returns>First matching IWebElement or Null</returns>
        public static ReadOnlyCollection<IWebElement> FindElementsOrDefault(this IWebElement element, By by)
        {
            return FindElementsOrDefault(element, by, 5);
        }


        /// <summary>
        ///     Find the element using the data-test-id within the parent element.
        ///     Return NULL if not found.
        /// </summary>
        /// <param name="parent">The parent IWebelement</param>
        /// <param name="dataTestId">data-test-id of the element searched</param>
        /// <param name="timeout">Timeout in seconds</param>
        /// <returns>IWebElement or NULL</returns>
        public static IWebElement FindElementByDataTestId(this IWebElement parent, string dataTestId,
            int timeout)
        {
            return parent.FindElementFirstOrDefault(By.CssSelector($"[data-test-id=\"{dataTestId}\"]"), timeout);
        }

        public static IWebElement FindElementByDataTestId(this IWebElement parent, string dataTestId)
        {
            return FindElementByDataTestId(parent, dataTestId, 5);
        }
    }
}
