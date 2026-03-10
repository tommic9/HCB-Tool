using System.Collections.Generic;

namespace HCB.RevitAddin.Features.CopyFilters.Models
{
    internal sealed class CopyFiltersResult
    {
        public int ProcessedViewsCount { get; set; }

        public int SelectedFiltersCount { get; set; }

        public int AddedFiltersCount { get; set; }

        public int UpdatedFiltersCount { get; set; }

        public int SkippedFiltersCount { get; set; }

        public List<string> Messages { get; } = new List<string>();
    }
}
