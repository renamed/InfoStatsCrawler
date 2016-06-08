using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoStats.Db
{
    /// <summary>
    /// Database queries repository
    /// </summary>
    public static class Queries
    {
        /// <summary>
        /// Inserts new values to the database
        /// </summary>
        public static readonly string InsertQuery = @"INSERT INTO [dbo].[Record] (ImpactFactor, Eigenfactor, InfluenceScore, InitialPage, EndPage, Pages, Published, Year, Id, Doi, BookTitle, Author, Journal, Title, Volume, Number, ISSN, Month, Keywords, IdConference, Country, CitationsCount, Visualizations) 
                                                      VALUES (@ImpactFactor, @Eigenfactor, @InfluenceScore, @InitialPage, @EndPage, @Pages, @Published, @Year, @Id, @Doi, @BooktTitle, @Author, @Journal, @Title, @Volume, @Number, @ISSN, @Month, @Keywords, @IdConference, @Country, @CitationCount, @Visualizations)";

        public static readonly string SelectAllQuery = @"SELECT * FROM [dbo].[Record]";
    }
}
