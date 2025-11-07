using Firmeza.Web.Interfaces; using Firmeza.Web.Models; using Firmeza.Web.Data; using Microsoft.EntityFrameworkCore; using System.Linq;
namespace Firmeza.Web.Repositories{
public class CustomerRepository:ICustomerRepository{
    private readonly AppDbContext _db; public CustomerRepository(AppDbContext db)=>_db=db;
    public async Task<List<Customer>> ListAsync(string? search=null){ var q=_db.Customers.AsNoTracking().AsQueryable(); if(!string.IsNullOrWhiteSpace(search)) q=q.Where(c=>c.FullName.ToLower().Contains(search.ToLower())); return await q.OrderBy(c=>c.FullName).ToListAsync(); }
    public Task<Customer?> GetAsync(Guid id)=>_db.Customers.FirstOrDefaultAsync(c=>c.Id==id);
    public async Task CreateAsync(Customer c){_db.Customers.Add(c); await _db.SaveChangesAsync();}
    public async Task UpdateAsync(Customer c){_db.Customers.Update(c); await _db.SaveChangesAsync();}
    public async Task DeleteAsync(Guid id){var e=await _db.Customers.FindAsync(id); if(e!=null){_db.Customers.Remove(e); await _db.SaveChangesAsync();}}
}}