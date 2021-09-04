

using UNC.Services.Enums;

namespace UNC.Services.Criteria
{
    public abstract class BasePagingCriteria
    {

        public int? PageSize { get; set; }
        public int? Index { get; set; }
        public string FilterText { get; set; }
        public string Sort { get; set; }

        public ListSortDirection? ListSortDirection { get; set; }
        
    }
}
