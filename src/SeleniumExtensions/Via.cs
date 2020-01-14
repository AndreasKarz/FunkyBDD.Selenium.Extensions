using OpenQA.Selenium;

namespace FunkyBDD.Selenium.Extensions
{
    public static class Via
    {
        /// <summary>
        ///     Selector to find the element by the Swiss Life data-test-id
        /// </summary>
        /// <param name="dataTestId">data-test-id</param>
        /// <returns>Selenium By selector</returns>
        public static By DataTestId(string dataTestId)
        {
            return By.CssSelector($"[data-test-id='{dataTestId}']");
        }

        /// <summary>
        ///     Selector to find the element where the Swiss Life data-test-id starts with search string
        /// </summary>
        /// <param name="startOfDataTestId">data-test-id</param>
        /// <returns>Selenium By selector</returns>
        public static By DataTestIdStartWith(string startOfDataTestId)
        {
            return By.CssSelector($"[data-test-id^='{startOfDataTestId}']");
        }

        /// <summary>
        ///     Selector to find the element by containing a specific class
        /// </summary>
        /// <param name="className">Class name</param>
        /// <returns>Selenium By selector</returns>
        public static By ClassContains(string className)
        {
            return By.CssSelector($"[class*='{className}']");
        }

        /// <summary>
        ///     Selector to find the element start with a specific class
        /// </summary>
        /// <param name="className">Class name</param>
        /// <returns>Selenium By selector</returns>
        public static By ClassStartWith(string className)
        {
            return By.CssSelector($"[class^='{className}']");
        }
    }
}
