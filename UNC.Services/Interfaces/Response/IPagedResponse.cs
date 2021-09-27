using System.Collections.Generic;


namespace UNC.Services.Interfaces.Response
{
    public interface IPagedResponse : IResponse
    {
        int PageSize { get; set; }
        int Index { get; set; }
        int TotalRecords { get; set; }
        int TotalPages();
    }
    public interface IPagedResponse<T> : IPagedResponse
    {
        
        IEnumerable<T> Entities { get; set; }
    }
}
