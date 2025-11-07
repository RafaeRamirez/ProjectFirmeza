using Firmeza.Web.Interfaces; using Firmeza.Web.Models; using Firmeza.Web.Data; using Microsoft.EntityFrameworkCore; using System.Linq;
namespace Firmeza.Web.Repositories{
public class SaleRepository:ISaleRepository{
    private readonly AppDbContext _db; public SaleRepository(AppDbContext db)=>_db=db;
    public async Task CreateAsync(Sale sale){_db.Sales.Add(sale); await _db.SaveChangesAsync();}
    public async Task<Sale?> GetAsync(Guid id)=> await _db.Sales.Include(s=>s.Customer).Include(s=>s.Items).ThenInclude(i=>i.Product).FirstOrDefaultAsync(s=>s.Id==id);
    public async Task<List<Sale>> ListAsync()=> await _db.Sales.Include(s=>s.Customer).OrderByDescending(s=>s.CreatedAt).ToListAsync();
}}