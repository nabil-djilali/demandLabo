using Microsoft.Playwright;

namespace BmLab
{
    
    [Parallelizable(ParallelScope.Self)]
    [TestFixture]
    public class DemandAnalyses
    {
        private IBrowser _browser { get; set; }
        private IPage _page { get; set; }
        private IPlaywright _playwright { get; set; }

        [SetUp]
        public async Task SetUp()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false // Set to false to open the browser visibly
            });
            var context = await _browser.NewContextAsync();
            _page = await context.NewPageAsync();
        }

        [TearDown]
        public async Task TearDown()
        {
            if (_browser != null)
                await _browser.DisposeAsync();
            if (_playwright != null)
                _playwright.Dispose();
        }

        [Test]
        public async Task MyTest()
        {
            // Goto demand page
            await _page.GotoAsync("https://senegal.bmvie.net/login?returnUrl=%2FWASM%2Fhome");
            await _page.Locator("#UserNameInput").ClickAsync();
            await _page.Locator("#UserNameInput").FillAsync("su@0");
            await _page.Locator("#PasswordInput").ClickAsync();
            await _page.Locator("#PasswordInput").FillAsync("41.02*27");
            await _page.GetByRole(AriaRole.Button, new() { Name = "Se connecter" }).ClickAsync();

            // Hover over the main menu item
            var demandeLink = _page.Locator("li.nav-item.px-3 > a.nav-link[href='/AnalyseDetail']");
            await demandeLink.HoverAsync();

            // Click the 'Demande' submenu item
            var subMenuItem = _page.Locator("div.FlySubMenu a[href='/AnalyseDetail']:has-text('Demande')");
            await _page.GotoAsync("https://senegal.bmvie.net/AnalyseDetail");

            // Select patient
            await _page.GetByPlaceholder("Sélectionner patient").ClickAsync();
            await _page.GetByRole(AriaRole.Cell, new() { Name = "1", Exact = true }).ClickAsync();

            // Select partenaire
            await _page.GetByPlaceholder("Selectionner partenaire").ClickAsync();
            await _page.WaitForSelectorAsync("table.dxbs-table");
            var firstPartenaireRow = _page.Locator("table.dxbs-table >> tbody >> tr").First;
            await firstPartenaireRow.ClickAsync();

            await Task.Delay(3000);

            // Wait for the analyse table to appear
            await _page.WaitForSelectorAsync("div.col-md-4 table.dxbl-grid-table");

            // Select the first two rows in the analyse table
            var analyseTableRows = _page.Locator("div.col-md-4 table.dxbl-grid-table >> tbody >> tr");
            for (int i = 0; i < 2; i++)
            {
                await analyseTableRows.Nth(i).Locator("input[type='checkbox']").CheckAsync();
            }

            // Locate and log the table rows before clicking "Ajouter"
            Console.WriteLine("Table rows before clicking 'Ajouter':");
            await LogTableRows(_page, "div.col-md-8 table.dxbl-grid-table >> tbody >> tr");

            // Click the "Ajouter" button
            await _page.GetByRole(AriaRole.Button, new() { Name = "Ajouter", Exact = true }).ClickAsync();
            await Task.Delay(3000);

            // Wait for the table to update after clicking "Ajouter"
            Console.WriteLine("Waiting for the table to update...");
            await _page.WaitForFunctionAsync(
                @"() => {
                    const rows = document.querySelectorAll('div.col-md-8 table.dxbl-grid-table tbody tr');
                    return rows.length > 0 && rows[0].innerText !== 'No data to display';
                }",
                new PageWaitForFunctionOptions { Timeout = 60000 }
            );

            // Locate and log the table rows after clicking "Ajouter"
            Console.WriteLine("Table rows after clicking 'Ajouter':");
            await LogTableRows(_page, "div.col-md-8 table.dxbl-grid-table >> tbody >> tr");

            // Assert that the table is not empty
            var tableRowsAfterUpdate = _page.Locator("div.col-md-8 table.dxbl-grid-table >> tbody >> tr");
            int rowCountAfterUpdate = await tableRowsAfterUpdate.CountAsync();

            Console.WriteLine("rowCountAfterUpdate"+ rowCountAfterUpdate);

            Assert.That(rowCountAfterUpdate, Is.GreaterThan(0), "The table should not be empty after clicking 'Ajouter'.");
        }

        private async Task LogTableRows(IPage page, string tableRowSelector)
        {
            var tableRows = page.Locator(tableRowSelector);
            int rowCount = await tableRows.CountAsync();

            if (rowCount == 0)
            {
                Console.WriteLine("The table has no rows.");
                return;
            }

            for (int i = 0; i < rowCount; i++)
            {
                var row = tableRows.Nth(i);
                var rowText = await row.InnerTextAsync();
                Console.WriteLine($"Row {i + 1}: {rowText}");
            }
        }
    }
}