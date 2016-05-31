using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InfoStats
{
    /// <summary>
    /// Crawls the IEEE cover web page for a paper in order to gather further information about it
    /// </summary>
    public class EnrichPaperInfo
    {
        /// <summary>
        /// The base URL for web page cover for a paper in IEEE
        /// </summary>
        public static readonly string BaseUrl = @"http://ieeexplore.ieee.org/xpl/articleDetails.jsp?arnumber={0}";

        /// <summary>
        /// Crawls and retrives information from the IEEE cover web page for papers
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public void EnrichObjectInfo(BibtexRecord bibtexRecord)
        {
            // sanity check
            if (bibtexRecord == null)
                throw new ArgumentNullException();
            // sanity check
            if (string.IsNullOrWhiteSpace(bibtexRecord.Id))
                throw new ArgumentNullException();

            // creating crawling URL
            string url = string.Format(BaseUrl, bibtexRecord.Id);
            // retrieving HTML page
            string pageHtml = GetPageHTML(url);
            // completing BibtexRecord object
            RetrieveInfoFromHtml(pageHtml, bibtexRecord);
        }

        /// <summary>
        /// Receives the page HTML document and mines the information on it 
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private void RetrieveInfoFromHtml(string html, BibtexRecord bibtexRecord)
        {
            // sanity check
            if (string.IsNullOrWhiteSpace(html))
                throw new ArgumentNullException();

            // the HTML document 
            HtmlDocument htmlDoc = new HtmlDocument();

            // default option to fix easy HTML mistakes
            htmlDoc.OptionFixNestedTags = true;
            // structuring the HTML document
            htmlDoc.LoadHtml(html);

            // sanity check
            if (htmlDoc.DocumentNode != null)
            {
                SetCountry(htmlDoc, bibtexRecord);
                SetCitations(htmlDoc, bibtexRecord);
                SetVisualizations(htmlDoc, bibtexRecord);
            }
        }

        /// <summary>
        /// Gets the visualization information from the HTML document and sets into the stats object
        /// </summary>
        /// <param name="htmlDoc"></param>
        /// <param name="stats"></param>
        private void SetVisualizations(HtmlDocument htmlDoc, BibtexRecord stats)
        {
            // getting the div whose id is 'countHeader'
            HtmlNode totalCount = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='total-count']");

            int visualizations = 0;
            // sanity check
            if (totalCount != null)
            {
                string innerText = totalCount.InnerText;
                if (!string.IsNullOrWhiteSpace(innerText))
                {
                    string[] tokens = innerText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens != null && tokens.Length > 1)                    
                        int.TryParse(tokens[0].Trim(), out visualizations);
                    
                }
            }
            stats.Visualizations = visualizations;
        }

        /// <summary>
        /// Gets the country information from the HTML document and sets into the stats object
        /// </summary>
        /// <param name="htmlDoc"></param>
        /// <param name="stats"></param>
        private void SetCountry(HtmlDocument htmlDoc, BibtexRecord stats)
        {
            // retrieving the 'span' element whose identifier is 'authorAffiliations'
            HtmlNode authorAffiliations = htmlDoc.DocumentNode.SelectSingleNode("//span[@id='authorAffiliations']");
            // sanity check
            if (authorAffiliations != null)
            {
                // retrieving its 'class' attribute, which contains the author's institute and country
                string authorAffiliationsClass = authorAffiliations.GetAttributeValue("class", null);
                // sanity check
                if (!string.IsNullOrWhiteSpace(authorAffiliationsClass))
                {
                    // splitting by comma to take each word separately
                    string[] authorTokens = authorAffiliationsClass.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    // sanity check
                    if (authorTokens != null)                    
                        // the country is the last token, sometimes it's followed by the string '|c|' which doesn't interest us
                        stats.Country = authorTokens[authorTokens.Length - 1].Replace("|c|", string.Empty).Trim();                    
                }
            }
        }

        /// <summary>
        /// Gets the citation information from the HTML document and sets into the stats object
        /// </summary>
        /// <param name="htmlDoc"></param>
        /// <param name="stats"></param>
        private void SetCitations(HtmlDocument htmlDoc, BibtexRecord stats)
        {
            // getting the div whose id is 'countHeader'
            HtmlNode countHeader = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='countHeader']");

            int citations = 0;
            // sanity check
            if (countHeader != null)
            {
                // retrieving the inner text of the div
                string innerText = countHeader.InnerText;
                // sanity check
                if (!string.IsNullOrWhiteSpace(innerText))
                {
                    // there are several tokens in this div. The first one is the citation count
                    string[] tokens = innerText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    // sanity check
                    if (tokens != null && tokens.Length > 1)                    
                        // trying to parse the first token
                        int.TryParse(tokens[0].Trim(), out citations);                    
                }
            }
            // setting the value to the object
            stats.CitationCount = citations;
        }

        /// <summary>
        /// Gets the HTML content of a web page. If a response code other than 200 is obtained, then
        /// 'null' is returned.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private string GetPageHTML(string url)
        {
            // code based on
            // http://www.seleniumhq.org/docs/03_webdriver.jsp#how-does-webdriver-drive-the-browser-compared-to-selenium-rc
            using (IWebDriver driver = new PhantomJSDriver())
            {
                // going to the URL
                driver.Navigate().GoToUrl(url);
                // sleeping to avoid IP blocking
                Thread.Sleep(2 * 1000);
                // clicking on the element to retrieve the statistics
                driver.FindElement(By.Id("abstract-citedby-tab")).Click();
                // sleeping so page can be rendered completly
                Thread.Sleep(2 * 1000);
                // clicking on the element to retrieve the statistics
                driver.FindElement(By.Id("abstract-metrics-tab")).Click();
                // sleeping so page can be fully rendered 
                Thread.Sleep(2 * 1000);
                // returning its HTML
                return driver.PageSource;
            }
        }
    }
}
