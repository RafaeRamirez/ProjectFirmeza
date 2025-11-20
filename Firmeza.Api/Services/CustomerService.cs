using AutoMapper;
using AutoMapper.QueryableExtensions;
using Firmeza.Api.Contracts.Dtos.Customers;
using Firmeza.Api.Contracts.Responses;
using Firmeza.Api.Data;
using Firmeza.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Api.Services;

public class CustomerService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public CustomerService(AppDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public Task<List<CustomerDto>> ListAsync(string? search = null) =>
        BuildQuery(search)
            .OrderBy(c => c.FullName)
            .ProjectTo<CustomerDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

    public Task<CustomerDto?> GetAsync(Guid id) =>
        _db.Customers
            .AsNoTracking()
            .Where(c => c.Id == id)
            .ProjectTo<CustomerDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

    public Task<CustomerDto?> GetByUserAsync(string userId) =>
        _db.Customers
            .AsNoTracking()
            .Where(c => c.CreatedByUserId == userId)
            .ProjectTo<CustomerDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

    public async Task<CustomerDto> CreateAsync(CustomerCreateDto dto, string userId)
    {
        var entity = _mapper.Map<Customer>(dto);
        entity.FullName = dto.FullName.Trim();
        entity.Email = dto.Email?.Trim().ToLowerInvariant();
        entity.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
        entity.CreatedByUserId = userId;

        _db.Customers.Add(entity);
        await _db.SaveChangesAsync();
        return _mapper.Map<CustomerDto>(entity);
    }

    public async Task<bool> UpdateAsync(CustomerUpdateDto dto)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(c => c.Id == dto.Id);
        if (entity is null)
        {
            return false;
        }

        entity.FullName = dto.FullName.Trim();
        entity.Email = dto.Email?.Trim().ToLowerInvariant();
        entity.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> UpdateAllByEmailAsync(string email, string fullName, string? phone)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return 0;
        }

        var normalized = email.Trim().ToLowerInvariant();
        var targets = await _db.Customers
            .Where(c => c.Email != null && c.Email.ToLower() == normalized)
            .ToListAsync();

        if (!targets.Any())
        {
            return 0;
        }

        var cleanName = fullName.Trim();
        var cleanPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        foreach (var entry in targets)
        {
            entry.FullName = cleanName;
            entry.Phone = cleanPhone;
            entry.Email = entry.Email?.Trim();
        }
        await _db.SaveChangesAsync();
        return targets.Count;
    }

    public async Task<CustomerDeleteResultDto> DeleteAsync(Guid id)
    {
        var result = new CustomerDeleteResultDto();
        var entity = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (entity is null)
        {
            result.NotFound = true;
            return result;
        }

        var hasSales = await _db.Sales.AnyAsync(s => s.CustomerId == id);
        if (hasSales)
        {
            result.HasSales = true;
            return result;
        }

        _db.Customers.Remove(entity);
        await _db.SaveChangesAsync();
        result.Removed = true;
        return result;
    }

    private IQueryable<Customer> BuildQuery(string? search)
    {
        var query = _db.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(c => EF.Functions.ILike(c.FullName, term) || (c.Email != null && EF.Functions.ILike(c.Email, term)));
        }

        return query;
    }
}
