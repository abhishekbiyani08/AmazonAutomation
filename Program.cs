using System;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace AmazonShoppingAutomation
{
    class Program
    {
        public static async Task Main()
        {
            Console.WriteLine("Starting Amazon shopping automation...");

            try
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = false,
                    SlowMo = 100,
                });

                var context = await browser.NewContextAsync(new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize { Width = 1366, Height = 768 }
                });

                var page = await context.NewPageAsync();

                // Step 1: Navigate to Amazon.in
                Console.WriteLine("Step 1: Navigating to Amazon.in");
                await page.GotoAsync("https://www.amazon.in/");
                await page.WaitForSelectorAsync("#nav-logo-sprites", new PageWaitForSelectorOptions { Timeout = 30000 });
                Console.WriteLine("Amazon.in loaded successfully");

                await HandlePopupsIfPresent(page);

                // Step 2: Search for boat headphones
                Console.WriteLine("Step 2: Searching for boat headphones");
                await page.FillAsync("#twotabsearchtextbox", "boat headphones");
                await page.PressAsync("#twotabsearchtextbox", "Enter");
                await page.WaitForSelectorAsync("[data-component-type='s-search-result']", new PageWaitForSelectorOptions { Timeout = 30000 });
                Console.WriteLine("Search results loaded");

                // Step 3: Select a product
                Console.WriteLine("Step 3: Selecting a boat headphone product");
                page = await SelectBoatHeadphones(page); // new tab opens, so navigate context to it

                Console.WriteLine("Waiting for product page to load...");
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new PageWaitForLoadStateOptions { Timeout = 60000 });

                // Step 4: Confirm product page loaded by checking product title first
                Console.WriteLine("Step 4: Waiting for product page to load (checking title)...");

                // Wait for the product title to appear
                await page.WaitForSelectorAsync("#productTitle", new PageWaitForSelectorOptions { Timeout = 30000 });

                var productTitleElement = await page.QuerySelectorAsync("#productTitle");
                var productTitle = await productTitleElement?.InnerTextAsync();
                Console.WriteLine($"Product page loaded: {productTitle?.Trim()}");

                // Now wait for Buy Now button
                Console.WriteLine("Checking for 'Buy Now' button...");
                var buyNowButton = await page.QuerySelectorAsync("#buy-now-button");

                if (buyNowButton != null)
                {
                    Console.WriteLine("Found 'Buy Now' button, clicking it...");
                    await buyNowButton.ClickAsync();
                }
                else
                {
                    Console.WriteLine("Buy Now button not found. Product might not be available for direct purchase.");
                }

                // Step 5: Enter mobile number on sign-in page
                Console.WriteLine("Step 5: Entering mobile number");
                Console.WriteLine("Step 5: Entering mobile number and waiting for OTP");
                await page.WaitForSelectorAsync("#ap_email_login", new PageWaitForSelectorOptions { Timeout = 30000 });
                await page.FillAsync("#ap_email_login", "+91 9699114832");
                await page.PressAsync("#ap_email_login", "Enter");

                // Step 6: Wait for password page
                Console.WriteLine("Step 6: Waiting for password or OTP input page");
                await page.WaitForSelectorAsync("#ap_password, #auth-pv-enter-code", new PageWaitForSelectorOptions { Timeout = 30000 });
                Console.WriteLine("Reached sign-in/verification page.");

                Console.WriteLine("Automation complete! Stopped at sign-in.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.GetType().Name}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        private static async Task HandlePopupsIfPresent(IPage page)
        {
            try
            {
                var languagePopup = await page.QuerySelectorAsync("#icp-nav-flyout");
                if (languagePopup != null)
                {
                    Console.WriteLine("Handling language popup...");
                    await languagePopup.ClickAsync();
                    await page.WaitForTimeoutAsync(500);

                    var englishOption = await page.QuerySelectorAsync("a[href*='language=en_IN']");
                    if (englishOption != null)
                    {
                        await englishOption.ClickAsync();
                        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    }
                }

                var locationPopup = await page.QuerySelectorAsync("#nav-global-location-popover-link");
                if (locationPopup != null)
                {
                    Console.WriteLine("Handling location popup...");
                    var closeButton = await page.QuerySelectorAsync("span.a-button-inner input[type='submit']");
                    if (closeButton != null)
                    {
                        await closeButton.ClickAsync();
                        await page.WaitForTimeoutAsync(500);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Non-critical popup error: {ex.Message}");
            }
        }

        private static async Task<IPage> SelectBoatHeadphones(IPage page)
        {
            try
            {
                // Start listening for popup (new tab)
                var popupTask = page.WaitForPopupAsync();

                // Click product link
                var boatProductContainer = await page.QuerySelectorAsync("//div[contains(@data-component-type, 's-search-result')]//span[contains(text(), 'boAt') or contains(text(), 'Boat')]/ancestor::div[contains(@data-component-type, 's-search-result')]");
                if (boatProductContainer != null)
                {
                    Console.WriteLine("Found boAt product using title match");
                    var productLink = await boatProductContainer.QuerySelectorAsync("a.a-link-normal");
                    if (productLink != null)
                    {
                        await productLink.ClickAsync();
                    }
                }
                else
                {
                    // fallback if no boAt product found
                    var fallbackLink = await page.QuerySelectorAsync("[data-component-type='s-search-result'] h2 a");
                    if (fallbackLink != null)
                    {
                        Console.WriteLine("Fallback: clicking first product");
                        await fallbackLink.ClickAsync();
                    }
                }

                // Wait for new tab to open
                var newTabPage = await popupTask;
                await newTabPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                Console.WriteLine("Switched context to new product tab!");

                return newTabPage;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error selecting product: {ex.Message}");
                return page; // fallback: stay on the same page
            }
        }
    }
}