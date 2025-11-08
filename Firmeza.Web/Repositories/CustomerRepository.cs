using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firmeza.Web.Interfaces;
using Firmeza.Web.Models;
using Firmeza.Web.Data;
using Microsoft.EntityFrameworkCore;
namespace Firmeza.Web.Repositories{
public class CustomerRepository:ICustomerRepository{
    private readonly AppDbContext _db; public CustomerRepository(AppDbContext db)=>_db=db;
    public async Task<List<Customer>> ListAsync(string? search=null, string? ownerId=null){
        var q=_db.Customers.AsNoTracking().AsQueryable();
        if(!string.IsNullOrWhiteSpace(ownerId)) q=q.Where(c=>c.CreatedByUserId==ownerId);
        if(!string.IsNullOrWhiteSpace(search)) q=q.Where(c=>c.FullName.ToLower().Contains(search.ToLower()));
        return await q.OrderBy(c=>c.FullName).ToListAsync();
    }
    public Task<Customer?> GetAsync(Guid id, string? ownerId=null)=>_db.Customers.FirstOrDefaultAsync(c=>c.Id==id && (ownerId==null || c.CreatedByUserId==ownerId));
    public async Task CreateAsync(Customer c){_db.Customers.Add(c); await _db.SaveChangesAsync();}
    public async Task UpdateAsync(Customer c, string? ownerId=null){
        var existing=await _db.Customers.FirstOrDefaultAsync(x=>x.Id==c.Id && (ownerId==null || x.CreatedByUserId==ownerId));
        if(existing==null) return;
        existing.FullName=c.FullName; existing.Email=c.Email; existing.Phone=c.Phone;
        await _db.SaveChangesAsync();
    }
    public async Task DeleteAsync(Guid id, string? ownerId=null){
        var e=await _db.Customers.FirstOrDefaultAsync(x=>x.Id==id && (ownerId==null || x.CreatedByUserId==ownerId));
        if(e!=null){_db.Customers.Remove(e); await _db.SaveChangesAsync();}
    }
}}
