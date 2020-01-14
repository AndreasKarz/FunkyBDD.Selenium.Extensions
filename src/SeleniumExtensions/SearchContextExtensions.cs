using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace FunkyBDD.Selenium.Extensions
{
    public static class SearchContextExtensions
    {
        public static IWebElement TryFindElement(this ISearchContext context, By by, int explicitWait)
        {
            var wait = new DefaultWait<ISearchContext>(context)
            {
                Timeout = TimeSpan.FromSeconds(explicitWait)
            };

            try
            {
                return wait.Until(
                    d => {
                        try
                        {
                            return context.FindElement(by);
                        }
                        catch (Exception)
                        {
                            return null;
                        }
                    }
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Element not found", ex.InnerException);
            }
        }

        public static IWebElement TryFindElement(this ISearchContext context, By by)
        {
            return TryFindElement(context, by, 5);
        }
    }
}
