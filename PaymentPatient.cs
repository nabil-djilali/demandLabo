using Microsoft.Playwright;

namespace BmLab
{
    [Parallelizable(ParallelScope.Self)]
    [TestFixture]
    public class PaymentPatient
    {
        private IBrowser _browser;
        private IPage _page;
        private IPlaywright _playwright;

        [SetUp]
        public async Task SetUp()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false // Set to false for visible browser interactions
            });
            var context = await _browser.NewContextAsync();
            _page = await context.NewPageAsync();
        }

        [TearDown]
        public async Task TearDown()
        {
            if (_browser != null)
                await _browser.DisposeAsync();

            _playwright?.Dispose();
        }

        [Test]
        public async Task MyPay()
        {
            // Navigate to the login page
            await _page.GotoAsync("https://senegal.bmvie.net/login?returnUrl=%2FWASM%2Fhome");

            // Perform login
            await LoginAsync("su@0", "41.02*27");

            // Navigate to the Analyse Detail page
            await _page.GotoAsync("https://senegal.bmvie.net/AnalyseDetail");

            // Select a patient
            await SelectPatientAsync();

            // Select the first two rows in the analysis table
            await SelectAnalysisRowsAsync(2);

            // Simulate payment process
            await ProcessPaymentAsync(1000);
        }

        private async Task LoginAsync(string username, string password)
        {
            await _page.Locator("#UserNameInput").FillAsync(username);
            await _page.Locator("#PasswordInput").FillAsync(password);
            await _page.GetByRole(AriaRole.Button, new() { Name = "Se connecter" }).ClickAsync();
        }

        private async Task SelectPatientAsync()
        {
            await _page.GetByPlaceholder("Sélectionner patient").ClickAsync();
            await _page.GetByRole(AriaRole.Cell, new() { Name = "1", Exact = true }).ClickAsync();
        }

        private async Task SelectAnalysisRowsAsync(int rowCount)
        {
            await Task.Delay(1000);
            await _page.WaitForSelectorAsync("div.col-md-4 table.dxbl-grid-table");
            var rows = _page.Locator("div.col-md-4 table.dxbl-grid-table >> tbody >> tr");

            for (int i = 0; i < rowCount; i++)
            {
                await rows.Nth(i).Locator("input[type='checkbox']").CheckAsync();
            }

            await _page.Keyboard.PressAsync("Enter");
        }

        private async Task ProcessPaymentAsync(int amount)
        {
            // PaymentPatientPart
            // Select the PaymentPatient window using the specific classes
            var element = _page.Locator("div.dxbl-tabs-item.PaymentCount.TabCount");

            // Click the element
            await element.ClickAsync();

            // Select the input field PaymentPatient
            var inputField = _page.Locator("div.dxbl-fl-ctrl input[name='amount']");

            // Fill the input field with the value 1000
            await inputField.FillAsync(amount.ToString());
            await _page.Keyboard.PressAsync("Enter");
            await Task.Delay(3000);

            // Use the label text to locate the input adjacent to "Total payé:"
            var inputLocator = _page.GetByLabel("Total payé:");

            // Wait for the input element to be visible with a longer timeout
            await inputLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 60000 });

            // Extract the value of the input element
            var inputValue = await inputLocator.InputValueAsync();

            // Save the value in a variable
            var totalPaye = inputValue;
            Console.WriteLine($"Total payé: {totalPaye}");
            Console.WriteLine($"amount : {amount}");
            // Format the decimal to two decimal places
            decimal Decimalamount = (decimal)amount;
            string formattedDecimalAmount = Decimalamount.ToString("F2");  // F2 ensures two decimal places

            // Print the formatted amount for debugging
            Console.WriteLine($"decimal amount : {formattedDecimalAmount}");

            // Assert the formatted value
            Assert.That(formattedDecimalAmount, Is.EqualTo(inputValue));
        }
    }
}
