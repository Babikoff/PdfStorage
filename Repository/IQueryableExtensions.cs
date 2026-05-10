using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Repository
{
    public static class IQueryableExtensions
    {
        /// <summary>
        /// Универсальная сортировка IQueryable по имени свойства и направлению сортировки.
        /// 
        /// Метод динамически строит Expression-дерево для вызова OrderBy или OrderByDescending
        /// на основе строкового имени свойства и направления сортировки.
        /// Это позволяет сортировать сущности по любому свойству без жёстко закодированных лямбд.
        /// 
        /// Логика работы:
        /// 1. Получает тип сущности TEntity.
        /// 2. Создаёт параметр выражения (ParameterExpression), представляющий входной элемент (например, t => ...).
        /// 3. Через рефлексию получает свойство сущности по имени sortBy.
        /// 4. Строит MemberExpression — обращение к свойству у параметра (например, t.PropertyName).
        /// 5. Оборачивает MemberExpression в лямбда-выражение.
        /// 6. Вызывает статический метод Queryable.OrderBy / Queryable.OrderByDescending через Expression.Call,
        ///    передавая исходный Expression запроса и скомпилированную лямбду в кавычках (Quote).
        /// 7. Выполняет полученное Expression через провайдер IQueryProvider и возвращает новый IQueryable.
        /// </summary>
        /// <typeparam name="TEntity">Тип сортируемых сущностей</typeparam>
        /// <param name="items">Исходный IQueryable для сортировки</param>
        /// <param name="sortBy">Имя свойства сущности, по которому выполняется сортировка (регистронезависимо)</param>
        /// <param name="sortOrder">Направление сортировки: "asc" — по возрастанию, "desc" — по убыванию</param>
        /// <returns>IQueryable с применённой сортировкой</returns>
        public static IQueryable<TEntity> OrderByCustom<TEntity>(this IQueryable<TEntity> items, string sortBy, string sortOrder)
        {
            var type = typeof(TEntity);
            var parameterExpression = Expression.Parameter(type, "t");
            var property = type.GetProperty(sortBy, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property == null)
                return items;

            var memberExpression = Expression.MakeMemberAccess(parameterExpression, property);
            var lambda = Expression.Lambda(memberExpression, parameterExpression);
            var result = Expression.Call(
                typeof(Queryable),
                sortOrder.ToLower() == "desc" ? "OrderByDescending" : "OrderBy",
                new Type[] { type, property.PropertyType },
                items.Expression,
                Expression.Quote(lambda));

            return items.Provider.CreateQuery<TEntity>(result);
        }

    }
}