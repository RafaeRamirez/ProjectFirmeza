using OfficeOpenXml; using Firmeza.Web.Interfaces; using Firmeza.Web.Models; using System.Collections.Generic; using System.Linq;
namespace Firmeza.Web.Services{
public class ExcelService:IExcelService{
    public async Task<byte[]> ExportProductsAsync(IEnumerable<Product> items){
        ExcelPackage.LicenseContext=LicenseContext.NonCommercial;
        using var pkg=new ExcelPackage();
        var ws=pkg.Workbook.Worksheets.Add("Productos");
        ws.Cells[1,1].Value="Nombre"; ws.Cells[1,2].Value="Precio"; ws.Cells[1,3].Value="Activo";
        int r=2; foreach(var p in items){ ws.Cells[r,1].Value=p.Name; ws.Cells[r,2].Value=p.UnitPrice; ws.Cells[r,3].Value=p.IsActive?"Sí":"No"; r++; }
        return await pkg.GetAsByteArrayAsync();
    }
    public async Task<(List<Product> ok, List<string> errors)> ImportProductsAsync(System.IO.Stream stream){
        ExcelPackage.LicenseContext=LicenseContext.NonCommercial;
        var ok=new List<Product>(); var errors=new List<string>();
        using var pkg=new ExcelPackage(stream); var ws=pkg.Workbook.Worksheets.FirstOrDefault(); if(ws==null){ errors.Add("No worksheet found."); return (ok,errors); }
        int row=2; while(true){ var name=ws.Cells[row,1].GetValue<string>(); if(string.IsNullOrWhiteSpace(name)) break; var price=ws.Cells[row,2].GetValue<decimal>(); var activeText=(ws.Cells[row,3].GetValue<string>()??"Sí").Trim().ToLower(); var active=activeText.StartsWith("s"); ok.Add(new Product{Name=name.Trim(), UnitPrice=price, IsActive=active}); row++; }
        return (ok,errors);
    }
}}