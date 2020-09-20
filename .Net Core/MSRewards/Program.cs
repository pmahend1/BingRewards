using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MSRewards
{
    internal class Program
    {
        private static string email, password;
        private List<string> wordList = new List<string>();

        private static async Task Main(string[] args)
        {
            if (args.Length < 2 || args.Length >3)
            {
                Console.WriteLine("Incorrect number of arguments"); ;
                Console.WriteLine("Usage\n ./MSRewards.exe \"myemail@somedomain.com\" \"mypassword\" Y/N(Use Firefox --optional)");
                return;
            }
            email = args[0];
            password = args[1];
            bool useFirefox = true;
            if (args.Length == 3)
            {
                useFirefox = args[2] != "N";
            }
            try
            {
                var program = new Program();
                await program.Run(useFirefox);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
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
            var checkbox = driverlocal.FindElement(By.Name(Constants.RememberMeCheckboxName));
            passwordEntry.SendKeys(password);

            checkbox?.Click();

            passwordEntry.SendKeys(Keys.Enter);
            await Task.Delay(3000);

            if (localwait.Until(e => e.Title.Equals(Constants.RewardsPageTitle)))
                driverlocal.SwitchTo().DefaultContent();
        }

        private async Task Run(bool useFirefox = true)
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
            driver.Close();
            foreach (var keyvalue in result)
            {
                var current = keyvalue.Value.x;
                var expected = keyvalue.Value.y;
                Console.WriteLine($"{keyvalue.Key} : {current} of {expected} completed");
                if (current < expected)
                {
                    Console.WriteLine("Starting Bing Search for " + keyvalue.Key);
                    await BingSearch(keyvalue.Key, current, expected, useFirefox);
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

            var titleDetailsList = waiter.Until(d => userPointsBreakdown.FindElements(By.XPath(".//div[@class='title-detail']")));

            foreach (var pointDetail in titleDetailsList)
            {
                var href = waiter.Until(p => pointDetail.FindElement(By.TagName("a")));

                var pointDetailsList = waiter.Until(d => pointDetail.FindElements(By.XPath(".//p[@class='pointsDetail c-subheading-3 ng-binding']")));
                try
                {
                    var pointSplits = pointDetailsList.FirstOrDefault()?.Text?.Replace(" ", "").Split("/");
                    if (pointSplits != null)
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
                    Console.WriteLine(ex.Message + " \n" + ex.InnerException?.Message);
                }
            }
            return result;
        }

        private async Task BingSearch(RewardType rewardType, int current, int target, bool useFirefox = true)
        {
            var rand = new Random();
            wordList = DownloadJsonData<List<string>>(Constants.WordsListUrl);

            if (rewardType == RewardType.EdgeBonus || !useFirefox)
            {
                using StreamReader r = new StreamReader("Resources.json");

                string jsonString = r.ReadToEnd();
                var jsonObject = JObject.Parse(jsonString);

                r.Close();

                if (jsonObject != null)
                {
                    var edgeBrowser = JsonConvert.DeserializeObject<EdgeBrowser>(jsonObject["Edge"].ToString());

                    try
                    {
                        var options = new EdgeOptions
                        {
                            UseChromium = true,
                            BinaryLocation = edgeBrowser.ExecutableName,
                        };
                        var edgeDriver = new EdgeDriver(options);

                        edgeDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(15);
                        var edgeWait = new WebDriverWait(edgeDriver, TimeSpan.FromSeconds(60));

                        await Login(edgeDriver, edgeWait);

                        Search(edgeDriver, edgeWait, Constants.BingSearchURL + "Give me Edge points");
                        edgeDriver.FindElement(By.Id("id_p"))?.Click();

                        while (current < target)
                        {
                            Search(edgeDriver, edgeWait, Constants.BingSearchURL + wordList[rand.Next(wordList.Count)]);
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
                        edgeDriver.Close();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
            }
            //Use Firefox
            else
            {
                var options = new FirefoxOptions();
                if (rewardType == RewardType.Mobile)
                    options.SetPreference(Constants.UserAgentKey, Constants.EdgeUserAgent);

                options.SetPreference(Constants.PrivateBrowsingKey, true);
                using var ffDriverEdge = new FirefoxDriver(options);
                WebDriverWait waitFFEdge = new WebDriverWait(ffDriverEdge, TimeSpan.FromSeconds(120));
                await Login(ffDriverEdge, waitFFEdge);

                ffDriverEdge.Navigate().GoToUrl(Constants.BingSearchURL + "Give me edge points");

                var id_p = ffDriverEdge.FindElement(By.Id("id_p"));
                if (id_p != null)
                {
                    id_p.Click();
                }

                while (current < target)
                {
                    Search(ffDriverEdge, waitFFEdge, Constants.BingSearchURL + wordList[rand.Next(wordList.Count)]);
                    current += 5;
                    if (current >= target)
                    {
                        var currentBreakDown = CheckBreakDown(ffDriverEdge, waitFFEdge);

                        if (currentBreakDown.ContainsKey(rewardType))
                        {
                            current = currentBreakDown[rewardType].x;
                            Console.WriteLine("{0} points of {1} completed", currentBreakDown[rewardType].x, currentBreakDown[rewardType].y);
                        }
                    }
                }
                ffDriverEdge.Close();
            }
        }

        private void Search(IWebDriver driver, WebDriverWait wait, string url)
        {
            try
            {
                driver.Navigate().GoToUrl(url);
                wait.Until(e => e.FindElement(By.Id("b_results")));

                var result = driver.FindElement(By.TagName("h2"));
                Console.WriteLine(result.Text);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
            }
        }
    }
}