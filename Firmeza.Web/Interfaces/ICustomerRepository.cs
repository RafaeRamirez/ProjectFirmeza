using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firmeza.Web.Models;
namespace Firmeza.Web.Interfaces{
 public interface ICustomerRepository{
  Task<List<Customer>> ListAsync(string? search=null, string? ownerId=null);
  Task<Customer?> GetAsync(Guid id, string? ownerId=null);
  Task<Customer?> GetByEmailAsync(string email, string? ownerId=null);
  Task CreateAsync(Customer c);
  Task UpdateAsync(Customer c, string? ownerId=null);
  Task DeleteAsync(Guid id, string? ownerId=null);
 } }
