using ebooks_dotnet7_api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<DataContext>(opt => opt.UseInMemoryDatabase("ebooks"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
var app = builder.Build();

var ebooks = app.MapGroup("api/ebook");

// TODO: Add more routes
ebooks.MapPost("/", CreateEBookAsync);
ebooks.MapGet("/genre={genre}", GetBooks);
ebooks.MapPut("/{id}",UpdateBook);
ebooks.MapPut("/{id}/change-availability", ChangeAvailability);
ebooks.MapPut("/{id}/increment-stock", IncrementStock);
ebooks.MapPost("/purchase",ShopBook);
ebooks.MapDelete("/{id}", DeleteBook);



app.Run();

// TODO: Add more methods
async Task<IResult> CreateEBookAsync([FromBody] EBook book,DataContext context)
{
    var existingBook =  await context.EBooks.Where(b => b.Author == book.Author || b.Title == book.Author).FirstOrDefaultAsync();
    if(existingBook != null){
        return TypedResults.BadRequest("El libro ya existe");
    }
    context.EBooks.Add(book);
    context.SaveChanges();
    return TypedResults.Created($"/created/{book.Id}",book);
}

async Task<IResult> GetBooks(string genre, DataContext context)
{
    var books = await context.EBooks.Where(b => b.Genre == genre).ToListAsync();
    return TypedResults.Ok(books);
}


async Task<IResult> UpdateBook(int id, [FromBody] UpdateBookDto updateBookDto, DataContext context)
{
    var existingBook = await context.EBooks.Where(i => i.Id == id).FirstOrDefaultAsync();
    if(existingBook == null){
        return TypedResults.BadRequest("El libro no existe");
    }
    
    existingBook.Title = updateBookDto.Title;
    existingBook.Author = updateBookDto.Author;
    existingBook.Genre = updateBookDto.Genre;
    existingBook.Format = updateBookDto.Format;
    existingBook.Price = updateBookDto.Price;
    context.SaveChanges();
    return TypedResults.Ok();

}

async Task<IResult> ChangeAvailability(int id, DataContext context)
{
    var existingBook = await context.EBooks.Where(b => b.Id == id).FirstOrDefaultAsync();
    if(existingBook == null){
        return TypedResults.BadRequest("El libro no existe");
    }
    if(existingBook.IsAvailable){
        existingBook.IsAvailable = false;
    }else{
        existingBook.IsAvailable = true;
    }
    context.SaveChanges();
    return TypedResults.Ok("La disponibilidad del libro ha cambiado");
}

async Task<IResult> IncrementStock(int id, [FromBody] UpdateStockDto updateStock, DataContext context)
{
    var existingBook = await context.EBooks.Where(b => b.Id == id).FirstOrDefaultAsync();
    if(existingBook == null){
        return TypedResults.BadRequest("El libro no existe");
    }
    if(updateStock.Stock <= 0){
        return TypedResults.BadRequest("Necesita ingresar un valor de stock mayor a 0");
    }
    existingBook.Stock = updateStock.Stock;
    context.SaveChanges();
    return TypedResults.Ok("Stock actualizado con exito");
}

async Task<IResult> ShopBook([FromBody] ShopBookDto shopBookDto ,DataContext context)
{
    var existingBook = await context.EBooks.Where(b => b.Id == shopBookDto.Id).FirstOrDefaultAsync();
    if(existingBook == null){
        return TypedResults.BadRequest("El libro no existe");
    }
    if(existingBook.Stock < shopBookDto.Stock){
        return TypedResults.BadRequest("Debe ingresar una cantidad igual o menor al stock del libro");
    }
    var total = existingBook.Price * shopBookDto.Stock;
    if(total != shopBookDto.Price){
        return TypedResults.BadRequest("El precio pagado no concuerda con el total de la compra");
    }
    context.SaveChanges();
    return TypedResults.Ok("La compra fue exitosa");
}


async Task<IResult> DeleteBook(int id, DataContext context)
{
    var existingBook = await context.EBooks.FindAsync(id);
    if(existingBook == null){
        return TypedResults.BadRequest("El libro no existe");
    }
    context.EBooks.Remove(existingBook);
    context.SaveChanges();
    return TypedResults.Ok();
}
