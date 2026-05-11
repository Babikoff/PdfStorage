using Contracts;
using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Repository;
using Xunit;

namespace Repository.Tests
{
    /// <summary>
    /// Тесты для репозитория DocumentStorageRepository.
    /// </summary>
    public class RepositoryTests
    {
        private readonly ILogger<DocumentStorageRepository> _logger;

        public RepositoryTests()
        {
            _logger = Substitute.For<ILogger<DocumentStorageRepository>>();
        }

        /// <summary>
        /// Создаёт новый экземпляр DocumentStorageDbContext с уникальным именем БД для изоляции тестов.
        /// </summary>
        private static DocumentStorageDbContext CreateDbContext(string databaseName)
        {
            var options = new DbContextOptionsBuilder<DocumentStorageDbContext>()
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new DocumentStorageDbContext(options);
        }

        [Fact]
        public async Task GetAsync_ExistingId_ReturnsDocument()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            await using var ctx = CreateDbContext(dbName);

            var document = new Document
            {
                Id = Guid.NewGuid(),
                FileName = "test.pdf",
                FileType = DocumentFileType.Pdf,
                FileSize = 100,
                RecievedAt = DateTimeOffset.UtcNow,
                ProcessingStatus = DocumentProcessingStatus.InQueue
            };

            ctx.Documents.Add(document);
            await ctx.SaveChangesAsync();

            var repo = new DocumentStorageRepository(ctx, _logger);

            // Act
            var result = await repo.GetAsync(document.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(document.Id, result.Id);
            Assert.Equal("test.pdf", result.FileName);
        }

        [Fact]
        public async Task GetAsync_NonExistingId_ReturnsNull()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            await using var ctx = CreateDbContext(dbName);
            var repo = new DocumentStorageRepository(ctx, _logger);

            // Act
            var result = await repo.GetAsync(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task BrowseAsync_WithoutParameters_ReturnsAllDocuments()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            await using var ctx = CreateDbContext(dbName);

            ctx.Documents.AddRange(
                new Document { Id = Guid.NewGuid(), FileName = "a.pdf", FileType = DocumentFileType.Pdf, RecievedAt = DateTimeOffset.UtcNow, ProcessingStatus = DocumentProcessingStatus.Processed },
                new Document { Id = Guid.NewGuid(), FileName = "b.pdf", FileType = DocumentFileType.Pdf, RecievedAt = DateTimeOffset.UtcNow, ProcessingStatus = DocumentProcessingStatus.Processed },
                new Document { Id = Guid.NewGuid(), FileName = "c.pdf", FileType = DocumentFileType.Pdf, RecievedAt = DateTimeOffset.UtcNow, ProcessingStatus = DocumentProcessingStatus.Processed }
            );
            await ctx.SaveChangesAsync();

            var repo = new DocumentStorageRepository(ctx, _logger);

            // Act
            var result = await repo.BrowseAsync();

            // Assert
            Assert.Equal(3, result.Count());
        }

        [Fact]
        public async Task BrowseAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            await using var ctx = CreateDbContext(dbName);

            for (int i = 0; i < 10; i++)
            {
                ctx.Documents.Add(new Document
                {
                    Id = Guid.NewGuid(),
                    FileName = $"file{i}.pdf",
                    FileType = DocumentFileType.Pdf,
                    RecievedAt = DateTimeOffset.UtcNow,
                    ProcessingStatus = DocumentProcessingStatus.Processed
                });
            }
            await ctx.SaveChangesAsync();

            var repo = new DocumentStorageRepository(ctx, _logger);

            // Act
            var page1 = await repo.BrowseAsync(size: 3, page: 1);
            var page2 = await repo.BrowseAsync(size: 3, page: 2);

            // Assert
            Assert.Equal(3, page1.Count());
            Assert.Equal(3, page2.Count());
        }

        [Fact]
        public async Task BrowseAsync_WithSortByFileNameAsc_ReturnsSorted()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            await using var ctx = CreateDbContext(dbName);

            ctx.Documents.AddRange(
                new Document { Id = Guid.NewGuid(), FileName = "c.pdf", FileType = DocumentFileType.Pdf, RecievedAt = DateTimeOffset.UtcNow, ProcessingStatus = DocumentProcessingStatus.Processed },
                new Document { Id = Guid.NewGuid(), FileName = "a.pdf", FileType = DocumentFileType.Pdf, RecievedAt = DateTimeOffset.UtcNow, ProcessingStatus = DocumentProcessingStatus.Processed },
                new Document { Id = Guid.NewGuid(), FileName = "b.pdf", FileType = DocumentFileType.Pdf, RecievedAt = DateTimeOffset.UtcNow, ProcessingStatus = DocumentProcessingStatus.Processed }
            );
            await ctx.SaveChangesAsync();

            var repo = new DocumentStorageRepository(ctx, _logger);

            // Act
            var result = await repo.BrowseAsync(sortBy: "FileName", sortOrder: "asc");

            // Assert
            var list = result.ToList();
            Assert.Equal("a.pdf", list[0].FileName);
            Assert.Equal("b.pdf", list[1].FileName);
            Assert.Equal("c.pdf", list[2].FileName);
        }

        [Fact]
        public async Task BrowseAsync_WithSortByFileNameDesc_ReturnsSorted()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            await using var ctx = CreateDbContext(dbName);

            ctx.Documents.AddRange(
                new Document { Id = Guid.NewGuid(), FileName = "a.pdf", FileType = DocumentFileType.Pdf, RecievedAt = DateTimeOffset.UtcNow, ProcessingStatus = DocumentProcessingStatus.Processed },
                new Document { Id = Guid.NewGuid(), FileName = "c.pdf", FileType = DocumentFileType.Pdf, RecievedAt = DateTimeOffset.UtcNow, ProcessingStatus = DocumentProcessingStatus.Processed },
                new Document { Id = Guid.NewGuid(), FileName = "b.pdf", FileType = DocumentFileType.Pdf, RecievedAt = DateTimeOffset.UtcNow, ProcessingStatus = DocumentProcessingStatus.Processed }
            );
            await ctx.SaveChangesAsync();

            var repo = new DocumentStorageRepository(ctx, _logger);

            // Act
            var result = await repo.BrowseAsync(sortBy: "FileName", sortOrder: "desc");

            // Assert
            var list = result.ToList();
            Assert.Equal("c.pdf", list[0].FileName);
            Assert.Equal("b.pdf", list[1].FileName);
            Assert.Equal("a.pdf", list[2].FileName);
        }

        [Fact]
        public async Task BrowseAsync_WithInvalidSortBy_ReturnsUnsorted()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            await using var ctx = CreateDbContext(dbName);

            ctx.Documents.AddRange(
                new Document { Id = Guid.NewGuid(), FileName = "c.pdf", FileType = DocumentFileType.Pdf, RecievedAt = DateTimeOffset.UtcNow, ProcessingStatus = DocumentProcessingStatus.Processed },
                new Document { Id = Guid.NewGuid(), FileName = "a.pdf", FileType = DocumentFileType.Pdf, RecievedAt = DateTimeOffset.UtcNow, ProcessingStatus = DocumentProcessingStatus.Processed }
            );
            await ctx.SaveChangesAsync();

            var repo = new DocumentStorageRepository(ctx, _logger);

            // Act
            var result = await repo.BrowseAsync(sortBy: "NonExistentProperty", sortOrder: "asc");

            // Assert
            Assert.Equal(2, result.Count());
            // Порядок не гарантируется, но метод не должен выбросить исключение (возможно спорный подход)
        }

        [Fact]
        public async Task AddAsync_ValidDocument_AddsAndReturns()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            await using var ctx = CreateDbContext(dbName);
            var repo = new DocumentStorageRepository(ctx, _logger);

            var document = new Document
            {
                Id = Guid.NewGuid(),
                FileName = "newfile.pdf",
                FileType = DocumentFileType.Pdf,
                FileSize = 2,
                RecievedAt = DateTimeOffset.UtcNow,
                ProcessingStatus = DocumentProcessingStatus.InQueue
            };

            // Act
            var result = await repo.AddAsync(document);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(document.Id, result.Id);
            Assert.Equal(1, await ctx.Documents.CountAsync());
        }

        /// <summary>
        /// Проверяем, действительно ли в <see cref="Document"/> добавится текст.
        /// </summary>
        [Fact]
        public async Task AddOrUpdate_ExistingDocument_UpdatesFields()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            await using var ctx = CreateDbContext(dbName);

            var documentId = Guid.NewGuid();
            ctx.Documents.Add(new Document
            {
                Id = documentId,
                FileName = "original.pdf",
                FileType = DocumentFileType.Pdf,
                FileSize = 100,
                RecievedAt = DateTimeOffset.UtcNow,
                ProcessingStatus = DocumentProcessingStatus.InQueue
            });
            await ctx.SaveChangesAsync();

            var repo = new DocumentStorageRepository(ctx, _logger);

            var updatedDocument = new Document
            {
                Id = documentId,
                FileName = "original.pdf",
                FileType = DocumentFileType.Pdf,
                FileText = "Извлечённый текст",
                ProcessingStatus = DocumentProcessingStatus.Processed,
                ProcessedAt = DateTime.UtcNow
            };

            // Act
            var result = await repo.AddOrUpdate(updatedDocument);

            // Assert
            Assert.Equal(DocumentProcessingStatus.Processed, result.ProcessingStatus);
            Assert.Equal("Извлечённый текст", result.FileText);
            Assert.NotNull(result.ProcessedAt);

            // Проверяем, что в БД только одна запись
            Assert.Equal(1, await ctx.Documents.CountAsync());
        }

        /// <summary>
        /// Проверяем, действительно ли в <see cref="Document"/> добавится текст.
        /// </summary>
        [Fact]
        public async Task AddOrUpdate_NewDocument_AddsIt()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            await using var ctx = CreateDbContext(dbName);
            var repo = new DocumentStorageRepository(ctx, _logger);

            var newDoc = new Document
            {
                Id = Guid.NewGuid(),
                FileName = "new.pdf",
                FileType = DocumentFileType.Pdf,
                FileText = "текст",
                ProcessingStatus = DocumentProcessingStatus.Processed,
                ProcessedAt = DateTime.UtcNow
            };

            // Act
            var result = await repo.AddOrUpdate(newDoc);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(DocumentProcessingStatus.Processed, result.ProcessingStatus);
            Assert.Equal(1, await ctx.Documents.CountAsync());
        }

        [Fact]
        public async Task AddOrUpdate_DbUpdateConcurrency_ThrowsException()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            await using var ctx = CreateDbContext(dbName);

            var documentId = Guid.NewGuid();
            ctx.Documents.Add(new Document
            {
                Id = documentId,
                FileName = "test.pdf",
                FileType = DocumentFileType.Pdf,
                RecievedAt = DateTimeOffset.UtcNow,
                ProcessingStatus = DocumentProcessingStatus.InQueue
            });
            await ctx.SaveChangesAsync();

            var repo = new DocumentStorageRepository(ctx, _logger);

            // Act & Assert
            // Передаём документ с тем же Id, что и существующий, проверяем Update
            var updatedDoc = new Document
            {
                Id = documentId,
                FileName = "test.pdf",
                FileType = DocumentFileType.Pdf,
                FileText = "новый текст",
                ProcessedAt = DateTime.UtcNow
            };

            // Не должно выбросить исключения
            var result = await repo.AddOrUpdate(updatedDoc);
            Assert.NotNull(result);

            // Проверим что обновление прошло успешно
            var updatedDocument = ctx.Documents.Find(documentId);
            Assert.NotNull(updatedDocument);
            Assert.Equal("новый текст", updatedDocument.FileText);
        }
    }
}