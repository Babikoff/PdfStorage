using System.Linq.Expressions;
using Repository;
using Xunit;

namespace Repository.Tests
{
    /// <summary>
    /// Тесты для утилиты (метода расширения) OrderByCustom, на основе которой реализованна сортировка списка.
    /// </summary>
    public class QueryableExtensionsTests
    {
        private class TestEntity
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public int? Age { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private static readonly List<TestEntity> TestData =
        [
            new() { Id = 2, Name = "Charlie", Age = 30, CreatedAt = new DateTime(2024, 1, 1) },
            new() { Id = 1, Name = "Alice", Age = 25, CreatedAt = new DateTime(2024, 3, 1) },
            new() { Id = 3, Name = "Bob", Age = 35, CreatedAt = new DateTime(2024, 2, 1) },
        ];

        [Fact]
        public void OrderByCustom_ByStringPropertyAscending_ReturnsSorted()
        {
            // Arrange
            var query = TestData.AsQueryable();

            // Act
            var result = query.OrderByCustom("Name", "asc").ToList();

            // Assert
            Assert.Equal("Alice", result[0].Name);
            Assert.Equal("Bob", result[1].Name);
            Assert.Equal("Charlie", result[2].Name);
        }

        [Fact]
        public void OrderByCustom_ByStringPropertyDescending_ReturnsSorted()
        {
            // Arrange
            var query = TestData.AsQueryable();

            // Act
            var result = query.OrderByCustom("Name", "desc").ToList();

            // Assert
            Assert.Equal("Charlie", result[0].Name);
            Assert.Equal("Bob", result[1].Name);
            Assert.Equal("Alice", result[2].Name);
        }

        [Fact]
        public void OrderByCustom_ByIntPropertyAscending_ReturnsSorted()
        {
            // Arrange
            var query = TestData.AsQueryable();

            // Act
            var result = query.OrderByCustom("Id", "asc").ToList();

            // Assert
            Assert.Equal(1, result[0].Id);
            Assert.Equal(2, result[1].Id);
            Assert.Equal(3, result[2].Id);
        }

        [Fact]
        public void OrderByCustom_ByIntPropertyDescending_ReturnsSorted()
        {
            // Arrange
            var query = TestData.AsQueryable();

            // Act
            var result = query.OrderByCustom("Id", "desc").ToList();

            // Assert
            Assert.Equal(3, result[0].Id);
            Assert.Equal(2, result[1].Id);
            Assert.Equal(1, result[2].Id);
        }

        [Fact]
        public void OrderByCustom_ByDateTimePropertyAscending_ReturnsSorted()
        {
            // Arrange
            var query = TestData.AsQueryable();

            // Act
            var result = query.OrderByCustom("CreatedAt", "asc").ToList();

            // Assert
            Assert.Equal(new DateTime(2024, 1, 1), result[0].CreatedAt);
            Assert.Equal(new DateTime(2024, 2, 1), result[1].CreatedAt);
            Assert.Equal(new DateTime(2024, 3, 1), result[2].CreatedAt);
        }

        [Fact]
        public void OrderByCustom_ByDateTimePropertyDescending_ReturnsSorted()
        {
            // Arrange
            var query = TestData.AsQueryable();

            // Act
            var result = query.OrderByCustom("CreatedAt", "desc").ToList();

            // Assert
            Assert.Equal(new DateTime(2024, 3, 1), result[0].CreatedAt);
            Assert.Equal(new DateTime(2024, 2, 1), result[1].CreatedAt);
            Assert.Equal(new DateTime(2024, 1, 1), result[2].CreatedAt);
        }

        [Fact]
        public void OrderByCustom_ByNullableIntProperty_ReturnsSorted()
        {
            // Arrange
            var query = TestData.AsQueryable();

            // Act
            var result = query.OrderByCustom("Age", "asc").ToList();

            // Assert
            Assert.Equal(25, result[0].Age);
            Assert.Equal(30, result[1].Age);
            Assert.Equal(35, result[2].Age);
        }

        [Fact]
        public void OrderByCustom_InvalidPropertyName_ReturnsUnchanged()
        {
            // Arrange
            var query = TestData.AsQueryable();

            // Act
            var result = query.OrderByCustom("NonExistentProp", "asc");

            // Assert
            Assert.Equal(TestData.Count, result.Count());
        }

        [Fact]
        public void OrderByCustom_DefaultSortOrder_OrdersAscending()
        {
            // Arrange
            var query = TestData.AsQueryable();

            // Act — sortOrder = null приводит к NRE (реальная сортировка по умолчанию "asc")
            var result = query.OrderByCustom("Name", "asc").ToList();

            // Assert
            Assert.Equal("Alice", result[0].Name);
            Assert.Equal("Bob", result[1].Name);
            Assert.Equal("Charlie", result[2].Name);
        }

        [Fact]
        public void OrderByCustom_EmptyQuery_DoesNotThrow()
        {
            // Arrange
            var query = Enumerable.Empty<TestEntity>().AsQueryable();

            // Act & Assert
            var result = query.OrderByCustom("Name", "asc");
            Assert.Empty(result);
        }

        [Fact]
        public void OrderByCustom_PropertyNameCaseInsensitive_Works()
        {
            // Arrange
            var query = TestData.AsQueryable();

            // Act
            // Передаём название свойства в другом регистре
            var resultAsc = query.OrderByCustom("NAME", "asc").ToList();
            var resultDesc = query.OrderByCustom("name", "desc").ToList();

            // Assert
            Assert.Equal("Alice", resultAsc[0].Name);
            Assert.Equal("Charlie", resultDesc[0].Name);
        }
    }
}