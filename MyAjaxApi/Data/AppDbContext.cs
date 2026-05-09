using Microsoft.EntityFrameworkCore;
using MyAjaxApi.Models;

namespace MyAjaxApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Todo> Todos => Set<Todo>();
}
