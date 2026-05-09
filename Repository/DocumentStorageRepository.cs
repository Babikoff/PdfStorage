using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApiCommon;

namespace Repository
{
    public class DocumentStorageRepository : IDocumentStorageRepository
    {
        private readonly DocumentStorageDbContext _context;
        private readonly IMemoryCache _cache;

        public DocumentStorageRepository(
            DocumentStorageDbContext context,
            IMemoryCache cache
            )
        {
            _context = context;
            _cache = cache;
        }

        public async Task<Document> GetAsync(Guid id)
        {
            return await _context.Documents.FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<Document>> BrowseAsync(int size = 0, int page = 1, string sortBy = null, string sortOrder = null)
        {
            IQueryable<Document> documents = _context.Documents;

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                if (typeof(Document).GetProperty(sortBy) != null)
                {
                    documents = documents.OrderByCustom(sortBy, sortOrder);
                }
            }

            if (size > 0)
            {
                documents = documents.Skip(size * (page - 1)).Take(size);
            }

            return await documents.ToArrayAsync();
        }

        public async Task<Document> AddAsync(Document document)
        {
            await _context.Documents.AddAsync(document);
            await _context.SaveChangesAsync();
            return document;
        }

    }
}
