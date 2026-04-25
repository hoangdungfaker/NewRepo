using Microsoft.AspNetCore.Mvc;
using PBL3_DUTLibrary.Data;
using PBL3_DUTLibrary.Models;
using PBL3_DUTLibrary.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;

namespace PBL3_DUTLibrary.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : Controller
    {
        private readonly LibraryContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        // Define available roles - you might want to manage this more dynamically in a real app
        private readonly List<string> _availableRoles = new List<string> { "Admin", "User" };

        public AdminUsersController(LibraryContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        private void LoadRolesToViewBag()
        {
            ViewBag.AvailableRoles = _availableRoles;
        }

        // GET: AdminUsers
        public async Task<IActionResult> Index(string? searchQuery, string? sortOrder, string? roleFilter, int page = 1, int pageSize = 10)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.UsernameSortParm = string.IsNullOrEmpty(sortOrder) ? "username_desc" : "";
            ViewBag.EmailSortParm = sortOrder == "email_asc" ? "email_desc" : "email_asc";
            ViewBag.RoleSortParm = sortOrder == "role_asc" ? "role_desc" : "role_asc";
            ViewBag.RealNameSortParm = sortOrder == "realname_asc" ? "realname_desc" : "realname_asc";
            ViewBag.CurrentRoleFilter = roleFilter;
            LoadRolesToViewBag(); // For the filter dropdown

            IQueryable<WebUser> usersQuery = _context.WebUsers.AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                usersQuery = usersQuery.Where(u =>
                    (u.Username != null && u.Username.ToLower().Contains(searchQuery.ToLower())) ||
                    (u.Email != null && u.Email.ToLower().Contains(searchQuery.ToLower())) ||
                    (u.RealName != null && u.RealName.ToLower().Contains(searchQuery.ToLower()))
                );
            }

            if (!string.IsNullOrEmpty(roleFilter))
            {
                usersQuery = usersQuery.Where(u => u.Role == roleFilter);
            }

            switch (sortOrder)
            {
                case "username_desc":
                    usersQuery = usersQuery.OrderByDescending(u => u.Username);
                    break;
                case "email_asc":
                    usersQuery = usersQuery.OrderBy(u => u.Email);
                    break;
                case "email_desc":
                    usersQuery = usersQuery.OrderByDescending(u => u.Email);
                    break;
                case "role_asc":
                    usersQuery = usersQuery.OrderBy(u => u.Role);
                    break;
                case "role_desc":
                    usersQuery = usersQuery.OrderByDescending(u => u.Role);
                    break;
                case "realname_asc":
                    usersQuery = usersQuery.OrderBy(u => u.RealName);
                    break;
                case "realname_desc":
                    usersQuery = usersQuery.OrderByDescending(u => u.RealName);
                    break;
                default: // Default sort: Username ascending
                    usersQuery = usersQuery.OrderBy(u => u.Username);
                    break;
            }

            var totalUsers = await usersQuery.CountAsync();
            var usersOnPage = await usersQuery
                                     .Skip((page - 1) * pageSize)
                                     .Take(pageSize)
                                     .ToListAsync();

            // Get the borrow data for all users on this page
            var userIds = usersOnPage.Select(u => u.UserId).ToList();
            var borrowData = await _context.Borrows
                .Where(b => userIds.Contains(b.UserId ?? 0))
                .GroupBy(b => b.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalBorrows = g.Count(),
                    ActiveBorrows = g.Count(b => b.Status != 3 && b.Status != 4), // Exclude both Returned (3) and Rejected (4)
                    LastBorrowDate = g.Max(b => b.Time)
                })
                .ToListAsync();

            var userViewModels = usersOnPage.Select(u => new AdminWebUserListViewModel
            {
                UserId = u.UserId,
                Username = u.Username,
                RealName = u.RealName,
                Sdt = u.Sdt == 0 ? null : u.Sdt, // If Sdt is 0, set it to null
                Email = u.Email,
                Role = u.Role,
                Status = u.Status,
                Image = u.Image,
                // Populate borrow information if available
                BorrowCount = borrowData.FirstOrDefault(b => b.UserId == u.UserId)?.TotalBorrows ?? 0,
                ActiveBorrowCount = borrowData.FirstOrDefault(b => b.UserId == u.UserId)?.ActiveBorrows ?? 0,
                LastBorrowDate = borrowData.FirstOrDefault(b => b.UserId == u.UserId)?.LastBorrowDate
            }).ToList();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);
            ViewBag.SearchQuery = searchQuery;

            // Calculate user stats
            ViewBag.TotalUserCount = await _context.WebUsers.CountAsync();
            ViewBag.ActiveUserCount = await _context.WebUsers.CountAsync(u => u.Status == true);
            ViewBag.InactiveUserCount = ViewBag.TotalUserCount - ViewBag.ActiveUserCount;

            // Enhanced user statistics
            ViewBag.AdminCount = await _context.WebUsers.CountAsync(u => u.Role == "Admin");
            ViewBag.RegularUserCount = ViewBag.TotalUserCount - ViewBag.AdminCount;

            // Users with borrowing activity
            ViewBag.UsersWithActiveBorrows = await _context.Borrows
                .Where(b => b.ReturnedTime == null && b.Status != 4) // Not returned and not rejected
                .Select(b => b.UserId)
                .Distinct()
                .CountAsync();

            // Users with overdue items
            ViewBag.UsersWithOverdueItems = await _context.Borrows
                .Where(b => b.ReturnedTime == null && b.Status != 4 && b.Time.AddDays(b.Deadline) < DateTime.Now)
                .Select(b => b.UserId)
                .Distinct()
                .CountAsync();

            // Most recent users - for potential display in a "newest members" section
            var recentUsersList = await _context.WebUsers
                .OrderByDescending(u => u.UserId) // Assuming higher IDs are more recent
                .Take(5)
                .Select(u => new {
                    UserId = u.UserId,
                    Username = u.Username,
                    RealName = u.RealName,
                    Image = u.Image,
                    Role = u.Role
                })
                .ToListAsync();

            // Convert to List<dynamic> to avoid casting issues in the view
            ViewBag.RecentUsers = recentUsersList.Cast<dynamic>().ToList();

            return View(userViewModels);
        }

        // GET: AdminUsers/GetUserDetails/5
        [HttpGet]
        public async Task<IActionResult> GetUserDetails(int userId)
        {
            var user = await _context.WebUsers.FindAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            // Get user's borrowing history
            var borrows = await _context.Borrows
                .Where(b => b.UserId == userId)
                .Include(b => b.Book)
                .OrderByDescending(b => b.Time) // Order by borrow time descending
                .Take(10) // Limit to most recent 10 borrows
                .ToListAsync();

            // Calculate statistics - with corrections for status definitions
            var totalBorrows = borrows.Count;
            // Only count Pending (0) and Approved (1) without ReturnedTime as active
            var activeBorrows = borrows.Count(b => b.ReturnedTime == null && b.Status != 4); // Exclude Rejected (4) and any with ReturnedTime
            var completedBorrows = borrows.Count(b => b.ReturnedTime != null || b.Status == 4); // Both Returned and Rejected count as completed
            var lateBorrows = borrows.Count(b => b.ReturnedTime == null && b.Status == 2); // Overdue status

            var viewModel = new EditUserViewModel
            {
                UserId = user.UserId,
                Username = user.Username ?? string.Empty,
                RealName = user.RealName,
                Sdt = user.Sdt == 0 ? "" : user.Sdt.ToString("00000000000").TrimStart('0'), // Preserve for formatting
                Email = user.Email ?? string.Empty,
                Role = user.Role ?? string.Empty,
                Status = user.Status ?? true, // Default to true if null
                ExistingImage = user.Image
            };

            // Return borrowing information with corrected status mapping
            return Json(new
            {
                success = true,
                data = viewModel,
                borrowStats = new
                {
                    totalBorrows,
                    activeBorrows,
                    completedBorrows,
                    lateBorrows
                },
                borrowStatusInfo = new
                {
                    explanation = "Statuses: Pending (awaiting approval), Approved (borrowed), Rejected (denied), Returned (completed), Overdue (past deadline)"
                },
                recentBorrows = borrows.Select(b => new {
                    borrowId = b.BorrowId,
                    bookId = b.BookId,
                    bookTitle = b.Book?.Title,
                    bookImage = b.Book?.Image, // Include book cover image
                    borrowDate = b.Time,
                    deadline = b.Time.AddDays(b.Deadline),
                    // Determine status correctly:
                    // 1. If returned, show as Returned
                    // 2. If rejected (status 2), show as Rejected
                    // 3. If past deadline and not returned or rejected, show as Overdue
                    // 4. Otherwise show actual status (Pending, Approved)
                    status = b.ReturnedTime != null ? "Returned" :
                             b.Status == 4 ? "Rejected" : // Status 4 là Rejected (từ chối yêu cầu mượn)
                             (b.ReturnedTime == null && b.Time.AddDays(b.Deadline) < DateTime.Now) ? "Overdue" : // Quá hạn
                             GetBorrowStatusText(b.Status),
                    statusCode = b.Status, // Send the actual status code for reference
                    returnedDate = b.ReturnedTime,
                    isOverdue = (b.ReturnedTime == null && b.Status != 2 && b.Time.AddDays(b.Deadline) < DateTime.Now)
                }).ToList()
            });
        }

        private string GetBorrowStatusText(int status)
        {
            return status switch
            {
                0 => "Pending",
                1 => "Approved",
                2 => "Overdue",    // Giữ status 2 là Overdue, đây là trạng thái cần xử lý gấp
                3 => "Returned",
                4 => "Rejected",   // Giữ status 4 là Rejected, đây là trạng thái đã xử lý xong
                _ => "Unknown"
            };
        }

        // POST: AdminUsers/Create
        [HttpPost]
        public async Task<IActionResult> Create(CreateUserViewModel viewModel, IFormFile? UserImage)
        {
            LoadRolesToViewBag(); // For repopulating dropdown on error
            if (UserImage != null) viewModel.GetType().GetProperty("UserImage")?.SetValue(viewModel, UserImage); // Manual binding if needed

            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray());
                return Json(new { success = false, message = "Validation failed. Please check the form for errors.", errors = errors });
            }

            // Check for duplicate username
            if (await _context.WebUsers.AnyAsync(u => u.Username == viewModel.Username))
            {
                return Json(new { success = false, message = "Username already exists.", errors = new { Username = new[] { "Username already exists." } } });
            }
            // Check for duplicate email
            if (await _context.WebUsers.AnyAsync(u => u.Email == viewModel.Email))
            {
                return Json(new { success = false, message = "Email already exists.", errors = new { Email = new[] { "Email already exists." } } });
            }

            try
            {
                int nextUserId = 1;
                if (await _context.WebUsers.AnyAsync())
                {
                    nextUserId = await _context.WebUsers.MaxAsync(u => u.UserId) + 1;
                }

                // Handle phone number - if empty, set Sdt to 0
                int sdtValue = 0; // Default if null/empty/invalid
                if (!string.IsNullOrWhiteSpace(viewModel.Sdt))
                {
                    // Only save the numeric part of the phone number
                    // This will automatically strip leading zeros
                    if (int.TryParse(viewModel.Sdt, out int parsedValue))
                    {
                        sdtValue = parsedValue;
                    }
                }

                var newUser = new WebUser
                {
                    UserId = nextUserId,
                    Username = viewModel.Username,
                    RealName = viewModel.RealName,
                    Sdt = sdtValue, // Store numeric value (leading zeros will be handled in display)
                    Email = viewModel.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(viewModel.Password), // Hash the password
                    Role = viewModel.Role,
                    Status = viewModel.Status // From CreateUserViewModel
                };

                if (UserImage != null && UserImage.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads/users");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(UserImage.FileName); // Sanitize FileName
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    Directory.CreateDirectory(uploadsFolder); // Ensure directory exists
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await UserImage.CopyToAsync(fileStream);
                    }
                    newUser.Image = "/uploads/users/" + uniqueFileName;
                }
                else
                {
                    newUser.Image = "https://mdbcdn.b-cdn.net/img/Photos/new-templates/bootstrap-chat/ava1-bg.webp"; // Default avatar if no image uploaded
                }

                _context.WebUsers.Add(newUser);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "User created successfully!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Creating User: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        // POST: AdminUsers/Edit
        [HttpPost]
        public async Task<IActionResult> Edit(EditUserViewModel viewModel, IFormFile? UserImage)
        {
            LoadRolesToViewBag(); // For repopulating dropdown on error
            if (UserImage != null) viewModel.GetType().GetProperty("UserImage")?.SetValue(viewModel, UserImage); // Manual binding for IFormFile

            // Remove password validation if NewPassword is not provided
            if (string.IsNullOrEmpty(viewModel.NewPassword))
            {
                ModelState.Remove("NewPassword");
                ModelState.Remove("ConfirmNewPassword");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray());
                return Json(new { success = false, message = "Validation failed. Please check the form for errors.", errors = errors });
            }

            var userToUpdate = await _context.WebUsers.FindAsync(viewModel.UserId);
            if (userToUpdate == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            // Check for duplicate username (for a DIFFERENT user)
            if (await _context.WebUsers.AnyAsync(u => u.Username == viewModel.Username && u.UserId != viewModel.UserId))
            {
                return Json(new { success = false, message = "Username already exists for another user.", errors = new { Username = new[] { "Username already exists for another user." } } });
            }
            // Check for duplicate email (for a DIFFERENT user)
            if (await _context.WebUsers.AnyAsync(u => u.Email == viewModel.Email && u.UserId != viewModel.UserId))
            {
                return Json(new { success = false, message = "Email already exists for another user.", errors = new { Email = new[] { "Email already exists for another user." } } });
            }

            try
            {
                userToUpdate.Username = viewModel.Username;
                userToUpdate.RealName = viewModel.RealName;

                // Handle phone number - parse numeric value only
                if (string.IsNullOrWhiteSpace(viewModel.Sdt))
                {
                    userToUpdate.Sdt = 0; // Default for empty strings
                }
                else if (int.TryParse(viewModel.Sdt, out int parsedValue))
                {
                    userToUpdate.Sdt = parsedValue; // Leading zeros will be handled in display
                }

                userToUpdate.Email = viewModel.Email;
                userToUpdate.Role = viewModel.Role;
                userToUpdate.Status = viewModel.Status;

                if (!string.IsNullOrEmpty(viewModel.NewPassword))
                {
                    userToUpdate.Password = BCrypt.Net.BCrypt.HashPassword(viewModel.NewPassword);
                }

                if (UserImage != null && UserImage.Length > 0)
                {
                    // Optional: Delete old image if it exists and is not a default one
                    if (!string.IsNullOrEmpty(userToUpdate.Image) && !userToUpdate.Image.Contains("default-avatar.png") && !userToUpdate.Image.Contains("ava1-bg.webp"))
                    {
                        var imagePath = Path.Combine(_hostEnvironment.WebRootPath, userToUpdate.Image.TrimStart('/'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }

                    string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads/users");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(UserImage.FileName); // Sanitize FileName
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    Directory.CreateDirectory(uploadsFolder); // Ensure directory exists
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await UserImage.CopyToAsync(fileStream);
                    }
                    userToUpdate.Image = "/uploads/users/" + uniqueFileName;
                }
                else if (string.IsNullOrEmpty(userToUpdate.Image))
                {
                    // If user doesn't have an image, set default
                    userToUpdate.Image = "https://mdbcdn.b-cdn.net/img/Photos/new-templates/bootstrap-chat/ava1-bg.webp";
                }

                _context.WebUsers.Update(userToUpdate);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "User updated successfully!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Editing User (ID: {viewModel.UserId}): {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        // POST: AdminUsers/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int userId)
        {
            try
            {
                var userToDelete = await _context.WebUsers.FindAsync(userId);
                if (userToDelete == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                if (userToDelete.UserId == 1 && userToDelete.Role?.ToLower() == "admin")
                {
                    return Json(new { success = false, message = "This primary admin user cannot be deleted." });
                }

                // Kiểm tra xem người dùng có đang mượn sách không
                var activeBorrows = await _context.Borrows
                    .CountAsync(b => b.UserId == userId && b.ReturnedTime == null && b.Status != 4);

                if (activeBorrows > 0)
                {
                    return Json(new { success = false, message = $"Cannot delete this user. They currently have {activeBorrows} active book borrows. Please process all returns first." });
                }

                if (!string.IsNullOrEmpty(userToDelete.Image) && !userToDelete.Image.Contains("default-avatar.png") && !userToDelete.Image.Contains("ava1-bg.webp"))
                {
                    var imagePath = Path.Combine(_hostEnvironment.WebRootPath, userToDelete.Image.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath)) { System.IO.File.Delete(imagePath); }
                }

                _context.WebUsers.Remove(userToDelete);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "User deleted successfully!" });
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"DB Update Error Deleting User (ID: {userId}): {dbEx.Message}");
                if (dbEx.InnerException != null) Console.WriteLine($"Inner Exception: {dbEx.InnerException.Message}");
                if (dbEx.InnerException != null && (dbEx.InnerException.Message.Contains("constraint failed") || dbEx.InnerException.Message.Contains("FOREIGN KEY")))
                {
                    return Json(new { success = false, message = "Cannot delete user: related records exist (e.g., borrows). Reassign or remove them first." });
                }
                return Json(new { success = false, message = "Database error deleting user. Check logs." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Deleting User (ID: {userId}): {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        // POST: AdminUsers/BorrowAction
        [HttpPost]
        public async Task<IActionResult> BorrowAction(int borrowId, string action, bool isDamaged = false, bool isLost = false, long fee = 0)
        {
            try
            {
                var borrow = await _context.Borrows.FindAsync(borrowId);
                if (borrow == null)
                {
                    return Json(new { success = false, message = "Borrow record not found" });
                }

                switch (action.ToLower())
                {
                    case "approve":
                        // Only allow approval of pending borrows
                        if (borrow.Status != 0) // 0 = Pending
                        {
                            return Json(new { success = false, message = "Only pending borrow requests can be approved" });
                        }
                        borrow.Status = 1; // Set to Approved
                        break;

                    case "reject":
                        // Only allow rejection of pending borrows
                        if (borrow.Status != 0) // 0 = Pending
                        {
                            return Json(new { success = false, message = "Only pending borrow requests can be rejected" });
                        }
                        borrow.Status = 4; // Set to Rejected

                        // Restore the book copy when rejecting a borrow request
                        try
                        {
                            if (borrow.BookId.HasValue)
                            {
                                var book = await _context.Books.FindAsync(borrow.BookId);
                                if (book != null)
                                {
                                    // Tăng Book.Amount lên 1 khi từ chối yêu cầu mượn
                                    if (book.Amount.HasValue)
                                    {
                                        book.Amount++;
                                        _context.Books.Update(book);
                                    }

                                    // Update book copy status to reflect the rejected borrow request
                                    var bookCopy = await _context.BookCopies
                                        .Where(bc => bc.BookId == borrow.BookId && bc.Status == "Borrowed")
                                        .FirstOrDefaultAsync();

                                    if (bookCopy != null)
                                    {
                                        bookCopy.Status = "Available";
                                        _context.BookCopies.Update(bookCopy);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log but continue - this is a secondary operation
                            Console.WriteLine($"Error restoring book copy: {ex.Message}");
                        }
                        break;

                    case "return":
                        // Only allow marking as returned if not already returned
                        if (borrow.ReturnedTime != null)
                        {
                            return Json(new { success = false, message = "This book has already been returned" });
                        }
                        borrow.ReturnedTime = DateTime.Now;
                        borrow.Status = 3; // Set to Returned

                        // Set fee based on parameters
                        borrow.Fee = fee;

                        // Update Book status following the AdminBooksController pattern
                        try
                        {
                            if (borrow.BookId.HasValue)
                            {
                                var book = await _context.Books.FindAsync(borrow.BookId);
                                if (book != null)
                                {
                                    if (isLost)
                                    {
                                        // Nếu sách bị mất, chỉ giảm tổng số sách (TotalAmount)
                                        // Amount đã được giảm khi mượn sách rồi
                                        if (book.TotalAmount > 0)
                                        {
                                            book.TotalAmount -= 1;
                                        }
                                    }
                                    else
                                    {
                                        // Khi trả sách mà không bị mất, tăng số lượng sách có sẵn (Amount)
                                        book.Amount++;
                                    }

                                    // Update the book entity
                                    _context.Books.Update(book);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log but continue - this is a secondary operation
                            Console.WriteLine($"Error updating Book status: {ex.Message}");
                        }
                        break;

                    default:
                        return Json(new { success = false, message = "Invalid action requested" });
                }

                await _context.SaveChangesAsync();
                return Json(new
                {
                    success = true,
                    message = $"Borrow successfully {action}d",
                    newStatus = GetBorrowStatusText(borrow.Status),
                    returnDate = borrow.ReturnedTime,
                    fee = borrow.Fee
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}