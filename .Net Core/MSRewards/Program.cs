using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace MSRewards
{
    internal class Program
    {
        private static string email, password;
        private List<string> wordList = new List<string>();

        private static void Main(string[] args)
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

        private void Login(IWebDriver driverlocal, WebDriverWait localwait)
        {
            //page 1
            driverlocal.Navigate().GoToUrl(Constants.LiveLoginUrl);
            localwait?.Until(d => d.FindElement(By.Name(Constants.LoginEntryName)));
            var username = driverlocal.FindElement(By.Name(Constants.LoginEntryName));
            username.SendKeys(email);
            username.SendKeys(Keys.Enter);
            //page2
            localwait?.Until(d => d.FindElement(By.Name(Constants.PasswordEntryName)));

           // await Task.Delay(3000);

            var passwordEntry = driverlocal.FindElement(By.Name(Constants.PasswordEntryName));
            passwordEntry.SendKeys(password);

            var checkbox = driverlocal.FindElement(By.Name(Constants.CheckboxName));
            checkbox.Click();
            passwordEntry.SendKeys(Keys.Enter);

            //await Task.Delay(3000);

            localwait?.Until(e => e.Title.Equals(Constants.RewardsPageTitle));
            driverlocal.SwitchTo().DefaultContent();

            //await Task.Delay(3000);
        }

        private void RunProcess()
        {
            var options = new FirefoxOptions();
            wordList = DownloadJsonData<List<string>>(Constants.WordsListUrl);
            Dictionary<string, (int current, int available)> pointsTuple = new Dictionary<string, (int current, int available)>();
            using IWebDriver driver = new FirefoxDriver();
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(120));

            Login(driver, wait);
            var result = CheckBreakDown(driver, wait);

            if (result.Count > 0)
            {
                pointsTuple.Add("pcsearch", result[0]);
                pointsTuple.Add("mobilesearch", result[1]);
                pointsTuple.Add("edgesearch", result[2]);

                foreach (var item in pointsTuple)
                    Console.WriteLine("{0}: {1}/{2}", item.Key, item.Value.current, item.Value.available);

                try
                {
                    var randy = new Random();

                    var item = pointsTuple["pcsearch"];
                    var (currentPC, availablePC) = item;
                    if (currentPC < availablePC)
                    {
                        Console.WriteLine("Running PC Search");
                        driver.Navigate().GoToUrl(Constants.BingSearchURL + "PC Search");

                        driver.FindElement(By.Id("id_n")).Click();
                        while (currentPC < availablePC)
                        {
                            var index = randy.Next(wordList.Count);
                            Search(driver, wait, Constants.BingSearchURL + wordList[index]);
                            currentPC += 5;
                            if (currentPC >= availablePC)
                            {
                                var currentBreakDown = CheckBreakDown(driver, wait);
                                currentPC = currentBreakDown[0].x;
                                Console.WriteLine("{0} points of {1} completed", currentBreakDown[0].x, currentBreakDown[0].y);
                            }
                        }
                    }

                    driver.Close();
                    driver?.Dispose();
                    var mob = pointsTuple["mobilesearch"];

                    var (currentMobile, availableMobile) = mob;

                    if (currentMobile < availableMobile)
                    {
                        Console.WriteLine("Running Mobile Search");
                        options.SetPreference(Constants.UserAgentKey, Constants.MobileUserAgent);
                        options.SetPreference(Constants.PrivateBrowingKey, true);
                        using var driverM = new FirefoxDriver(options);
                        WebDriverWait waitM = new WebDriverWait(driverM, TimeSpan.FromSeconds(120));
                        Login(driverM, waitM);

                        driverM.Navigate().GoToUrl(Constants.BingSearchURL + "Mobile Search");

                        while (currentMobile < availableMobile)
                        {
                            var index = randy.Next(wordList.Count);
                            Search(driverM, waitM, Constants.BingSearchURL + wordList[index]);
                            currentMobile += 5;

                            if (currentMobile >= availableMobile)
                            {
                                var currentBreakDown = CheckBreakDown(driverM, waitM);
                                currentMobile = currentBreakDown[1].x;
                                Console.WriteLine("{0} points of {1} completed", currentBreakDown[1].x, currentBreakDown[1].y);
                            }
                        }
                        driverM.Close();
                        driverM?.Dispose();
                    }

                    var (edgeCur, edgeTot) = pointsTuple["edgesearch"];
                    if (edgeCur < edgeTot)
                    {
                        Console.WriteLine("Running Edge Search");

                        EdgeSearch(edgeCur, edgeTot);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                }
            }
        }

        private List<(int x, int y)> CheckBreakDown(IWebDriver webDriver, WebDriverWait waiter)
        {
            var result = new List<(int x, int y)>();
            webDriver.Navigate().GoToUrl(new Uri(Constants.PointsBreakdownUrl));
            webDriver.SwitchTo().DefaultContent();
            webDriver.SwitchTo().ActiveElement();

            waiter.Until(d => d.FindElement(By.Id(Constants.UserPointsBreakdownId)));
            var userPointsBreakdown = webDriver.FindElement(By.Id(Constants.UserPointsBreakdownId));

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

        private void EdgeSearch(int current, int target, bool useFirefox = true)
        {
            var rand = new Random();

            if (useFirefox)
            {
                var options = new FirefoxOptions();
                options.SetPreference(Constants.UserAgentKey, Constants.EdgeUserAgent);
                options.SetPreference(Constants.PrivateBrowingKey, true);
                using var ffDriverEdge = new FirefoxDriver(options);
                WebDriverWait waitFFEdge = new WebDriverWait(ffDriverEdge, TimeSpan.FromSeconds(120));
                Login(ffDriverEdge, waitFFEdge);

                ffDriverEdge.Navigate().GoToUrl(Constants.BingSearchURL + "Give me edge points");

                var id_p = ffDriverEdge.FindElement(By.Id("id_p"));
                if(id_p != null)
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
                        current = currentBreakDown[2].x;
                        Console.WriteLine("{0} points of {1} completed", currentBreakDown[2].x, currentBreakDown[2].y);
                    }
                }
                ffDriverEdge.Close();
            }
            else
            {
                using StreamReader r = new StreamReader("Resources.json");

                string jsonString = r.ReadToEnd();
                var jsonObject = JObject.Parse(jsonString);

                r.Close();

                if (jsonObject != null)
                {
                    var edgeBrowser = JsonConvert.DeserializeObject<EdgeBrowser>(jsonObject["Edge"].ToString());

                    var service = EdgeDriverService.CreateDefaultService(edgeBrowser.DriverLocation, edgeBrowser.DriverExecutableName);
                    service.UseSpecCompliantProtocol = true;

                    service.Start();

                    var caps = new DesiredCapabilities(new Dictionary<string, object>()
                    {
                        { "ms:edgeOptions", new Dictionary<string, object>() {
                            {  "binary", edgeBrowser.ExecutableName }
                        }}
                    });

                    var edgeDriver = new RemoteWebDriver(service.ServiceUrl, caps);

                    var edgeWait = new WebDriverWait(edgeDriver, TimeSpan.FromSeconds(15));
                    //await Task.Delay(3000);
                    Login(edgeDriver, edgeWait);

                    Search(edgeDriver, edgeWait, Constants.BingSearchURL + "Give me Edge points");
                    edgeDriver.FindElement(By.Id("id_p"))?.Click();

                    while (current < target)
                    {
                        Search(edgeDriver, edgeWait, Constants.BingSearchURL + jsonString[rand.Next(wordList.Count)]);
                        current += 5;
                        if (current >= target)
                        {
                            var currentBreakDown = CheckBreakDown(edgeDriver, edgeWait);
                            current = currentBreakDown[2].x;
                            Console.WriteLine("{0} points of {1} completed", currentBreakDown[2].x, currentBreakDown[2].y);
                        }
                    }
                    edgeDriver.Close();
                    service.Dispose();
                }
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