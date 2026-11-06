
using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using CloudQA.SelfHealing.Config;
using CloudQA.SelfHealing.Engine;

class Program
{
    static void Main(string[] args)
    {
        string url = args.Length > 0
            ? args[0]
            : AppConfig.DefaultUrl;

        IWebDriver? driver = null;

        bool namePassed = false;
        bool genderPassed = false;
        bool statePassed = false;

        try
        {
            Console.WriteLine("================================");
            Console.WriteLine(" CLOUDQA SELF-HEALING AUTOMATION");
            Console.WriteLine("================================");

            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--start-maximized");

            driver = new ChromeDriver(options);

            driver.Navigate().GoToUrl(url);

            WebDriverWait wait = new WebDriverWait(
                driver,
                TimeSpan.FromSeconds(20)
            );

            wait.Until(d =>
                ((IJavaScriptExecutor)d)
                    .ExecuteScript("return document.readyState")
                    ?.ToString() == "complete"
            );

            Console.WriteLine(
                "\nWebsite opened successfully!"
            );

            SmartElementFinder finder =
                new SmartElementFinder(driver);
   


            // TEST 1: FIRST NAME
            Console.WriteLine("\n[TEST 1] First Name");

            try
            {
                IWebElement firstName = wait.Until(d =>
                {
                    try
                    {
                        var element = finder.FindTextField(
                            "First Name"
                        );

                        return element.Displayed && element.Enabled
                            ? element
                            : null;
                    }
                    catch (NoSuchElementException)
                    {
                        return null;
                    }
                });

                firstName.Clear();
                firstName.SendKeys(AppConfig.FirstName);

                namePassed =
                    firstName.GetAttribute("value")
                    == AppConfig.FirstName;

                Console.WriteLine(
                    namePassed
                        ? "PASS: First Name entered"
                        : "FAIL: First Name not entered"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "FAIL: First Name - " + ex.Message
                );
            }

            // TEST 2: GENDER
            Console.WriteLine("\n[TEST 2] Gender");

            try
            {
                IWebElement gender = finder.FindRadioOption(
                    "Gender",
                    AppConfig.Gender
                );

                gender.Click();

                genderPassed = gender.Selected;

                Console.WriteLine(
                    genderPassed
                        ? "PASS: Gender selected"
                        : "FAIL: Gender not selected"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "FAIL: Gender - " + ex.Message
                );
            }

            // TEST 3: STATE
            Console.WriteLine("\n[TEST 3] State");

            try
            {
                IWebElement state = finder.FindDropdown(
                    "State"
                );

                SelectElement dropdown =
                    new SelectElement(state);

                dropdown.SelectByText(
                    AppConfig.StateOption
                );

                statePassed = dropdown.SelectedOption.Text
                    .Trim()
                    .Equals(
                        AppConfig.StateOption,
                        StringComparison.OrdinalIgnoreCase
                    );

                Console.WriteLine(
                    statePassed
                        ? "PASS: State selected"
                        : "FAIL: State not selected"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "FAIL: State - " + ex.Message
                );
            }

            // FINAL TEST SUMMARY
            int passedTests = 0;

            if (namePassed) passedTests++;
            if (genderPassed) passedTests++;
            if (statePassed) passedTests++;

            Console.WriteLine(
                "\n================================"
            );

            Console.WriteLine("         TEST SUMMARY");

            Console.WriteLine(
                "================================"
            );

            Console.WriteLine(
                $"First Name: {(namePassed ? "PASS" : "FAIL")}"
            );

            Console.WriteLine(
                $"Gender:     {(genderPassed ? "PASS" : "FAIL")}"
            );

            Console.WriteLine(
                $"State:      {(statePassed ? "PASS" : "FAIL")}"
            );

            Console.WriteLine(
                "--------------------------------"
            );

            Console.WriteLine(
                $"RESULT: {passedTests}/3 tests passed"
            );

            Console.WriteLine(
                "--------------------------------"
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "\nAUTOMATION FAILED"
            );

            Console.WriteLine(ex.ToString());
        }
        finally
        {
            if (driver != null)
            {
                Console.WriteLine(
                    "\nClosing browser..."
                );

                driver.Quit();
                driver.Dispose();
            }
        }
    }
}
