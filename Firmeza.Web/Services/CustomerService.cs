using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firmeza.Web.Interfaces;
using Firmeza.Web.Models;
namespace Firmeza.Web.Services{
 public class CustomerService{
  private readonly ICustomerRepository _repo; private readonly IStringSanitizer _san;
  public CustomerService(ICustomerRepository repo,IStringSanitizer san){_repo=repo;_san=san;}
  public Task<List<Customer>> ListAsync(string? q=null, string? ownerId=null)=>_repo.ListAsync(q, ownerId);
  public Task<Customer?> GetAsync(Guid id, string? ownerId=null)=>_repo.GetAsync(id, ownerId);
  public async Task CreateAsync(Customer c){c.FullName=_san.Clean(c.FullName); await _repo.CreateAsync(c);}
  public async Task UpdateAsync(Customer c, string? ownerId=null){c.FullName=_san.Clean(c.FullName); await _repo.UpdateAsync(c, ownerId);}
  public Task DeleteAsync(Guid id, string? ownerId=null)=>_repo.DeleteAsync(id, ownerId);
 } }
