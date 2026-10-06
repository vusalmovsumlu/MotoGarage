using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoGarage.Application.DTOs.Product;
using MotoGarage.Application.Interfaces.Services;
using System.Security.Claims;

namespace MotoGarage.API.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(
        IProductService productService)
    {
        _productService = productService;
    }

    // GET:
    // api/products
    // api/products?search=brake&brand=Brembo&minPrice=50&maxPrice=300
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? brand,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice)
    {
        var hasFilter =
            !string.IsNullOrWhiteSpace(search) ||
            !string.IsNullOrWhiteSpace(brand) ||
            minPrice.HasValue ||
            maxPrice.HasValue;

        if (hasFilter)
        {
            var filteredProducts =
                await _productService.SearchAsync(
                    search,
                    brand,
                    minPrice,
                    maxPrice);

            return Ok(filteredProducts);
        }

        var products =
            await _productService.GetAllAsync();

        return Ok(products);
    }

    // GET: api/products/brembo-brake-pad
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(
        string slug)
    {
        var product =
            await _productService.GetBySlugAsync(slug);

        return Ok(product);
    }

    // GET: api/products/store/motoparts-baku
    [HttpGet("store/{storeSlug}")]
    public async Task<IActionResult> GetByStoreSlug(
        string storeSlug)
    {
        var products =
            await _productService.GetByStoreSlugAsync(
                storeSlug);

        return Ok(products);
    }

    // POST: api/products
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateProductDto dto)
    {
        var userId = GetCurrentUserId();

        var product =
            await _productService.CreateAsync(
                userId,
                dto);

        return CreatedAtAction(
            nameof(GetBySlug),
            new { slug = product.Slug },
            product);
    }

    // PUT: api/products/brembo-brake-pad
    [HttpPut("{slug}")]
    public async Task<IActionResult> Update(
        string slug,
        UpdateProductDto dto)
    {
        var userId = GetCurrentUserId();

        await _productService.UpdateAsync(
            slug,
            userId,
            dto);

        return NoContent();
    }

    // PATCH: api/products/brembo-brake-pad/stock
    [HttpPatch("{slug}/stock")]
    public async Task<IActionResult> UpdateStock(
        string slug,
        UpdateStockDto dto)
    {
        var userId = GetCurrentUserId();

        await _productService.UpdateStockAsync(
            slug,
            userId,
            dto);

        return NoContent();
    }

    // DELETE: api/products/brembo-brake-pad
    [HttpDelete("{slug}")]
    public async Task<IActionResult> Delete(
        string slug)
    {
        var userId = GetCurrentUserId();

        await _productService.DeleteAsync(
            slug,
            userId);

        return NoContent();
    }

    private int GetCurrentUserId()
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId) ||
            !int.TryParse(userId, out var id))
        {
            throw new UnauthorizedAccessException(
                "İstifadəçi məlumatı tapılmadı.");
        }

        return id;
    }
}