using Microsoft.AspNetCore.Mvc;
using PBL3_DUTLibrary.Models;
using PBL3_DUTLibrary.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using PBL3_DUTLibrary.ViewModels;

namespace PBL3_DUTLibrary.Controllers
{
    public class AdminBooksController : Controller
    {
        private readonly LibraryContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminBooksController(LibraryContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        private async Task LoadGenresToViewBag()
        {
            ViewBag.AllGenres = await _context.Genres
                                            .Select(g => g.Genre1)
                                            .Distinct()
                                            .OrderBy(name => name)
                                            .ToListAsync();
        }

        // Hiển thị danh sách sách với phân trang, tìm kiếm và sắp xếp
        public async Task<IActionResult> Index(string? searchQuery, string? sortOrder, int page = 1, int pageSize = 10)
        {
            // --- Handle Sorting ---
            ViewBag.CurrentSort = sortOrder; // Pass current sort order to view
            ViewBag.IdSortParm = string.IsNullOrEmpty(sortOrder) ? "id_desc" : ""; // Default sort or toggle to asc
            ViewBag.TitleSortParm = sortOrder == "title_asc" ? "title_desc" : "title_asc";
            ViewBag.AuthorSortParm = sortOrder == "author_asc" ? "author_desc" : "author_asc";
            // Add more parameters for other columns if needed

            int totalBooks = 0;
            int uniqueBookTitles = 0;
            int availableBooks = 0;
            int borrowedBooks = 0;

            try
            {
                // Calculate overall stats accurately
                long totalAmountSum = await _context.Books.SumAsync(b => (long?)b.Amount ?? 0L); // Sum of all physical copies
                totalBooks = (int)totalAmountSum; // Stays as total physical copies

                // Exclude rejected borrows (Status = 4) from the count of borrowed books
                int actualCurrentlyBorrowed = await _context.Borrows
                    .CountAsync(b => b.ReturnedTime == null && b.Status != 4);

                availableBooks = totalBooks - actualCurrentlyBorrowed; // Based on total physical copies
                borrowedBooks = actualCurrentlyBorrowed;

                if (availableBooks < 0)
                {
                    availableBooks = 0;
                    borrowedBooks = totalBooks; // Cap borrowed at total if inconsistent
                }

                uniqueBookTitles = await _context.Books.CountAsync(); // Count of unique book entries/titles
            }
            catch (Exception ex)
            {
                // Log error if calculation fails
                Console.WriteLine($"Error calculating overall book statistics: {ex.Message}");
                // Assign default values or handle as appropriate
                totalBooks = 0;
                availableBooks = 0;
                borrowedBooks = 0;
                uniqueBookTitles = 0;
            }

            // Pass overall stats via ViewBag
            ViewBag.OverallTotalBooks = totalBooks; // Total physical copies
            ViewBag.OverallUniqueBookTitles = uniqueBookTitles; // <<< NEW STAT: Unique book titles
            ViewBag.OverallAvailableBooks = availableBooks;
            ViewBag.OverallBorrowedBooks = borrowedBooks;

            // Base query including Genres
            IQueryable<Book> booksQuery = _context.Books
                                                .Include(b => b.GenresNavigation)
                                                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(searchQuery))
            {
                booksQuery = booksQuery.Where(b => b.Title.Contains(searchQuery) || b.Author.Contains(searchQuery));
            }

            // --- Apply Sorting ---
            switch (sortOrder)
            {
                case "id_desc":
                    booksQuery = booksQuery.OrderByDescending(b => b.BookId);
                    break;
                case "title_asc":
                    booksQuery = booksQuery.OrderBy(b => b.Title);
                    break;
                case "title_desc":
                    booksQuery = booksQuery.OrderByDescending(b => b.Title);
                    break;
                case "author_asc":
                    booksQuery = booksQuery.OrderBy(b => b.Author);
                    break;
                case "author_desc":
                    booksQuery = booksQuery.OrderByDescending(b => b.Author);
                    break;
                default: // Default sort: BookId ascending
                    booksQuery = booksQuery.OrderBy(b => b.BookId);
                    break;
            }
            // --- End Apply Sorting ---

            // Get total count *after* filtering
            var filteredTotalBooks = await booksQuery.CountAsync();

            // Handle pagination and "All" page size
            List<Book> booksOnPage;
            int actualPageSizeForCalculation = pageSize; // Use pageSize directly unless it's "All"

            if (pageSize == -1) // "All" selected
            {
                actualPageSizeForCalculation = filteredTotalBooks > 0 ? filteredTotalBooks : 1; // Avoid division by zero
                page = 1; // Reset to page 1 when showing all
                booksOnPage = await booksQuery.ToListAsync(); // Fetch all filtered books
            }
            else
            {
                actualPageSizeForCalculation = pageSize <= 0 ? 10 : pageSize; // Ensure valid page size
                                                                              // Apply pagination to the query BEFORE executing ToListAsync
                booksOnPage = await booksQuery
                                      .Skip((page - 1) * actualPageSizeForCalculation)
                                      .Take(actualPageSizeForCalculation)
                                      .ToListAsync(); // Fetch only the books for the current page
            }

            // --- DEBUGGING GENRES (Keep temporarily) ---
            // Console.WriteLine("--- Debugging Book Genres Start ---");
            // foreach(var b_debug in booksOnPage)
            // {
            //     Console.WriteLine($"Book ID: {b_debug.BookId}, Title: {b_debug.Title}");
            //     if (b_debug.GenresNavigation != null && b_debug.GenresNavigation.Any())
            //     {
            //         Console.WriteLine($"  Genres for {b_debug.BookId}: {string.Join(", ", b_debug.GenresNavigation.Select(g => g.Genre1))}");
            //     }
            //     else
            //     {
            //         Console.WriteLine($"  No genres loaded for Book ID: {b_debug.BookId} (b_debug.GenresNavigation is null or empty). Check Include() and data in 'Belong' table.");
            //     }
            // }
            // Console.WriteLine("--- Debugging Book Genres End ---");
            // --- END DEBUGGING GENRES ---


            // Calculate total pages
            int totalPages = (filteredTotalBooks > 0 && pageSize != -1 && actualPageSizeForCalculation > 0)
                                ? (int)Math.Ceiling((double)filteredTotalBooks / actualPageSizeForCalculation)
                                : 1; // At least one page, even if empty or showing all
            page = Math.Clamp(page, 1, totalPages); // Ensure page is within valid range

            // Pass pagination info to View
            ViewBag.FilteredTotalBooks = filteredTotalBooks; // For "Showing X to Y of Z"
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize; // Pass the original pageSize to the view for the selector
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchQuery = searchQuery; // Pass search query back to keep it in the input

            // --- Calculate Available Count for Each Book on Page (Optimized) ---
            var bookIdsOnPage = booksOnPage.Select(b => b.BookId).ToList();

            // Query active borrow counts for the books on the current page efficiently
            var activeBorrowCounts = await _context.Borrows
                .Where(b => b.BookId.HasValue && // Ensure FK is not null
                            bookIdsOnPage.Contains(b.BookId.Value) && // Filter by books on page
                            b.ReturnedTime == null && // Only count active borrows
                            b.Status != 4) // Exclude rejected borrows (Status = 4)
                .GroupBy(b => b.BookId.Value) // Group by BookId
                .Select(g => new { BookId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BookId, x => x.Count); // Create a dictionary for quick lookup

            var bookViewModels = new List<AdminBookListViewModel>();

            // Populate ViewModels using the already fetched 'booksOnPage' list
            foreach (var book in booksOnPage)
            {
                // User wants ViewModel.Available to come from book.Amount
                // and ViewModel.Amount (total) to come from book.TotalAmount
                int availableCountFromBookAmount = (int)(book.Amount ?? 0L);
                int totalAmountFromBookTotalAmount = (int)(book.TotalAmount ?? 0L);

                // Ensure available is not greater than total, if there's any inconsistency
                if (availableCountFromBookAmount > totalAmountFromBookTotalAmount)
                {
                    availableCountFromBookAmount = totalAmountFromBookTotalAmount;
                }
                if (availableCountFromBookAmount < 0)
                {
                    availableCountFromBookAmount = 0; // Should not be negative
                }

                bookViewModels.Add(new AdminBookListViewModel
                {
                    BookId = book.BookId,
                    Title = book.Title,
                    Author = book.Author,
                    TotalAmount = totalAmountFromBookTotalAmount, // ViewModel.TotalAmount gets Book.TotalAmount
                    Image = book.Image,
                    Amount = availableCountFromBookAmount,    // ViewModel.Amount gets Book.Amount (which is available)
                    GenresDisplay = book.GenresNavigation != null && book.GenresNavigation.Any()
                                    ? string.Join(", ", book.GenresNavigation.Select(g => g.Genre1).OrderBy(name => name))
                                    : "N/A"
                });
            }
            // --- End Calculating Available Count ---

            await LoadGenresToViewBag(); // Load genres for the modal dropdown
            return View(bookViewModels); // Pass the list of ViewModels to the View
        }

        // Lấy thông tin chi tiết sách qua AJAX
        [HttpGet]
        public async Task<IActionResult> GetBookDetails(long bookId)
        {
            var book = await _context.Books
                .Include(b => b.GenresNavigation)
                .FirstOrDefaultAsync(b => b.BookId == bookId);

            if (book == null)
            {
                return Json(new { success = false, message = "Book not found" });
            }

            // User wants totalCopiesForThisBook to come from book.TotalAmount
            // and displayAvailableCopies to come from book.Amount
            long totalCopiesForThisBook = book.TotalAmount ?? 0L;
            long displayAvailableCopies = book.Amount ?? 0L;

            // Ensure available is not greater than total, if there's any inconsistency
            if (displayAvailableCopies > totalCopiesForThisBook)
            {
                displayAvailableCopies = totalCopiesForThisBook;
            }
            if (displayAvailableCopies < 0)
            {
                displayAvailableCopies = 0;
            }

            // Lấy lịch sử mượn sách
            var borrowHistory = await _context.Borrows
                .Where(b => b.BookId == bookId)
                .Include(b => b.User) // Include user information
                .OrderByDescending(b => b.Time) // Sắp xếp theo thời gian mượn giảm dần (mới nhất lên đầu)
                .Select(b => new
                {
                    borrowDate = b.Time.ToString("MMM dd, yyyy"),
                    returnDate = b.ReturnedTime.HasValue ? b.ReturnedTime.Value.ToString("MMM dd, yyyy") : null,
                    isReturned = b.ReturnedTime.HasValue,
                    isRejected = b.Status == 4,  // Status 4 is Rejected
                    userName = b.User != null ? b.User.Username : "Unknown User",
                    userImage = b.User != null ? b.User.Image : null
                })
                .ToListAsync();

            // Changed to return genre names (strings) instead of IDs
            var genreNames = book.GenresNavigation?.Select(g => g.Genre1).OrderBy(name => name).ToList() ?? new List<string>();
            // selectedGenreNames is no longer applicable, use selectedGenreNames for the view

            return Json(new
            {
                success = true,
                bookId = book.BookId,
                title = book.Title,
                author = book.Author,
                amount = totalCopiesForThisBook,
                available = displayAvailableCopies,
                description = book.Description,
                image = book.Image,
                borrowHistory = borrowHistory,
                // Pass selectedGenreNames (list of strings) instead of selectedGenreIds
                selectedGenreNames = genreNames, // For Edit modal pre-selection
                genreNames = genreNames // For View Details modal display (already correct)
            });
        }

        // Tạo mới sách
        [HttpPost]
        public async Task<IActionResult> Create(CreateBookViewModel viewModel, IFormFile BookImage)
        {
            // Check validation based on CreateBookViewModel attributes
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage).ToArray()
                    );
                return Json(new { success = false, message = "Validation failed", errors = errors });
            }

            // --- Check for Duplicate Title ---
            bool titleExists = await _context.Books.AnyAsync(b => b.Title.ToLower() == viewModel.Title.ToLower());
            if (titleExists)
            {
                ModelState.AddModelError("Title", "A book with this title already exists.");
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                return Json(new { success = false, message = "Validation failed", errors = errors });
            }
            // --- End Check for Duplicate Title ---

            try
            {
                // --- Calculate Next BookId ---
                long nextBookId = 1;
                if (await _context.Books.AnyAsync())
                {
                    nextBookId = await _context.Books.MaxAsync(b => b.BookId) + 1;
                }
                // --- End Calculate Next BookId ---

                var newBook = new Book
                {
                    BookId = nextBookId,
                    Title = viewModel.Title,
                    Author = viewModel.Author,
                    Description = viewModel.Description,
                    Amount = viewModel.Amount,     // Amount ban đầu = TotalAmount (vì chưa có ai mượn)
                    TotalAmount = viewModel.Amount // Thêm TotalAmount
                    // Image will be set below
                    // GenresNavigation will be populated later
                };

                // Handle image upload
                if (BookImage != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/books");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + BookImage.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    if (!Directory.Exists(uploadsFolder)) { Directory.CreateDirectory(uploadsFolder); }

                    using (var fileStream = new FileStream(filePath, FileMode.Create)) { await BookImage.CopyToAsync(fileStream); }
                    newBook.Image = "/uploads/books/" + uniqueFileName;
                }

                // Add the new Book to the context (but don't save yet)
                _context.Books.Add(newBook);
                // It's generally better to save all related changes together if possible,
                // but we need the BookId first for Genres and BookCopies.
                await _context.SaveChangesAsync(); // Save book first to get its ID (and ensure BookId is available)

                // --- Process Selected Genres (Using Genre entity and 'genre' table) ---
                Console.WriteLine("--- Received Genres for Create ---"); // DEBUG LOG
                if (viewModel.SelectedGenreNames != null && viewModel.SelectedGenreNames.Any())
                {
                    Console.WriteLine($"Raw SelectedGenreNames count: {viewModel.SelectedGenreNames.Count}"); // DEBUG LOG
                    foreach (var genreName in viewModel.SelectedGenreNames)
                    {
                        Console.WriteLine($"Processing raw genre name: '{genreName}'"); // DEBUG LOG
                        if (!string.IsNullOrWhiteSpace(genreName))
                        {
                            var trimmedGenreName = genreName.Trim();
                            Console.WriteLine($"  -> Attempting to add trimmed genre: '{trimmedGenreName}' for BookId: {newBook.BookId}"); // DEBUG LOG
                            var newGenreEntry = new Genre
                            {
                                BookId = newBook.BookId,
                                Genre1 = trimmedGenreName
                            };
                            _context.Genres.Add(newGenreEntry);
                        }
                        else
                        {
                            Console.WriteLine("  -> Skipping null or whitespace genre name."); // DEBUG LOG
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No selected genres received or list is empty."); // DEBUG LOG
                }
                Console.WriteLine("--- Finished Processing Genres for Create ---"); // DEBUG LOG
                // --- End Process Selected Genres ---

                // --- Calculate starting BookCopyId ---
                int maxOverallBookCopyId = 0; // Default if table is empty
                if (await _context.BookCopies.AnyAsync())
                {
                    maxOverallBookCopyId = await _context.BookCopies.MaxAsync(bc => bc.BookCopyId);
                }
                // --- End Calculate starting BookCopyId ---

                // Create BookCopy instances
                if (newBook.Amount.HasValue)
                {
                    for (int i = 0; i < newBook.Amount.Value; i++)
                    {
                        int nextBookCopyId = maxOverallBookCopyId + 1 + i;
                        var bookCopy = new BookCopy
                        {
                            BookCopyId = nextBookCopyId,
                            BookId = newBook.BookId,
                            Status = "Available"
                        };
                        _context.BookCopies.Add(bookCopy);
                    }
                }

                // Save Genres and BookCopies (and potentially the book again if it was modified)
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Log the exception and inner exception for debugging
                Console.WriteLine($"Error Creating Book: {ex.Message}");
                if (ex.InnerException != null) { Console.WriteLine($"Inner Exception: {ex.InnerException.Message}"); }
                return Json(new { success = false, message = "An error occurred while saving the book. Please check logs for details." });
            }
        }

        // Sửa sách
        [HttpPost]
        public async Task<IActionResult> Edit(EditBookViewModel viewModel, IFormFile BookImage) // BookImage is optional
        {
            ModelState.Remove("BookImage"); // Remove validation for BookImage if not required on edit

            // Check validation based on EditBookViewModel attributes
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage).ToArray()
                    );
                return Json(new { success = false, message = "Validation failed", errors = errors });
            }

            // --- Check for Duplicate Title (for a DIFFERENT book) ---
            bool titleExistsOnOtherBook = await _context.Books
                                      .AnyAsync(b => b.Title.ToLower() == viewModel.Title.ToLower()
                                                   && b.BookId != viewModel.BookId);
            if (titleExistsOnOtherBook)
            {
                ModelState.AddModelError("Title", "Another book with this title already exists.");
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                return Json(new { success = false, message = "Validation failed", errors = errors });
            }
            // --- End Check for Duplicate Title ---

            try
            {
                // Fetch the existing book from DB, including copies and CURRENT genres
                var existingBook = await _context.Books
                    .Include(b => b.BookCopies)
                    .Include(b => b.GenresNavigation) // Include current Genre entities
                    .FirstOrDefaultAsync(b => b.BookId == viewModel.BookId);

                if (existingBook == null)
                {
                    return Json(new { success = false, message = "Book not found" });
                }

                // Kiểm tra xem sách có đang được mượn không và người dùng có đang cố gắng thay đổi thông tin khác ngoài số lượng không
                int currentlyBorrowedCount = await _context.Borrows
                    .CountAsync(b => b.BookId == existingBook.BookId && b.ReturnedTime == null && b.Status != 4);

                if (currentlyBorrowedCount > 0)
                {
                    // Nếu cố gắng thay đổi thông tin quan trọng (title, author) trong khi sách đang được mượn
                    if (existingBook.Title != viewModel.Title || existingBook.Author != viewModel.Author)
                    {
                        return Json(new { success = false, message = $"Cannot modify book title or author while {currentlyBorrowedCount} copies are currently borrowed. You can only change the number of copies." });
                    }

                    // Chỉ cho phép chỉnh sửa số lượng và mô tả
                    existingBook.Description = viewModel.Description;
                }
                else
                {
                    // Nếu không có ai đang mượn, cho phép chỉnh sửa mọi thông tin
                    existingBook.Title = viewModel.Title;
                    existingBook.Author = viewModel.Author;
                    existingBook.Description = viewModel.Description;
                }

                // Handle image update (only if a new image is provided)
                if (BookImage != null && BookImage.Length > 0)
                {
                    // Nếu sách đang được mượn, không cho phép thay đổi hình ảnh
                    if (currentlyBorrowedCount > 0)
                    {
                        return Json(new { success = false, message = $"Cannot change book cover while {currentlyBorrowedCount} copies are currently borrowed. You can only change the number of copies." });
                    }

                    // Delete old image (if not default)
                    if (!string.IsNullOrEmpty(existingBook.Image) && !existingBook.Image.Contains("default-book.png"))
                    {
                        var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingBook.Image.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath)) { System.IO.File.Delete(oldImagePath); }
                    }
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/books");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + BookImage.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    if (!Directory.Exists(uploadsFolder)) { Directory.CreateDirectory(uploadsFolder); }
                    using (var fileStream = new FileStream(filePath, FileMode.Create)) { await BookImage.CopyToAsync(fileStream); }
                    existingBook.Image = "/uploads/books/" + uniqueFileName;
                }

                // Calculate amount changes
                int oldTotalAmount = (int)(existingBook.TotalAmount ?? existingBook.BookCopies.Count); // Use TotalAmount if available, otherwise count BookCopies
                int newTotalAmount = (int)(viewModel.Amount ?? 0); // Form's Amount field represents total copies

                // --- Check borrow constraint before changing amount ---
                if (newTotalAmount < oldTotalAmount)
                {
                    // Kiểm tra số sách đang được mượn từ bảng Borrows
                    int currentlyBorrowedCountForThisBook = await _context.Borrows
                        .CountAsync(b => b.BookId == existingBook.BookId && b.ReturnedTime == null && b.Status != 4);

                    if (newTotalAmount < currentlyBorrowedCountForThisBook)
                    {
                        return Json(new { success = false, message = $"Cannot reduce copies to {newTotalAmount}. Currently, {currentlyBorrowedCountForThisBook} copies are borrowed." });
                    }
                }
                // --- End Check ---

                // Cập nhật TotalAmount - là tổng số sách
                existingBook.TotalAmount = newTotalAmount;

                // Sử dụng giá trị currentlyBorrowedCount đã tính ở trên thay vì khai báo lại
                // Cập nhật Amount - số sách có thể cho mượn
                existingBook.Amount = newTotalAmount - currentlyBorrowedCount;

                // Adjust BookCopies
                if (newTotalAmount > oldTotalAmount)
                {
                    // ... (existing logic for adding BookCopies, including ID calculation) ...
                    int maxOverallBookCopyId = 0;
                    if (await _context.BookCopies.AnyAsync()) { maxOverallBookCopyId = await _context.BookCopies.MaxAsync(bc => bc.BookCopyId); }
                    for (int i = 0; i < newTotalAmount - oldTotalAmount; i++)
                    {
                        int nextBookCopyId = maxOverallBookCopyId + 1 + i;
                        var bookCopy = new BookCopy { BookCopyId = nextBookCopyId, BookId = existingBook.BookId, Status = "Available" };
                        _context.BookCopies.Add(bookCopy);
                    }
                }
                else if (newTotalAmount < oldTotalAmount)
                {
                    // ... (existing logic for removing available BookCopies) ...
                    var copiesToRemoveCount = oldTotalAmount - newTotalAmount;
                    var availableCopiesToRemove = existingBook.BookCopies
                        .Where(bc => bc.Status == "Available") // Ensure status is checked if applicable for removal
                        .OrderBy(bc => bc.BookCopyId) // Consistent removal order
                        .Take(copiesToRemoveCount)
                        .ToList();
                    _context.BookCopies.RemoveRange(availableCopiesToRemove);
                    // Log warning if not enough available copies were found (though borrow check should prevent major issues)
                    if (availableCopiesToRemove.Count < copiesToRemoveCount)
                    {
                        Console.WriteLine($"Warning: Tried to remove {copiesToRemoveCount} copies, but only found {availableCopiesToRemove.Count} available for BookId {existingBook.BookId}.");
                    }
                }

                // --- Update Genres (Using Genre entity and 'genre' table) ---
                Console.WriteLine("--- Starting Genre Update for Edit --- "); // DEBUG LOG
                var currentGenreNames = existingBook.GenresNavigation
                                                    .Select(g => g.Genre1)
                                                    .ToList();
                Console.WriteLine($"Current genre names in DB: [{string.Join(", ", currentGenreNames)}]"); // DEBUG LOG

                var submittedGenreNames = viewModel.SelectedGenreNames?
                                                 .Where(gn => !string.IsNullOrWhiteSpace(gn))
                                                 .Select(gn => gn.Trim())
                                                 .Distinct()
                                                 .ToList()
                                                 ?? new List<string>();
                Console.WriteLine($"Submitted genre names from form: [{string.Join(", ", submittedGenreNames)}]"); // DEBUG LOG

                // Kiểm tra xem có đang cố gắng thay đổi genres khi sách đang được mượn không
                if (currentlyBorrowedCount > 0)
                {
                    // Kiểm tra xem genres có thay đổi không
                    bool genresChanged = !currentGenreNames.OrderBy(g => g).SequenceEqual(submittedGenreNames.OrderBy(g => g));

                    if (genresChanged)
                    {
                        return Json(new { success = false, message = $"Cannot change book genres while {currentlyBorrowedCount} copies are currently borrowed. You can only change the number of copies." });
                    }

                    // Nếu không có thay đổi genres, bỏ qua phần cập nhật genres
                    Console.WriteLine("Genres not changed or book is borrowed - skipping genre updates");
                }
                else
                {
                    // Genres to Remove: Find names currently associated that are NOT in the submitted list
                    var namesToRemove = currentGenreNames.Except(submittedGenreNames).ToList();
                    Console.WriteLine($"Genre names to remove: [{string.Join(", ", namesToRemove)}]"); // DEBUG LOG
                    if (namesToRemove.Any())
                    {
                        var genresToRemove = existingBook.GenresNavigation
                                                         .Where(g => namesToRemove.Contains(g.Genre1))
                                                         .ToList();
                        Console.WriteLine($"Removing {genresToRemove.Count} Genre entities."); // DEBUG LOG
                        _context.Genres.RemoveRange(genresToRemove);
                    }

                    // Genres to Add: Find names in the submitted list that are NOT currently associated
                    var namesToAdd = submittedGenreNames.Except(currentGenreNames).ToList();
                    Console.WriteLine($"Genre names to add: [{string.Join(", ", namesToAdd)}]"); // DEBUG LOG
                    if (namesToAdd.Any())
                    {
                        Console.WriteLine($"Adding {namesToAdd.Count} new Genre entities."); // DEBUG LOG
                        foreach (var nameToAdd in namesToAdd)
                        {
                            var newGenre = new Genre
                            {
                                BookId = existingBook.BookId,
                                Genre1 = nameToAdd
                            };
                            _context.Genres.Add(newGenre);
                        }
                    }
                }
                Console.WriteLine("--- Finished Genre Update Logic for Edit --- "); // DEBUG LOG
                // --- End Update Genres ---

                await _context.SaveChangesAsync(); // Save all changes (Book, BookCopies, Genres)
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Editing Book (ID: {viewModel.BookId}): {ex.Message}");
                if (ex.InnerException != null) { Console.WriteLine($"Inner Exception: {ex.InnerException.Message}"); }
                return Json(new { success = false, message = "An error occurred while saving changes. Please check logs for details." });
            }
        }

        // Xóa sách
        [HttpPost]
        public async Task<IActionResult> Delete(long bookId)
        {
            try
            {
                // Kiểm tra trực tiếp từ bảng Borrows thay vì dựa vào trạng thái của BookCopies
                int activeBorrows = await _context.Borrows
                    .CountAsync(b => b.BookId == bookId && b.ReturnedTime == null && b.Status != 4);

                if (activeBorrows > 0)
                {
                    return Json(new { success = false, message = $"Cannot delete this book. {activeBorrows} copies are currently borrowed." });
                }

                var book = await _context.Books
                    .Include(b => b.BookCopies)
                    .FirstOrDefaultAsync(b => b.BookId == bookId);

                if (book == null)
                {
                    return Json(new { success = false, message = "Book not found" });
                }

                if (!string.IsNullOrEmpty(book.Image) && !book.Image.Contains("default-book.png"))
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, book.Image.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.BookCopies.RemoveRange(book.BookCopies);
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetManageableGenres()
        {
            try
            {
                var allGenreNames = await _context.Genres
                                            .Select(g => g.Genre1)
                                            .Where(gName => !string.IsNullOrEmpty(gName)) // Ensure we don't get null/empty strings
                                            .Distinct()
                                            .OrderBy(name => name)
                                            .ToListAsync();
                return Json(new { success = true, genres = allGenreNames });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching manageable genres: {ex.Message}");
                return Json(new { success = false, message = "Could not load genres." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGenreByName(string genreName)
        {
            if (string.IsNullOrWhiteSpace(genreName))
            {
                return Json(new { success = false, message = "Genre name cannot be empty." });
            }

            try
            {
                // Find all genre entries matching the name
                var genreEntriesToRemove = await _context.Genres
                                                    .Where(g => g.Genre1 == genreName)
                                                    .ToListAsync();

                if (!genreEntriesToRemove.Any())
                {
                    return Json(new { success = false, message = $"Genre '{genreName}' not found or already removed." });
                }

                _context.Genres.RemoveRange(genreEntriesToRemove);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Genre '{genreName}' has been removed from all books." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting genre '{genreName}': {ex.Message}");
                return Json(new { success = false, message = $"An error occurred while trying to delete genre '{genreName}'. Check logs." });
            }
        }
    }
}

