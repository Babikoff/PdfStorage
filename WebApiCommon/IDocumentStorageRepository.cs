using Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApiCommon
{
    public interface IDocumentStorageRepository
    {
        Task<Document> GetAsync(Guid id);
        Task<IEnumerable<Document>> BrowseAsync(int size = 0, int page = 1, string sortBy = null, string sortOrder = null);
        Task<Document> AddAsync(Document document);
    }
}
