using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol;
using Pustok.Areas.Admin.ViewModels;
using Pustok.DAL;
using Pustok.Helpers;
using Pustok.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Pustok.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BookController : Controller
    {
        private readonly PustokDbContext _context;
        private readonly IWebHostEnvironment _env;

        public BookController(PustokDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }
        public IActionResult Index(int page = 1)
        {

            var query = _context.Books
                .Include(x => x.Author)
                .Include(x => x.BookImages)
                .Include(x => x.Genre);

            var model = PaginatedList<Book>.Create(query, page, 10);

            return View(model);
        }

        public IActionResult Create()
        {
            ViewBag.Genres = _context.Genres.ToList();
            ViewBag.Authors = _context.Authors.ToList();
            return View();
        }
        [HttpPost]
        public IActionResult Create(Book book)
        {
            if (book.PosterImg == null)
                ModelState.AddModelError("PosterImg", "This Field Is Required");
            if (book.HoverImg == null)
                ModelState.AddModelError("HoverImg", "This Field Is Required");

            if (!ModelState.IsValid)
            {
                ViewBag.Genres = _context.Genres.ToList();
                ViewBag.Authors = _context.Authors.ToList();
                return View();
            }

            BookImage bookPoster = new BookImage
            {
                PosterStatus = true,
                Image = FileManager.Save(book.PosterImg, _env.WebRootPath, "Uploads/Books", 100)
            };
            book.BookImages.Add(bookPoster);

            BookImage bookHover = new BookImage
            {
                PosterStatus = false,
                Image = FileManager.Save(book.HoverImg, _env.WebRootPath, "Uploads/Books", 100)
            };
            book.BookImages.Add(bookHover);


            if (book.ImageFiles != null)
            {
                foreach (var img in book.ImageFiles)
                {
                    BookImage bookImage = new BookImage
                    {
                        PosterStatus = null,
                        Image = FileManager.Save(img, _env.WebRootPath, "Uploads/Books", 100)
                    };
                    book.BookImages.Add(bookImage);
                }
            }

            book.CreatedAt = DateTime.UtcNow.AddHours(4);
            book.ModifiedAt = DateTime.UtcNow.AddHours(4);

            _context.Books.Add(book);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
        public IActionResult Edit(int id)
        {
            var model = _context.Books.Include(x => x.Author).Include(x => x.BookImages).Include(x => x.Genre).FirstOrDefault(x => x.Id == id);
            ViewBag.Genres = _context.Genres.ToList();
            ViewBag.Authors = _context.Authors.ToList();
            return View(model);
        }
        [HttpPost]
        public IActionResult Edit(Book book)
        {
            var existBook = _context.Books.Include(x => x.Author).Include(x => x.BookImages).Include(x => x.Genre).FirstOrDefault(x => x.Id == book.Id);
            if (existBook == null)
                return RedirectToAction("Error", "DashBoard");

            if (!ModelState.IsValid)
            {
                ViewBag.Genres = _context.Genres.ToList();
                ViewBag.Authors = _context.Authors.ToList();
                return View(existBook);
            }



            if (book.PosterImg != null)
            {
                BookImage BookImage = existBook.BookImages.FirstOrDefault(x => x.PosterStatus == true);
                if (BookImage != null)
                {
                    FileManager.Delete(_env.WebRootPath, "Uploads/Books", BookImage.Image);
                    BookImage.Image = FileManager.Save(book.PosterImg, _env.WebRootPath, "Uploads/Books", 100);
                }
                else
                    return RedirectToAction("Error", "Dashboard");

            }

            if (book.HoverImg != null)
            {
                BookImage BookImage = existBook.BookImages.FirstOrDefault(x => x.PosterStatus == false);
                if (BookImage != null)
                {
                    FileManager.Delete(_env.WebRootPath, "Uploads/Books", BookImage.Image);
                    BookImage.Image = FileManager.Save(book.HoverImg, _env.WebRootPath, "Uploads/Books", 100);
                }
                else
                    return RedirectToAction("Error", "Dashboard");

            }

            if (book.ImageFiles != null)
            {
                foreach (var img in book.ImageFiles)
                {
                    BookImage bookImage = new BookImage
                    {
                        Image = FileManager.Save(img, _env.WebRootPath, "Uploads/Books", 100)
                    };
                    existBook.BookImages.Add(bookImage);
                }
            }

            List<BookImage> removedBkImgs = existBook.BookImages.FindAll(x => x.PosterStatus == null && !book.BookImageIds.Contains(x.Id));
            
            foreach (var file in removedBkImgs)
            {
                FileManager.Delete(_env.WebRootPath, "Uploads/Books", file.Image);
            }
            existBook.BookImages.RemoveAll(x => removedBkImgs.Contains(x));



            existBook.GenreId = book.GenreId;
            existBook.AuthorId = book.AuthorId;
            existBook.Name = book.Name;
            existBook.SalePrice = book.SalePrice;
            existBook.DiscountPercent = book.DiscountPercent;
            existBook.CostPrice = book.CostPrice;
            existBook.IsNew = book.IsNew;
            existBook.IsSpecial = book.IsSpecial;
            existBook.StockStatus = book.StockStatus;
            existBook.ModifiedAt = DateTime.UtcNow.AddHours(4);


            _context.SaveChanges();

            return RedirectToAction("Index");
        }
      
    }
}

