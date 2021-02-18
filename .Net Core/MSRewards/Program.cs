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
            await program.RunAsync(useFirefox);
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
            result.WithNotParsed(HandleParseError);
        }

        private async Task<T> DownloadJsonDataAsync<T>(string url) where T : new()
        {
            using (var w = new WebClient())
            {
                var json_data = string.Empty;
                try
                {
                    json_data = await w.DownloadStringTaskAsync(url);
                }
                catch (Exception) { }

                return !string.IsNullOrEmpty(json_data) ? JsonConvert.DeserializeObject<T>(json_data) : new T();
            }
        }

        private async Task LoginAsync(IWebDriver driverlocal, WebDriverWait localwait)
        {
            //page 1
            driverlocal.Navigate().GoToUrl(Constants.LiveLoginUrl);

            var username = localwait.Until(d => d.FindElement(By.Name(Constants.LoginEntryName)));
            username.SendKeys(email);
            username.SendKeys(Keys.Enter);

            await Task.Delay(2000);

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
            finally
            {
                await Task.Delay(3000);

                if (FindElementSafely(localwait, d => d.Title.Equals(Constants.RewardsPageTitle)))
                {
                    driverlocal.SwitchTo().DefaultContent();
                }
            }
        }

        private bool FindElementSafely(WebDriverWait localwait, Func<IWebDriver, bool> action)
        {
            var result = false;

            try
            {
                result = localwait.Until(action);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            return result;
        }

        private async Task RunAsync(bool useFirefox = false)
        {
            RemoteWebDriver driver;

            if (useFirefox)
            {
                var firefoxOptions = new FirefoxOptions();
                firefoxOptions.SetPreference(Constants.PrivateBrowsingKey, true);

                driver = new FirefoxDriver(firefoxOptions);
            }
            else
            {
                DriverOptions driverOptions = new EdgeOptions { UseChromium = true };
                driver = new EdgeDriver(driverOptions as EdgeOptions);
            }
            Dictionary<RewardType, PointStatus> result;
            using (driver)
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                wordList = await DownloadJsonDataAsync<List<string>>(Constants.WordsListUrl);

                await LoginAsync(driver, wait);
                result = CheckBreakDown(driver, wait);

                // foreach (var keyvalue in result)
                // {
                //     var current = keyvalue.Value.Current;
                //     var expected = keyvalue.Value.Maximum;
                //     Console.WriteLine($"{keyvalue.Key} : {current} of {expected} completed");
                //     if (current < expected)
                //     {
                //         Console.WriteLine("Starting Bing Search for " + keyvalue.Key);
                //         await BingSearchAsync(keyvalue.Key, current, expected, useFirefox);
                //     }
                // }
                driver?.Quit();
            }
            foreach (var keyvalue in result)
            {
                var current = keyvalue.Value.Current;
                var expected = keyvalue.Value.Maximum;
                Console.WriteLine($"{keyvalue.Key} : {current} of {expected} completed");
                if (current < expected)
                {
                    Console.WriteLine("Starting Bing Search for " + keyvalue.Key);
                    await BingSearchAsync(keyvalue.Key, current, expected, useFirefox);
                }
            }
        }

        private Dictionary<RewardType, PointStatus> CheckBreakDown(IWebDriver webDriver, WebDriverWait waiter)
        {
            var result = new Dictionary<RewardType, PointStatus>();
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
                        result.Add(key, new PointStatus(current, total));
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

        private async Task BingSearchAsync(RewardType rewardType, int current, int target, bool useFirefox = false)
        {
            try
            {
                var rand = new Random();
                wordList = await DownloadJsonDataAsync<List<string>>(Constants.WordsListUrl);

                //Edge browser
                if (rewardType == RewardType.EdgeBonus || !useFirefox)
                {
                    var options = new EdgeOptions
                    {
                        UseChromium = true,
                    };

                    if (rewardType == RewardType.Mobile)
                        options.AddArgument($"{ Constants.EdgeUserAgentArgument}={Constants.MobileUserAgent}");

                    using (var edgeDriver = new EdgeDriver(options))
                    {
                        //edgeDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(15);
                        var edgeWait = new WebDriverWait(edgeDriver, TimeSpan.FromSeconds(30));

                        await LoginAsync(edgeDriver, edgeWait);

                        Search(edgeDriver, edgeWait, Constants.BingSearchURL + "Give me Edge points");

                        await Task.Delay(2000);

                        try
                        {
                            if (rewardType == RewardType.Mobile)
                            {
                                var hamburg = edgeWait.Until(d => d.FindElement(By.Id(Constants.MHamburger)));
                                hamburg?.Click();

                                var signin = edgeWait.Until(d => d.FindElement(By.Id(Constants.HbS)));
                                signin?.Click();

                                await Task.Delay(1500);
                            }
                            else
                            {
                                try
                                {
                                    var id_p = edgeWait.Until(d => d.FindElement(By.Id(Constants.ID_P)));
                                    if (id_p != null)
                                        id_p?.Click();
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex.Message);
                                    Debug.WriteLine(ex.StackTrace);
                                    var id_a = edgeWait.Until(d => d.FindElement(By.Id(Constants.ID_A)));
                                    if (id_a != null)
                                        id_a?.Click();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            Debug.WriteLine(ex.StackTrace);
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
                                    current = currentBreakDown[rewardType].Current;
                                    Console.WriteLine("{0} points of {1} completed", currentBreakDown[rewardType].Current, currentBreakDown[rewardType].Maximum);
                                }
                            }
                        }
                        edgeDriver?.Quit();
                    }
                }
                //Use Firefox
                else
                {
                    var options = new FirefoxOptions();
                    TimeSpan timeout = TimeSpan.FromSeconds(60);
                    if (rewardType == RewardType.Mobile)
                    {
                        options.SetPreference(Constants.UserAgentKey, Constants.MobileUserAgent);
                        timeout = TimeSpan.FromSeconds(30);
                    }

                    options.SetPreference(Constants.PrivateBrowsingKey, true);
                    using (var firefoxDriver = new FirefoxDriver(options))
                    {
                        WebDriverWait driverWait = new WebDriverWait(firefoxDriver, timeout);
                        await LoginAsync(firefoxDriver, driverWait);

                        firefoxDriver.Navigate().GoToUrl(Constants.BingSearchURL + "Edge Points");

                        await Task.Delay(2000);

                        try
                        {
                            if (rewardType == RewardType.Mobile)
                            {
                                var hamburg = driverWait.Until(d => d.FindElement(By.Id(Constants.MHamburger)));
                                hamburg?.Click();

                                var signin = driverWait.Until(d => d.FindElement(By.Id(Constants.HbS)));
                                signin?.Click();

                                await Task.Delay(3000);
                            }
                            else
                            {
                                try
                                {
                                    var id_p = driverWait.Until(d => d.FindElement(By.Id(Constants.ID_P)));
                                    if (id_p != null)
                                        id_p.Click();
                                }
                                catch (System.Exception ex)
                                {
                                    Debug.WriteLine(ex.Message);
                                    Debug.WriteLine(ex.StackTrace);
                                    var id_a = driverWait.Until(d => d.FindElement(By.Id(Constants.ID_A)));
                                    if (id_a != null)
                                        id_a.Click();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            Debug.WriteLine(ex.StackTrace);
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
                                    current = currentBreakDown[rewardType].Current;
                                    Console.WriteLine($"{currentBreakDown[rewardType].Current} points of {currentBreakDown[rewardType].Maximum} completed");
                                }
                            }
                        }
                        firefoxDriver?.Quit();
                    }
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