using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;

namespace MSRewards
{
    public static class Extensions
    {
        public static bool IsClickable(this IWebElement webElement)
        {
            if (webElement != null)
                return webElement.Displayed && webElement.Enabled;
            else
                return false;
        }

        public static IWebElement FindClickableElement(this IWebDriver driver, By condition, int fromSeconds = 10)
        {
            bool isElementPresent = false;
            IWebElement singleElement = null;

            var driverWait = new WebDriverWait(driver, TimeSpan.FromSeconds(fromSeconds));
            driverWait.IgnoreExceptionTypes(typeof(NoSuchElementException));

            try
            {
                isElementPresent = driverWait.Until(d => d.FindElement(condition).IsClickable());

                if (isElementPresent)
                {
                    singleElement = driver.FindElement(condition);
                }
            }
            catch
            {
                // log any errors
            }

            return singleElement;
        }

        public static bool WaitUntilClickable(this IWebDriver driver, By condition, int fromSeconds = 10)
        {
            var element = driver.FindClickableElement(condition, fromSeconds);
            return (element != null);
        }


        public static void WaitUntilElementFound(this IWebDriver driver, By condition, int fromSeconds = 10)
        {
            WebDriverWait waiter = new WebDriverWait(driver, TimeSpan.FromSeconds(fromSeconds));
            waiter.IgnoreExceptionTypes(typeof(NoSuchElementException));
            waiter.Until(d => d.FindElement(condition) != null); 
        }
    }
}