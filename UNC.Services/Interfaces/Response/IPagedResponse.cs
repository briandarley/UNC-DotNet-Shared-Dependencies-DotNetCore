using System.Collections.Generic;


namespace UNC.Services.Interfaces.Response
{
    public interface IPagedResponse : IResponse
    {

    }
    public interface IPagedResponse<T> : IPagedResponse
    {
        int PageSize { get; set; }
        int Index { get; set; }
        int TotalRecords { get; set; }
        int TotalPages();

        IEnumerable<T> Entities { get; set; }
    }
}
