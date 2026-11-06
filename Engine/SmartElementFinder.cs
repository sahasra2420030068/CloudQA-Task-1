
using System;
using System.Linq;
using OpenQA.Selenium;

namespace CloudQA.SelfHealing.Engine
{
    public class SmartElementFinder
    {
        private readonly IWebDriver driver;

        public SmartElementFinder(IWebDriver driver)
        {
            this.driver = driver;
        }

        // Normalize text before comparing attributes and labels.
        private string Normalize(string value)
        {
            return new string(
                (value ?? "")
                    .Where(char.IsLetterOrDigit)
                    .ToArray()
            ).ToLowerInvariant();
        }

        // Retrieve the label associated with an element.
        private string GetLabel(
            IWebElement element,
            ISearchContext context)
        {
            string id = element.GetAttribute("id") ?? "";

            if (!string.IsNullOrWhiteSpace(id))
            {
                var labels = context.FindElements(
                    By.XPath($".//label[@for='{id}']")
                );

                if (labels.Count > 0)
                    return labels[0].Text.Trim();
            }

            try
            {
                return element.FindElement(
                    By.XPath("./preceding-sibling::label[1]")
                ).Text.Trim();
            }
            catch (NoSuchElementException)
            {
                return "";
            }
        }

        // Calculate the similarity score of a candidate element.
        private int CalculateScore(
            IWebElement element,
            string fieldLabel,
            ISearchContext context)
        {
            int score = 0;

            string expected = Normalize(fieldLabel);

            string id = Normalize(
                element.GetAttribute("id") ?? ""
            );

            string name = Normalize(
                element.GetAttribute("name") ?? ""
            );

            string placeholder = Normalize(
                element.GetAttribute("placeholder") ?? ""
            );

            string ariaLabel = Normalize(
                element.GetAttribute("aria-label") ?? ""
            );

            string label = Normalize(
                GetLabel(element, context)
            );

            if (id == expected)
                score += 100;

            if (name == expected)
                score += 100;

            if (placeholder == expected)
                score += 80;

            if (ariaLabel == expected)
                score += 90;

            if (label == expected)
                score += 150;

            return score;
        }

        // Find the form containing the requested fields.
        private IWebElement FindTargetForm()
        {
            Console.WriteLine(
                "\n--- IDENTIFYING TARGET FORM ---"
            );

            var forms = driver.FindElements(
                By.TagName("form")
            );

            IWebElement? bestForm = null;
            int highestFormScore = 0;

            foreach (var form in forms)
            {
                if (!form.Displayed)
                    continue;

                var inputs = form.FindElements(
                    By.CssSelector("input")
                );

                var selects = form.FindElements(
                    By.TagName("select")
                );

                // Find evidence for the First Name field.
                int firstNameScore = inputs
                    .Where(input =>
                    {
                        string type = Normalize(
                            input.GetAttribute("type") ?? "text"
                        );

                        return type == "text";
                    })
                    .Select(input =>
                        CalculateScore(input, "First Name", form)
                    )
                    .DefaultIfEmpty(0)
                    .Max();

                // Find evidence for the Gender radio group.
                bool hasGender = inputs.Any(input =>
                {
                    string type = Normalize(
                        input.GetAttribute("type") ?? ""
                    );

                    string name = Normalize(
                        input.GetAttribute("name") ?? ""
                    );

                    return type == "radio"
                        && name == "gender";
                });

                // Find evidence for the State dropdown.
                int stateScore = selects
                    .Select(select =>
                        CalculateScore(select, "State", form)
                    )
                    .DefaultIfEmpty(0)
                    .Max();

                bool hasFirstName = firstNameScore >= 80;
                bool hasState = stateScore >= 80;

                if (hasFirstName && hasGender && hasState)
                {
                    int formScore =
                        firstNameScore + stateScore + 100;

                    Console.WriteLine(
                        $"Candidate form score: {formScore}"
                    );

                    // Keep the highest-scoring form.
                    // On a tie, the first visible form is retained.
                    if (formScore > highestFormScore)
                    {
                        highestFormScore = formScore;
                        bestForm = form;
                    }
                }
            }

            if (bestForm == null)
            {
                throw new NoSuchElementException(
                    "Could not identify the target form."
                );
            }

            Console.WriteLine(
                "Target form identified successfully."
            );

            return bestForm;
        }

        // Find the First Name text field inside the target form.
        public IWebElement FindTextField(string fieldLabel)
        {
            Console.WriteLine(
                "\n--- SELF-HEALING TEXT FIELD SEARCH ---"
            );

            IWebElement targetForm = FindTargetForm();

            var candidates = targetForm.FindElements(
                By.CssSelector(
                    "input[type='text'], input:not([type]), textarea"
                )
            );

            IWebElement? bestMatch = null;
            int highestScore = 0;

            foreach (var element in candidates)
            {
                if (!element.Displayed || !element.Enabled)
                    continue;

                int score = CalculateScore(
                    element,
                    fieldLabel,
                    targetForm
                );

                Console.WriteLine(
                    $"ID: {element.GetAttribute("id")} | " +
                    $"Name: {element.GetAttribute("name")} | " +
                    $"Score: {score}"
                );

                if (score > highestScore)
                {
                    highestScore = score;
                    bestMatch = element;
                }
            }

            if (bestMatch == null || highestScore < 80)
            {
                throw new NoSuchElementException(
                    $"Could not confidently identify: {fieldLabel}"
                );
            }

            Console.WriteLine(
                $"Matched text field: {fieldLabel}"
            );

            return bestMatch;
        }

        // Find a radio option inside the target form.
        public IWebElement FindRadioOption(
            string groupLabel,
            string optionText)
        {
            Console.WriteLine(
                "\n--- SELF-HEALING RADIO SEARCH ---"
            );

            IWebElement targetForm = FindTargetForm();

            var candidates = targetForm.FindElements(
                By.CssSelector("input[type='radio']")
            );

            foreach (var radio in candidates)
            {
                if (!radio.Displayed || !radio.Enabled)
                    continue;

                string name = Normalize(
                    radio.GetAttribute("name") ?? ""
                );

                string value = Normalize(
                    radio.GetAttribute("value") ?? ""
                );

                string id = radio.GetAttribute("id") ?? "";
                string labelText = "";

                if (!string.IsNullOrWhiteSpace(id))
                {
                    var labels = targetForm.FindElements(
                        By.XPath($".//label[@for='{id}']")
                    );

                    if (labels.Count > 0)
                        labelText = labels[0].Text.Trim();
                }

                if (string.IsNullOrWhiteSpace(labelText))
                {
                    try
                    {
                        labelText = radio.FindElement(
                            By.XPath("./following-sibling::span[1]")
                        ).Text.Trim();
                    }
                    catch (NoSuchElementException)
                    {
                        // Value attribute is used as fallback.
                    }
                }

                bool groupMatches =
                    name == Normalize(groupLabel);

                bool optionMatches =
                    value == Normalize(optionText)
                    || Normalize(labelText) == Normalize(optionText);

                if (groupMatches && optionMatches)
                {
                    Console.WriteLine(
                        $"Matched {groupLabel}: {optionText}"
                    );

                    return radio;
                }
            }

            throw new NoSuchElementException(
                $"Could not find '{optionText}' in '{groupLabel}'."
            );
        }

        // Find the State dropdown inside the target form.
        public IWebElement FindDropdown(string fieldLabel)
        {
            Console.WriteLine(
                "\n--- SELF-HEALING DROPDOWN SEARCH ---"
            );

            IWebElement targetForm = FindTargetForm();

            var dropdowns = targetForm.FindElements(
                By.TagName("select")
            );

            IWebElement? bestMatch = null;
            int highestScore = 0;

            foreach (var dropdown in dropdowns)
            {
                if (!dropdown.Displayed || !dropdown.Enabled)
                    continue;

                int score = CalculateScore(
                    dropdown,
                    fieldLabel,
                    targetForm
                );

                Console.WriteLine(
                    $"ID: {dropdown.GetAttribute("id")} | " +
                    $"Name: {dropdown.GetAttribute("name")} | " +
                    $"Score: {score}"
                );

                if (score > highestScore)
                {
                    highestScore = score;
                    bestMatch = dropdown;
                }
            }

            if (bestMatch == null || highestScore < 80)
            {
                throw new NoSuchElementException(
                    $"Could not confidently identify: {fieldLabel}"
                );
            }

            Console.WriteLine(
                $"Matched dropdown: {fieldLabel}"
            );

            return bestMatch;
        }
    }
}
