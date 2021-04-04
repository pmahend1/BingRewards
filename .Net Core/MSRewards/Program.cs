using Colorify;
using Colorify.UI;
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
        private static Random rand = new Random();
        private static Format colorify { get; set; }

        private static string email, password;
        private List<string> wordList = new List<string>();
        private Dictionary<RewardType, PointStatus> pointsDictionary;

        private static async Task Main(string[] args)
        {
            try
            {
                colorify = new Format(Theme.Dark);
                var result = Parser.Default.ParseArguments<Options>(args);
                await result.WithParsedAsync(async (x) => await RunOptions(x));
                result.WithNotParsed(HandleParseError);
            }
            finally
            {
                colorify.ResetColor();
                colorify?.Clear();
            }
        }

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
                colorify.WriteLine(error.ToString(), Colors.bgDanger);
            }
        }

        private async Task RunAsync(bool useFirefox = false)
        {
            try
            {
                var driver = Initialize(useFirefox);

                using (driver)
                {
                    Login(driver);
                    pointsDictionary = CheckBreakDown(driver);
                    driver?.Quit();
                }

                if (pointsDictionary != null && pointsDictionary.Count > 0)
                {
                    colorify.DivisionLine('-', Colors.txtMuted);
                    foreach (var pointsKV in pointsDictionary)
                    {
                        colorify.Write(pointsKV.Key.ToString(), Colors.txtInfo);
                        colorify.WriteLine($": {pointsKV.Value?.Current} of {pointsKV.Value?.Maximum} completed", Colors.txtPrimary);
                    }
                    colorify.DivisionLine('-', Colors.txtMuted);

                    wordList = await DownloadJsonDataAsync<List<string>>(Constants.WordsListUrl);

                    for (int i = 0; i < pointsDictionary.Count; i++)
                    {
                        var pkv = pointsDictionary.ElementAt(i);
                        var current = pkv.Value.Current;
                        var expected = pkv.Value.Maximum;
                        if (current < expected)
                        {
                            colorify.WriteLine($"Starting Bing search for {pkv.Key}...", Colors.txtInfo);

                            LoginAndBingSearch(pkv.Key, pkv.Value, useFirefox);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                colorify.WriteLine(ex.Message, Colors.txtDanger);
                Debug.WriteLine(ex.StackTrace);
            }
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

        private void Login(IWebDriver driverlocal)
        {
            //page 1
            driverlocal.Navigate().GoToUrl(Constants.LiveLoginUrl);
            var username = driverlocal.FindClickableElement(By.Name(Constants.LoginEntryName));
            username?.SendKeys(email);
            username?.SendKeys(Keys.Enter);

            //page2
            try
            {
                var checkbox = driverlocal.FindClickableElement(By.Name(Constants.RememberMeCheckboxName));
                checkbox?.Click();
            }
            catch (Exception ex)
            {
                colorify.WriteLine(ex.Message, Colors.txtDanger);
                Debug.WriteLine(ex.StackTrace);
            }
            var passwordEntry = driverlocal.FindClickableElement(By.Id(Constants.PasswordEntryId));

            passwordEntry?.SendKeys(password);
            passwordEntry?.SendKeys(Keys.Enter);

            try
            {
                var dontShowThisAgain = driverlocal.FindClickableElement(By.Id(Constants.CheckboxId));
                dontShowThisAgain?.Click();
                var yesButton = driverlocal.FindClickableElement(By.Id(Constants.IdSIButton9));
                yesButton?.Click();
            }
            catch (Exception ex)
            {
                colorify.WriteLine(ex.Message, Colors.txtDanger);
                Debug.WriteLine(ex.StackTrace);
            }
        }
        private void ReLogin(IWebDriver driverlocal, bool isMobile = false)
        {
            driverlocal.Navigate().GoToUrl(Constants.BingHome);

            try
            {
                if (isMobile)
                {
                    do
                    {
                        var hamburg = driverlocal.FindClickableElement(By.Id(Constants.MHamburger));
                        hamburg?.Click();

                        var signin = driverlocal.FindClickableElement(By.Id("HBSignIn"));
                        signin?.Click();

                        hamburg = driverlocal.FindClickableElement(By.Id(Constants.MHamburger));
                        hamburg?.Click();
                    } while (!driverlocal.WaitUntilClickable(By.Id("hb_n")));

                    var hbLeft = driverlocal.FindClickableElement(By.Id("HBleft"));
                    hbLeft?.Click();

                }
                else
                {
                    do
                    {
                        var id_s = driverlocal.FindClickableElement(By.Id("id_s"));
                        id_s?.Click();
                    } while (!driverlocal.WaitUntilClickable(By.Id(Constants.ID_P)));
                }
            }
            catch (Exception ex)
            {
                colorify.WriteLine(ex.Message, Colors.txtDanger);
                Debug.WriteLine(ex.StackTrace);
            }
        }
        private RemoteWebDriver Initialize(bool useFirefox = false, bool isMobile = false)
        {
            RemoteWebDriver driver;

            if (useFirefox)
            {
                var firefoxOptions = new FirefoxOptions();
                firefoxOptions.SetPreference(Constants.PrivateBrowsingKey, true);
                if (isMobile)
                    firefoxOptions.SetPreference(Constants.UserAgentKey, Constants.MobileUserAgent);
                driver = new FirefoxDriver(firefoxOptions);
            }
            else
            {
                EdgeOptions driverOptions = new EdgeOptions { UseChromium = true };

                if (isMobile)
                    driverOptions.AddArgument($"{ Constants.EdgeUserAgentArgument}={Constants.MobileUserAgent}");
                driver = new EdgeDriver(driverOptions);
                driver.Manage().Cookies.DeleteAllCookies();
            }

            driver.Manage().Window.Maximize();

            return driver;
        }


        private Dictionary<RewardType, PointStatus> CheckBreakDown(IWebDriver webDriver)
        {
            WebDriverWait waiter = new WebDriverWait(webDriver, TimeSpan.FromSeconds(10));
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
                    colorify.WriteLine(ex.Message, Colors.txtDanger);
                    Debug.WriteLine(ex.StackTrace);
                }
            }
            return result;
        }

        private void LoginAndBingSearch(RewardType rewardType, PointStatus pointStatus, bool useFirefox = false)
        {
            try
            {
                int current = pointStatus.Current;
                int target = pointStatus.Maximum;
                bool isMobile = rewardType == RewardType.Mobile;
                var viaFirefox = (rewardType != RewardType.EdgeBonus) && useFirefox;
                var webDriver = Initialize(viaFirefox, isMobile);
                using (webDriver)
                {
                    webDriver.Manage().Window.Maximize();
                    Login(webDriver);

                    ReLogin(webDriver, isMobile);
                    while (current < target)
                    {
                        var nextInt = rand.Next(wordList.Count + 1);
                        Search(webDriver, Constants.BingSearchURL + wordList[nextInt]);
                        current += 5;
                        if (current >= target)
                        {
                            pointsDictionary = CheckBreakDown(webDriver);

                            if (pointsDictionary.ContainsKey(rewardType))
                            {
                                current = pointsDictionary[rewardType].Current;
                                colorify.WriteLine($"{rewardType}: {pointsDictionary[rewardType].Current} points of {pointsDictionary[rewardType].Maximum} completed", Colors.txtPrimary);
                            }
                        }
                    }
                    webDriver?.Quit();
                }
            }
            catch (Exception ex)
            {
                colorify.WriteLine(ex.Message, Colors.txtDanger);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private void Search(IWebDriver driver, string url)
        {
            try
            {
                driver.Navigate().GoToUrl(new Uri(url));
                driver.WaitUntilElementFound(By.Id(Constants.B_Results));
            }
            catch (Exception ex)
            {
                colorify.WriteLine(ex.Message, Colors.txtDanger);
                Debug.WriteLine(ex.StackTrace);
            }
        }
    }
}