using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        public static readonly string PaperaBaseUrl = @"http://ieeexplore.ieee.org/xpl/articleDetails.jsp?arnumber={0}";
        /// <summary>
        /// The base URL for web page cover for a conference in IEEE
        /// </summary>
        public static readonly string ConferenceBaseUrl = @"http://ieeexplore.ieee.org/xpl/RecentIssue.jsp?punumber={0}";

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
                        
            // retrieving HTML page
            string pageHtml = GetPaperCoverPage(bibtexRecord.Id);
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
                // retrieving conference identifier
                GetPublishedInDiv(htmlDoc, bibtexRecord);

                // if identifier has been found successfuly, 
                // we start a thread to discover the conference impact factor
                // in the meantime, we obtain the other properties whose
                // HTML is already loaded.
                Thread threadImpactFactor = null;
                if (!string.IsNullOrWhiteSpace(bibtexRecord.IdConference))
                {
                    threadImpactFactor = new Thread(() => GetImpactFactor(bibtexRecord));
                    threadImpactFactor.Start();
                }

                // retrieving country
                SetCountry(htmlDoc, bibtexRecord);
                // retrieving number of citations
                SetCitations(htmlDoc, bibtexRecord);
                // retrieving number of visualizations
                SetVisualizations(htmlDoc, bibtexRecord);

                // waiting for impact factor thread in case 
                // it didn't finish yet
                if (threadImpactFactor != null)
                    threadImpactFactor.Join();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bibtexRecord"></param>
        private void GetImpactFactor(BibtexRecord bibtexRecord)
        {
            string pageHtml = GetConferenceCoverPage(bibtexRecord.IdConference);

            // the HTML document 
            HtmlDocument htmlDoc = new HtmlDocument();

            // default option to fix easy HTML mistakes
            htmlDoc.OptionFixNestedTags = true;
            // structuring the HTML document
            htmlDoc.LoadHtml(pageHtml);

            // sanity check
            if (htmlDoc.DocumentNode != null)
            {
                double auxResults;
                // Impact factor node
                HtmlNode impactFactorNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='journal-page-bdy']/div[1]/div[2]/a[1]/span[1]");
                
                // sanity check
                if (impactFactorNode != null)
                {
                    double.TryParse(impactFactorNode.InnerText, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out auxResults);
                    bibtexRecord.ImpactFactor = auxResults;
                }
                impactFactorNode = null;

                // Eigenfactor node
                auxResults = 0;

                HtmlNode eigenfactorNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='journal-page-bdy']/div[1]/div[2]/a[2]/span[1]");
                // sanity check
                if (eigenfactorNode != null)
                {
                    double.TryParse(eigenfactorNode.InnerText, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out auxResults);
                    bibtexRecord.Eigenfactor = auxResults;
                }
                eigenfactorNode = null;


                // Article Influence Score node
                auxResults = 0;

                HtmlNode articleInfluenceScoreNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='journal-page-bdy']/div[1]/div[2]/a[3]/span[1]");
                // sanity check
                if (articleInfluenceScoreNode != null)
                {
                    double.TryParse(articleInfluenceScoreNode.InnerText, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out auxResults);
                    bibtexRecord.InfluenceScore = auxResults;
                }
                articleInfluenceScoreNode = null;
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
        /// 
        /// </summary>
        /// <param name="htmlDoc"></param>
        /// <returns></returns>
        private void GetPublishedInDiv(HtmlDocument htmlDoc, BibtexRecord stats)
        {
            // retrieving HTML element
            HtmlNode link = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='articleDetails']/div/div[2]/a[1]/@href");
            // sanity check
            if (link != null)
            {
                // retrieving the 'href' property from the retrieved element
                string linkHref = link.GetAttributeValue("href", null);
                // sanity check
                if (!string.IsNullOrWhiteSpace(linkHref))
                {
                    // splitting the URL so we can get the parameter
                    // we cannot use the 'Uri' and 'HttpUtility' because they do not work with relative paths
                    string[] splitTokens = linkHref.Split(new string[] { "punumber=" }, StringSplitOptions.RemoveEmptyEntries);
                    // sanity check
                    if (splitTokens != null && splitTokens.Length > 1)
                    {
                        // retrieving the second part of the split
                        string idPage = splitTokens[1];
                        // if it's all made of numeric digits we set it to the proper object
                        if (idPage.All(char.IsDigit))
                            stats.IdConference = idPage;
                    }
                }
            }
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
            stats.CitationsCount = citations;
        }

        /// <summary>
        /// Gets the HTML content of a web page. If a response code other than 200 is obtained, then
        /// 'null' is returned.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private string GetPaperCoverPage(string idUrl)
        {
            // creating crawling URL
            string url = string.Format(PaperaBaseUrl, idUrl);
            // code based on
            // http://www.seleniumhq.org/docs/03_webdriver.jsp#how-does-webdriver-drive-the-browser-compared-to-selenium-rc
            using (PhantomJSDriverService driverService = PhantomJSDriverService.CreateDefaultService())
            {
                driverService.HideCommandPromptWindow = true;
                using (IWebDriver driver = new PhantomJSDriver(driverService, new PhantomJSOptions(), TimeSpan.FromMinutes(2)))
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
        /// <summary>
        /// Gets the HTML source of the conference cover web page
        /// </summary>
        /// <param name="conferenceId"></param>
        /// <returns></returns>
        private string GetConferenceCoverPage(string conferenceId)
        {
            // creating crawling URL
            string url = string.Format(ConferenceBaseUrl, conferenceId);
            using (PhantomJSDriverService driverService = PhantomJSDriverService.CreateDefaultService())
            {
                driverService.HideCommandPromptWindow = true;
                using (IWebDriver driver = new PhantomJSDriver(driverService, new PhantomJSOptions(), TimeSpan.FromMinutes(2)))
                {
                    driver.Navigate().GoToUrl(url);
                    return driver.PageSource;
                }
            }
        }
    }
}
