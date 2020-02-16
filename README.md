# BingRewards

Python code automate bing search to get daily rewards

1. Python
1. .NetCore(C#)

## Python 

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

- Run program 

    ```powershell
    ./MSRewards.exe "myemail@somedomain.com" "mypassword"
    ```

## Known Issues

- Since release of Edgium(New Microsoft Edge based on Chromium) and subsequent releases of web drivers there is an issue with Selenium driver run.