using Microsoft.EntityFrameworkCore;

namespace DocumentStorageWebApi
{
    public class DbInitializer
    {
        public static void Initialize(DbContext context)
        {
            context.Database.Migrate();
        }
    }
}
