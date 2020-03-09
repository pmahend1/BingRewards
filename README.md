# BingRewards

Code automate bing search to get daily search rewards.

1. Python
1. .NetCore(C#)

## Python

Check out Medium blog [Earn 500+ daily Microsoft rewards points automatically with a simple Python program](https://medium.com/@prateekrm/earn-500-daily-microsoft-rewards-points-automatically-with-a-simple-python-program-38fe648ff2a9) or [GitHub blog](https://pmahend1.github.io/BingRewards/)

### Prerequisites

- Python 3+ 
- requests

    ```shell
    pip install requests
    ```

- selenium

    ```shell
    pip install selenium
    ```

- Turn off 2 step verification on Microsoft account.

### Execution

- Clone repository
  
    ```shell
    git clone https://github.com/pmahend1/BingRewards
    ```

- Replace `your-email-id` in the code with your enail id.
  
- Replace `your_password` in the code with your password.

- Run

    ```shell
    python get_rewards_firefox_desktop.py
    ```

## .Net Core(C#)

This will run for both PC Search and Mobile points until all the points are gained.

- Build the project via Visual Studio 2019 

- Edit **Resources.json** file to point to respective locations for Edgium. Be sure to have right version of Edgium driver downloaded and pointed based on your existing Edgium version. You can download [Edgium driver from here](https://developer.microsoft.com/en-us/microsoft-edge/tools/webdriver/)

- Run program 

    ```powershell
    ./MSRewards.exe "myemail@somedomain.com" "mypassword"
    ```

## Known Issues

- Since release of Edgium(New Microsoft Edge based on Chromium) and subsequent releases of web drivers there may be an issue with Selenium driver run sometimes. Please [raise an issue](https://github.com/pmahend1/BingRewards/issues) to report them.

## Binaries

- You can download latest binaries from [Releases](https://github.com/pmahend1/BingRewards/releases) and run on Windows. If you build on Linux it should have similar files.
