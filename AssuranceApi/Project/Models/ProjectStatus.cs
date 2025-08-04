namespace AssuranceApi.Project.Models
{
    /// <summary>
    /// Represents the status of a project, including completion metrics and RAG ratings.
    /// </summary>
    public class ProjectStatus
    {
        /// <summary>
        /// The total score of the completed standards for the project.
        /// </summary>
        public int ScoreOfStandardsCompleted { get; set; } = 0;

        /// <summary>
        /// Gets or sets the number of standards completed for the project.
        /// </summary>
        public int NumberOfStandardsCompleted { get; set; } = 0;

        private double _percentageAcrossAllStandards = 0;

        /// <summary>
        /// Gets or sets the percentage score for the project based on the total number of standards available.
        /// </summary>
        public double PercentageAcrossAllStandards
        {
            get
            {
                return _percentageAcrossAllStandards;
            }
            set
            {
                if (value < 0 || value > 100)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Percentage must be between 0 and 100.");
                }

                _percentageAcrossAllStandards = value;
            }
        }

        private double _percentageAcrossCompletedStandards = 0;

        /// <summary>
        /// Gets or sets the percentage score for the project based on the total number of standards that have been completed by the team.
        /// </summary>
        public double PercentageAcrossCompletedStandards
        {
            get
            {
                return _percentageAcrossCompletedStandards;
            }
            set
            {
                if (value < 0 || value > 100)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Percentage must be between 0 and 100.");
                }

                _percentageAcrossCompletedStandards = value;
            }
        }

        /// <summary>
        /// Gets or sets the calculated RAG rating for the project.
        /// </summary>
        public string CalculatedRag { get; set; } = null!;

        /// <summary>
        /// Gets or sets the lowest RAG rating for the project.
        /// </summary>
        public string LowestRag { get; set; } = null!;
    }
}
