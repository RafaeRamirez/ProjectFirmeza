using Firmeza.Web.Interfaces; using Firmeza.Web.Models; using Firmeza.Web.Data; using Microsoft.EntityFrameworkCore;
namespace Firmeza.Web.Repositories{
public class ProductRepository:IProductRepository{
    private readonly AppDbContext _db; public ProductRepository(AppDbContext db)=>_db=db;
    public Task<Product?> GetAsync(Guid id)=>_db.Products.FirstOrDefaultAsync(p=>p.Id==id);
    public async Task<List<Product>> ListAsync()=>await _db.Products.AsNoTracking().OrderBy(p=>p.Name).ToListAsync();
    public async Task CreateAsync(Product p){_db.Products.Add(p); await _db.SaveChangesAsync();}
    public async Task UpdateAsync(Product p){_db.Products.Update(p); await _db.SaveChangesAsync();}
    public async Task DeleteAsync(Guid id){var e=await _db.Products.FindAsync(id); if(e!=null){_db.Products.Remove(e); await _db.SaveChangesAsync();}}
}}