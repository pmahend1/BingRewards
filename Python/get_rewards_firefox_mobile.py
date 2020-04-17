import json
import random
import time

import requests
from selenium import webdriver
from selenium.webdriver.common.keys import Keys

def wait_for(sec=2):
    time.sleep(sec)


randomlists_url = "https://www.randomlists.com/data/words.json"
response = requests.get(randomlists_url)
words_list = random.sample(json.loads(response.text)['data'], 60)
print('{0} words selected from {1}'.format(len(words_list), randomlists_url))


profile = webdriver.FirefoxProfile()
profile.set_preference("general.useragent.override","Mozilla/5.0 (Android 6.0.1; Mobile; rv:77.0) Gecko/77.0 Firefox/77.0")
driver = webdriver.Firefox(firefox_profile=profile, executable_path='fullpath-to-geckodriver')

try:
    driver.get("https://login.live.com/")
    wait_for(15)
    elem = driver.find_element_by_name('loginfmt')
    elem.clear()
    elem.send_keys("your-email-id") # add your login email id
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


url_base = 'http://www.bing.com/search?q='

wait_for(5)

for num, word in enumerate(words_list):
    print('{0}. URL : {1}'.format(str(num + 1), url_base + word))
    try:
        driver.get(url_base + word)
        print('\t' + driver.find_element_by_tag_name('h2').text)
    except Exception as e1:
        print(e1)
    wait_for(5)
driver.close()