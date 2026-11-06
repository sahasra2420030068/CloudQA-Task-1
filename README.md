# CloudQA Task 1 — Self-Healing Test Automation

A C# and Selenium WebDriver project that automates selected fields on the CloudQA Automation Practice Form. The implementation uses weighted attribute matching and form-content checks to reduce dependence on brittle, single-attribute locators.

## Target application

https://app.cloudqa.io/home/AutomationPracticeForm

## What the automation does

The console application runs three browser checks:

1. **First Name** — locates the text field, enters the configured value, and verifies the input value.
2. **Gender** — locates the requested radio option, clicks it, and verifies that it is selected.
3. **State** — locates the State dropdown, selects the configured option, and verifies the selected text.

The target URL can be supplied as the first command-line argument. If omitted, the application uses the default URL in `Config/AppConfig.cs`.

## Technology

- C#
- .NET 10
- Selenium WebDriver 4
- Selenium.Support
- Google Chrome / ChromeDriver

## Project structure

```text
CloudQA.SelfHealing/
├── Config/
│   └── AppConfig.cs
├── Engine/
│   └── SmartElementFinder.cs
├── Models/
├── Tests/
├── Program.cs
└── CloudQA.SelfHealing.csproj
```

## How the self-healing locator works

The `SmartElementFinder` class uses multiple signals to identify elements rather than relying on one fixed CSS selector or XPath:

- HTML `id`
- HTML `name`
- `placeholder`
- `aria-label`
- associated label text

Values are normalized by removing non-alphanumeric characters and converting them to lowercase before comparison. Text fields and dropdowns receive a weighted match score. A minimum score is required; when no sufficiently strong candidate is found, the finder throws `NoSuchElementException` rather than returning an arbitrary element.

For this target page, the finder identifies a visible form by checking for evidence of the First Name field, a Gender radio group, and a State dropdown. It then searches for the controls inside the selected form. When more than one matching form is present, the current implementation chooses the matching form with the highest calculated form score; a tie retains the first matching form encountered.

Radio options are currently matched using the radio group's `name` and the option's `value` or nearby label text.

## Configuration

Edit `Config/AppConfig.cs` to change the default values:

- `DefaultUrl` — target page URL
- `FirstName` — text to enter
- `Gender` — radio option to select
- `StateOption` — dropdown option to select

Example:

```csharp
public static string DefaultUrl =
    "https://app.cloudqa.io/home/AutomationPracticeForm";

public static string FirstName = "Sahasra";
public static string Gender = "Female";
public static string StateOption = "India";
```

A different URL can also be passed at runtime:

```powershell
dotnet run -- "https://app.cloudqa.io/home/AutomationPracticeForm"
```

## Run locally

### Prerequisites

- .NET 10 SDK
- Google Chrome
- Internet connection

### Commands

Run these commands from the directory containing `CloudQA.SelfHealing.csproj`:

```powershell
dotnet restore
dotnet build
dotnet run
```

Selenium must be able to obtain or locate a ChromeDriver compatible with the installed Chrome version.

## Validation performed

The following scenarios were exercised during local runs:

| Scenario | Observed result |
|---|---|
| Original First Name field | PASS |
| First Name `id` changed | PASS |
| First Name `id` and `name` changed | PASS |
| First Name `id`, `name`, and `placeholder` changed | PASS |
| Gender radio button `id` changed | PASS |
| State dropdown `id` changed | PASS |
| Original page after removing temporary mutation | PASS |

The three functional checks (First Name, Gender, and State) passed in the reported runs, and `dotnet build` completed successfully. The mutation scenarios were tested by temporarily injecting JavaScript from `Program.cs` to change the live page DOM. Those snippets are test aids and should be removed from the final normal run.

## Limitations and assumptions

- Form identification relies on recognizable evidence for First Name, the Gender radio-group name, and State. If all relevant identifying signals change, the form may not be identified.
- Radio-group matching currently depends on the group's `name` attribute matching the requested group label. Changing that attribute has not been demonstrated as a supported mutation.
- When multiple forms satisfy the current checks, the highest-scoring one is selected. Equal scores are resolved by retaining the first match; this heuristic is not a universal duplicate-form solution.
- Matching uses exact normalized attribute or label comparisons with weighted scoring. It is not an AI/ML locator model.
- The target page's HTML may change, so scoring rules may need maintenance.
- The current program is a sequential console runner for these three controls, not a general natural-language test generator.

## Possible future improvements

- Detect and report ambiguous candidates when multiple elements receive the same score.
- Expand semantic matching for radio groups and labels when attributes change.
- Move DOM mutation scenarios into repeatable automated tests.
- Add structured logging and screenshots on failure.
- Separate test cases from the console entry point and introduce a dedicated test runner.

## Repository

https://github.com/sahasra2420030068/CloudQA-Task-1
