using CommandLine;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MSRewards
{
    internal class Program
    {
        private static async Task RunOptions(Options opts)
        {
            email = opts.Email;
            password = opts.Password;
            bool useFirefox = opts.Firefox;

            var program = new Program();
            await program.Run(useFirefox);
        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
            foreach (var error in errs)
            {
                Console.WriteLine(error.ToString());
            }
        }

        private static string email, password;
        private List<string> wordList = new List<string>();

        private static async Task Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<Options>(args);
            await result.WithParsedAsync(async (x) => await RunOptions(x));
            result
               .WithNotParsed(HandleParseError);
        }

        private T DownloadJsonData<T>(string url) where T : new()
        {
            using (var w = new WebClient())
            {
                var json_data = string.Empty;
                try
                {
                    json_data = w.DownloadString(url);
                }
                catch (Exception) { }

                return !string.IsNullOrEmpty(json_data) ? JsonConvert.DeserializeObject<T>(json_data) : new T();
            }
        }

        private async Task Login(IWebDriver driverlocal, WebDriverWait localwait)
        {
            //page 1
            driverlocal.Navigate().GoToUrl(Constants.LiveLoginUrl);

            var username = localwait.Until(d => d.FindElement(By.Name(Constants.LoginEntryName)));
            username.SendKeys(email);
            username.SendKeys(Keys.Enter);

            await Task.Delay(3000);

            //page2
            var passwordEntry = localwait?.Until(d => d.FindElement(By.Id(Constants.PasswordEntryId)));
            try
            {
                var checkbox = driverlocal.FindElement(By.Name(Constants.RememberMeCheckboxName));
                checkbox?.Click();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
            passwordEntry.SendKeys(password);

            passwordEntry.SendKeys(Keys.Enter);

            try
            {
                var dontShowThisAgain = localwait.Until(driver => driver.FindElement(By.Id(Constants.CheckboxId)));

                dontShowThisAgain?.Click();

                var yesButton = localwait.Until(d => d.FindElement(By.Id(Constants.IdSIButton9)));

                yesButton?.Click();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                try
                {
                    var yesButton = localwait.Until(d => d.FindElement(By.Id(Constants.IdSIButton9)));
                    yesButton?.Click();
                }
                catch (Exception ex2)
                {

                    Debug.WriteLine(ex2.Message);
                    Debug.WriteLine(ex2.StackTrace);
                }

            }

            await Task.Delay(3000);

            if (localwait.Until(e => e.Title.Equals(Constants.RewardsPageTitle)))
                driverlocal.SwitchTo().DefaultContent();
        }

        private async Task Run(bool useFirefox = false)
        {
            wordList = DownloadJsonData<List<string>>(Constants.WordsListUrl);
            DriverOptions driverOptions;
            RemoteWebDriver driver;

            if (useFirefox)
            {
                var firefoxOptions = new FirefoxOptions();
                firefoxOptions.SetPreference(Constants.PrivateBrowsingKey, true);
                driverOptions = firefoxOptions;

                driver = new FirefoxDriver(firefoxOptions);
            }
            else
            {
                driverOptions = new EdgeOptions { UseChromium = true };
                driver = new EdgeDriver(driverOptions as EdgeOptions);
            }
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
            wordList = DownloadJsonData<List<string>>(Constants.WordsListUrl);

            await Login(driver, wait);
            var result = CheckBreakDown(driver, wait);
            driver?.Dispose();
            driver?.Quit();
            foreach (var keyvalue in result)
            {
                var current = keyvalue.Value.x;
                var expected = keyvalue.Value.y;
                Console.WriteLine($"{keyvalue.Key} : {current} of {expected} completed");
                if (current < expected)
                {
                    Console.WriteLine("Starting Bing Search for " + keyvalue.Key);
                    await BingSearch(keyvalue.Key, current, expected, useFirefox);
                    Environment.Exit(0);
                }
            }
        }

        private Dictionary<RewardType, (int x, int y)> CheckBreakDown(IWebDriver webDriver, WebDriverWait waiter)
        {
            var result = new Dictionary<RewardType, (int x, int y)>();
            webDriver.Navigate().GoToUrl(new Uri(Constants.PointsBreakdownUrl));
            webDriver.SwitchTo().DefaultContent();
            webDriver.SwitchTo().ActiveElement();

            var userPointsBreakdown = waiter.Until(d => d.FindElement(By.Id(Constants.UserPointsBreakdownId)));

            var titleDetailsList = waiter.Until(d => userPointsBreakdown.FindElements(By.XPath(Constants.TitleDetailXPath)));

            foreach (var pointDetail in titleDetailsList)
            {
                var href = waiter.Until(p => pointDetail.FindElement(By.TagName(Constants.A)));

                var pointDetailsList = waiter.Until(d => pointDetail.FindElements(By.XPath(Constants.PointDetailXpath)));
                try
                {
                    var pointSplits = pointDetailsList.FirstOrDefault()?.Text?.Replace(" ", "").Split("/");
                    if (pointSplits != null && pointSplits?.Length >= 2)
                    {
                        int.TryParse(pointSplits[0].Trim(), out var current);
                        int.TryParse(pointSplits[1].Trim(), out var total);
                        var keytext = href.Text.Trim();
                        RewardType key;

                        if (keytext.Contains("PC"))
                            key = RewardType.PC;
                        else if (keytext.Contains("Mobile"))
                            key = RewardType.Mobile;
                        else if (keytext.Contains("Edge bonus"))
                            key = RewardType.EdgeBonus;
                        else
                            key = RewardType.None;
                        result.Add(key, (x: current, y: total));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                }
            }
            return result;
        }

        private async Task BingSearch(RewardType rewardType, int current, int target, bool useFirefox = false)
        {
            try
            {
                var rand = new Random();
                wordList = DownloadJsonData<List<string>>(Constants.WordsListUrl);

                if (rewardType == RewardType.EdgeBonus || !useFirefox)
                {
                    var options = new EdgeOptions
                    {
                        UseChromium = true,
                    };

                    var edgeDriver = new EdgeDriver(options);

                    edgeDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(15);
                    var edgeWait = new WebDriverWait(edgeDriver, TimeSpan.FromSeconds(60));

                    await Login(edgeDriver, edgeWait);

                    Search(edgeDriver, edgeWait, Constants.BingSearchURL + "Give me Edge points");

                    await Task.Delay(4000);

                    var id_p = edgeWait.Until(d => d.FindElement(By.Id(Constants.ID_P)));
                    if (id_p != null)
                    {
                        id_p?.Click();
                    }

                    while (current < target)
                    {
                        var nextInt = rand.Next(wordList.Count);
                        Search(edgeDriver, edgeWait, Constants.BingSearchURL + wordList[nextInt <= wordList.Count ? nextInt : 0]);
                        current += 5;
                        if (current >= target)
                        {
                            var currentBreakDown = CheckBreakDown(edgeDriver, edgeWait);

                            if (currentBreakDown.ContainsKey(rewardType))
                            {
                                current = currentBreakDown[rewardType].x;
                                Console.WriteLine("{0} points of {1} completed", currentBreakDown[rewardType].x, currentBreakDown[rewardType].y);
                            }
                        }
                    }
                    edgeDriver?.Dispose();
                    edgeDriver?.Quit();
                    //}
                }
                //Use Firefox
                else
                {
                    var options = new FirefoxOptions();
                    if (rewardType == RewardType.Mobile)
                        options.SetPreference(Constants.UserAgentKey, Constants.MobileUserAgent);

                    options.SetPreference(Constants.PrivateBrowsingKey, true);
                    using var firefoxDriver = new FirefoxDriver(options);
                    WebDriverWait driverWait = new WebDriverWait(firefoxDriver, TimeSpan.FromSeconds(120));
                    await Login(firefoxDriver, driverWait);

                    firefoxDriver.Navigate().GoToUrl(Constants.BingSearchURL + "Give me edge points");

                    await Task.Delay(4000);

                    var id_p = driverWait.Until(d => d.FindElement(By.Id(Constants.ID_P)));
                    if (id_p != null)
                    {
                        id_p.Click();
                    }

                    while (current < target)
                    {
                        var nextInt = rand.Next(wordList.Count);
                        Search(firefoxDriver, driverWait, Constants.BingSearchURL + wordList[nextInt <= wordList.Count ? nextInt : 0]);
                        current += 5;
                        if (current >= target)
                        {
                            var currentBreakDown = CheckBreakDown(firefoxDriver, driverWait);

                            if (currentBreakDown.ContainsKey(rewardType))
                            {
                                current = currentBreakDown[rewardType].x;
                                Console.WriteLine("{0} points of {1} completed", currentBreakDown[rewardType].x, currentBreakDown[rewardType].y);
                            }
                        }
                    }
                    firefoxDriver?.Dispose();
                    firefoxDriver?.Quit();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private void Search(IWebDriver driver, WebDriverWait wait, string url)
        {
            try
            {
                driver.Navigate().GoToUrl(url);
                wait.Until(e => e.FindElement(By.Id(Constants.B_Results)));

                var result = driver.FindElement(By.TagName(Constants.H2));
                Console.WriteLine(result?.Text);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }
    }
}