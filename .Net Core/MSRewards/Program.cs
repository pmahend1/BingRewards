using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace MSRewards
{
    class Program
    {
        static string email, password;
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Incorrect number of arguments"); ;
                Console.WriteLine("Usage\n ./MSRewards.exe \"myemail@somedomain.com\" \"mypassword\"");
                return;
            }
            email = args[0];
            password = args[1];
            try
            {
                new Program().RunProcess();
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


        void Login(IWebDriver driverlocal, WebDriverWait localwait)
        {
            //page 1
            driverlocal.Navigate().GoToUrl(Constants.LiveLoginUrl);
            localwait.Until(d => d.FindElement(By.Name(Constants.LoginEntryName)));
            var username = driverlocal.FindElement(By.Name(Constants.LoginEntryName));
            username.SendKeys(email);
            username.SendKeys(Keys.Enter);

            //page2
            localwait.Until(d => d.FindElement(By.Name(Constants.PasswordEntryName)));
            var passwordEntry = driverlocal.FindElement(By.Name(Constants.PasswordEntryName));
            var checkbox = driverlocal.FindElement(By.Name(Constants.CheckboxName));
            checkbox.Click();

            passwordEntry.SendKeys(password);
            passwordEntry.SendKeys(Keys.Enter);

            localwait.Until(e => e.Title.Equals(Constants.RewardsPageTitle));
            driverlocal.SwitchTo().DefaultContent();
            Task.Delay(3000);
        }

        void RunProcess()
        {
            var options = new FirefoxOptions();
            var json = DownloadJsonData<List<string>>(Constants.WordsListUrl);
            Dictionary<string, (int current, int available)> v = new Dictionary<string, (int current, int available)>();
            using IWebDriver driver = new FirefoxDriver();
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

            Login(driver, wait);
            var result = CheckBreakDown(driver);

            if (result.Count > 0)
            {
                v.Add("pcsearch", result[0]);
                v.Add("mobilesearch", result[1]);
                v.Add("edgesearch", result[2]);

                foreach (var item in v)
                    Console.WriteLine("{0}: {1}/{2}", item.Key, item.Value.current, item.Value.available);

                try
                {
                    var randy = new Random();

                    var item = v["pcsearch"];
                    var (c, a) = item;
                    while (c < a)
                    {

                        for (int i = c; i < a; i += 5)
                        {
                            var index = randy.Next(json.Count);

                            Search(driver, wait, Constants.BingSearchURL + json[index]);
                        }
                        var currentBreakDown = CheckBreakDown(driver);

                        currentBreakDown.ForEach(x => Console.WriteLine("{1}/{2}", x.x, x.y));

                        if (currentBreakDown[0].x < currentBreakDown[0].y)
                            c = currentBreakDown[0].x;
                        else
                            break;

                    }
                    driver.Dispose();

                    var mob = v["mobilesearch"];

                    var (currentMobile, availableMobile) = mob;

                    if (currentMobile < availableMobile)
                    {
                        options.SetPreference(Constants.UserAgentKey, Constants.MobileUserAgent);
                        options.SetPreference(Constants.PrivateBrowingKey, true);
                        using var driverM = new FirefoxDriver(options);
                        WebDriverWait waitM = new WebDriverWait(driverM, TimeSpan.FromSeconds(10));
                        Login(driverM, waitM);

                        driverM.Navigate().GoToUrl(Constants.BingSearchURL + "Mobile Search");
                        driverM.Navigate().GoToUrl(Constants.BingSearchURL + "Get me my rewards");

                        while (currentMobile <= availableMobile)
                        {

                            for (int i = currentMobile; i <= availableMobile; i += 5)
                            {
                                var index = randy.Next(json.Count);

                                Search(driverM, waitM, Constants.BingSearchURL + json[index]);
                            }
                            var currentBreakDown = CheckBreakDown(driverM);
                            currentBreakDown.ForEach(x => Console.WriteLine("{1}/{2}", x.x, x.y));
                            if (currentBreakDown[1].x < currentBreakDown[1].y)
                                currentMobile = currentBreakDown[1].x;
                            else
                                break;

                        }
                        driverM.Dispose();
                    }

                    var (edgeCur, edgeTot) = v["edgesearch"];
                    if (edgeCur < edgeTot)
                    {
                        using var edgeDriver = new EdgeDriver();
                        var edgeWait = new WebDriverWait(edgeDriver, TimeSpan.FromSeconds(10));

                        Login(edgeDriver, edgeWait);

                        while (edgeCur < edgeTot)
                        {
                            for (int i = edgeCur; i <= edgeTot; i += 5)
                            {
                                var index = randy.Next(json.Count);
                                Search(edgeDriver, edgeWait, Constants.BingSearchURL + json[index]);
                            }
                            var currentBreakDown = CheckBreakDown(edgeDriver);
                            currentBreakDown.ForEach(x => Console.WriteLine("{1}/{2}", x.x, x.y));
                            if (currentBreakDown[2].x < currentBreakDown[2].y)
                                edgeCur = currentBreakDown[2].x;
                            else
                                break;
                        }
                        edgeDriver.Dispose();
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                }
            }

        }
        private List<(int x, int y)> CheckBreakDown(IWebDriver webDriver)
        {
            var result = new List<(int x, int y)>();
            webDriver.Navigate().GoToUrl(new Uri(Constants.PointsBreakdownUrl));
            webDriver.SwitchTo().DefaultContent();
            webDriver.SwitchTo().ActiveElement();
            var userPointsBreakdown = webDriver.FindElement(By.Id("userPointsBreakdown"));

            var pointDetailsList = userPointsBreakdown.FindElements(By.XPath(".//p[@class='pointsDetail c-subheading-3 ng-binding']"));
            foreach (var pointDetail in pointDetailsList)
            {
                try
                {
                    var pointSplits = pointDetail.Text.Replace(" ", "").Split("/");
                    int.TryParse(pointSplits[0].Trim(), out var current);
                    int.TryParse(pointSplits[1].Trim(), out var total);

                    result.Add((x: current, y: total));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + " \n" + ex.InnerException?.Message);
                }

            }
            return result;
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
