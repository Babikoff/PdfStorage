using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace PdfStorageWebApi
{
    public class DbInitializer
    {
        public static void Initialize(DbContext context)
        {
            context.Database.EnsureCreated();
            context.SaveChanges();
        }
    }
}