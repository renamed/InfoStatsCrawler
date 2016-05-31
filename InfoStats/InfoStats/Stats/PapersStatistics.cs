using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoStats.Stats
{
    /// <summary>
    /// Calculates some statistics from the paper's info
    /// </summary>
    public class PapersStatistics
    {
        /// <summary>
        /// Paper's info
        /// </summary>
        private List<BibtexRecord> _thePapers;

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="paperInfo"></param>
        public PapersStatistics(List<BibtexRecord> paperInfo)
        {
            // sanity check
            if (paperInfo == null)
                throw new ArgumentNullException();

            // setting the property
            _thePapers = paperInfo;
        }

        /// <summary>
        /// Get the number of publishing papers by year
        /// </summary>
        public List<GroupByCountResult> CountPapersByYear()
        {
            // Papers count by year
            List<GroupByCountResult> papersByYear = new List<GroupByCountResult>();

            // getting the distinct years of the records
            IEnumerable<string> uniqueYears = _thePapers.Select(i => i.Year).Distinct();

            // we may have few unique years.
            foreach(string currentYear in uniqueYears)
            {
                // adding the record to the result list
                papersByYear.Add(new GroupByCountResult()
                {
                    // setting the current year
                    Grouping = currentYear,
                    // the amount of papers in this particupar year
                    Count = _thePapers.Where(i => i.Year.Equals(currentYear)).Count()
                });
            }

            // sorting the list by year
            papersByYear.Sort((x, y) => string.Compare(x.Grouping, y.Grouping));

            // returning
            return papersByYear;
        }
        /// <summary>
        /// Citations / Total citations by year
        /// </summary>
        public List<PapersByYearAvgStdByYear> GetCitationByOverallCitationsInYear()
        {
            // getting the distinct years of the records
            List<string> uniqueYears = _thePapers.Select(i => i.Year).Distinct().ToList();

            // avg and std dev of papers by year
            List<PapersByYearAvgStdByYear> papersByYearAvgStdByYear = new List<PapersByYearAvgStdByYear>();

            // we may have few unique years.
            for (int i = 3; i < uniqueYears.Count; i++)
            {
                // calculating total citations of 3 years ago
                int totalCitations3 = _thePapers.Where(k => k.Year.Equals(uniqueYears[i - 3])).Sum(k => k.CitationCount);
                int totalCitations2 = _thePapers.Where(k => k.Year.Equals(uniqueYears[i - 2])).Sum(k => k.CitationCount);
                int totalCitations1 = _thePapers.Where(k => k.Year.Equals(uniqueYears[i - 1])).Sum(k => k.CitationCount);

                // summing all 3 years total citations
                int totalCitationsLast3Years = totalCitations3 + totalCitations2 + totalCitations1;

                // calculating citation metrics for this particular year
                List<double> allCitationsInYear = _thePapers.Where(k => k.Year.Equals(uniqueYears[i])).Select(k => 1.0 * k.CitationCount / totalCitationsLast3Years).ToList();

                // calculating the year's statistics
                PapersByYearAvgStdByYear currentPapersByYearAvgStdByYear = new PapersByYearAvgStdByYear();
                currentPapersByYearAvgStdByYear.Year   = uniqueYears[i];
                currentPapersByYearAvgStdByYear.Avg    = allCitationsInYear.Average();
                currentPapersByYearAvgStdByYear.StdDev = allCitationsInYear.StdDev();

                // adding in the returning object
                papersByYearAvgStdByYear.Add(currentPapersByYearAvgStdByYear);
            }

            // go!
            return papersByYearAvgStdByYear;
        }

        /// <summary>
        /// Returns the number of distinct countries by year
        /// </summary>
        public List<GroupByCountResult> CountDistinctCountriesByYear()
        {
            // Papers count by year
            List<GroupByCountResult> distinctCountiesByYear = new List<GroupByCountResult>();

            // getting the distinct years of the records
            IEnumerable<string> uniqueYears = _thePapers.Select(i => i.Year).Distinct();

            // we may have few unique years.
            foreach (string currentYear in uniqueYears)
            {
                distinctCountiesByYear.Add(new GroupByCountResult()
                {
                    Grouping = currentYear,
                    Count = _thePapers.Where(i => i.Year.Equals(currentYear)).Select(i => i.Country).Distinct().Count()
                });
            }

            // sort by year
            distinctCountiesByYear.Sort((x, y) => { return x.Grouping.CompareTo(y.Grouping); });

            // go
            return distinctCountiesByYear;
        }

        /// <summary>
        /// Calculates the countries whose publishing count is above the std dev
        /// </summary>
        /// <param name="limit">The max number of contries to be returned</param>
        /// <param name="yearLimit"></param>
        /// <returns>
        /// A sorted list in which the first element is the one farthest from the std dev
        /// in steps to get to the std dev. 
        /// </returns>
        public List<GroupByCountResult> GetStepsFromStdDevByCountry(int limit, int yearLimit)
        {
            // sanity check
            if (limit < 0)
                throw new ArgumentOutOfRangeException("Limit may not be < 0");
            // avoiding unnecessary computing
            if (limit == 0)
                return new List<GroupByCountResult>();

            // retrieving the unique years, sorting and
            // taking the first 'yearLimit' elements.
            IEnumerable<string> uniqueYears = _thePapers.Select(i => i.Year).Distinct().OrderByDescending(i => i);
            List<BibtexRecord> workingList = _thePapers;

            // checking if we should decrease our working object
            if (yearLimit < uniqueYears.Count())
            {
                uniqueYears = uniqueYears.Take(yearLimit);
                workingList = workingList.Where(i => uniqueYears.Contains(i.Year)).ToList();
            }

            // sanity check
            if (workingList.Count == 0)
                return new List<GroupByCountResult>();

            // average publications by country
            double averageByCountry = 1.0 * workingList.Count / workingList.Select(i => i.Country).Distinct().Count();

            // std dev
            Dictionary<string, int> _countByCountry = new Dictionary<string, int>();
            // counting how many publications each country has
            foreach(BibtexRecord currentRecord in workingList)
            {
                // if country has not been seen yet, initialize it with 1
                if (!_countByCountry.ContainsKey(currentRecord.Country))
                    _countByCountry[currentRecord.Country] = 1;
                else
                // else, increment the number of papers
                    _countByCountry[currentRecord.Country]++;
            }

            // calculating the std dev
            double auxStdDev = 0;
            // powering the difference between the current value and the avg
            foreach (KeyValuePair<string, int> currentContry in _countByCountry)            
                auxStdDev += Math.Pow(currentContry.Value - averageByCountry, 2);
            
            // dividing by the number of distinct countries
            auxStdDev /= workingList.Select(i => i.Country).Distinct().Count();

            // taking the square root to obtain the standard deviation
            double stdDev = Math.Sqrt(auxStdDev);
            
            // the high threshold
            double supThreshold = averageByCountry + stdDev + 0.00001;

            // countries above the std dev
            List<GroupByCountResult> aboveStdDev = new List<GroupByCountResult>();

            // visiting each country
            foreach (KeyValuePair<string, int> currentCountry in _countByCountry)
            {
                int mod   = Convert.ToInt32(currentCountry.Value % supThreshold);
                int steps = Convert.ToInt32(currentCountry.Value / supThreshold);
                if (mod == 0)
                    steps++;

                if (steps > 0)
                {
                    aboveStdDev.Add(new GroupByCountResult()
                    {
                        Grouping = currentCountry.Key,
                        Count = steps
                    });
                }
            }

            // sorting the current array
            aboveStdDev.Sort((x, y) => { return y.Count.CompareTo(x.Count); });

            // taking the requested number of elements
            if (aboveStdDev.Count > limit)
                aboveStdDev = aboveStdDev.Take(limit).ToList();

            // go!
            return aboveStdDev;
        } 

        /// <summary>
        /// Calculates and returns the countries with the largest number of publishings
        /// </summary>
        /// <param name="limit">The top number limit</param>
        public List<KeyValuePair<string, int>> GetCountriesWithMostPublishing(int limit, int yearLimit)
        {

            // retrieving the unique years, sorting and
            // taking the first 'yearLimit' elements.
            IEnumerable<string> uniqueYears = _thePapers.Select(i => i.Year).Distinct().OrderByDescending(i => i);
            List<BibtexRecord> workingList = _thePapers;

            // checking if we should decrease our working object
            if (yearLimit < uniqueYears.Count())
            {
                uniqueYears = uniqueYears.Take(yearLimit);
                workingList = workingList.Where(i => uniqueYears.Contains(i.Year)).ToList();
            }
            uniqueYears = null;

            Dictionary<string, int> _countByCountry = new Dictionary<string, int>();

            // counting how many publications each country has
            foreach (BibtexRecord currentRecord in workingList)
            {
                // if country has not been seen yet, initialize it with 1
                if (!_countByCountry.ContainsKey(currentRecord.Country))
                    _countByCountry[currentRecord.Country] = 1;
                else
                    // else, increment the number of papers
                    _countByCountry[currentRecord.Country]++;
            }

            // from dictionary to sorted list
            List<KeyValuePair<string, int>> countryList = _countByCountry.ToList().OrderByDescending(i => i.Value).ToList();

            // go !
            return countryList.Take(limit).ToList();

        }
        /// <summary>
        /// Calculates the following statistics for countries:
        ///     average
        ///     std dev
        ///     variance
        ///     mean
        ///     largest value
        ///     lowest value
        /// In relation to the year
        /// </summary>
        public List<CountryStats> GetStats(int limit, int yearLimit)
        {   
            // retrieving the unique years, sorting and
            // taking the first 'yearLimit' elements.
            IEnumerable <string> uniqueYears = _thePapers.Select(i => i.Year).Distinct().OrderByDescending(i => i);
            List<BibtexRecord> workingList = _thePapers;

            // checking if we should decrease our working object
            if (yearLimit < uniqueYears.Count())
            {
                uniqueYears = uniqueYears.Take(yearLimit);
                workingList = workingList.Where(i => uniqueYears.Contains(i.Year)).ToList();
            }
            uniqueYears = null;

            // number of papers published by country
            Dictionary<string, int> _countByCountry = new Dictionary<string, int>();           

            // counting how many publications each country has
            foreach (BibtexRecord currentRecord in workingList)
            {
                // if country has not been seen yet, initialize it with 1
                if (!_countByCountry.ContainsKey(currentRecord.Country))
                    _countByCountry[currentRecord.Country] = 1;
                else
                    // else, increment the number of papers
                    _countByCountry[currentRecord.Country]++;
            }

            List<CountryStats> countryStats = new List<CountryStats>();

            // avg
            foreach (KeyValuePair<string, int> currentRecord in _countByCountry)
            {
                int distinctYearsCount = workingList.Where(i => i.Country.Equals(currentRecord.Key)).Select(i => i.Year).Distinct().Count();
                countryStats.Add(new CountryStats()
                {
                    Country = currentRecord.Key,
                    Avg     = 1.0 * currentRecord.Value / distinctYearsCount
                });
            }

            // std dev, variance, highest, lowest and mean
            foreach (CountryStats currentCountryStats in countryStats)
            {
                // the sum to calculate the std dev and variance
                double sum = 0;

                // the highest value of the series
                int highestValue = int.MinValue;
                // the lowest value of the series
                int lowestValue  = int.MaxValue;

                // storing all values to calculate the mean
                List<int> allValues = new List<int>();

                // aux dictionary to calculate the mode
                Dictionary<int, int> dicMode = new Dictionary<int, int>();

                // retrieving all years to calculate it separately
                IEnumerable<string> distinctYears = workingList.Where(i => i.Country.Equals(currentCountryStats.Country)).Select(i => i.Year).Distinct();
                foreach(string currentYear in distinctYears)
                {
                    // number of publications for a particular country and year
                    int publishingNumber = workingList.Where(i => i.Country.Equals(currentCountryStats.Country) && i.Year.Equals(currentYear)).Count();
                    // desv pad calc
                    sum += Math.Pow(publishingNumber - currentCountryStats.Avg, 2);

                    // calculating the mode
                    if (!dicMode.ContainsKey(publishingNumber))
                        dicMode[publishingNumber] = 1;
                    else
                        dicMode[publishingNumber]++;

                    // storing highest and lowest values
                    if (publishingNumber > highestValue)
                        highestValue = publishingNumber;
                    if (publishingNumber < lowestValue)
                        lowestValue = publishingNumber;

                    // adding current value to calculate the mean later on
                    allValues.Add(publishingNumber);
                }

                // variance and std dev
                currentCountryStats.Var    = sum / distinctYears.Count();
                currentCountryStats.StdDev = Math.Sqrt(currentCountryStats.Var);

                // highest and lowest values
                currentCountryStats.HighestValue = highestValue;
                currentCountryStats.LowestValue  = lowestValue;

                // sorting list to calculate the mean
                allValues.Sort((x, y) => { return x.CompareTo(y); });

                // testing each possible scenario to obtain the list mean
                if (allValues.Count == 0)
                    currentCountryStats.Mean = 0;
                else if (allValues.Count == 1)
                    currentCountryStats.Mean = allValues[0];
                if (allValues.Count % 2 != 0)
                    currentCountryStats.Mean = allValues[allValues.Count / 2];
                else
                    currentCountryStats.Mean = (allValues[allValues.Count / 2 - 1] + allValues[allValues.Count / 2]) / 2.0;

                // mode
                int valueMode = dicMode.Values.Max();
                if (valueMode > 1)
                    currentCountryStats.Mode = dicMode.Where(i => i.Value == valueMode).Select(i => i.Key).FirstOrDefault();
                else
                    currentCountryStats.Mode = -1;

                //median point
                currentCountryStats.MedianPoint = (currentCountryStats.HighestValue + currentCountryStats.LowestValue) / 2.0;
            }
            // sorting and taking the amount asked
            return countryStats.OrderByDescending(i => i.Mean).Take(limit).ToList();
        }

        /// <summary>
        /// Count and returns the most used keywords
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="yearLimit"></param>
        /// <returns></returns>
        public List<KeyValuePair<string, int>> CountKeywords(int limit, int yearLimit)
        {
            // retrieving the unique years, sorting and
            // taking the first 'yearLimit' elements.
            IEnumerable<string> uniqueYears = _thePapers.Select(i => i.Year).Distinct().OrderByDescending(i => i);
            List<BibtexRecord> workingList = _thePapers;

            // checking if we should decrease our working object
            if (yearLimit < uniqueYears.Count())
            {
                uniqueYears = uniqueYears.Take(yearLimit);
                workingList = workingList.Where(i => uniqueYears.Contains(i.Year)).ToList();
            }
            uniqueYears = null;

            // dictionary to count the occurrence of each keyword
            Dictionary<string, int> keywordsCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // visiting each valid record
            foreach(BibtexRecord currentRecord in workingList)
            {
                // sanity check
                if (string.IsNullOrWhiteSpace(currentRecord.KeyWords))
                    continue;
                // splitting the keywords string into an array so 
                // each position will be one separated keyword
                string[] tokens = currentRecord.KeyWords.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                // visiting each keyword
                foreach(string currentToken in tokens)
                {
                    // sanity check
                    if (!string.IsNullOrWhiteSpace(currentToken))
                    {
                        // removing empty spaces from the beginning and ending
                        string auxCurrentToken = currentToken.Trim();
                        // adding the new keyword to the dictionary
                        // or incrementing if it's not been seen so far
                        if (!keywordsCount.ContainsKey(auxCurrentToken))
                            keywordsCount[auxCurrentToken] = 1;
                        else
                            keywordsCount[auxCurrentToken]++;
                    }
                }
            }
            // sorting, taking the number of elements requested and returning
            return keywordsCount.ToList().OrderByDescending(i => i.Value).Take(limit).ToList();            
        }
    }
}
