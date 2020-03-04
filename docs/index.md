# Earn 500+ daily Microsoft rewards points automatically with a simple Python program


<img src="https://miro.medium.com/max/1302/1*uq3a-Jc3RNMCGfxBo-sKLA.png" width="1024" height="768" />

## Microsoft rewards

[Microsoft rewards](https://account.microsoft.com/rewards) users for using their Bing search engine with points. You can redeem these points as various gift cards, sweepstakes and donations to organizations. One can earn a lot of points just by using Edge browser and using Bing as search engine. I do not use Bing as my default search engine , instead of myself forcing to use it I thought of writing and scheduling program that runs everyday to give me maximum points without any effort.

For this I thought what better programming language than Python ! I have some skills with `requests` , `Beautifulsoup`. I tried but one has to login to get rewards so it was tough to automate login with these two. Then with some research I found that Selenium python is package good fit for this.

**Packages/Resources we need:**
-------------------------------

**`random`** - randomizing selection of search words

**`json`** - load JSON strings

**`time`** - adding delays

**`requests`** - querying URL

**`selenium`** - for automating browser

**Edge web driver** - because additional 20 points for using edge

**Firefox web driver** - optional , you can do with Edge too.

Download Edge web driver from [https://developer.microsoft.com/en-us/microsoft-edge/tools/webdriver/](https://developer.microsoft.com/en-us/microsoft-edge/tools/webdriver/) save it in convenient location. You need to chose correct version of driver for your machine, my Windows machine is on [**Release 17134**](https://download.microsoft.com/download/F/8/A/F8AF50AB-3C3A-4BC4-8773-DC27B32988DD/MicrosoftWebDriver.exe) at the time of writing this blog.

We may also want Firefox web driver Geckodriver , I am using that to emulate mobile browsing and earn 200 points. Link for downloading Mozilla Firefox geckodriver is — [https://github.com/mozilla/geckodriver/releases](https://github.com/mozilla/geckodriver/releases) ; choose the appropriate one for per your computer architecture.

## Programming

So lets jump right into programming.Shall we?

Import all packages

```python
import json
import random
import time

import requests
from selenium import webdriver
from selenium.webdriver.common.keys import Keys
```

Write a delay function which provides delay when URL is being loaded, provide a default delay in parameter.

```python
def wait_for(sec=2):
    time.sleep(sec)
```

Get random list of words from a website or create your own and store in a list.

```python
randomlists_url = "https://www.randomlists.com/data/words.json"
response = requests.get(randomlists_url)
words_list = random.sample(json.loads(response.text)['data'], 60)
print('{0} words selected from {1}'.format(len(words_list), randomlists_url))
```

I found [**randomlists.com**](https://www.randomlists.com) website for some random word list in Json, query URL from requests and convert the response and random sample it for N words. Below I have chosen 60 words.

```python
driver = webdriver.Edge(executable_path='C:\Coding\MicrosoftWebDriver.exe')
wait_for(5)
driver.get("https://login.live.com")
wait_for(5)
```

Next we use selenium to run these drivers from python code, remember to call `wait_for(N)` function to add delays in between ,

```python
driver = webdriver.Edge(executable_path='C:\Coding\MicrosoftWebDriver.exe')
wait_for(5)
driver.get("https://login.live.com")
wait_for(5)
```

Now login with your credentials , but from code. We use `find_element_by_name` function to identify login field and proceed button click. Awesome when you don’t have to press even button with this. Isn’t it?

```python
try:
    elem = driver.find_element_by_name('loginfmt')
    elem.clear()
    elem.send_keys("your_email_id") # add your login email id
    elem.send_keys(Keys.RETURN)
wait_for(5)
elem1 = driver.find_element_by_name('passwd')
    elem1.clear()
    elem1.send_keys("your_password") # add your password
elem1.send_keys(Keys.ENTER)
    wait_for(7)
 
except Exception as e:
    print(e)
    wait_for(4)
```

I have put this block in _try-catch_ block since sometimes I am already logged in to Bing and it wont find the login fields.

Once logged in we straight ahead query the words with Bing search engine. Its very simple is it supports `q=` mode of parameter passing. I am also validating result by printing it on console.

Some queries can be thought as location search queries by engine and it will pop a location request which will break the program execution, so hence there is again try-catch for each query.

```python
url_base = 'http://www.bing.com/search?q='
wait_for(5)
for num, word in enumerate(words_list):
    print('{0}. URL : {1}'.format(str(num + 1), url_base + word))
    try:
        driver.get(url_base + word)
        print('\t' + driver.find_element_by_tag_name('h2').text)
    except Exception as e1:
        print(e1)
    wait_for()
driver.close()
```

The last line closes the web driver, you can comment it ,sometimes due to insufficient delays login wont work or your browsing activity is not counted in. Then you can just manually login to gain those pending points.

## Mobile browsing points :

I also thought it would be nice if we had selenium mobile web drivers, I could not find useful one so I checked if we can change user agent of selenium driver , and of course we can. I just checked what is my user agent from my phone and modified one step in programming and driver is thought to be a mobile. This can be done by `FirefoxProfile` class and setting custom user agent as preference.

```python
profile = webdriver.FirefoxProfile()
profile.set_preference("general.useragent.override","Mozilla/5.0 (Android 9; Mobile; rv:75.0) Gecko/75.0 Firefox/75.0")
driver = webdriver.Firefox(firefox_profile=profile, executable_path='C:\Coding\geckodriver.exe')
```

That’s it, if you run the program with this change you will get mobile browsing points, make sure to change the number of random words chosen to be 40 since its 200 points limit.

We can use one python file to call both Edge and Firefox drivers and be done with 150+200+20 = **520** _points_ at once, but to separate things out I made 2 files

Next step is to schedule these files in running on windows everyday, I don't suppose you wanted it to manually run every day. It took some time to figure out correct parameter option for Task scheduler scheduling option , since by default it takes system user group and was unable to fire python program.

## Scheduling Python program for daily run

Go to Start → Task scheduler → Task Scheduler Library → Right Click → Create a task

<img src="https://miro.medium.com/max/632/1*R0AKKeosFdczg1TSKeaB4A.png" width="632" height="477"/>

General task settings

In Security options change the running user to yourself by searching it as below.

<img src="https://miro.medium.com/max/711/1*y7Q6-M7ALd8m8R1z5BpHww.png" width="711" height="585"/>

Security — User

Add a daily trigger

<img  src="https://miro.medium.com/max/584/1*oHo6_kdOCWce_hyDfUtFHg.png" width="584" height="506"/>

Trigger settings

Add an Action to run python executable and .py file we just wrote.

<img  src="https://miro.medium.com/max/1024/1*B9oIdkMSYRjQ76Qj3OMcag.png" width="512" height="418"/>

Action — python file locations

Modify settings so that task can be run ad-hoc and kills itself if stuck for long , and parallelization is enabled.

<img  src="https://miro.medium.com/max/1264/1*6y6VDtTbPrgDRVFJ5fhTpw.png" width="632" height="480"/>

That’s it, save it and check it runs manually.

## Full Code 

Entire code can be found at [Github/pmahend1/BingRewards](https://github.com/pmahend1/BingRewards)

## Summary

With selenium we can automate fixed browsing activities, since its available on Python code is short and sweet. This is an attempt to make use of basic Python coding skills and also explore Windows task scheduler. Main goal is to have fun and still be lazy! Hope you guys try it out and have fun automating too. Let me know if it works for you.

Thanks for reading.