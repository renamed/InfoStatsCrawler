using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoStats
{
    /// <summary>
    /// A Bibtex reference record from the bib file.
    /// </summary>
    public class BibtexRecord 
    {
        private string _pages;
        private string _year;

        public double ImpactFactor { get; set; }
        public double Eigenfactor { get; set; }
        public double InfluenceScore { get; set; }

        public int InitialPage { get; private set; }
        public int EndPage { get; private set; }
        public int Published { get; private set; }

        public string Pages { get { return _pages; } set { SetPages(value); } }
        public string Year { get { return _year; } set { SetYear(value); } }

        public string Id { get; set; }
        public string Doi { get; set; }
        public string BooktTitle { get; set; }
        public string Author { get; set; }
        public string Journal { get; set; }
        public string Title { get; set; }
        public string Volume { get; set; }
        public string Number { get; set; }
        public string ISSN { get; set; }
        public string Month { get; set; }
        public string KeyWords { get; set; }
        public string IdConference { get; set; }

        public string Country { get; set; }
        public int CitationsCount { get; set; }
        public int Visualizations { get; set; }



        /// <summary>
        /// Set initial and ending pages
        /// </summary>
        public void SetPages(string pages)
        {
            // setting the default value to both pages
            InitialPage = 0;
            EndPage = 0;
            _pages = pages;

            // sanity check
            if (!string.IsNullOrWhiteSpace(pages))
            {
                // Pages come in format "BEGIN-END"
                string[] tokens = pages.Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);

                // sanity check
                if (tokens.Length == 2)
                {
                    // we can only set the initial and end pages if both exist
                    int auxBegin, auxEnd;
                    if (!int.TryParse(tokens[0], out auxBegin)) return;
                    if (!int.TryParse(tokens[1], out auxEnd)) return;

                    // sanity check
                    if (InitialPage > EndPage) return;

                    // everything's ok
                    InitialPage = auxBegin;
                    EndPage = auxEnd;
                }
            }

        }

        /// <summary>
        /// It is necessary to convert the year from string to int.
        /// </summary>
        /// <param name="year"></param>
        public void SetYear(string year)
        {
            int auxYear = 0;

            if (!int.TryParse(year, out auxYear))
                _year = null;
            else
                _year = year;

            Published = auxYear;
        }

    }
}
