using System.Collections.Generic;
using UNC.Services.Interfaces.Response;

namespace UNC.Services.Responses
{
    public class PagedResponse 
    {
        public static PagedResponse<T> Response<T>(IEnumerable<T> entities, int pageSize, int totalPages)
        {
            return new PagedResponse<T>() { Entities = entities, Index = 0, PageSize = pageSize, TotalRecords = totalPages };
        }

        public static PagedResponse<T> Response<T>(IEnumerable<T> entities, int pageSize, int totalPages, int index)
        {
            return new PagedResponse<T>() { Entities = entities, Index = index, PageSize = pageSize, TotalRecords = totalPages };
        }
    }
    public class PagedResponse<T>:IPagedResponse<T>
    {
        public int PageSize { get; set; }
        public int Index { get; set; }
        public int TotalRecords { get; set; }
        public IEnumerable<T> Entities { get; set; }

        public int TotalPages()
        {
            return TotalRecords / PageSize + (TotalRecords % PageSize > 0 ? 1 : 0);
        }

    }
}
