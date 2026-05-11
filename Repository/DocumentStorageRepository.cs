using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using WebApiCommon;

namespace Repository
{
    public class DocumentStorageRepository : IDocumentStorageRepository
    {
        private readonly DocumentStorageDbContext _context;
        private readonly ILogger<DocumentStorageRepository> _logger;

        public DocumentStorageRepository(
            DocumentStorageDbContext context,
            ILogger<DocumentStorageRepository> logger
            )
        {
            _context = context;
            _logger = logger;
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
                if (typeof(Document).GetProperty(sortBy, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) != null)
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

        public async Task<Document> AddOrUpdate(Document document)
        {
            var existingDoc = await _context.Documents.FindAsync(document.Id);
            if (existingDoc != null)
            {
                existingDoc.FileText = document.FileText;
                existingDoc.ProcessingStatus = DocumentProcessingStatus.Processed;
                existingDoc.ProcessedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return existingDoc;
            }
            else 
            {
                _logger.LogWarning("Restoring not existing document with Id {Id}", document.Id);
                document.ProcessingStatus = DocumentProcessingStatus.Processed;
                await _context.Documents.AddAsync(document);
                await _context.SaveChangesAsync();
                return document;
            }
        }
    }
}
